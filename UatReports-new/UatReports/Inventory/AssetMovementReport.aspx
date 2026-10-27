<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Data.SqlClient" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="iTextSharp.text" %>
<%@ Import Namespace="iTextSharp.text.pdf" %>
<%@ Import Namespace="System.IO" %>
<script runat="server">

    // ════════════════════════════════════════════════════════════════════════
    // OBMS Asset Movement / Allocation Report — PDF / Excel
    // Single-file inline C# (no code-behind) — same pattern as StockIssueListReport.aspx
    // Flow: Angular → ASPX → sp_AssetMovementReport → PDF / Excel → Browser
    //
    // Combines: Allocation | Transfer | Return
    // Columns: No, Movement Type, Doc No, Date, Asset Code, Asset Name,
    //          Asset Type, Serial No, Brand, From Party, To Party,
    //          Current Status, Condition, Remarks
    //
    // PDF: colour-coded by type
    //   Allocation = light blue | Transfer = light green | Return = light amber
    // ════════════════════════════════════════════════════════════════════════

    protected void Page_Load(object sender, EventArgs e)
    {
        // ── 1. Read query-string params ───────────────────────────────────
        string loginID       = (Request.QueryString["LoginID"]       ?? "").Trim();
        string assetIDStr    = (Request.QueryString["AssetID"]       ?? "0").Trim();
        string movementType  = (Request.QueryString["MovementType"]  ?? "ALL").Trim();
        string startDateStr  = (Request.QueryString["StartDate"]     ?? "").Trim();
        string endDateStr    = (Request.QueryString["EndDate"]       ?? "").Trim();
        string format        = (Request.QueryString["format"]        ?? "pdf").ToLower().Trim();

        // ── 2. Validate dates ─────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(startDateStr) || string.IsNullOrWhiteSpace(endDateStr))
        { SendError("Please select a valid date range."); return; }

        DateTime startDate, endDate;
        if (!DateTime.TryParse(startDateStr, out startDate) ||
            !DateTime.TryParse(endDateStr,   out endDate))
        { SendError("Please select a valid date range."); return; }

        if (startDate.Date > endDate.Date)
        { SendError("Start date must be before end date."); return; }

        int assetID = 0; int.TryParse(assetIDStr, out assetID);
        if (string.IsNullOrWhiteSpace(movementType)) movementType = "ALL";

        // ── 3. Execute stored procedure ───────────────────────────────────
        DataTable dt;
        try   { dt = RunSP(assetID, movementType, startDate, endDate); }
        catch (Exception ex)
        {
            LogErr("sp_AssetMovementReport", ex.Message);
            SendError("Unable to retrieve asset movement data. Please try again.");
            return;
        }

        if (dt == null || dt.Rows.Count == 0)
        { SendError("No asset movement data found for the selected filters."); return; }

        // ── 4. Resolve display names ──────────────────────────────────────
        string assetName  = assetID > 0
            ? (Lookup("SELECT Name FROM AssetMaster WHERE ID=@p", assetID.ToString()) ?? assetIDStr)
            : "All Assets";
        string typeLabel  = movementType == "ALL"        ? "All Movements"
                          : movementType == "ALLOCATION" ? "Allocation"
                          : movementType == "TRANSFER"   ? "Transfer"
                          : movementType == "RETURN"     ? "Return"
                          : movementType;
        string period     = startDate.ToString("dd MMM yyyy") + " to " + endDate.ToString("dd MMM yyyy");

        // ── 5. Build PDF or Excel ─────────────────────────────────────────
        if (format == "excel")
        {
            byte[] xlsx;
            try   { xlsx = BuildExcel(assetName, typeLabel, period, dt); }
            catch (Exception ex)
            {
                LogErr("BuildExcel", ex.Message);
                SendError("Unable to generate Excel. Please try again.");
                return;
            }
            Response.Clear();
            Response.Buffer      = false;
            Response.ContentType = "application/vnd.ms-excel";
            Response.AddHeader("Content-Disposition",
                "attachment; filename=AssetMovement_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls");
            Response.AddHeader("Content-Length", xlsx.Length.ToString());
            Response.AddHeader("Access-Control-Allow-Origin", "*");
            Response.BinaryWrite(xlsx);
            Response.Flush();
            Response.End();
            return;
        }

        // ── 5b. Build PDF ─────────────────────────────────────────────────
        byte[] pdf;
        try   { pdf = BuildPdf(loginID, assetName, typeLabel, period, dt); }
        catch (Exception ex)
        {
            LogErr("BuildPdf", ex.Message);
            SendError("Unable to generate PDF. Please try again.");
            return;
        }
        Response.Clear();
        Response.Buffer      = false;
        Response.ContentType = "application/pdf";
        Response.AddHeader("Content-Disposition",
            "inline; filename=AssetMovement_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
        Response.AddHeader("Content-Length", pdf.Length.ToString());
        Response.AddHeader("Access-Control-Allow-Origin", "*");
        Response.BinaryWrite(pdf);
        Response.Flush();
        Response.End();
    }

    // ════════════════════════════════════════════════════════════════════════
    // PDF BUILDER
    // ════════════════════════════════════════════════════════════════════════

    private byte[] BuildPdf(string loginID, string assetName, string typeLabel,
                            string period, DataTable dt)
    {
        // ── Colours ────────────────────────────────────────────────────────
        BaseColor navy     = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor headerBg = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor rowAlt   = new BaseColor(0xf4, 0xf7, 0xfb);
        BaseColor totalBg  = new BaseColor(0xe8, 0xf0, 0xfe);
        BaseColor border   = new BaseColor(0xcc, 0xcc, 0xcc);
        BaseColor gold     = new BaseColor(0xD4, 0xA0, 0x17);
        BaseColor allocBg  = new BaseColor(0xe0, 0xf0, 0xff);   // light blue  — Allocation
        BaseColor transBg  = new BaseColor(0xe6, 0xf4, 0xe6);   // light green — Transfer
        BaseColor retBg    = new BaseColor(0xff, 0xf3, 0xcd);   // light amber — Return

        // ── Fonts ──────────────────────────────────────────────────────────
        Font fTitle    = new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD,   navy);
        Font fFilter   = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, new BaseColor(0x44,0x44,0x44));
        Font fFilterB  = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   new BaseColor(0x44,0x44,0x44));
        Font fTH       = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   BaseColor.WHITE);
        Font fTD       = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.NORMAL, BaseColor.BLACK);
        Font fTDBlue   = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.BOLD,   new BaseColor(0x00,0x44,0x88));
        Font fTDGreen  = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.BOLD,   new BaseColor(0x1a,0x6e,0x1a));
        Font fTDAmber  = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.BOLD,   new BaseColor(0x99,0x66,0x00));
        Font fTotal    = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
        Font fFoot     = new Font(Font.FontFamily.HELVETICA,  7f, Font.NORMAL, BaseColor.GRAY);
        Font fLegend   = new Font(Font.FontFamily.HELVETICA, 7.5f, Font.NORMAL, new BaseColor(0x44,0x44,0x44));

        // ── Column definitions ─────────────────────────────────────────────
        var cols = new object[][]
        {
            new object[]{ "S.NO.",         "#",              1.2f,  true  },
            new object[]{ "Movement",      "MovementType",   1.7f,  false },
            new object[]{ "Doc No",        "DocNo",          1.7f,  false },
            new object[]{ "Date",          "DocDate",        1.4f,  false },
            new object[]{ "Asset Code",    "AssetCode",      1.4f,  false },
            new object[]{ "Asset Name",    "AssetName",      2.8f,  false },
            new object[]{ "Asset Type",    "AssetType",      1.5f,  false },
            new object[]{ "Serial No",     "SerialNumber",   1.8f,  false },
            new object[]{ "Brand",         "BrandName",      1.4f,  false },
            new object[]{ "From",          "FromParty",      2.5f,  false },
            new object[]{ "To",            "ToParty",        2.5f,  false },
            new object[]{ "Status",        "CurrentStatus",  1.5f,  false },
            new object[]{ "Condition",     "AssetCondition", 1.3f,  false },
            new object[]{ "Remarks",       "Remarks",        2.0f,  false },
        };

        float[] widths = new float[cols.Length];
        for (int i = 0; i < cols.Length; i++) widths[i] = (float)cols[i][2];

        using (var ms = new MemoryStream())
        {
            var doc    = new Document(PageSize.A4.Rotate(), 12f, 12f, 30f, 30f);
            var writer = PdfWriter.GetInstance(doc, ms);
            writer.PageEvent = new FooterEvent(loginID, fFoot, navy);
            doc.Open();

            // ── Company header ─────────────────────────────────────────────
            string logoPath = Server.MapPath("~/images/logo-white.png");
            Font fCoName = new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD,   navy);
            Font fCoReg  = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
            Font fCoSub  = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, BaseColor.DARK_GRAY);

            var hdrTable = new PdfPTable(2) { WidthPercentage = 100f, SpacingAfter = 0f };
            hdrTable.SetWidths(new float[] { 1.2f, 8.8f });
            hdrTable.DefaultCell.Border = Rectangle.NO_BORDER;

            PdfPCell logoCell;
            if (System.IO.File.Exists(logoPath))
            {
                var logo = iTextSharp.text.Image.GetInstance(logoPath);
                logo.ScaleToFit(55f, 55f);
                logoCell = new PdfPCell(logo)
                {
                    Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_MIDDLE,
                    HorizontalAlignment = Element.ALIGN_LEFT, Padding = 2f
                };
            }
            else logoCell = new PdfPCell(new Phrase("")) { Border = Rectangle.NO_BORDER };
            hdrTable.AddCell(logoCell);

            var textPhrase = new Phrase();
            textPhrase.Add(new Chunk(AppSettings("CompanyName") + " ", fCoName));
            textPhrase.Add(new Chunk(AppSettings("Registration") + "\n", fCoReg));
            textPhrase.Add(new Chunk(AppSettings("Address1") + "\n", fCoSub));
            textPhrase.Add(new Chunk(AppSettings("Address2") + "\n", fCoSub));
            textPhrase.Add(new Chunk(AppSettings("PostCode") + " " + AppSettings("City") + "\n", fCoSub));
            textPhrase.Add(new Chunk(AppSettings("State") + "\n", fCoSub));
            textPhrase.Add(new Chunk(AppSettings("Phone"), fCoSub));
            hdrTable.AddCell(new PdfPCell(textPhrase)
            {
                Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_MIDDLE,
                HorizontalAlignment = Element.ALIGN_LEFT, Padding = 4f, PaddingLeft = 8f
            });
            doc.Add(hdrTable);

            doc.Add(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(2.5f, 100f, gold, Element.ALIGN_CENTER, -2f)));
            doc.Add(Spacer(6f));

            P(doc, "ASSET MOVEMENT / ALLOCATION REPORT", fTitle, Element.ALIGN_CENTER, 4f);

            // ── Filter summary ─────────────────────────────────────────────
            var ft = new PdfPTable(2) { WidthPercentage = 100f, SpacingAfter = 4f };
            ft.SetWidths(new float[] { 1f, 1f });
            ft.DefaultCell.Border = Rectangle.NO_BORDER;
            string[][] filterPairs = new string[][]
            {
                new[]{ "Asset:",          assetName },
                new[]{ "Movement Type:",  typeLabel },
                new[]{ "Period:",         period    },
                new[]{ "Total Records:",  dt.Rows.Count.ToString() }
            };
            foreach (var f in filterPairs)
            {
                var ph = new Phrase();
                ph.Add(new Chunk(f[0] + " ", fFilterB));
                ph.Add(new Chunk(f[1], fFilter));
                ft.AddCell(new PdfPCell(ph) { Border = Rectangle.NO_BORDER, Padding = 2f });
            }
            doc.Add(ft);

            // ── Colour legend ──────────────────────────────────────────────
            var legTable = new PdfPTable(4) { WidthPercentage = 70f, SpacingAfter = 6f };
            legTable.HorizontalAlignment = Element.ALIGN_LEFT;
            legTable.DefaultCell.Border  = Rectangle.NO_BORDER;
            legTable.AddCell(new PdfPCell(new Phrase("  Allocation  ", fLegend))
                { BackgroundColor = allocBg, Border = Rectangle.BOX, BorderColor = border, Padding = 3f });
            legTable.AddCell(new PdfPCell(new Phrase("  Transfer  ", fLegend))
                { BackgroundColor = transBg, Border = Rectangle.BOX, BorderColor = border, Padding = 3f });
            legTable.AddCell(new PdfPCell(new Phrase("  Return  ", fLegend))
                { BackgroundColor = retBg,   Border = Rectangle.BOX, BorderColor = border, Padding = 3f });
            legTable.AddCell(new PdfPCell(new Phrase("")) { Border = Rectangle.NO_BORDER });
            doc.Add(legTable);

            // ── Data table ─────────────────────────────────────────────────
            var tbl = new PdfPTable(cols.Length) { WidthPercentage = 100f, HeaderRows = 1 };
            tbl.SetWidths(widths);

            // Column header row
            foreach (var c in cols)
            {
                bool isNum = (bool)c[3];
                tbl.AddCell(new PdfPCell(new Phrase((string)c[0], fTH))
                {
                    BackgroundColor     = headerBg,
                    HorizontalAlignment = isNum ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT,
                    Padding = 5f, Border = Rectangle.BOX, BorderColor = border
                });
            }

            // Data rows
            int ri = 0;
            foreach (DataRow dr in dt.Rows)
            {
                ri++;
                string mtc  = dr["MovementTypeCode"] != DBNull.Value ? dr["MovementTypeCode"].ToString() : "";
                BaseColor rowBg = mtc == "ALLOCATION" ? allocBg
                                : mtc == "TRANSFER"   ? transBg
                                : mtc == "RETURN"     ? retBg
                                : (ri % 2 == 0 ? rowAlt : BaseColor.WHITE);

                foreach (var c in cols)
                {
                    string field = (string)c[1];
                    bool   isNum = (bool)c[3];
                    Font   cf    = fTD;
                    string txt;

                    if (field == "#")
                    {
                        txt = ri.ToString();
                    }
                    else
                    {
                        object raw = dt.Columns.Contains(field) ? dr[field] : null;
                        txt = FmtCell(raw, isNum);

                        if (field == "MovementType")
                            cf = mtc == "ALLOCATION" ? fTDBlue
                               : mtc == "TRANSFER"   ? fTDGreen
                               : mtc == "RETURN"     ? fTDAmber
                               : fTD;
                    }

                    tbl.AddCell(new PdfPCell(new Phrase(txt, cf))
                    {
                        BackgroundColor     = rowBg,
                        HorizontalAlignment = (isNum || field == "#") ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT,
                        Padding = 3f, Border = Rectangle.BOX, BorderColor = border
                    });
                }
            }

            // ── Summary row ────────────────────────────────────────────────
            // Count by type
            int cntAlloc = 0, cntTrans = 0, cntRet = 0;
            foreach (DataRow dr in dt.Rows)
            {
                string mtc = dr["MovementTypeCode"] != DBNull.Value ? dr["MovementTypeCode"].ToString() : "";
                if (mtc == "ALLOCATION") cntAlloc++;
                else if (mtc == "TRANSFER") cntTrans++;
                else if (mtc == "RETURN")   cntRet++;
            }
            string summaryTxt = string.Format("TOTAL: {0} record(s)   |   Allocations: {1}   |   Transfers: {2}   |   Returns: {3}",
                dt.Rows.Count, cntAlloc, cntTrans, cntRet);
            tbl.AddCell(new PdfPCell(new Phrase(summaryTxt, fTotal))
            {
                Colspan             = cols.Length,
                BackgroundColor     = totalBg,
                HorizontalAlignment = Element.ALIGN_LEFT,
                Padding = 5f, Border = Rectangle.BOX, BorderColor = navy, BorderWidth = 1.5f
            });

            doc.Add(tbl);
            doc.Close();
            return ms.ToArray();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // EXCEL BUILDER
    // ════════════════════════════════════════════════════════════════════════

    private byte[] BuildExcel(string assetName, string typeLabel, string period, DataTable dt)
    {
        var cols = new string[][]
        {
            new[]{ "S.NO.",        "#"               },
            new[]{ "Movement",     "MovementType"    },
            new[]{ "Doc No",       "DocNo"           },
            new[]{ "Date",         "DocDate"         },
            new[]{ "Asset Code",   "AssetCode"       },
            new[]{ "Asset Name",   "AssetName"       },
            new[]{ "Asset Type",   "AssetType"       },
            new[]{ "Serial No",    "SerialNumber"    },
            new[]{ "Brand",        "BrandName"       },
            new[]{ "From",         "FromParty"       },
            new[]{ "To",           "ToParty"         },
            new[]{ "Status",       "CurrentStatus"   },
            new[]{ "Condition",    "AssetCondition"  },
            new[]{ "Remarks",      "Remarks"         },
            new[]{ "Prepared By",  "PreparedBy"      },
        };

        var numFlds = new System.Collections.Generic.HashSet<string>();  // no numeric cols

        int span = cols.Length;
        var sb   = new System.Text.StringBuilder();

        sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" " +
                      "xmlns:x=\"urn:schemas-microsoft-com:office:excel\" " +
                      "xmlns=\"http://www.w3.org/TR/REC-html40\">");
        sb.AppendLine("<head><meta charset=\"UTF-8\"/>");
        sb.AppendLine("<!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets>" +
                      "<x:ExcelWorksheet><x:Name>Asset Movement</x:Name>" +
                      "<x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions>" +
                      "</x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]-->");
        sb.AppendLine("<style>");
        sb.AppendLine("body,td,th{font-family:Arial;font-size:10pt;}");
        sb.AppendLine(".co{font-size:13pt;font-weight:bold;color:#1a3a5c;}");
        sb.AppendLine(".sub{font-size:9pt;color:#555;}");
        sb.AppendLine(".ttl{font-size:12pt;font-weight:bold;color:#1a3a5c;text-align:center;}");
        sb.AppendLine(".fl{font-weight:bold;font-size:9pt;}");
        sb.AppendLine(".fv{font-size:9pt;}");
        sb.AppendLine(".th{background:#1a3a5c;color:#fff;font-weight:bold;font-size:9pt;padding:5px;border:1px solid #ccc;}");
        sb.AppendLine(".td{font-size:8.5pt;padding:4px;border:1px solid #ddd;}");
        sb.AppendLine(".tn{font-size:8.5pt;padding:4px;border:1px solid #ddd;text-align:right;}");
        sb.AppendLine(".alloc{background:#e0f0ff;}");
        sb.AppendLine(".trans{background:#e6f4e6;}");
        sb.AppendLine(".ret{background:#fff3cd;}");
        sb.AppendLine(".alt{background:#f4f7fb;}");
        sb.AppendLine(".tot{background:#e8f0fe;font-weight:bold;border-top:2px solid #1a3a5c;}");
        sb.AppendLine(".ftr{font-size:8pt;color:#888;}");
        sb.AppendLine(".gold{border-bottom:2px solid #D4A017;}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<table style=\"width:100%;border-collapse:collapse;\">");

        // Company header
        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"co\">{1} <span style=\"font-size:9pt\">{2}</span></td></tr>\n",
            span, H(AppSettings("CompanyName")), H(AppSettings("Registration")));
        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"sub\">{1} {2}</td></tr>\n",
            span, H(AppSettings("Address1")), H(AppSettings("Address2")));
        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"sub\">{1} {2}, {3}</td></tr>\n",
            span, H(AppSettings("PostCode")), H(AppSettings("City")), H(AppSettings("State")));
        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"sub\">{1}</td></tr>\n",
            span, H(AppSettings("Phone")));
        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"gold\" style=\"padding:2px;\"></td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:3px;\"></td></tr>\n", span);

        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"ttl\">ASSET MOVEMENT / ALLOCATION REPORT</td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:2px;\"></td></tr>\n", span);

        sb.AppendFormat("<tr><td colspan=\"{0}\">", span);
        sb.AppendFormat("<span class=\"fl\">Asset: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;",        H(assetName));
        sb.AppendFormat("<span class=\"fl\">Movement Type: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;", H(typeLabel));
        sb.AppendFormat("<span class=\"fl\">Period: </span><span class=\"fv\">{0}</span>",                          H(period));
        sb.AppendLine("</td></tr>");

        // Legend
        sb.AppendFormat("<tr><td colspan=\"{0}\">", span);
        sb.Append("<span style=\"background:#e0f0ff;padding:2px 6px;border:1px solid #ccc;\">Allocation</span>&nbsp;");
        sb.Append("<span style=\"background:#e6f4e6;padding:2px 6px;border:1px solid #ccc;\">Transfer</span>&nbsp;");
        sb.Append("<span style=\"background:#fff3cd;padding:2px 6px;border:1px solid #ccc;\">Return</span>");
        sb.AppendLine("</td></tr>");
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:3px;\"></td></tr>\n", span);

        // Column headers
        sb.AppendLine("<tr>");
        foreach (var c in cols) sb.AppendFormat("<td class=\"th\">{0}</td>", H(c[0]));
        sb.AppendLine("</tr>");

        // Data rows
        int ri = 0;
        int cntAlloc = 0, cntTrans = 0, cntRet = 0;
        foreach (DataRow dr in dt.Rows)
        {
            ri++;
            string mtc  = dr["MovementTypeCode"] != DBNull.Value ? dr["MovementTypeCode"].ToString() : "";
            bool isAlloc = mtc == "ALLOCATION";
            bool isTrans = mtc == "TRANSFER";
            bool isRet   = mtc == "RETURN";
            string alt   = (ri % 2 == 1) ? "" : " alt";
            string baseCls = isAlloc ? "alloc" : isTrans ? "trans" : isRet ? "ret" : "";

            if (isAlloc) cntAlloc++;
            else if (isTrans) cntTrans++;
            else if (isRet)   cntRet++;

            sb.AppendLine("<tr>");
            foreach (var c in cols)
            {
                string fld = c[1];
                if (fld == "#") { sb.AppendFormat("<td class=\"tn{0}\">{1}</td>", alt, ri); continue; }

                object raw = dt.Columns.Contains(fld) ? dr[fld] : null;
                string txt = raw != null && raw != DBNull.Value ? raw.ToString() : "";
                string cls = !string.IsNullOrEmpty(baseCls) ? baseCls + " td" : "td" + alt;
                sb.AppendFormat("<td class=\"{0}\">{1}</td>", cls, H(txt));
            }
            sb.AppendLine("</tr>");
        }

        // Summary row
        sb.AppendLine("<tr>");
        string sumTxt = string.Format("TOTAL: {0}  |  Allocations: {1}  |  Transfers: {2}  |  Returns: {3}",
            dt.Rows.Count, cntAlloc, cntTrans, cntRet);
        sb.AppendFormat("<td class=\"tot td\" colspan=\"{0}\"><b>{1}</b></td>", span, H(sumTxt));
        sb.AppendLine("</tr>");

        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:4px;\"></td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"ftr\">Generated: {1}</td></tr>\n",
            span, DateTime.Now.ToString("dd MMM yyyy hh:mm tt"));
        sb.AppendLine("</table></body></html>");
        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ════════════════════════════════════════════════════════════════════════
    // DATA ACCESS
    // ════════════════════════════════════════════════════════════════════════

    private DataTable RunSP(int assetID, string movementType, DateTime from, DateTime to)
    {
        var dt = new DataTable();
        using (var conn = new SqlConnection(ConnStr()))
        using (var cmd  = new SqlCommand("sp_AssetMovementReport", conn))
        {
            cmd.CommandType    = CommandType.StoredProcedure;
            cmd.CommandTimeout = 60;
            cmd.Parameters.AddWithValue("@AssetID",      assetID);
            cmd.Parameters.AddWithValue("@MovementType", movementType ?? "ALL");
            cmd.Parameters.AddWithValue("@FromDate",     from.Date);
            cmd.Parameters.AddWithValue("@ToDate",       to.Date);
            conn.Open();
            new SqlDataAdapter(cmd).Fill(dt);
        }
        return dt;
    }

    private string Lookup(string sql, string paramValue)
    {
        try
        {
            using (var conn = new SqlConnection(ConnStr()))
            using (var cmd  = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@p", paramValue);
                conn.Open();
                object r = cmd.ExecuteScalar();
                return (r != null && r != DBNull.Value) ? r.ToString() : null;
            }
        }
        catch { return null; }
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

    private static string FmtCell(object val, bool isNum)
    {
        if (val == null || val == DBNull.Value) return isNum ? "\u2014" : "";
        if (isNum) { decimal d; return decimal.TryParse(val.ToString(), out d) ? d.ToString("N2") : val.ToString(); }
        return val.ToString();
    }

    private static string H(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;");
    }

    private void SendError(string msg)
    {
        Response.Clear(); Response.ContentType = "text/html";
        Response.Write("<html><body style='font-family:Arial;padding:30px'>" +
            "<h3 style='color:#c62828'>Asset Movement / Allocation Report</h3>" +
            "<p style='color:#333'>" + Server.HtmlEncode(msg) + "</p></body></html>");
        Response.End();
    }

    private void LogErr(string src, string msg)
    {
        try
        {
            string path  = Server.MapPath("~/ReportParamLog.txt");
            string entry = string.Format("[{0}] ERROR in {1}: {2}{3}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), src, msg, Environment.NewLine);
            System.IO.File.AppendAllText(path, entry);
        }
        catch { }
    }

    private static void P(Document doc, string text, Font font, int align, float spacingAfter)
    { doc.Add(new Paragraph(text, font) { Alignment = align, SpacingAfter = spacingAfter }); }

    private static Paragraph Spacer(float h)
    { return new Paragraph(" ") { SpacingAfter = h }; }

    private class FooterEvent : PdfPageEventHelper
    {
        private readonly string               _user;
        private readonly iTextSharp.text.Font _font;
        private readonly BaseColor            _navy;

        public FooterEvent(string user, iTextSharp.text.Font font, BaseColor navy)
        { _user = user; _font = font; _navy = navy; }

        public override void OnEndPage(PdfWriter writer, Document doc)
        {
            var cb = writer.DirectContent;
            var pg = doc.PageSize;
            cb.SetLineWidth(0.5f);
            cb.SetColorStroke(new BaseColor(0xcc, 0xcc, 0xcc));
            cb.MoveTo(doc.LeftMargin, doc.BottomMargin - 5f);
            cb.LineTo(pg.Width - doc.RightMargin, doc.BottomMargin - 5f);
            cb.Stroke();
            string left = "Generated by: " + _user + "   |   " +
                          DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
            ColumnText.ShowTextAligned(cb, Element.ALIGN_LEFT,
                new Phrase(left, _font), doc.LeftMargin, doc.BottomMargin - 15f, 0);
            ColumnText.ShowTextAligned(cb, Element.ALIGN_RIGHT,
                new Phrase("Page " + writer.PageNumber, _font),
                pg.Width - doc.RightMargin, doc.BottomMargin - 15f, 0);
        }
    }

</script>
