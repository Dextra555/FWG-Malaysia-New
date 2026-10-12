<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Data.SqlClient" %>
<%@ Import Namespace="iTextSharp.text" %>
<%@ Import Namespace="iTextSharp.text.pdf" %>
<%@ Import Namespace="System.IO" %>
<script runat="server">

    // ════════════════════════════════════════════════════════════════════════
    // OBMS Stock Issue Return PDF Report
    // Pattern: identical to StockIssueReport.aspx — all inline,
    //          no code-behind, iTextSharp 5.5.13.x from bin/.
    //
    // URL: IssueReturnReport.aspx?LoginID=<user>&IssueReturnID=<id>
    //
    // Calls sp_Report_StockIssueReturn (Migration 22):
    //   Result Set 1 — document header row
    //   Result Set 2 — detail / line item rows
    //
    // Read-only: does NOT modify StockLedger, stock balances,
    // salary deductions, UniformLoan links, or any other record.
    // ════════════════════════════════════════════════════════════════════════

    protected void Page_Load(object sender, EventArgs e)
    {
        // ── 1. Read + validate query-string ──────────────────────────────
        string loginID      = (Request.QueryString["LoginID"]       ?? "").Trim();
        string returnIDStr  = (Request.QueryString["IssueReturnID"] ?? "0").Trim();

        int issueReturnID = 0;
        if (!int.TryParse(returnIDStr, out issueReturnID) || issueReturnID <= 0)
        { SendError("Invalid or missing Stock Issue Return ID."); return; }

        // ── 2. Execute stored procedure (2 result sets) ───────────────────
        DataTable dtHeader  = new DataTable();
        DataTable dtDetails = new DataTable();
        try
        {
            using (var conn = new SqlConnection(ConnStr()))
            using (var cmd  = new SqlCommand("sp_Report_StockIssueReturn", conn))
            {
                cmd.CommandType    = CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;
                cmd.Parameters.AddWithValue("@IssueReturnID", issueReturnID);
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
            LogErr("sp_Report_StockIssueReturn", ex.Message);
            SendError("Unable to retrieve stock issue return data. Please try again.");
            return;
        }

        if (dtHeader == null || dtHeader.Rows.Count == 0)
        { SendError("Stock Issue Return not found or has been deleted."); return; }

        // ── 3. Build PDF ──────────────────────────────────────────────────
        byte[] pdf;
        try   { pdf = BuildPdf(loginID, dtHeader.Rows[0], dtDetails); }
        catch (Exception ex)
        {
            LogErr("BuildPdf_IssueReturn", ex.Message);
            SendError("Unable to generate PDF. Please try again.");
            return;
        }

        // ── 4. Stream to browser ──────────────────────────────────────────
        string returnNo = dtHeader.Rows[0]["ReturnNo"].ToString();
        Response.Clear();
        Response.Buffer      = false;
        Response.ContentType = "application/pdf";
        Response.AddHeader("Content-Disposition",
            "inline; filename=IssueReturn_" + returnNo + ".pdf");
        Response.AddHeader("Content-Length", pdf.Length.ToString());
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
        // ── Colours (OBMS palette — same as all other transaction reports) ─
        BaseColor navy     = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor gold     = new BaseColor(0xD4, 0xA0, 0x17);
        BaseColor headerBg = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor rowAlt   = new BaseColor(0xf4, 0xf7, 0xfb);
        BaseColor totalBg  = new BaseColor(0xe8, 0xf0, 0xfe);
        BaseColor border   = new BaseColor(0xcc, 0xcc, 0xcc);

        // ── Fonts ─────────────────────────────────────────────────────────
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
        Font fSigLbl  = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, new BaseColor(0x55,0x55,0x55));

        using (var ms = new MemoryStream())
        {
            // A4 landscape — 8 detail columns fit comfortably
            var doc    = new Document(PageSize.A4.Rotate(), 30f, 30f, 35f, 35f);
            var writer = PdfWriter.GetInstance(doc, ms);
            writer.PageEvent = new FooterEvent(loginID, fFoot, navy);
            doc.Open();

            // ── Company header (logo-left + text-right, gold separator) ──
            AddCompanyHeader(doc, navy, fCoName, fCoReg, fCoSub);
            doc.Add(new Chunk(
                new iTextSharp.text.pdf.draw.LineSeparator(2.5f, 100f, gold, Element.ALIGN_CENTER, -2f)));
            doc.Add(Spacer(6f));

            // ── Report title ──────────────────────────────────────────────
            P(doc, "STOCK ISSUE RETURN", fTitle, Element.ALIGN_CENTER, 8f);

            // ── Document header info (2-column grid) ──────────────────────
            // Reads only columns confirmed in sp_Report_StockIssueReturn result set 1.
            var infoTbl = new PdfPTable(4) { WidthPercentage = 100f, SpacingAfter = 10f };
            infoTbl.SetWidths(new float[] { 2.2f, 4f, 2.2f, 4f });
            infoTbl.DefaultCell.Border = Rectangle.NO_BORDER;

            AddInfoRow(infoTbl, "Return No",       Safe(hdr, "ReturnNo"),         fLabel, fValue);
            AddInfoRow(infoTbl, "Return Date",     Safe(hdr, "ReturnDate"),       fLabel, fValue);
            AddInfoRow(infoTbl, "Original Issue",  Safe(hdr, "OriginalIssueNo"),  fLabel, fValue);
            AddInfoRow(infoTbl, "Return Warehouse", Safe(hdr, "WarehouseName"),   fLabel, fValue);
            AddInfoRow(infoTbl, "Issued To",       SafeOrDash(hdr, "IssueToName"), fLabel, fValue);
            AddInfoRow(infoTbl, "Prepared By",     SafeOrDash(hdr, "PreparedBy"),  fLabel, fValue);
            AddInfoRow(infoTbl, "Date Printed",    DateTime.Now.ToString("dd MMM yyyy"), fLabel, fValue);
            AddInfoRow(infoTbl, "Last Updated",    SafeOrDash(hdr, "LastUpdateDate"), fLabel, fValue);

            string remarks = Safe(hdr, "Remarks");
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                var ph = new Phrase();
                ph.Add(new Chunk("Remarks: ", fLabel));
                ph.Add(new Chunk(remarks, fValue));
                infoTbl.AddCell(new PdfPCell(ph)
                {
                    Colspan = 4, Border = Rectangle.NO_BORDER,
                    Padding = 2f, PaddingTop = 4f
                });
            }
            doc.Add(infoTbl);

            // Thin navy separator before item table
            doc.Add(new Chunk(
                new iTextSharp.text.pdf.draw.LineSeparator(0.5f, 100f, navy, Element.ALIGN_CENTER, -2f)));
            doc.Add(Spacer(4f));

            // ── Items table ───────────────────────────────────────────────
            P(doc, "Returned Items", new Font(Font.FontFamily.HELVETICA, 9f, Font.BOLD, navy),
              Element.ALIGN_LEFT, 4f);

            // 8 columns — fits A4 landscape at 30mm margins
            // Columns use only fields confirmed in sp_Report_StockIssueReturn result set 2.
            var tbl = new PdfPTable(8)
            {
                WidthPercentage = 100f,
                HeaderRows      = 1,
                SpacingAfter    = 16f
            };
            tbl.SetWidths(new float[] { 0.5f, 1.8f, 4f, 2f, 1.5f, 1.8f, 2f, 2.5f });

            string[] colHdrs = { "#", "Code", "Item Name", "Category", "UOM",
                                 "Return Qty", "Unit Cost (RM)", "Total (RM)" };
            bool[]   isNum   = { false, false, false, false, false, true, true, true };

            foreach (var pair in new[] {
                new { h = colHdrs[0], n = isNum[0] },
                new { h = colHdrs[1], n = isNum[1] },
                new { h = colHdrs[2], n = isNum[2] },
                new { h = colHdrs[3], n = isNum[3] },
                new { h = colHdrs[4], n = isNum[4] },
                new { h = colHdrs[5], n = isNum[5] },
                new { h = colHdrs[6], n = isNum[6] },
                new { h = colHdrs[7], n = isNum[7] },
            })
            {
                tbl.AddCell(new PdfPCell(new Phrase(pair.h, fTH))
                {
                    BackgroundColor     = headerBg,
                    HorizontalAlignment = pair.n ? Element.ALIGN_RIGHT : Element.ALIGN_CENTER,
                    Padding = 5f, Border = Rectangle.BOX, BorderColor = border
                });
            }

            int rowNo = 0;
            decimal totalQty = 0m, grandTotal = 0m;

            foreach (DataRow dr in dtDetails.Rows)
            {
                var bg = (rowNo % 2 == 0) ? BaseColor.WHITE : rowAlt;
                rowNo++;

                decimal returnQty  = ParseDec(dr, "ReturnQty");
                decimal unitCost   = ParseDec(dr, "UnitCost");
                decimal lineTotal  = ParseDec(dr, "LineTotal");
                totalQty   += returnQty;
                grandTotal += lineTotal;

                AddCell(tbl, rowNo.ToString(),          fTD, Element.ALIGN_CENTER, bg, border);
                AddCell(tbl, Safe(dr, "ItemCode"),       fTD, Element.ALIGN_LEFT,   bg, border);
                AddCell(tbl, Safe(dr, "ItemName"),       fTD, Element.ALIGN_LEFT,   bg, border);
                AddCell(tbl, Safe(dr, "CategoryName"),   fTD, Element.ALIGN_LEFT,   bg, border);
                AddCell(tbl, Safe(dr, "UOMName"),        fTD, Element.ALIGN_LEFT,   bg, border);
                AddCell(tbl, returnQty.ToString("N2"),   fTD, Element.ALIGN_RIGHT,  bg, border);
                AddCell(tbl, unitCost.ToString("N2"),    fTD, Element.ALIGN_RIGHT,  bg, border);
                AddCell(tbl, lineTotal.ToString("N2"),   fTD, Element.ALIGN_RIGHT,  bg, border);
            }

            if (dtDetails.Rows.Count == 0)
            {
                tbl.AddCell(new PdfPCell(new Phrase("No items found.", fTD))
                {
                    Colspan = 8, HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 8f, Border = Rectangle.BOX, BorderColor = border
                });
            }
            else
            {
                // ── Totals row ────────────────────────────────────────────
                for (int i = 0; i < 4; i++)
                    AddCell(tbl, "", fTot, Element.ALIGN_LEFT, totalBg, border);
                AddCell(tbl, "Total",                    fTot, Element.ALIGN_RIGHT, totalBg, border);
                AddCell(tbl, totalQty.ToString("N2"),     fTot, Element.ALIGN_RIGHT, totalBg, border);
                AddCell(tbl, "Grand Total (RM)",          fTot, Element.ALIGN_RIGHT, totalBg, border);
                AddCell(tbl, grandTotal.ToString("N2"),   fTot, Element.ALIGN_RIGHT, totalBg, border);
            }

            doc.Add(tbl);

            // ── Signature section ─────────────────────────────────────────
            AddSignatureRow(doc, fSigLbl,
                "Prepared By",   SafeOrDash(hdr, "PreparedBy"),
                "Returned By",   "________________________");

            doc.Close();
            return ms.ToArray();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // SHARED HELPERS  (same pattern as StockIssueReport.aspx / PRReport.aspx)
    // ════════════════════════════════════════════════════════════════════════

    private void AddCompanyHeader(Document doc, BaseColor navy, Font fCoName, Font fCoReg, Font fCoSub)
    {
        string logoPath = Server.MapPath("~/images/logo-white.png");
        var hdr = new PdfPTable(2) { WidthPercentage = 100f, SpacingAfter = 0f };
        hdr.SetWidths(new float[] { 1.2f, 8.8f });
        hdr.DefaultCell.Border = Rectangle.NO_BORDER;

        // Left cell — logo (falls back gracefully if file missing)
        PdfPCell logoCell;
        if (System.IO.File.Exists(logoPath))
        {
            var logo = iTextSharp.text.Image.GetInstance(logoPath);
            logo.ScaleToFit(55f, 55f);
            logoCell = new PdfPCell(logo)
            {
                Border            = Rectangle.NO_BORDER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding           = 2f
            };
        }
        else
        {
            logoCell = new PdfPCell(new Phrase("")) { Border = Rectangle.NO_BORDER };
        }
        hdr.AddCell(logoCell);

        // Right cell — company name + address
        var txt = new Phrase();
        txt.Add(new Chunk(AppSettings("CompanyName") + " ",       fCoName));
        txt.Add(new Chunk(AppSettings("Registration") + "\n",     fCoReg));
        txt.Add(new Chunk(AppSettings("Address1") + "\n",         fCoSub));
        txt.Add(new Chunk(AppSettings("Address2") + "\n",         fCoSub));
        txt.Add(new Chunk(AppSettings("PostCode") + " " +
                          AppSettings("City") + "\n",             fCoSub));
        txt.Add(new Chunk(AppSettings("State") + "\n",            fCoSub));
        txt.Add(new Chunk(AppSettings("Phone"),                   fCoSub));

        hdr.AddCell(new PdfPCell(txt)
        {
            Border            = Rectangle.NO_BORDER,
            VerticalAlignment = Element.ALIGN_MIDDLE,
            Padding           = 4f,
            PaddingLeft       = 8f
        });
        doc.Add(hdr);
    }

    private static void AddInfoRow(PdfPTable tbl, string label, string value,
                                   Font fLabel, Font fValue)
    {
        tbl.AddCell(new PdfPCell(new Phrase(label + ":", fLabel))
            { Border = Rectangle.NO_BORDER, Padding = 2f });
        tbl.AddCell(new PdfPCell(new Phrase(value, fValue))
            { Border = Rectangle.NO_BORDER, Padding = 2f });
    }

    private static void AddCell(PdfPTable tbl, string text, Font font,
                                 int align, BaseColor bg, BaseColor borderCol)
    {
        tbl.AddCell(new PdfPCell(new Phrase(text, font))
        {
            HorizontalAlignment = align,
            BackgroundColor     = bg,
            Padding             = 4f,
            Border              = Rectangle.BOX,
            BorderColor         = borderCol
        });
    }

    private static void AddSignatureRow(Document doc, Font fLbl,
        string lLabel, string lName,
        string rLabel, string rName)
    {
        var sigTbl = new PdfPTable(2)
        {
            WidthPercentage    = 80f,
            HorizontalAlignment = Element.ALIGN_LEFT,
            SpacingBefore       = 14f
        };
        sigTbl.SetWidths(new float[] { 1f, 1f });
        foreach (var pair in new[] { new[] { lLabel, lName }, new[] { rLabel, rName } })
        {
            var ph = new Phrase();
            ph.Add(new Chunk(pair[0] + "\n\n\n", fLbl));
            ph.Add(new Chunk("_______________________\n", fLbl));
            ph.Add(new Chunk(pair[1], fLbl));
            sigTbl.AddCell(new PdfPCell(ph) { Border = Rectangle.NO_BORDER, Padding = 6f });
        }
        doc.Add(sigTbl);
    }

    // ── Utility helpers ───────────────────────────────────────────────────

    private static string ConnStr()
    {
        return string.Format("Server={0};Database={1};User Id={2};Password={3};",
            ConfigurationManager.AppSettings["Server"]   ?? ".",
            ConfigurationManager.AppSettings["Database"] ?? "",
            ConfigurationManager.AppSettings["UserID"]   ?? "",
            ConfigurationManager.AppSettings["Password"] ?? "");
    }

    private static string AppSettings(string key)
    {
        return ConfigurationManager.AppSettings[key] ?? "";
    }

    /// <summary>Returns the field value as a trimmed string, or "" if null/missing.</summary>
    private static string Safe(DataRow dr, string col)
    {
        if (dr == null || !dr.Table.Columns.Contains(col)) return "";
        return dr[col] == DBNull.Value ? "" : (dr[col] ?? "").ToString().Trim();
    }

    /// <summary>Like Safe, but returns "—" for empty/whitespace values.</summary>
    private static string SafeOrDash(DataRow dr, string col)
    {
        string v = Safe(dr, col);
        return string.IsNullOrWhiteSpace(v) ? "—" : v;
    }

    /// <summary>Parses a decimal from a DataRow field; returns 0 on failure.</summary>
    private static decimal ParseDec(DataRow dr, string col)
    {
        if (dr == null || !dr.Table.Columns.Contains(col) || dr[col] == DBNull.Value)
            return 0m;
        decimal v;
        decimal.TryParse(dr[col].ToString(), out v);
        return v;
    }

    private static void P(Document doc, string text, Font font, int align, float spacingAfter)
    {
        doc.Add(new Paragraph(text, font) { Alignment = align, SpacingAfter = spacingAfter });
    }

    private static Paragraph Spacer(float h)
    {
        return new Paragraph(" ") { SpacingAfter = h };
    }

    private void SendError(string msg)
    {
        Response.Clear();
        Response.ContentType = "text/html";
        Response.Write(
            "<html><body style='font-family:Arial;padding:30px'>" +
            "<h3 style='color:#c62828'>Stock Issue Return Report</h3>" +
            "<p style='color:#333'>" + Server.HtmlEncode(msg) + "</p>" +
            "</body></html>");
        Response.End();
    }

    private void LogErr(string src, string msg)
    {
        try
        {
            string path  = Server.MapPath("~/ReportParamLog.txt");
            string entry = string.Format("[{0}] ERROR in {1}: {2}{3}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                src, msg, Environment.NewLine);
            System.IO.File.AppendAllText(path, entry);
        }
        catch { /* never let logging crash the response */ }
    }

    // ── Page footer event (page number + generated-by) ────────────────────
    // Identical pattern to StockIssueReport.aspx / PRReport.aspx etc.
    private class FooterEvent : PdfPageEventHelper
    {
        private readonly string    _user;
        private readonly Font      _font;
        private readonly BaseColor _navy;

        public FooterEvent(string user, Font font, BaseColor navy)
        { _user = user; _font = font; _navy = navy; }

        public override void OnEndPage(PdfWriter writer, Document doc)
        {
            var cb = writer.DirectContent;
            var pg = doc.PageSize;

            // Thin separator line above footer text
            cb.SetLineWidth(0.5f);
            cb.SetColorStroke(new BaseColor(0xcc, 0xcc, 0xcc));
            cb.MoveTo(doc.LeftMargin, doc.BottomMargin - 5f);
            cb.LineTo(pg.Width - doc.RightMargin, doc.BottomMargin - 5f);
            cb.Stroke();

            string left = "Generated by: " + _user + "   |   " +
                          DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
            ColumnText.ShowTextAligned(cb, Element.ALIGN_LEFT,
                new Phrase(left, _font),
                doc.LeftMargin, doc.BottomMargin - 15f, 0);

            ColumnText.ShowTextAligned(cb, Element.ALIGN_RIGHT,
                new Phrase("Page " + writer.PageNumber, _font),
                pg.Width - doc.RightMargin, doc.BottomMargin - 15f, 0);
        }
    }

</script>
