<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Data.SqlClient" %>
<%@ Import Namespace="iTextSharp.text" %>
<%@ Import Namespace="iTextSharp.text.pdf" %>
<%@ Import Namespace="System.IO" %>
<script runat="server">

    // ════════════════════════════════════════════════════════════════════════
    // OBMS Staff / Guard Item Holding Report — PDF / Excel
    // Shows net items currently held (issued - returned) per employee.
    // Params: LoginID, IssueToType, EmployeeID
    // ════════════════════════════════════════════════════════════════════════

    protected void Page_Load(object sender, EventArgs e)
    {
        string loginID      = (Request.QueryString["LoginID"]     ?? "").Trim();
        string issueToType  = (Request.QueryString["IssueToType"] ?? "ALL").Trim();
        string empIDStr     = (Request.QueryString["EmployeeID"]  ?? "0").Trim();
        string startDateStr = (Request.QueryString["StartDate"]   ?? "").Trim();
        string endDateStr   = (Request.QueryString["EndDate"]     ?? "").Trim();
        string format       = (Request.QueryString["format"]      ?? "pdf").ToLower().Trim();

        int employeeID = 0; int.TryParse(empIDStr, out employeeID);
        if (string.IsNullOrWhiteSpace(issueToType)) issueToType = "ALL";

        // Build period label
        DateTime startDate, endDate;
        bool hasStart = DateTime.TryParse(startDateStr, out startDate);
        bool hasEnd   = DateTime.TryParse(endDateStr,   out endDate);
        string period = (hasStart && hasEnd)
            ? startDate.ToString("dd MMM yyyy") + " to " + endDate.ToString("dd MMM yyyy")
            : (hasStart ? startDate.ToString("dd MMM yyyy") + " to " + DateTime.Now.ToString("dd MMM yyyy")
                        : "01 " + DateTime.Now.ToString("MMM yyyy") + " to " + DateTime.Now.ToString("dd MMM yyyy"));

        DataTable dt;
        try   { dt = RunQuery(issueToType, employeeID); }
        catch (Exception ex)
        {
            LogErr("RunQuery", ex.Message);
            SendError("Unable to retrieve data. Please try again.");
            return;
        }

        if (dt == null || dt.Rows.Count == 0)
        { SendError("No item holding data found for the selected filters."); return; }

        string typeLabel = issueToType == "ALL" ? "All Types" : issueToType;
        string empLabel  = employeeID > 0
            ? (Lookup("SELECT EMP_NAME FROM Employee WHERE EMP_ID=@p", employeeID.ToString()) ?? empIDStr)
            : "All Employees";

        if (format == "excel")
        {
            byte[] xlsx;
            try   { xlsx = BuildExcel(typeLabel, empLabel, period, dt); }
            catch (Exception ex) { LogErr("BuildExcel", ex.Message); SendError("Unable to generate Excel."); return; }

            Response.Clear();
            Response.ContentType = "application/vnd.ms-excel";
            Response.AddHeader("Content-Disposition", "attachment; filename=StaffItemHolding_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls");
            Response.AddHeader("Content-Length", xlsx.Length.ToString());
            Response.AddHeader("Access-Control-Allow-Origin", "*");
            Response.BinaryWrite(xlsx); Response.Flush(); Response.End();
            return;
        }

        byte[] pdf;
        try   { pdf = BuildPdf(loginID, typeLabel, empLabel, period, dt); }
        catch (Exception ex) { LogErr("BuildPdf", ex.Message); SendError("Unable to generate PDF."); return; }

        Response.Clear();
        Response.ContentType = "application/pdf";
        Response.AddHeader("Content-Disposition", "inline; filename=StaffItemHolding_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
        Response.AddHeader("Content-Length", pdf.Length.ToString());
        Response.AddHeader("Access-Control-Allow-Origin", "*");
        Response.BinaryWrite(pdf); Response.Flush(); Response.End();
    }

    // ── PDF Builder ───────────────────────────────────────────────────────────
    private byte[] BuildPdf(string loginID, string typeLabel, string empLabel, string period, DataTable dt)
    {
        BaseColor navy     = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor headerBg = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor rowAlt   = new BaseColor(0xf4, 0xf7, 0xfb);
        BaseColor totalBg  = new BaseColor(0xe8, 0xf0, 0xfe);
        BaseColor border   = new BaseColor(0xcc, 0xcc, 0xcc);
        BaseColor gold     = new BaseColor(0xD4, 0xA0, 0x17);

        Font fTitle   = new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD,   navy);
        Font fFilterB = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   new BaseColor(0x44,0x44,0x44));
        Font fFilter  = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, new BaseColor(0x44,0x44,0x44));
        Font fTH      = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   BaseColor.WHITE);
        Font fTD      = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.NORMAL, BaseColor.BLACK);
        Font fTotal   = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
        Font fFoot    = new Font(Font.FontFamily.HELVETICA,  7f, Font.NORMAL, BaseColor.GRAY);

        using (var ms = new MemoryStream())
        {
            var doc    = new Document(PageSize.A4.Rotate(), 20f, 20f, 28f, 28f);
            var writer = PdfWriter.GetInstance(doc, ms);
            writer.PageEvent = new FooterEvent(loginID, fFoot, navy);
            doc.Open();

            // Company header
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
                logoCell = new PdfPCell(logo) { Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_MIDDLE, HorizontalAlignment = Element.ALIGN_LEFT, Padding = 2f };
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
            hdrTable.AddCell(new PdfPCell(textPhrase) { Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_MIDDLE, HorizontalAlignment = Element.ALIGN_LEFT, Padding = 4f, PaddingLeft = 8f });
            doc.Add(hdrTable);

            doc.Add(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(2.5f, 100f, gold, Element.ALIGN_CENTER, -2f)));
            doc.Add(Spacer(6f));
            P(doc, "STAFF / GUARD ITEM HOLDING REPORT", fTitle, Element.ALIGN_CENTER, 6f);

            // Filter summary
            var ft = new PdfPTable(3) { WidthPercentage = 100f, SpacingAfter = 8f };
            ft.DefaultCell.Border = Rectangle.NO_BORDER;
            string[][] fp = new string[][] {
                new[]{ "Issue To Type:", typeLabel },
                new[]{ "Employee:",      empLabel  },
                new[]{ "Period:",        period    },
                new[]{ "Print Date:",    DateTime.Now.ToString("dd MMM yyyy hh:mm tt") },
                new[]{ "", "" }, new[]{ "", "" }
            };
            foreach (var f in fp)
            {
                var ph = new Phrase(); ph.Add(new Chunk(f[0] + " ", fFilterB)); ph.Add(new Chunk(f[1], fFilter));
                ft.AddCell(new PdfPCell(ph) { Border = Rectangle.NO_BORDER, Padding = 2f });
            }
            doc.Add(ft);

            // Columns: S.NO | Employee | Type | Client | Item Code | Item Name | Category | UOM | Issued | Returned | Net Holding | Unit Cost | Total Value | Last Issue Date
            var cols = new object[][] {
                new object[]{ "S.NO.",        "#",             1.0f, true  },
                new object[]{ "Employee",     "EmployeeName",  2.8f, false },
                new object[]{ "Type",         "IssueToType",   1.5f, false },
                new object[]{ "Client",       "ClientName",    2.2f, false },
                new object[]{ "Item Code",    "ItemCode",      1.5f, false },
                new object[]{ "Item Name",    "ItemName",      3.0f, false },
                new object[]{ "Category",     "CategoryName",  1.8f, false },
                new object[]{ "UOM",          "UOMName",       0.9f, false },
                new object[]{ "Issued",       "TotalIssued",   1.3f, true  },
                new object[]{ "Returned",     "TotalReturned", 1.3f, true  },
                new object[]{ "Net Holding",  "NetHolding",    1.4f, true  },
                new object[]{ "Unit Cost",    "UnitCost",      1.6f, true  },
                new object[]{ "Total Value",  "TotalValue",    1.8f, true  },
                new object[]{ "Last Issue",   "LastIssueDate", 1.6f, false },
            };

            float[] widths = new float[cols.Length];
            for (int i = 0; i < cols.Length; i++) widths[i] = (float)cols[i][2];

            var tbl = new PdfPTable(cols.Length) { WidthPercentage = 100f, HeaderRows = 1 };
            tbl.SetWidths(widths);

            foreach (var c in cols)
            {
                bool isNum = (bool)c[3];
                tbl.AddCell(new PdfPCell(new Phrase((string)c[0], fTH)) {
                    BackgroundColor = headerBg,
                    HorizontalAlignment = isNum ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT,
                    Padding = 4f, Border = Rectangle.BOX, BorderColor = border
                });
            }

            int ri = 0;
            decimal totIssued = 0, totReturned = 0, totNet = 0, totValue = 0;

            foreach (DataRow dr in dt.Rows)
            {
                ri++;
                BaseColor rowBg = (ri % 2 == 0) ? rowAlt : BaseColor.WHITE;
                foreach (var c in cols)
                {
                    string field = (string)c[1]; bool isNum = (bool)c[3];
                    string txt;
                    if (field == "#") { txt = ri.ToString(); }
                    else if (field == "LastIssueDate") {
                        object raw = dt.Columns.Contains(field) ? dr[field] : null;
                        txt = (raw != null && raw != DBNull.Value) ? Convert.ToDateTime(raw).ToString("dd MMM yyyy") : "";
                    }
                    else {
                        object raw = dt.Columns.Contains(field) ? dr[field] : null;
                        txt = FmtCell(raw, isNum);
                        if (raw != null && raw != DBNull.Value) {
                            decimal v;
                            if (decimal.TryParse(raw.ToString(), out v)) {
                                if (field == "TotalIssued")   totIssued   += v;
                                if (field == "TotalReturned") totReturned += v;
                                if (field == "NetHolding")    totNet      += v;
                                if (field == "TotalValue")    totValue    += v;
                            }
                        }
                    }
                    tbl.AddCell(new PdfPCell(new Phrase(txt, fTD)) {
                        BackgroundColor = rowBg,
                        HorizontalAlignment = (isNum || field == "#") ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT,
                        Padding = 3.5f, Border = Rectangle.BOX, BorderColor = border
                    });
                }
            }

            // Totals row
            tbl.AddCell(new PdfPCell(new Phrase("TOTALS", fTotal)) {
                Colspan = 8, BackgroundColor = totalBg,
                HorizontalAlignment = Element.ALIGN_LEFT,
                Padding = 4f, Border = Rectangle.BOX, BorderColor = navy, BorderWidth = 1.5f
            });
            string[] totVals = new string[] { totIssued.ToString("N2"), totReturned.ToString("N2"), totNet.ToString("N2"), "", totValue.ToString("N2"), "" };
            foreach (var tv in totVals) {
                tbl.AddCell(new PdfPCell(new Phrase(tv, fTotal)) {
                    BackgroundColor = totalBg,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Padding = 4f, Border = Rectangle.BOX, BorderColor = navy, BorderWidth = 1.5f
                });
            }

            doc.Add(tbl);
            doc.Close();
            return ms.ToArray();
        }
    }

    // ── Excel Builder ─────────────────────────────────────────────────────────
    private byte[] BuildExcel(string typeLabel, string empLabel, string period, DataTable dt)
    {
        string[] headers = { "S.NO.", "Employee", "Type", "Client", "Item Code", "Item Name", "Category", "UOM",
                             "Issued Qty", "Returned Qty", "Net Holding", "Unit Cost", "Total Value (RM)", "Last Issue Date" };
        int span = headers.Length;
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\">");
        sb.AppendLine("<head><meta charset=\"UTF-8\"/><style>");
        sb.AppendLine("body,td,th{font-family:Arial;font-size:10pt;} .co{font-size:13pt;font-weight:bold;color:#1a3a5c;} .sub{font-size:9pt;color:#555;}");
        sb.AppendLine(".ttl{font-size:12pt;font-weight:bold;color:#1a3a5c;text-align:center;} .fl{font-weight:bold;font-size:9pt;} .fv{font-size:9pt;}");
        sb.AppendLine(".th{background:#1a3a5c;color:#fff;font-weight:bold;padding:5px;border:1px solid #ccc;}");
        sb.AppendLine(".td{padding:4px;border:1px solid #ddd;} .tn{padding:4px;border:1px solid #ddd;text-align:right;}");
        sb.AppendLine(".alt{background:#f4f7fb;} .tot{background:#e8f0fe;font-weight:bold;border-top:2px solid #1a3a5c;} .gold{border-bottom:2px solid #D4A017;}");
        sb.AppendLine("</style></head><body><table style=\"width:100%;border-collapse:collapse;\">");

        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"co\">{1} <span style=\"font-size:9pt\">{2}</span></td></tr>\n", span, H(AppSettings("CompanyName")), H(AppSettings("Registration")));
        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"sub\">{1} {2}, {3} {4}</td></tr>\n", span, H(AppSettings("Address1")), H(AppSettings("Address2")), H(AppSettings("PostCode")), H(AppSettings("City")));
        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"gold\" style=\"padding:2px;\"></td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"ttl\">STAFF / GUARD ITEM HOLDING REPORT</td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:2px;\"></td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\"><span class=\"fl\">Type:</span> <span class=\"fv\">{1}</span>&nbsp;&nbsp;&nbsp;<span class=\"fl\">Employee:</span> <span class=\"fv\">{2}</span>&nbsp;&nbsp;&nbsp;<span class=\"fl\">Period:</span> <span class=\"fv\">{3}</span></td></tr>\n", span, H(typeLabel), H(empLabel), H(period));
        sb.AppendFormat("<tr><td colspan=\"{0}\"><span class=\"fl\">Print Date:</span> <span class=\"fv\">{1}</span></td></tr>\n", span, DateTime.Now.ToString("dd MMM yyyy hh:mm tt"));
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:3px;\"></td></tr>\n", span);

        sb.AppendLine("<tr>"); foreach (var h in headers) sb.AppendFormat("<td class=\"th\">{0}</td>", H(h)); sb.AppendLine("</tr>");

        decimal totIssued = 0, totReturned = 0, totNet = 0, totValue = 0;
        int ri = 0;
        foreach (DataRow dr in dt.Rows)
        {
            ri++;
            string alt  = ri % 2 == 1 ? "" : " alt";
            string ldStr = dt.Columns.Contains("LastIssueDate") && dr["LastIssueDate"] != DBNull.Value ? Convert.ToDateTime(dr["LastIssueDate"]).ToString("dd MMM yyyy") : "";
            decimal ti = GetDec(dr, "TotalIssued"); decimal tr2 = GetDec(dr, "TotalReturned");
            decimal nh = GetDec(dr, "NetHolding");  decimal tv  = GetDec(dr, "TotalValue");
            totIssued += ti; totReturned += tr2; totNet += nh; totValue += tv;

            sb.AppendLine("<tr>");
            sb.AppendFormat("<td class=\"tn{0}\">{1}</td>", alt, ri);
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(GetStr(dr, "EmployeeName")));
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(GetStr(dr, "IssueToType")));
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(GetStr(dr, "ClientName")));
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(GetStr(dr, "ItemCode")));
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(GetStr(dr, "ItemName")));
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(GetStr(dr, "CategoryName")));
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(GetStr(dr, "UOMName")));
            sb.AppendFormat("<td class=\"tn{0}\">{1}</td>", alt, ti.ToString("N2"));
            sb.AppendFormat("<td class=\"tn{0}\">{1}</td>", alt, tr2.ToString("N2"));
            sb.AppendFormat("<td class=\"tn{0}\">{1}</td>", alt, nh.ToString("N2"));
            sb.AppendFormat("<td class=\"tn{0}\">{1}</td>", alt, GetDec(dr,"UnitCost").ToString("N2"));
            sb.AppendFormat("<td class=\"tn{0}\">{1}</td>", alt, tv.ToString("N2"));
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(ldStr));
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("<tr>");
        sb.AppendFormat("<td class=\"tot td\" colspan=\"8\"><b>TOTALS</b></td>");
        sb.AppendFormat("<td class=\"tot tn\">{0}</td>", totIssued.ToString("N2"));
        sb.AppendFormat("<td class=\"tot tn\">{0}</td>", totReturned.ToString("N2"));
        sb.AppendFormat("<td class=\"tot tn\">{0}</td>", totNet.ToString("N2"));
        sb.AppendFormat("<td class=\"tot td\"></td>");
        sb.AppendFormat("<td class=\"tot tn\">{0}</td>", totValue.ToString("N2"));
        sb.AppendFormat("<td class=\"tot td\"></td>");
        sb.AppendLine("</tr>");

        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:4px;\"></td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"font-size:8pt;color:#888;\">Generated: {1}</td></tr>\n", span, DateTime.Now.ToString("dd MMM yyyy hh:mm tt"));
        sb.AppendLine("</table></body></html>");
        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ── Data Access ───────────────────────────────────────────────────────────
    private DataTable RunQuery(string issueToType, int employeeID)
    {
        // Check if SP exists; fall back to inline SQL if not yet migrated
        bool spExists = false;
        using (var conn0 = new SqlConnection(ConnStr()))
        {
            conn0.Open();
            using (var chk = new SqlCommand(
                "SELECT 1 FROM sys.objects WHERE type='P' AND name='sp_StaffItemHoldingReport'", conn0))
            {
                spExists = (chk.ExecuteScalar() != null);
            }
        }

        var dt = new DataTable();
        using (var conn = new SqlConnection(ConnStr()))
        {
            conn.Open();
            if (spExists)
            {
                using (var cmd = new SqlCommand("sp_StaffItemHoldingReport", conn))
                {
                    cmd.CommandType    = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 60;
                    cmd.Parameters.AddWithValue("@IssueToType", string.IsNullOrWhiteSpace(issueToType) ? "ALL" : issueToType);
                    cmd.Parameters.AddWithValue("@EmployeeID",  employeeID);
                    new SqlDataAdapter(cmd).Fill(dt);
                }
            }
            else
            {
                // Inline fallback (used until Migration 65 is run on this server)
                var sb = new System.Text.StringBuilder(@"
                    SELECT
                        si.IssueToName                                             AS EmployeeName,
                        si.IssueToID                                               AS EmployeeID,
                        si.IssueToType,
                        ISNULL(cl.Name, '')                                        AS ClientName,
                        ISNULL(im.Code, '')                                        AS ItemCode,
                        im.Name                                                    AS ItemName,
                        ISNULL(ic.Name, '')                                        AS CategoryName,
                        ISNULL(um.ShortName, '')                                   AS UOMName,
                        SUM(sd.IssueQty)                                           AS TotalIssued,
                        ISNULL((
                            SELECT SUM(rd.ReturnQty)
                            FROM StockIssueReturnDetail rd
                            INNER JOIN StockIssueDocDetail sd2 ON sd2.ID  = rd.OriginalIssueDetailID
                            INNER JOIN StockIssueDoc       si2 ON si2.ID  = sd2.IssueDocID
                            WHERE si2.IssueToID = si.IssueToID
                              AND sd2.ItemID    = sd.ItemID
                              AND ISNULL(sd2.ClientID,0) = ISNULL(sd.ClientID,0)
                              AND si2.IsActive  = 1
                        ), 0)                                                      AS TotalReturned,
                        SUM(sd.IssueQty) - ISNULL((
                            SELECT SUM(rd.ReturnQty)
                            FROM StockIssueReturnDetail rd
                            INNER JOIN StockIssueDocDetail sd2 ON sd2.ID  = rd.OriginalIssueDetailID
                            INNER JOIN StockIssueDoc       si2 ON si2.ID  = sd2.IssueDocID
                            WHERE si2.IssueToID = si.IssueToID
                              AND sd2.ItemID    = sd.ItemID
                              AND ISNULL(sd2.ClientID,0) = ISNULL(sd.ClientID,0)
                              AND si2.IsActive  = 1
                        ), 0)                                                      AS NetHolding,
                        MAX(ISNULL(sd.UnitCost, 0))                                AS UnitCost,
                        (SUM(sd.IssueQty) - ISNULL((
                            SELECT SUM(rd.ReturnQty)
                            FROM StockIssueReturnDetail rd
                            INNER JOIN StockIssueDocDetail sd2 ON sd2.ID  = rd.OriginalIssueDetailID
                            INNER JOIN StockIssueDoc       si2 ON si2.ID  = sd2.IssueDocID
                            WHERE si2.IssueToID = si.IssueToID
                              AND sd2.ItemID    = sd.ItemID
                              AND ISNULL(sd2.ClientID,0) = ISNULL(sd.ClientID,0)
                              AND si2.IsActive  = 1
                        ), 0)) * MAX(ISNULL(sd.UnitCost,0))                        AS TotalValue,
                        MAX(si.IssueDate)                                          AS LastIssueDate
                    FROM  StockIssueDoc        si
                    INNER JOIN StockIssueDocDetail sd ON sd.IssueDocID = si.ID
                    INNER JOIN ItemMaster           im ON im.ID = sd.ItemID
                    LEFT  JOIN InventoryCategory    ic ON ic.ID = im.CategoryID
                    LEFT  JOIN UOMMaster            um ON um.ID = im.UOMID
                    LEFT  JOIN ClientMaster         cl ON cl.ID = sd.ClientID
                    WHERE si.IsActive = 1
                      AND si.IssueToType IN ('Staff','Guard','Other Guards')
                ");
                if (issueToType != "ALL") sb.Append(" AND si.IssueToType = @IssueToType ");
                if (employeeID > 0)       sb.Append(" AND si.IssueToID   = @EmployeeID ");
                sb.Append(@"
                    GROUP BY si.IssueToName, si.IssueToID, si.IssueToType,
                             cl.Name, im.Code, im.Name, ic.Name, um.ShortName, sd.ItemID, sd.ClientID
                    HAVING (SUM(sd.IssueQty) - ISNULL((
                        SELECT SUM(rd.ReturnQty)
                        FROM StockIssueReturnDetail rd
                        INNER JOIN StockIssueDocDetail sd2 ON sd2.ID  = rd.OriginalIssueDetailID
                        INNER JOIN StockIssueDoc       si2 ON si2.ID  = sd2.IssueDocID
                        WHERE si2.IssueToID = si.IssueToID
                          AND sd2.ItemID    = sd.ItemID
                          AND ISNULL(sd2.ClientID,0) = ISNULL(sd.ClientID,0)
                          AND si2.IsActive  = 1
                    ), 0)) > 0
                    ORDER BY si.IssueToName, im.Name
                ");

                using (var cmd = new SqlCommand(sb.ToString(), conn))
                {
                    cmd.CommandTimeout = 60;
                    if (issueToType != "ALL") cmd.Parameters.AddWithValue("@IssueToType", issueToType);
                    if (employeeID > 0)       cmd.Parameters.AddWithValue("@EmployeeID",  employeeID);
                    new SqlDataAdapter(cmd).Fill(dt);
                }
            }
        }
        return dt;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static string ConnStr()
    {
        return string.Format("Server={0};Database={1};User Id={2};Password={3};",
            ConfigurationManager.AppSettings["Server"]   ?? ".",
            ConfigurationManager.AppSettings["Database"] ?? "",
            ConfigurationManager.AppSettings["UserID"]   ?? "",
            ConfigurationManager.AppSettings["Password"] ?? "");
    }
    private static string AppSettings(string key) { return ConfigurationManager.AppSettings[key] ?? ""; }
    private string Lookup(string sql, string paramValue) {
        try {
            using (var conn = new SqlConnection(ConnStr()))
            using (var cmd  = new SqlCommand(sql, conn))
            { cmd.Parameters.AddWithValue("@p", paramValue); conn.Open(); object r = cmd.ExecuteScalar(); return (r != null && r != DBNull.Value) ? r.ToString() : null; }
        } catch { return null; }
    }
    private static string FmtCell(object val, bool isNum) {
        if (val == null || val == DBNull.Value) return isNum ? "-" : "";
        if (isNum) { decimal d; return decimal.TryParse(val.ToString(), out d) ? d.ToString("N2") : val.ToString(); }
        return val.ToString();
    }
    private static decimal GetDec(DataRow dr, string col) { try { return dr.Table.Columns.Contains(col) && dr[col] != DBNull.Value ? Convert.ToDecimal(dr[col]) : 0; } catch { return 0; } }
    private static string  GetStr(DataRow dr, string col) { try { return dr.Table.Columns.Contains(col) && dr[col] != DBNull.Value ? dr[col].ToString() : ""; } catch { return ""; } }
    private static string  H(string s) { return System.Web.HttpUtility.HtmlEncode(s ?? ""); }
    private static iTextSharp.text.Rectangle Spacer(float h) { var r = new iTextSharp.text.Rectangle(0, 0); r.Border = Rectangle.NO_BORDER; return r; }
    private static void P(Document doc, string text, Font f, int align, float spaceAfter) {
        var p = new Paragraph(text, f) { Alignment = align, SpacingAfter = spaceAfter }; doc.Add(p);
    }
    private void SendError(string msg) {
        Response.Clear(); Response.ContentType = "text/html";
        Response.Write("<html><body style='font-family:Arial;padding:30px'><h3 style='color:#c62828'>Report Error</h3><p>" + Server.HtmlEncode(msg) + "</p></body></html>"); Response.End();
    }
    private void LogErr(string src, string msg) {
        try { System.IO.File.AppendAllText(Server.MapPath("~/ReportParamLog.txt"), string.Format("[{0}] ERROR in {1}: {2}\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), src, msg)); } catch { }
    }

    public class FooterEvent : PdfPageEventHelper {
        private string _user; private Font _font; private BaseColor _navy;
        public FooterEvent(string user, Font font, BaseColor navy) { _user = user; _font = font; _navy = navy; }
        public override void OnEndPage(PdfWriter writer, Document doc) {
            var cb = writer.DirectContent;
            cb.SetLineWidth(0.5f); cb.SetColorStroke(_navy);
            cb.MoveTo(doc.Left, doc.Bottom - 5f); cb.LineTo(doc.Right, doc.Bottom - 5f); cb.Stroke();
            var tbl = new PdfPTable(3) { TotalWidth = doc.Right - doc.Left };
            tbl.SetWidths(new float[] { 3f, 4f, 3f });
            Action<string, int> addCell = (txt, align) => tbl.AddCell(new PdfPCell(new Phrase(txt, _font)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = align, Padding = 2f });
            addCell("Printed by: " + (_user ?? ""), Element.ALIGN_LEFT);
            addCell("OBMS - Staff / Guard Item Holding", Element.ALIGN_CENTER);
            addCell("Page " + writer.PageNumber, Element.ALIGN_RIGHT);
            tbl.WriteSelectedRows(0, -1, doc.Left, doc.Bottom - 7f, cb);
        }
    }

</script>
