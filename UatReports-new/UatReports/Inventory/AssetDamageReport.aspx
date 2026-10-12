<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Data.SqlClient" %>
<%@ Import Namespace="iTextSharp.text" %>
<%@ Import Namespace="iTextSharp.text.pdf" %>
<%@ Import Namespace="System.IO" %>
<script runat="server">
// OBMS Asset Damage PDF Report
// URL: AssetDamageReport.aspx?LoginID=<user>&DamageID=<id>
// SP:  sp_Report_AssetDamage (Migration 25) — single result set. Read-only.

protected void Page_Load(object sender, EventArgs e) {
    string loginID = (Request.QueryString["LoginID"] ?? "").Trim();
    string idStr   = (Request.QueryString["DamageID"] ?? "0").Trim();
    int id = 0;
    if (!int.TryParse(idStr, out id) || id <= 0) { SendError("Invalid or missing Damage ID."); return; }
    var dt = RunSP("sp_Report_AssetDamage", "@DamageID", id);
    if (dt == null || dt.Rows.Count == 0) { SendError("Asset Damage record not found or has been deleted."); return; }
    byte[] pdf; try { pdf = BuildPdf(loginID, dt.Rows[0]); } catch (Exception ex) { LogErr("BuildPdf_AssetDamage", ex.Message); SendError("Unable to generate PDF. Please try again."); return; }
    StreamPdf(pdf, "AssetDamage_" + dt.Rows[0]["DamageNo"] + ".pdf");
}

private byte[] BuildPdf(string loginID, DataRow dr) {
    BaseColor navy=Navy(); BaseColor gold=Gold();
    Font fCoName=FCoName(); Font fCoReg=FCoReg(); Font fCoSub=FCoSub();
    Font fTitle=FTitle(); Font fLabel=FLabel(); Font fValue=FValue(); Font fFoot=FFoot(); Font fSig=FSig();
    using (var ms = new MemoryStream()) {
        var doc=new Document(PageSize.A4,30f,30f,35f,35f);
        var writer=PdfWriter.GetInstance(doc,ms); writer.PageEvent=new FooterEvt(loginID,fFoot,navy); doc.Open();
        AddCoHdr(doc,navy,gold,fCoName,fCoReg,fCoSub);
        P(doc,"ASSET DAMAGE REPORT",fTitle,Element.ALIGN_CENTER,8f);
        var tbl=InfoTbl();
        AddIR(tbl,"Damage No",    Safe(dr,"DamageNo"),       fLabel,fValue);
        AddIR(tbl,"Damage Date",  Safe(dr,"DamageDate"),     fLabel,fValue);
        AddIR(tbl,"Asset Code",   Safe(dr,"AssetCode"),      fLabel,fValue);
        AddIR(tbl,"Asset Name",   Safe(dr,"AssetName"),      fLabel,fValue);
        AddIR(tbl,"Asset Type",   SafeD(dr,"AssetType"),     fLabel,fValue);
        AddIR(tbl,"Serial No",    SafeD(dr,"SerialNumber"),  fLabel,fValue);
        AddIR(tbl,"Damage Type",  SafeD(dr,"DamageType"),    fLabel,fValue);
        AddIR(tbl,"Condition",    SafeD(dr,"DamageCondition"),fLabel,fValue);
        AddIR(tbl,"Reported By",  SafeD(dr,"ReportedBy"),    fLabel,fValue);
        decimal cost=0m; decimal.TryParse(Safe(dr,"EstimatedRepairCost"),out cost);
        AddIR(tbl,"Est. Repair Cost (RM)",cost.ToString("N2"),fLabel,fValue);
        AddIR(tbl,"Prepared By",  SafeD(dr,"PreparedBy"),    fLabel,fValue);
        AddIR(tbl,"Date Printed", DateTime.Now.ToString("dd MMM yyyy"),fLabel,fValue);
        string desc=Safe(dr,"DamageDescription"); if (!string.IsNullOrWhiteSpace(desc)){ var ph=new Phrase();ph.Add(new Chunk("Damage Description: ",fLabel));ph.Add(new Chunk(desc,fValue));tbl.AddCell(new PdfPCell(ph){Colspan=4,Border=Rectangle.NO_BORDER,Padding=2f,PaddingTop=4f}); }
        string rem=Safe(dr,"Remarks"); if (!string.IsNullOrWhiteSpace(rem)) AddRemark(tbl,rem,fLabel,fValue);
        doc.Add(tbl);
        AddSig(doc,fSig,"Reported By",SafeD(dr,"ReportedBy"),"Verified By","________________________");
        doc.Close(); return ms.ToArray();
    }
}

private DataTable RunSP(string sp,string pn,int pv){var dt=new DataTable();try{using(var conn=new SqlConnection(ConnStr()))using(var cmd=new SqlCommand(sp,conn)){cmd.CommandType=CommandType.StoredProcedure;cmd.CommandTimeout=30;cmd.Parameters.AddWithValue(pn,pv);conn.Open();new SqlDataAdapter(cmd).Fill(dt);}}catch(Exception ex){LogErr(sp,ex.Message);return null;}return dt;}
private void StreamPdf(byte[] pdf,string fn){Response.Clear();Response.Buffer=false;Response.ContentType="application/pdf";Response.AddHeader("Content-Disposition","inline; filename="+fn);Response.AddHeader("Content-Length",pdf.Length.ToString());Response.AddHeader("Access-Control-Allow-Origin","*");Response.BinaryWrite(pdf);Response.Flush();Response.End();}
private void AddCoHdr(Document doc,BaseColor navy,BaseColor gold,Font fCoName,Font fCoReg,Font fCoSub){string lp=Server.MapPath("~/images/logo-white.png");var h=new PdfPTable(2){WidthPercentage=100f,SpacingAfter=0f};h.SetWidths(new float[]{1.2f,8.8f});h.DefaultCell.Border=Rectangle.NO_BORDER;PdfPCell lc;if(System.IO.File.Exists(lp)){var lg=iTextSharp.text.Image.GetInstance(lp);lg.ScaleToFit(55f,55f);lc=new PdfPCell(lg){Border=Rectangle.NO_BORDER,VerticalAlignment=Element.ALIGN_MIDDLE,Padding=2f};}else lc=new PdfPCell(new Phrase("")){Border=Rectangle.NO_BORDER};h.AddCell(lc);var tx=new Phrase();tx.Add(new Chunk(AppS("CompanyName")+" ",fCoName));tx.Add(new Chunk(AppS("Registration")+"\n",fCoReg));tx.Add(new Chunk(AppS("Address1")+"\n",fCoSub));tx.Add(new Chunk(AppS("Address2")+"\n",fCoSub));tx.Add(new Chunk(AppS("PostCode")+" "+AppS("City")+"\n",fCoSub));tx.Add(new Chunk(AppS("State")+"\n",fCoSub));tx.Add(new Chunk(AppS("Phone"),fCoSub));h.AddCell(new PdfPCell(tx){Border=Rectangle.NO_BORDER,VerticalAlignment=Element.ALIGN_MIDDLE,Padding=4f,PaddingLeft=8f});doc.Add(h);doc.Add(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(2.5f,100f,gold,Element.ALIGN_CENTER,-2f)));doc.Add(Spacer(6f));}
private static PdfPTable InfoTbl(){var t=new PdfPTable(4){WidthPercentage=100f,SpacingAfter=10f};t.SetWidths(new float[]{2.2f,4f,2.2f,4f});t.DefaultCell.Border=Rectangle.NO_BORDER;return t;}
private static void AddIR(PdfPTable t,string lbl,string val,Font fl,Font fv){t.AddCell(new PdfPCell(new Phrase(lbl+":",fl)){Border=Rectangle.NO_BORDER,Padding=2f});t.AddCell(new PdfPCell(new Phrase(val,fv)){Border=Rectangle.NO_BORDER,Padding=2f});}
private static void AddRemark(PdfPTable t,string rem,Font fl,Font fv){var ph=new Phrase();ph.Add(new Chunk("Remarks: ",fl));ph.Add(new Chunk(rem,fv));t.AddCell(new PdfPCell(ph){Colspan=4,Border=Rectangle.NO_BORDER,Padding=2f,PaddingTop=4f});}
private static void AddSig(Document doc,Font fSig,string lLbl,string lName,string rLbl,string rName){var s=new PdfPTable(2){WidthPercentage=80f,HorizontalAlignment=Element.ALIGN_LEFT,SpacingBefore=14f};s.SetWidths(new float[]{1f,1f});foreach(var p in new[]{new[]{lLbl,lName},new[]{rLbl,rName}}){var ph=new Phrase();ph.Add(new Chunk(p[0]+"\n\n\n",fSig));ph.Add(new Chunk("_______________________\n",fSig));ph.Add(new Chunk(p[1],fSig));s.AddCell(new PdfPCell(ph){Border=Rectangle.NO_BORDER,Padding=6f});}doc.Add(s);}
private static void P(Document doc,string t,Font f,int a,float sp){doc.Add(new Paragraph(t,f){Alignment=a,SpacingAfter=sp});}
private static Paragraph Spacer(float h){return new Paragraph(" "){SpacingAfter=h};}
private static string Safe(DataRow dr,string c){if(dr==null||!dr.Table.Columns.Contains(c))return "";return dr[c]==DBNull.Value?"":(dr[c]??"").ToString().Trim();}
private static string SafeD(DataRow dr,string c){string v=Safe(dr,c);return string.IsNullOrWhiteSpace(v)?"—":v;}
private static string ConnStr(){return string.Format("Server={0};Database={1};User Id={2};Password={3};",AppS("Server"),AppS("Database"),AppS("UserID"),AppS("Password"));}
private static string AppS(string k){return ConfigurationManager.AppSettings[k]??"";}
private static BaseColor Navy(){return new BaseColor(0x1a,0x3a,0x5c);}
private static BaseColor Gold(){return new BaseColor(0xD4,0xA0,0x17);}
private static Font FCoName(){return new Font(Font.FontFamily.HELVETICA,11f,Font.BOLD,Navy());}
private static Font FCoReg(){return new Font(Font.FontFamily.HELVETICA,8f,Font.BOLD,Navy());}
private static Font FCoSub(){return new Font(Font.FontFamily.HELVETICA,8f,Font.NORMAL,BaseColor.DARK_GRAY);}
private static Font FTitle(){return new Font(Font.FontFamily.HELVETICA,12f,Font.BOLD,Navy());}
private static Font FLabel(){return new Font(Font.FontFamily.HELVETICA,8f,Font.BOLD,new BaseColor(0x44,0x44,0x44));}
private static Font FValue(){return new Font(Font.FontFamily.HELVETICA,8f,Font.NORMAL,BaseColor.BLACK);}
private static Font FFoot(){return new Font(Font.FontFamily.HELVETICA,7f,Font.NORMAL,BaseColor.GRAY);}
private static Font FSig(){return new Font(Font.FontFamily.HELVETICA,8f,Font.NORMAL,new BaseColor(0x55,0x55,0x55));}
private void SendError(string msg){Response.Clear();Response.ContentType="text/html";Response.Write("<html><body style='font-family:Arial;padding:30px'><h3 style='color:#c62828'>Asset Damage Report</h3><p>"+Server.HtmlEncode(msg)+"</p></body></html>");Response.End();}
private void LogErr(string src,string msg){try{System.IO.File.AppendAllText(Server.MapPath("~/ReportParamLog.txt"),string.Format("[{0}] ERROR in {1}: {2}\r\n",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),src,msg));}catch{}}
private class FooterEvt:PdfPageEventHelper{private readonly string _u;private readonly Font _f;private readonly BaseColor _n;public FooterEvt(string u,Font f,BaseColor n){_u=u;_f=f;_n=n;}public override void OnEndPage(PdfWriter w,Document d){var cb=w.DirectContent;var pg=d.PageSize;cb.SetLineWidth(0.5f);cb.SetColorStroke(new BaseColor(0xcc,0xcc,0xcc));cb.MoveTo(d.LeftMargin,d.BottomMargin-5f);cb.LineTo(pg.Width-d.RightMargin,d.BottomMargin-5f);cb.Stroke();ColumnText.ShowTextAligned(cb,Element.ALIGN_LEFT,new Phrase("Generated by: "+_u+"   |   "+DateTime.Now.ToString("dd MMM yyyy hh:mm tt"),_f),d.LeftMargin,d.BottomMargin-15f,0);ColumnText.ShowTextAligned(cb,Element.ALIGN_RIGHT,new Phrase("Page "+w.PageNumber,_f),pg.Width-d.RightMargin,d.BottomMargin-15f,0);}}
</script>
