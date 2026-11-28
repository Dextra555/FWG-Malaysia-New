<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Data.SqlClient" %>
<%@ Import Namespace="iTextSharp.text" %>
<%@ Import Namespace="iTextSharp.text.pdf" %>
<%@ Import Namespace="System.IO" %>
<script runat="server">
// ════════════════════════════════════════════════════════════════════════
// OBMS Asset Master PDF Report
// URL:  AssetMasterReport.aspx?LoginID=<user>&AssetID=<id>
// SP:   sp_Report_AssetMaster (Migration 25)  — single result set
// Read-only: does NOT modify any asset record.
// ════════════════════════════════════════════════════════════════════════

protected void Page_Load(object sender, EventArgs e)
{
    string loginID = (Request.QueryString["LoginID"] ?? "").Trim();
    string idStr   = (Request.QueryString["AssetID"] ?? "0").Trim();
    int id = 0;
    if (!int.TryParse(idStr, out id) || id <= 0) { SendError("Invalid or missing Asset ID."); return; }

    // RS1: asset detail, RS2: installment schedule
    DataTable dt = null; DataTable dtInst = null;
    using (var conn = new SqlConnection(ConnStr()))
    using (var cmd  = new SqlCommand("sp_Report_AssetMaster", conn))
    {
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandTimeout = 30;
        cmd.Parameters.AddWithValue("@AssetID", id);
        conn.Open();
        using (var da = new System.Data.SqlClient.SqlDataAdapter(cmd))
        {
            var ds = new System.Data.DataSet();
            da.Fill(ds);
            if (ds.Tables.Count > 0) dt     = ds.Tables[0];
            if (ds.Tables.Count > 1) dtInst = ds.Tables[1];
        }
    }

    if (dt == null || dt.Rows.Count == 0) { SendError("Asset record not found or has been deleted."); return; }

    byte[] pdf;
    try { pdf = BuildPdf(loginID, dt.Rows[0], dtInst); }
    catch (Exception ex) { LogErr("BuildPdf_AssetMaster", ex.Message); SendError("Unable to generate PDF. Please try again."); return; }

    StreamPdf(pdf, "AssetMaster_" + dt.Rows[0]["AssetCode"] + ".pdf");
}

private byte[] BuildPdf(string loginID, DataRow dr, DataTable dtInst)
{
    BaseColor navy = Navy(); BaseColor gold = Gold(); BaseColor border = Border();
    Font fCoName=FCoName(); Font fCoReg=FCoReg(); Font fCoSub=FCoSub();
    Font fTitle=FTitle(); Font fLabel=FLabel(); Font fValue=FValue(); Font fFoot=FFoot(); Font fSig=FSig();

    using (var ms = new MemoryStream()) {
        var doc = new Document(PageSize.A4, 30f, 30f, 35f, 35f);
        var writer = PdfWriter.GetInstance(doc, ms);
        writer.PageEvent = new FooterEvt(loginID, fFoot, navy);
        doc.Open();

        AddCoHdr(doc, navy, gold, fCoName, fCoReg, fCoSub);
        P(doc, "ASSET MASTER", fTitle, Element.ALIGN_CENTER, 8f);

        var tbl = InfoTbl();
        AddIR(tbl,"Asset Code",      Safe(dr,"AssetCode"),            fLabel, fValue);
        AddIR(tbl,"Asset Name",      Safe(dr,"AssetName"),            fLabel, fValue);
        AddIR(tbl,"Asset Type",      Safe(dr,"AssetType"),            fLabel, fValue);
        AddIR(tbl,"Status",          Safe(dr,"AssetStatus"),          fLabel, fValue);
        AddIR(tbl,"Condition",       SafeD(dr,"AssetCondition"),      fLabel, fValue);
        AddIR(tbl,"Serial No",       SafeD(dr,"SerialNumber"),        fLabel, fValue);
        AddIR(tbl,"Model No",        SafeD(dr,"ModelNo"),             fLabel, fValue);
        AddIR(tbl,"Brand",           SafeD(dr,"BrandName"),           fLabel, fValue);
        AddIR(tbl,"Supplier",        SafeD(dr,"SupplierName"),        fLabel, fValue);
        AddIR(tbl,"Branch",          SafeD(dr,"Branch"),              fLabel, fValue);
        AddIR(tbl,"Purchase Date",   SafeD(dr,"PurchaseDate"),        fLabel, fValue);
        AddIR(tbl,"Purchase Amount", SafeD(dr,"PurchaseAmount"),      fLabel, fValue);
        AddIR(tbl,"Warranty Expiry", SafeD(dr,"WarrantyExpiryDate"),  fLabel, fValue);
        AddIR(tbl,"Current Location",SafeD(dr,"CurrentLocation"),     fLabel, fValue);
        AddIR(tbl,"Location Status", SafeD(dr,"LocationStatus"),      fLabel, fValue);
        AddIR(tbl,"Prepared By",     SafeD(dr,"PreparedBy"),          fLabel, fValue);
        AddIR(tbl,"Date Printed",    DateTime.Now.ToString("dd MMM yyyy"), fLabel, fValue);
        string rem = Safe(dr,"Remarks");
        if (!string.IsNullOrWhiteSpace(rem)) AddRemark(tbl, rem, fLabel, fValue);
        doc.Add(tbl);

        string desc = Safe(dr,"Description");
        if (!string.IsNullOrWhiteSpace(desc)) {
            P(doc, "Description", new Font(Font.FontFamily.HELVETICA,9f,Font.BOLD,navy), Element.ALIGN_LEFT, 2f);
            doc.Add(new Paragraph(desc, fValue) { SpacingAfter = 8f });
        }

        // ── Payment Terms section ─────────────────────────────────────────
        string payType = Safe(dr, "PaymentTermType");
        if (!string.IsNullOrWhiteSpace(payType) && payType != "FULL")
        {
            P(doc, "Payment Terms", new Font(Font.FontFamily.HELVETICA,9f,Font.BOLD,navy), Element.ALIGN_LEFT, 2f);
            var ptbl = InfoTbl();
            AddIR(ptbl, "Payment Type",       payType,                                  fLabel, fValue);
            AddIR(ptbl, "Purchase Amount",    "RM " + SafeD(dr,"PurchaseAmount"),        fLabel, fValue);
            AddIR(ptbl, "Deposit Amount",     "RM " + SafeD(dr,"DepositAmount"),        fLabel, fValue);
            AddIR(ptbl, "Monthly Amount",     "RM " + SafeD(dr,"MonthlyInstallmentAmount"), fLabel, fValue);
            AddIR(ptbl, "No. of Months",      SafeD(dr,"InstallmentMonths"),             fLabel, fValue);
            AddIR(ptbl, "Start Month",        SafeD(dr,"InstallmentStartDate"),          fLabel, fValue);
            doc.Add(ptbl);
        }

        // ── Installment Schedule table ────────────────────────────────────
        if (dtInst != null && dtInst.Rows.Count > 0)
        {
            doc.Add(Spacer(6f));
            P(doc, "Installment Schedule", new Font(Font.FontFamily.HELVETICA,9f,Font.BOLD,navy), Element.ALIGN_LEFT, 4f);

            BaseColor headerBg = new BaseColor(0x1a, 0x3a, 0x5c);
            BaseColor rowAlt   = new BaseColor(0xf4, 0xf7, 0xfb);
            BaseColor paidBg   = new BaseColor(0xe8, 0xf5, 0xe9);
            BaseColor pendBg   = new BaseColor(0xfff9c4);
            BaseColor totalBg  = new BaseColor(0xe8, 0xf0, 0xfe);
            BaseColor brd      = new BaseColor(0xcc, 0xcc, 0xcc);

            Font fTH  = new Font(Font.FontFamily.HELVETICA, 8f, Font.BOLD,   BaseColor.WHITE);
            Font fTD  = new Font(Font.FontFamily.HELVETICA, 8f, Font.NORMAL, BaseColor.BLACK);
            Font fTot = new Font(Font.FontFamily.HELVETICA, 8f, Font.BOLD,   navy);

            var itbl = new PdfPTable(6) { WidthPercentage = 70f, SpacingAfter = 8f };
            itbl.SetWidths(new float[] { 0.8f, 2.0f, 2.0f, 1.5f, 2.0f, 1.5f });

            string[] hdrs = { "No.", "Due Date", "Amount (RM)", "Paid (RM)", "Balance (RM)", "Status" };
            foreach (var h in hdrs)
                itbl.AddCell(new PdfPCell(new Phrase(h, fTH)) {
                    BackgroundColor = headerBg, Padding = 4f,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Border = Rectangle.BOX, BorderColor = brd
                });

            decimal totAmt = 0, totPaid = 0, totBal = 0;
            int ri = 0;
            foreach (DataRow ir in dtInst.Rows)
            {
                ri++;
                string status  = (ir["Status"]  ?? "Pending").ToString();
                decimal amt    = ir["Amount"]        != DBNull.Value ? Convert.ToDecimal(ir["Amount"])        : 0;
                decimal paid   = ir["PaidAmount"]    != DBNull.Value ? Convert.ToDecimal(ir["PaidAmount"])    : 0;
                decimal bal    = ir["BalanceAmount"] != DBNull.Value ? Convert.ToDecimal(ir["BalanceAmount"]) : 0;
                totAmt += amt; totPaid += paid; totBal += bal;

                BaseColor rowBg = status == "Paid" ? paidBg : (ri % 2 == 0 ? rowAlt : BaseColor.WHITE);

                Action<string, bool> addCell = (txt, isNum) =>
                    itbl.AddCell(new PdfPCell(new Phrase(txt, fTD)) {
                        BackgroundColor = rowBg, Padding = 3.5f,
                        HorizontalAlignment = isNum ? Element.ALIGN_RIGHT : Element.ALIGN_CENTER,
                        Border = Rectangle.BOX, BorderColor = brd
                    });

                addCell(ri.ToString(),              false);
                addCell((ir["DueDate"] ?? "").ToString(), false);
                addCell(amt.ToString("N2"),          true);
                addCell(paid.ToString("N2"),         true);
                addCell(bal.ToString("N2"),          true);
                addCell(status,                     false);
            }

            // Totals row
            itbl.AddCell(new PdfPCell(new Phrase("TOTAL", fTot)) {
                Colspan = 2, BackgroundColor = totalBg, Padding = 4f,
                HorizontalAlignment = Element.ALIGN_RIGHT,
                Border = Rectangle.BOX, BorderColor = navy, BorderWidth = 1.5f
            });
            foreach (var tv in new decimal[] { totAmt, totPaid, totBal })
                itbl.AddCell(new PdfPCell(new Phrase(tv.ToString("N2"), fTot)) {
                    BackgroundColor = totalBg, Padding = 4f,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Border = Rectangle.BOX, BorderColor = navy, BorderWidth = 1.5f
                });
            itbl.AddCell(new PdfPCell(new Phrase("", fTot)) {
                BackgroundColor = totalBg, Padding = 4f,
                Border = Rectangle.BOX, BorderColor = navy, BorderWidth = 1.5f
            });
            doc.Add(itbl);
        }

        AddSig(doc, fSig, "Prepared By", "", "Verified By", "");
        doc.Close(); return ms.ToArray();
    }
}

// ════════════════════════════════════════════════════════════════════════
// SHARED HELPERS — same pattern as StockIssueReport.aspx etc.
// ════════════════════════════════════════════════════════════════════════

private DataTable RunSP(string sp, string paramName, int paramVal) {
    var dt = new DataTable();
    try {
        using (var conn = new SqlConnection(ConnStr()))
        using (var cmd  = new SqlCommand(sp, conn)) {
            cmd.CommandType = CommandType.StoredProcedure; cmd.CommandTimeout = 30;
            cmd.Parameters.AddWithValue(paramName, paramVal);
            conn.Open(); new SqlDataAdapter(cmd).Fill(dt);
        }
    } catch (Exception ex) { LogErr(sp, ex.Message); return null; }
    return dt;
}

private void StreamPdf(byte[] pdf, string filename) {
    Response.Clear(); Response.Buffer = false; Response.ContentType = "application/pdf";
    Response.AddHeader("Content-Disposition", "inline; filename=" + filename);
    Response.AddHeader("Content-Length", pdf.Length.ToString());
    Response.AddHeader("Access-Control-Allow-Origin", "*");
    Response.BinaryWrite(pdf); Response.Flush(); Response.End();
}

private void AddCoHdr(Document doc, BaseColor navy, BaseColor gold, Font fCoName, Font fCoReg, Font fCoSub) {
    string lp = Server.MapPath("~/images/logo-white.png");
    var h = new PdfPTable(2) { WidthPercentage=100f, SpacingAfter=0f };
    h.SetWidths(new float[]{1.2f,8.8f}); h.DefaultCell.Border = Rectangle.NO_BORDER;
    PdfPCell lc;
    if (System.IO.File.Exists(lp)) {
        var lg = iTextSharp.text.Image.GetInstance(lp); lg.ScaleToFit(55f,55f);
        lc = new PdfPCell(lg){Border=Rectangle.NO_BORDER,VerticalAlignment=Element.ALIGN_MIDDLE,Padding=2f};
    } else lc = new PdfPCell(new Phrase("")){Border=Rectangle.NO_BORDER};
    h.AddCell(lc);
    var tx = new Phrase();
    tx.Add(new Chunk(AppS("CompanyName")+" ",fCoName));
    tx.Add(new Chunk(AppS("Registration")+"\n",fCoReg));
    tx.Add(new Chunk(AppS("Address1")+"\n",fCoSub));
    tx.Add(new Chunk(AppS("Address2")+"\n",fCoSub));
    tx.Add(new Chunk(AppS("PostCode")+" "+AppS("City")+"\n",fCoSub));
    tx.Add(new Chunk(AppS("State")+"\n",fCoSub));
    tx.Add(new Chunk(AppS("Phone"),fCoSub));
    h.AddCell(new PdfPCell(tx){Border=Rectangle.NO_BORDER,VerticalAlignment=Element.ALIGN_MIDDLE,Padding=4f,PaddingLeft=8f});
    doc.Add(h);
    doc.Add(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(2.5f,100f,gold,Element.ALIGN_CENTER,-2f)));
    doc.Add(Spacer(6f));
}

private static PdfPTable InfoTbl() {
    var t = new PdfPTable(4){WidthPercentage=100f,SpacingAfter=10f};
    t.SetWidths(new float[]{2.2f,4f,2.2f,4f}); t.DefaultCell.Border=Rectangle.NO_BORDER; return t;
}

private static void AddIR(PdfPTable t, string lbl, string val, Font fl, Font fv) {
    t.AddCell(new PdfPCell(new Phrase(lbl+":",fl)){Border=Rectangle.NO_BORDER,Padding=2f});
    t.AddCell(new PdfPCell(new Phrase(val,fv)){Border=Rectangle.NO_BORDER,Padding=2f});
}

private static void AddRemark(PdfPTable t, string rem, Font fl, Font fv) {
    var ph = new Phrase(); ph.Add(new Chunk("Remarks: ",fl)); ph.Add(new Chunk(rem,fv));
    t.AddCell(new PdfPCell(ph){Colspan=4,Border=Rectangle.NO_BORDER,Padding=2f,PaddingTop=4f});
}

private static void AddSig(Document doc, Font fSig, string lLbl, string lName, string rLbl, string rName) {
    var s = new PdfPTable(2){WidthPercentage=80f,HorizontalAlignment=Element.ALIGN_LEFT,SpacingBefore=14f};
    s.SetWidths(new float[]{1f,1f});
    foreach (var p in new[]{new[]{lLbl,lName},new[]{rLbl,rName}}) {
        var ph = new Phrase();
        ph.Add(new Chunk(p[0]+"\n\n\n",fSig)); ph.Add(new Chunk("_______________________\n",fSig)); ph.Add(new Chunk(p[1],fSig));
        s.AddCell(new PdfPCell(ph){Border=Rectangle.NO_BORDER,Padding=6f});
    }
    doc.Add(s);
}

private static void P(Document doc, string t, Font f, int a, float sp) { doc.Add(new Paragraph(t,f){Alignment=a,SpacingAfter=sp}); }
private static Paragraph Spacer(float h) { return new Paragraph(" "){SpacingAfter=h}; }
private static string Safe(DataRow dr, string c) { if(dr==null||!dr.Table.Columns.Contains(c))return ""; return dr[c]==DBNull.Value?"":(dr[c]??"").ToString().Trim(); }
private static string SafeD(DataRow dr, string c) { string v=Safe(dr,c); return string.IsNullOrWhiteSpace(v)?"-":v; }
private static string CleanUnicode(string s){if(string.IsNullOrEmpty(s))return s;return s.Replace("\u2013","-").Replace("\u2014","-").Replace("\u2018","'").Replace("\u2019","'").Replace("\u201C","\"").Replace("\u201D","\"").Replace("\u2026","...").Replace("\u00A0"," ");}
private static string ConnStr() { return string.Format("Server={0};Database={1};User Id={2};Password={3};",AppS("Server"),AppS("Database"),AppS("UserID"),AppS("Password")); }
private static string AppS(string k) { return ConfigurationManager.AppSettings[k]??""; }

// Colours
private static BaseColor Navy()   { return new BaseColor(0x1a,0x3a,0x5c); }
private static BaseColor Gold()   { return new BaseColor(0xD4,0xA0,0x17); }
private static BaseColor Border() { return new BaseColor(0xcc,0xcc,0xcc); }
// Fonts
private static Font FCoName() { return new Font(Font.FontFamily.HELVETICA,11f,Font.BOLD,  Navy()); }
private static Font FCoReg()  { return new Font(Font.FontFamily.HELVETICA, 8f,Font.BOLD,  Navy()); }
private static Font FCoSub()  { return new Font(Font.FontFamily.HELVETICA, 8f,Font.NORMAL,BaseColor.DARK_GRAY); }
private static Font FTitle()  { return new Font(Font.FontFamily.HELVETICA,12f,Font.BOLD,  Navy()); }
private static Font FLabel()  { return new Font(Font.FontFamily.HELVETICA, 8f,Font.BOLD,  new BaseColor(0x44,0x44,0x44)); }
private static Font FValue()  { return new Font(Font.FontFamily.HELVETICA, 8f,Font.NORMAL,BaseColor.BLACK); }
private static Font FFoot()   { return new Font(Font.FontFamily.HELVETICA, 7f,Font.NORMAL,BaseColor.GRAY); }
private static Font FSig()    { return new Font(Font.FontFamily.HELVETICA, 8f,Font.NORMAL,new BaseColor(0x55,0x55,0x55)); }

private void SendError(string msg) {
    Response.Clear(); Response.ContentType="text/html";
    Response.Write("<html><body style='font-family:Arial;padding:30px'><h3 style='color:#c62828'>Asset Master Report</h3><p>"+Server.HtmlEncode(msg)+"</p></body></html>");
    Response.End();
}
private void LogErr(string src, string msg) {
    try { System.IO.File.AppendAllText(Server.MapPath("~/ReportParamLog.txt"),
        string.Format("[{0}] ERROR in {1}: {2}\r\n",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),src,msg)); } catch {}
}

private class FooterEvt : PdfPageEventHelper {
    private readonly string _u; private readonly Font _f; private readonly BaseColor _n;
    public FooterEvt(string u,Font f,BaseColor n){_u=u;_f=f;_n=n;}
    public override void OnEndPage(PdfWriter w, Document d) {
        var cb=w.DirectContent; var pg=d.PageSize;
        cb.SetLineWidth(0.5f); cb.SetColorStroke(new BaseColor(0xcc,0xcc,0xcc));
        cb.MoveTo(d.LeftMargin,d.BottomMargin-5f); cb.LineTo(pg.Width-d.RightMargin,d.BottomMargin-5f); cb.Stroke();
        ColumnText.ShowTextAligned(cb,Element.ALIGN_LEFT,new Phrase("Generated by: "+_u+"   |   "+DateTime.Now.ToString("dd MMM yyyy hh:mm tt"),_f),d.LeftMargin,d.BottomMargin-15f,0);
        ColumnText.ShowTextAligned(cb,Element.ALIGN_RIGHT,new Phrase("Page "+w.PageNumber,_f),pg.Width-d.RightMargin,d.BottomMargin-15f,0);
    }
}
</script>
