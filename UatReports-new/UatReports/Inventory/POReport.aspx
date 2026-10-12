<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Data.SqlClient" %>
<%@ Import Namespace="iTextSharp.text" %>
<%@ Import Namespace="iTextSharp.text.pdf" %>
<%@ Import Namespace="System.IO" %>
<script runat="server">

    // ════════════════════════════════════════════════════════════════════════
    // OBMS Purchase Order PDF Report
    // URL: POReport.aspx?LoginID=<user>&POID=<id>
    // A4 Landscape — more columns (item + price columns need the width).
    // ════════════════════════════════════════════════════════════════════════

    protected void Page_Load(object sender, EventArgs e)
    {
        string loginID = (Request.QueryString["LoginID"] ?? "").Trim();
        string poIDStr = (Request.QueryString["POID"]    ?? "0").Trim();

        int poID = 0;
        if (!int.TryParse(poIDStr, out poID) || poID <= 0)
        { SendError("Invalid or missing Purchase Order ID."); return; }

        DataTable dtHeader  = new DataTable();
        DataTable dtDetails = new DataTable();
        try
        {
            using (var conn = new SqlConnection(ConnStr()))
            using (var cmd  = new SqlCommand("sp_Report_PO", conn))
            {
                cmd.CommandType    = CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;
                cmd.Parameters.AddWithValue("@POID", poID);
                conn.Open();
                using (var da = new SqlDataAdapter(cmd))
                {
                    var ds = new DataSet();
                    da.Fill(ds);
                    if (ds.Tables.Count > 0) dtHeader  = ds.Tables[0];
                    if (ds.Tables.Count > 1) dtDetails = ds.Tables[1];
                }
            }
        }
        catch (Exception ex)
        {
            LogErr("sp_Report_PO", ex.Message);
            SendError("Unable to retrieve purchase order data. Please try again.");
            return;
        }

        if (dtHeader == null || dtHeader.Rows.Count == 0)
        { SendError("Purchase Order not found or has been deleted."); return; }

        byte[] pdf;
        try   { pdf = BuildPdf(loginID, dtHeader.Rows[0], dtDetails); }
        catch (Exception ex)
        {
            LogErr("BuildPdf_PO", ex.Message);
            SendError("Unable to generate PDF. Please try again.");
            return;
        }

        string docNo = dtHeader.Rows[0]["PONumber"].ToString();
        Response.Clear();
        Response.Buffer      = false;
        Response.ContentType = "application/pdf";
        Response.AddHeader("Content-Disposition", "inline; filename=PO_" + docNo + ".pdf");
        Response.AddHeader("Content-Length",      pdf.Length.ToString());
        Response.AddHeader("Access-Control-Allow-Origin", "*");
        Response.BinaryWrite(pdf);
        Response.Flush();
        Response.End();
    }

    // ════════════════════════════════════════════════════════════════════════
    // PDF BUILDER
    // ════════════════════════════════════════════════════════════════════════

    private byte[] BuildPdf(string loginID, DataRow hdr, DataTable dtDetails)
    {
        BaseColor navy     = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor gold     = new BaseColor(0xD4, 0xA0, 0x17);
        BaseColor headerBg = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor rowAlt   = new BaseColor(0xf4, 0xf7, 0xfb);
        BaseColor totalBg  = new BaseColor(0xe8, 0xf0, 0xfe);
        BaseColor border   = new BaseColor(0xcc, 0xcc, 0xcc);

        Font fCoName  = new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD,   navy);
        Font fCoReg   = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
        Font fCoSub   = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, BaseColor.DARK_GRAY);
        Font fTitle   = new Font(Font.FontFamily.HELVETICA, 12f, Font.BOLD,   navy);
        Font fLabel   = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   new BaseColor(0x44,0x44,0x44));
        Font fValue   = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, BaseColor.BLACK);
        Font fTH      = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   BaseColor.WHITE);
        Font fTD      = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.NORMAL, BaseColor.BLACK);
        Font fTot     = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
        Font fFoot    = new Font(Font.FontFamily.HELVETICA,  7f, Font.NORMAL, BaseColor.GRAY);
        Font fStatus  = new Font(Font.FontFamily.HELVETICA,  9f, Font.BOLD,   navy);
        Font fSigLbl  = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, new BaseColor(0x55,0x55,0x55));

        using (var ms = new MemoryStream())
        {
            // Landscape — more room for price columns
            var doc    = new Document(PageSize.A4.Rotate(), 30f, 30f, 35f, 35f);
            var writer = PdfWriter.GetInstance(doc, ms);
            writer.PageEvent = new FooterEvent(loginID, fFoot, navy);
            doc.Open();

            // Company header
            AddCompanyHeader(doc, navy, fCoName, fCoReg, fCoSub);
            var goldSep = new iTextSharp.text.pdf.draw.LineSeparator(2.5f, 100f, gold, Element.ALIGN_CENTER, -2f);
            doc.Add(new Chunk(goldSep));
            doc.Add(Spacer(6f));

            P(doc, "PURCHASE ORDER", fTitle, Element.ALIGN_CENTER, 8f);

            // ── Document header info ──────────────────────────────────────
            var infoTbl = new PdfPTable(4) { WidthPercentage = 100f, SpacingAfter = 10f };
            infoTbl.SetWidths(new float[] { 2f, 4f, 2f, 4f });
            infoTbl.DefaultCell.Border = Rectangle.NO_BORDER;

            AddInfoRow(infoTbl, "PO Number",      Safe(hdr, "PONumber"),       fLabel, fValue);
            AddInfoRow(infoTbl, "Status",         Safe(hdr, "Status"),          fLabel, fStatus);
            AddInfoRow(infoTbl, "PO Date",        Safe(hdr, "PODate"),          fLabel, fValue);
            AddInfoRow(infoTbl, "Expected Date",  Safe(hdr, "ExpectedDate"),    fLabel, fValue);
            AddInfoRow(infoTbl, "Supplier",       Safe(hdr, "SupplierName"),    fLabel, fValue);
            AddInfoRow(infoTbl, "Warehouse",      Safe(hdr, "WarehouseName"),   fLabel, fValue);

            string linkedPR = Safe(hdr, "LinkedPRNumber");
            AddInfoRow(infoTbl, "Linked PR",      string.IsNullOrWhiteSpace(linkedPR) ? "—" : linkedPR, fLabel, fValue);
            AddInfoRow(infoTbl, "Approved By",    SafeOrDash(hdr, "ApprovedBy"),    fLabel, fValue);
            AddInfoRow(infoTbl, "Prepared By",    SafeOrDash(hdr, "RequestedBy"),   fLabel, fValue);
            AddInfoRow(infoTbl, "Approved Date",  SafeOrDash(hdr, "ApprovedDate"),  fLabel, fValue);

            string remarks = Safe(hdr, "Remarks");
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                var ph = new Phrase(); ph.Add(new Chunk("Remarks: ", fLabel)); ph.Add(new Chunk(remarks, fValue));
                infoTbl.AddCell(new PdfPCell(ph) { Colspan = 4, Border = Rectangle.NO_BORDER, Padding = 2f, PaddingTop = 4f });
            }
            doc.Add(infoTbl);

            // Reject reason
            string rejectReason = Safe(hdr, "RejectReason");
            if (!string.IsNullOrWhiteSpace(rejectReason))
            {
                var ph = new Phrase(); ph.Add(new Chunk("Reject Reason: ", fLabel)); ph.Add(new Chunk(rejectReason, fValue));
                doc.Add(new Paragraph(ph) { SpacingAfter = 6f });
            }

            // ── Items table ───────────────────────────────────────────────
            P(doc, "Order Items", new Font(Font.FontFamily.HELVETICA, 9f, Font.BOLD, navy), Element.ALIGN_LEFT, 4f);

            // 10 columns: #, Code, Name, Category, UOM, Ordered, Received, Pending, Unit Price, Total
            var tbl = new PdfPTable(10) { WidthPercentage = 100f, HeaderRows = 1, SpacingAfter = 16f };
            tbl.SetWidths(new float[] { 0.5f, 1.8f, 3.5f, 2f, 1.2f, 1.5f, 1.5f, 1.5f, 2f, 2f });

            string[] hdrs   = { "#", "Code", "Item Name", "Category", "UOM", "Ordered", "Received", "Pending", "Unit Price (RM)", "Total (RM)" };
            bool[]   numCol = { false, false, false, false, false, true, true, true, true, true };
            for (int i = 0; i < hdrs.Length; i++)
            {
                tbl.AddCell(new PdfPCell(new Phrase(hdrs[i], fTH))
                {
                    BackgroundColor     = headerBg,
                    HorizontalAlignment = numCol[i] ? Element.ALIGN_RIGHT : Element.ALIGN_CENTER,
                    Padding = 5f, Border = Rectangle.BOX, BorderColor = border
                });
            }

            int rowNo = 0;
            decimal grandTotal = 0;
            foreach (DataRow dr in dtDetails.Rows)
            {
                var bg = (rowNo % 2 == 0) ? BaseColor.WHITE : rowAlt;
                rowNo++;

                decimal ordQty  = ParseDecimal(dr, "OrderedQty");
                decimal recQty  = ParseDecimal(dr, "ReceivedQty");
                decimal pending = ParseDecimal(dr, "PendingQty");
                decimal price   = ParseDecimal(dr, "UnitPrice");
                decimal total   = ParseDecimal(dr, "LineTotal");
                grandTotal += total;

                AddCell(tbl, rowNo.ToString(),          fTD, Element.ALIGN_CENTER, bg, border);
                AddCell(tbl, Safe(dr, "ItemCode"),       fTD, Element.ALIGN_LEFT,   bg, border);
                AddCell(tbl, Safe(dr, "ItemName"),       fTD, Element.ALIGN_LEFT,   bg, border);
                AddCell(tbl, Safe(dr, "CategoryName"),   fTD, Element.ALIGN_LEFT,   bg, border);
                AddCell(tbl, Safe(dr, "UOMName"),        fTD, Element.ALIGN_LEFT,   bg, border);
                AddCell(tbl, ordQty.ToString("N2"),      fTD, Element.ALIGN_RIGHT,  bg, border);
                AddCell(tbl, recQty.ToString("N2"),      fTD, Element.ALIGN_RIGHT,  bg, border);
                AddCell(tbl, pending.ToString("N2"),     fTD, Element.ALIGN_RIGHT,  bg, border);
                AddCell(tbl, price.ToString("N2"),       fTD, Element.ALIGN_RIGHT,  bg, border);
                AddCell(tbl, total.ToString("N2"),       fTD, Element.ALIGN_RIGHT,  bg, border);
            }

            if (dtDetails.Rows.Count == 0)
            {
                tbl.AddCell(new PdfPCell(new Phrase("No items found.", fTD))
                { Colspan = 10, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 8f, Border = Rectangle.BOX, BorderColor = border });
            }
            else
            {
                // Grand total row
                for (int i = 0; i < 9; i++)
                {
                    string txt = (i == 4) ? "Grand Total (RM)" : "";
                    int align  = (i == 4) ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT;
                    tbl.AddCell(new PdfPCell(new Phrase(txt, fTot))
                    { HorizontalAlignment = align, BackgroundColor = totalBg, Padding = 5f, Border = Rectangle.BOX, BorderColor = border });
                }
                tbl.AddCell(new PdfPCell(new Phrase(grandTotal.ToString("N2"), fTot))
                { HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = totalBg, Padding = 5f, Border = Rectangle.BOX, BorderColor = border });
            }
            doc.Add(tbl);

            // Signature row
            AddSignatureRow(doc, fSigLbl, border,
                "Prepared By",  SafeOrDash(hdr, "RequestedBy"),
                "Approved By",  SafeOrDash(hdr, "ApprovedBy"));

            doc.Close();
            return ms.ToArray();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // SHARED HELPERS
    // ════════════════════════════════════════════════════════════════════════

    private void AddCompanyHeader(Document doc, BaseColor navy, Font fCoName, Font fCoReg, Font fCoSub)
    {
        string logoPath = Server.MapPath("~/images/logo-white.png");
        var hdr = new PdfPTable(2) { WidthPercentage = 100f, SpacingAfter = 0f };
        hdr.SetWidths(new float[] { 1.2f, 8.8f });
        hdr.DefaultCell.Border = Rectangle.NO_BORDER;
        PdfPCell logoCell;
        if (System.IO.File.Exists(logoPath))
        {
            var logo = iTextSharp.text.Image.GetInstance(logoPath);
            logo.ScaleToFit(55f, 55f);
            logoCell = new PdfPCell(logo) { Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_MIDDLE, Padding = 2f };
        }
        else logoCell = new PdfPCell(new Phrase("")) { Border = Rectangle.NO_BORDER };
        hdr.AddCell(logoCell);
        var txt = new Phrase();
        txt.Add(new Chunk(AppSettings("CompanyName") + " ", fCoName));
        txt.Add(new Chunk(AppSettings("Registration") + "\n", fCoReg));
        txt.Add(new Chunk(AppSettings("Address1") + "\n", fCoSub));
        txt.Add(new Chunk(AppSettings("Address2") + "\n", fCoSub));
        txt.Add(new Chunk(AppSettings("PostCode") + " " + AppSettings("City") + "\n", fCoSub));
        txt.Add(new Chunk(AppSettings("State") + "\n", fCoSub));
        txt.Add(new Chunk(AppSettings("Phone"), fCoSub));
        hdr.AddCell(new PdfPCell(txt) { Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_MIDDLE, Padding = 4f, PaddingLeft = 8f });
        doc.Add(hdr);
    }

    private static void AddInfoRow(PdfPTable tbl, string label, string value, Font fLabel, Font fValue)
    {
        tbl.AddCell(new PdfPCell(new Phrase(label + ":", fLabel)) { Border = Rectangle.NO_BORDER, Padding = 2f });
        tbl.AddCell(new PdfPCell(new Phrase(value, fValue))       { Border = Rectangle.NO_BORDER, Padding = 2f });
    }

    private static void AddCell(PdfPTable tbl, string text, Font font, int align, BaseColor bg, BaseColor border)
    {
        tbl.AddCell(new PdfPCell(new Phrase(text, font))
        { HorizontalAlignment = align, BackgroundColor = bg, Padding = 4f, Border = Rectangle.BOX, BorderColor = border });
    }

    private static void AddSignatureRow(Document doc, Font fLbl, BaseColor border,
        string lLabel, string lName, string rLabel, string rName)
    {
        var sigTbl = new PdfPTable(2) { WidthPercentage = 80f, HorizontalAlignment = Element.ALIGN_LEFT, SpacingBefore = 10f };
        sigTbl.SetWidths(new float[] { 1f, 1f });
        foreach (var pair in new[] { new[]{lLabel, lName}, new[]{rLabel, rName} })
        {
            var ph = new Phrase();
            ph.Add(new Chunk(pair[0] + "\n\n\n", fLbl));
            ph.Add(new Chunk("_______________________\n", fLbl));
            ph.Add(new Chunk(pair[1], fLbl));
            sigTbl.AddCell(new PdfPCell(ph) { Border = Rectangle.NO_BORDER, Padding = 6f });
        }
        doc.Add(sigTbl);
    }

    private static decimal ParseDecimal(DataRow dr, string col)
    {
        if (dr == null || !dr.Table.Columns.Contains(col) || dr[col] == DBNull.Value) return 0m;
        decimal v; decimal.TryParse(dr[col].ToString(), out v); return v;
    }

    private static string ConnStr()
    {
        return string.Format("Server={0};Database={1};User Id={2};Password={3};",
            ConfigurationManager.AppSettings["Server"]   ?? ".",
            ConfigurationManager.AppSettings["Database"] ?? "",
            ConfigurationManager.AppSettings["UserID"]   ?? "",
            ConfigurationManager.AppSettings["Password"] ?? "");
    }
    private static string AppSettings(string key) { return ConfigurationManager.AppSettings[key] ?? ""; }
    private static string Safe(DataRow dr, string col)
    {
        if (dr == null || !dr.Table.Columns.Contains(col)) return "";
        return dr[col] == DBNull.Value ? "" : (dr[col] ?? "").ToString().Trim();
    }
    private static string SafeOrDash(DataRow dr, string col)
    { string v = Safe(dr, col); return string.IsNullOrWhiteSpace(v) ? "—" : v; }
    private static void P(Document doc, string text, Font font, int align, float spacingAfter)
    { doc.Add(new Paragraph(text, font) { Alignment = align, SpacingAfter = spacingAfter }); }
    private static Paragraph Spacer(float h) { return new Paragraph(" ") { SpacingAfter = h }; }

    private void SendError(string msg)
    {
        Response.Clear(); Response.ContentType = "text/html";
        Response.Write("<html><body style='font-family:Arial;padding:30px'>" +
            "<h3 style='color:#c62828'>Purchase Order Report</h3>" +
            "<p style='color:#333'>" + Server.HtmlEncode(msg) + "</p></body></html>");
        Response.End();
    }
    private void LogErr(string src, string msg)
    {
        try
        {
            string path  = Server.MapPath("~/ReportParamLog.txt");
            System.IO.File.AppendAllText(path,
                string.Format("[{0}] ERROR in {1}: {2}{3}",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), src, msg, Environment.NewLine));
        }
        catch { }
    }

    private class FooterEvent : PdfPageEventHelper
    {
        private readonly string _user; private readonly Font _font; private readonly BaseColor _navy;
        public FooterEvent(string user, Font font, BaseColor navy) { _user = user; _font = font; _navy = navy; }
        public override void OnEndPage(PdfWriter writer, Document doc)
        {
            var cb = writer.DirectContent; var pg = doc.PageSize;
            cb.SetLineWidth(0.5f); cb.SetColorStroke(new BaseColor(0xcc, 0xcc, 0xcc));
            cb.MoveTo(doc.LeftMargin, doc.BottomMargin - 5f);
            cb.LineTo(pg.Width - doc.RightMargin, doc.BottomMargin - 5f); cb.Stroke();
            string left = "Generated by: " + _user + "   |   " + DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
            ColumnText.ShowTextAligned(cb, Element.ALIGN_LEFT,  new Phrase(left, _font), doc.LeftMargin, doc.BottomMargin - 15f, 0);
            ColumnText.ShowTextAligned(cb, Element.ALIGN_RIGHT, new Phrase("Page " + writer.PageNumber, _font), pg.Width - doc.RightMargin, doc.BottomMargin - 15f, 0);
        }
    }

</script>
