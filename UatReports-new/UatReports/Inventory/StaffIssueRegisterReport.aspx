<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Data.SqlClient" %>
<%@ Import Namespace="iTextSharp.text" %>
<%@ Import Namespace="iTextSharp.text.pdf" %>
<%@ Import Namespace="System.IO" %>
<script runat="server">

    // ════════════════════════════════════════════════════════════════════════
    // OBMS Staff / Guard Issue Register Report — PDF / Excel
    // Params: LoginID, StartDate, EndDate, IssueToType, EmployeeID, IsDeduction
    // ════════════════════════════════════════════════════════════════════════

    protected void Page_Load(object sender, EventArgs e)
    {
        string loginID       = (Request.QueryString["LoginID"]      ?? "").Trim();
        string startDateStr  = (Request.QueryString["StartDate"]    ?? "").Trim();
        string endDateStr    = (Request.QueryString["EndDate"]      ?? "").Trim();
        string issueToType   = (Request.QueryString["IssueToType"]  ?? "ALL").Trim();
        string empIDStr      = (Request.QueryString["EmployeeID"]   ?? "0").Trim();
        string isDeduction   = (Request.QueryString["IsDeduction"]  ?? "ALL").Trim();
        string format        = (Request.QueryString["format"]       ?? "pdf").ToLower().Trim();

        if (string.IsNullOrWhiteSpace(startDateStr) || string.IsNullOrWhiteSpace(endDateStr))
        { SendError("Please select a valid date range."); return; }

        DateTime startDate, endDate;
        if (!DateTime.TryParse(startDateStr, out startDate) ||
            !DateTime.TryParse(endDateStr,   out endDate))
        { SendError("Please select a valid date range."); return; }

        if (startDate.Date > endDate.Date)
        { SendError("Start date must be before end date."); return; }

        int employeeID = 0; int.TryParse(empIDStr, out employeeID);
        if (string.IsNullOrWhiteSpace(issueToType)) issueToType = "ALL";

        DataTable dt;
        try   { dt = RunQuery(startDate, endDate, issueToType, employeeID, isDeduction); }
        catch (Exception ex)
        {
            LogErr("RunQuery", ex.Message);
            SendError("Unable to retrieve data: " + ex.Message);
            return;
        }

        if (dt == null || dt.Rows.Count == 0)
        { SendError("No issue records found for the selected filters."); return; }

        string typeLabel   = issueToType == "ALL" ? "All Types" : issueToType;
        string dedLabel    = isDeduction == "true" ? "Deduction" : isDeduction == "false" ? "Non-Deduction" : "All";
        string period      = startDate.ToString("dd MMM yyyy") + " to " + endDate.ToString("dd MMM yyyy");
        string empLabel    = employeeID > 0
            ? (Lookup("SELECT EMP_NAME FROM Employee WHERE EMP_ID=@p", employeeID.ToString()) ?? empIDStr)
            : "All Employees";

        if (format == "excel")
        {
            byte[] xlsx;
            try   { xlsx = BuildExcel(typeLabel, dedLabel, empLabel, period, dt); }
            catch (Exception ex) { LogErr("BuildExcel", ex.Message); SendError("Unable to generate Excel."); return; }

            Response.Clear();
            Response.ContentType = "application/vnd.ms-excel";
            Response.AddHeader("Content-Disposition", "attachment; filename=StaffIssueRegister_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls");
            Response.AddHeader("Content-Length", xlsx.Length.ToString());
            Response.AddHeader("Access-Control-Allow-Origin", "*");
            Response.BinaryWrite(xlsx); Response.Flush(); Response.End();
            return;
        }

        byte[] pdf;
        try   { pdf = BuildPdf(loginID, typeLabel, dedLabel, empLabel, period, dt); }
        catch (Exception ex) { LogErr("BuildPdf", ex.Message); SendError("Unable to generate PDF."); return; }

        Response.Clear();
        Response.ContentType = "application/pdf";
        Response.AddHeader("Content-Disposition", "inline; filename=StaffIssueRegister_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
        Response.AddHeader("Content-Length", pdf.Length.ToString());
        Response.AddHeader("Access-Control-Allow-Origin", "*");
        Response.BinaryWrite(pdf); Response.Flush(); Response.End();
    }

    // ── PDF Builder ──────────────────────────────────────────────────────────
    private byte[] BuildPdf(string loginID, string typeLabel, string dedLabel,
                            string empLabel, string period, DataTable dt)
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
            P(doc, "STAFF / GUARD ISSUE REGISTER", fTitle, Element.ALIGN_CENTER, 6f);

            // Filter summary
            var ft = new PdfPTable(4) { WidthPercentage = 100f, SpacingAfter = 8f };
            ft.DefaultCell.Border = Rectangle.NO_BORDER;
            string[][] fp = new string[][] {
                new[]{ "Issue To Type:", typeLabel },
                new[]{ "Deduction:",     dedLabel  },
                new[]{ "Employee:",      empLabel  },
                new[]{ "Period:",        period    },
                new[]{ "Print Date:",    DateTime.Now.ToString("dd MMM yyyy hh:mm tt") },
                new[]{ "", "" }
            };
            foreach (var f in fp)
            {
                var ph = new Phrase(); ph.Add(new Chunk(f[0] + " ", fFilterB)); ph.Add(new Chunk(f[1], fFilter));
                ft.AddCell(new PdfPCell(ph) { Border = Rectangle.NO_BORDER, Padding = 2f });
            }
            doc.Add(ft);

            // Table columns: S.NO | Issue No | Date | Warehouse | Type | Employee | Client | Deduction | Item Code | Item Name | UOM | Issued | Returned | Net | Unit Cost | Total
            var cols = new object[][] {
                new object[]{ "S.NO.",       "#",             1.0f, true  },
                new object[]{ "Issue No",    "IssueNo",       2.0f, false },
                new object[]{ "Date",        "IssueDate",     1.6f, false },
                new object[]{ "Warehouse",   "WarehouseName", 2.2f, false },
                new object[]{ "Type",        "IssueToType",   1.5f, false },
                new object[]{ "Employee",    "EmployeeName",  2.8f, false },
                new object[]{ "Client",      "ClientName",    2.2f, false },
                new object[]{ "Deduction",   "IsDeduction",   1.4f, false },
                new object[]{ "Item Code",   "ItemCode",      1.5f, false },
                new object[]{ "Item Name",   "ItemName",      3.0f, false },
                new object[]{ "UOM",         "UOMName",       0.9f, false },
                new object[]{ "Issued",      "IssueQty",      1.3f, true  },
                new object[]{ "Returned",    "ReturnedQty",   1.3f, true  },
                new object[]{ "Net",         "NetQty",        1.2f, true  },
                new object[]{ "Unit Cost",   "UnitCost",      1.6f, true  },
                new object[]{ "Total (RM)",  "TotalValue",    1.8f, true  },
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
                    else if (field == "IsDeduction") {
                        object raw = dt.Columns.Contains(field) ? dr[field] : null;
                        bool ded = raw != null && raw != DBNull.Value && Convert.ToBoolean(raw);
                        txt = ded ? "Deduction" : "Non-Deduction";
                    }
                    else if (field == "IssueDate") {
                        object raw = dt.Columns.Contains(field) ? dr[field] : null;
                        txt = (raw != null && raw != DBNull.Value) ? Convert.ToDateTime(raw).ToString("dd MMM yyyy") : "";
                    }
                    else {
                        object raw = dt.Columns.Contains(field) ? dr[field] : null;
                        txt = FmtCell(raw, isNum);
                        if (raw != null && raw != DBNull.Value) {
                            decimal v;
                            if (decimal.TryParse(raw.ToString(), out v)) {
                                if (field == "IssueQty")    totIssued   += v;
                                if (field == "ReturnedQty") totReturned += v;
                                if (field == "NetQty")      totNet      += v;
                                if (field == "TotalValue")  totValue    += v;
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
                Colspan = 11, BackgroundColor = totalBg,
                HorizontalAlignment = Element.ALIGN_LEFT,
                Padding = 4f, Border = Rectangle.BOX, BorderColor = navy, BorderWidth = 1.5f
            });
            string[] totVals = new string[] { totIssued.ToString("N2"), totReturned.ToString("N2"), totNet.ToString("N2"), "", totValue.ToString("N2") };
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
    private byte[] BuildExcel(string typeLabel, string dedLabel, string empLabel, string period, DataTable dt)
    {
        string[] headers = { "S.NO.", "Issue No", "Date", "Warehouse", "Type", "Employee", "Client", "Deduction",
                             "Item Code", "Item Name", "UOM", "Issued Qty", "Returned Qty", "Net Qty", "Unit Cost", "Total (RM)" };
        int span = headers.Length;
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\">");
        sb.AppendLine("<head><meta charset=\"UTF-8\"/><style>");
        sb.AppendLine("body,td,th{font-family:Arial;font-size:10pt;}");
        sb.AppendLine(".co{font-size:13pt;font-weight:bold;color:#1a3a5c;} .sub{font-size:9pt;color:#555;}");
        sb.AppendLine(".ttl{font-size:12pt;font-weight:bold;color:#1a3a5c;text-align:center;}");
        sb.AppendLine(".fl{font-weight:bold;font-size:9pt;} .fv{font-size:9pt;}");
        sb.AppendLine(".th{background:#1a3a5c;color:#fff;font-weight:bold;padding:5px;border:1px solid #ccc;}");
        sb.AppendLine(".td{padding:4px;border:1px solid #ddd;} .tn{padding:4px;border:1px solid #ddd;text-align:right;}");
        sb.AppendLine(".alt{background:#f4f7fb;} .tot{background:#e8f0fe;font-weight:bold;border-top:2px solid #1a3a5c;}");
        sb.AppendLine(".gold{border-bottom:2px solid #D4A017;}");
        sb.AppendLine("</style></head><body><table style=\"width:100%;border-collapse:collapse;\">");

        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"co\">{1} <span style=\"font-size:9pt\">{2}</span></td></tr>\n", span, H(AppSettings("CompanyName")), H(AppSettings("Registration")));
        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"sub\">{1} {2}, {3} {4}</td></tr>\n", span, H(AppSettings("Address1")), H(AppSettings("Address2")), H(AppSettings("PostCode")), H(AppSettings("City")));
        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"gold\" style=\"padding:2px;\"></td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"ttl\">STAFF / GUARD ISSUE REGISTER</td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:2px;\"></td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\"><span class=\"fl\">Type:</span> <span class=\"fv\">{1}</span>&nbsp;&nbsp;&nbsp;<span class=\"fl\">Deduction:</span> <span class=\"fv\">{2}</span>&nbsp;&nbsp;&nbsp;<span class=\"fl\">Employee:</span> <span class=\"fv\">{3}</span>&nbsp;&nbsp;&nbsp;<span class=\"fl\">Period:</span> <span class=\"fv\">{4}</span></td></tr>\n", span, H(typeLabel), H(dedLabel), H(empLabel), H(period));
        sb.AppendFormat("<tr><td colspan=\"{0}\"><span class=\"fl\">Print Date:</span> <span class=\"fv\">{1}</span></td></tr>\n", span, DateTime.Now.ToString("dd MMM yyyy hh:mm tt"));
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:3px;\"></td></tr>\n", span);

        sb.AppendLine("<tr>"); foreach (var h in headers) sb.AppendFormat("<td class=\"th\">{0}</td>", H(h)); sb.AppendLine("</tr>");

        decimal totIssued = 0, totReturned = 0, totNet = 0, totValue = 0;
        int ri = 0;
        foreach (DataRow dr in dt.Rows)
        {
            ri++;
            string alt = ri % 2 == 1 ? "" : " alt";
            bool ded = dt.Columns.Contains("IsDeduction") && dr["IsDeduction"] != DBNull.Value && Convert.ToBoolean(dr["IsDeduction"]);
            string dateStr = dt.Columns.Contains("IssueDate") && dr["IssueDate"] != DBNull.Value ? Convert.ToDateTime(dr["IssueDate"]).ToString("dd MMM yyyy") : "";
            decimal iq = GetDec(dr, "IssueQty"); decimal rq = GetDec(dr, "ReturnedQty"); decimal nq = GetDec(dr, "NetQty"); decimal tv = GetDec(dr, "TotalValue");
            totIssued += iq; totReturned += rq; totNet += nq; totValue += tv;

            sb.AppendLine("<tr>");
            sb.AppendFormat("<td class=\"tn{0}\">{1}</td>", alt, ri);
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(GetStr(dr, "IssueNo")));
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(dateStr));
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(GetStr(dr, "WarehouseName")));
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(GetStr(dr, "IssueToType")));
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(GetStr(dr, "EmployeeName")));
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(GetStr(dr, "ClientName")));
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(ded ? "Deduction" : "Non-Deduction"));
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(GetStr(dr, "ItemCode")));
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(GetStr(dr, "ItemName")));
            sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(GetStr(dr, "UOMName")));
            sb.AppendFormat("<td class=\"tn{0}\">{1}</td>", alt, iq.ToString("N2"));
            sb.AppendFormat("<td class=\"tn{0}\">{1}</td>", alt, rq.ToString("N2"));
            sb.AppendFormat("<td class=\"tn{0}\">{1}</td>", alt, nq.ToString("N2"));
            sb.AppendFormat("<td class=\"tn{0}\">{1}</td>", alt, GetDec(dr,"UnitCost").ToString("N2"));
            sb.AppendFormat("<td class=\"tn{0}\">{1}</td>", alt, tv.ToString("N2"));
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("<tr>");
        sb.AppendFormat("<td class=\"tot td\" colspan=\"11\"><b>TOTALS</b></td>");
        sb.AppendFormat("<td class=\"tot tn\">{0}</td>", totIssued.ToString("N2"));
        sb.AppendFormat("<td class=\"tot tn\">{0}</td>", totReturned.ToString("N2"));
        sb.AppendFormat("<td class=\"tot tn\">{0}</td>", totNet.ToString("N2"));
        sb.AppendFormat("<td class=\"tot td\"></td>");
        sb.AppendFormat("<td class=\"tot tn\">{0}</td>", totValue.ToString("N2"));
        sb.AppendLine("</tr>");

        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:4px;\"></td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"font-size:8pt;color:#888;\">Generated: {1}</td></tr>\n", span, DateTime.Now.ToString("dd MMM yyyy hh:mm tt"));
        sb.AppendLine("</table></body></html>");
        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ── Data Access ───────────────────────────────────────────────────────────
    private DataTable RunQuery(DateTime from, DateTime to, string issueToType, int employeeID, string isDeduction)
    {
        // IsDeduction: 2=ALL, 1=Deduction only, 0=Non-deduction only
        byte isDeductionFlag = isDeduction == "true" ? (byte)1 : isDeduction == "false" ? (byte)0 : (byte)2;

        // Check if SP exists; fall back to inline SQL if not yet migrated
        bool spExists = false;
        using (var conn0 = new SqlConnection(ConnStr()))
        {
            conn0.Open();
            using (var chk = new SqlCommand(
                "SELECT 1 FROM sys.objects WHERE type='P' AND name='sp_StaffIssueRegisterReport'", conn0))
            {
                var r = chk.ExecuteScalar();
                spExists = (r != null);
            }
        }

        var dt = new DataTable();
        using (var conn = new SqlConnection(ConnStr()))
        {
            conn.Open();
            if (spExists)
            {
                using (var cmd = new SqlCommand("sp_StaffIssueRegisterReport", conn))
                {
                    cmd.CommandType    = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 60;
                    cmd.Parameters.AddWithValue("@FromDate",    from.Date);
                    cmd.Parameters.AddWithValue("@ToDate",      to.Date);
                    cmd.Parameters.AddWithValue("@IssueToType", string.IsNullOrWhiteSpace(issueToType) ? "ALL" : issueToType);
                    cmd.Parameters.AddWithValue("@EmployeeID",  employeeID);
                    cmd.Parameters.AddWithValue("@IsDeduction", isDeductionFlag);
                    new SqlDataAdapter(cmd).Fill(dt);
                }
            }
            else
            {
                // Inline fallback (used until Migration 64 is run on this server)
                var sb = new System.Text.StringBuilder(@"
                    SELECT
                        si.IssueNo,
                        si.IssueDate,
                        wh.Name                                    AS WarehouseName,
                        si.IssueToType,
                        si.IssueToName                             AS EmployeeName,
                        si.IssueToID                               AS EmployeeID,
                        ISNULL(cl.Name, '')                        AS ClientName,
                        si.IsDeduction,
                        im.Code                                    AS ItemCode,
                        im.Name                                    AS ItemName,
                        ISNULL(ic.Name, '')                        AS CategoryName,
                        ISNULL(um.ShortName, '')                   AS UOMName,
                        sd.IssueQty,
                        ISNULL(sd.UnitCost, 0)                     AS UnitCost,
                        sd.IssueQty * ISNULL(sd.UnitCost, 0)       AS TotalValue,
                        ISNULL((SELECT SUM(rd.ReturnQty) FROM StockIssueReturnDetail rd WHERE rd.OriginalIssueDetailID = sd.ID), 0) AS ReturnedQty,
                        sd.IssueQty - ISNULL((SELECT SUM(rd.ReturnQty) FROM StockIssueReturnDetail rd WHERE rd.OriginalIssueDetailID = sd.ID), 0) AS NetQty,
                        ISNULL(sd.Remarks, '')                     AS Remarks,
                        ISNULL(si.LastUpdatedBy, '')               AS IssuedBy
                    FROM  StockIssueDoc        si
                    INNER JOIN StockIssueDocDetail sd ON sd.IssueDocID  = si.ID
                    INNER JOIN WarehouseMaster      wh ON wh.ID           = si.WarehouseID
                    INNER JOIN ItemMaster           im ON im.ID           = sd.ItemID
                    LEFT  JOIN InventoryCategory    ic ON ic.ID           = im.CategoryID
                    LEFT  JOIN UOMMaster            um ON um.ID           = im.UOMID
                    LEFT  JOIN ClientMaster         cl ON cl.ID           = sd.ClientID
                    WHERE si.IsActive = 1
                      AND si.IssueToType IN ('Staff','Guard','Other Guards')
                      AND CAST(si.IssueDate AS DATE) BETWEEN @FromDate AND @ToDate
                ");
                if (issueToType != "ALL") sb.Append(" AND si.IssueToType = @IssueToType ");
                if (employeeID > 0)       sb.Append(" AND si.IssueToID   = @EmployeeID ");
                if (isDeductionFlag == 1) sb.Append(" AND si.IsDeduction = 1 ");
                if (isDeductionFlag == 0) sb.Append(" AND si.IsDeduction = 0 ");
                sb.Append(" ORDER BY si.IssueToName, si.IssueDate, im.Name ");

                using (var cmd = new SqlCommand(sb.ToString(), conn))
                {
                    cmd.CommandTimeout = 60;
                    cmd.Parameters.AddWithValue("@FromDate", from.Date);
                    cmd.Parameters.AddWithValue("@ToDate",   to.Date);
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

    private string Lookup(string sql, string paramValue)
    {
        try {
            using (var conn = new SqlConnection(ConnStr()))
            using (var cmd  = new SqlCommand(sql, conn))
            { cmd.Parameters.AddWithValue("@p", paramValue); conn.Open(); object r = cmd.ExecuteScalar(); return (r != null && r != DBNull.Value) ? r.ToString() : null; }
        } catch { return null; }
    }

    private static string FmtCell(object val, bool isNum)
    {
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

    // ── Page footer event ─────────────────────────────────────────────────────
    public class FooterEvent : PdfPageEventHelper {
        private string _user; private Font _font; private BaseColor _navy;
        public FooterEvent(string user, Font font, BaseColor navy) { _user = user; _font = font; _navy = navy; }
        public override void OnEndPage(PdfWriter writer, Document doc) {
            var cb   = writer.DirectContent;
            var line = new PdfContentByte(writer);
            cb.SetLineWidth(0.5f); cb.SetColorStroke(_navy);
            cb.MoveTo(doc.Left, doc.Bottom - 5f); cb.LineTo(doc.Right, doc.Bottom - 5f); cb.Stroke();
            var tbl = new PdfPTable(3) { TotalWidth = doc.Right - doc.Left };
            tbl.SetWidths(new float[] { 3f, 4f, 3f });
            Action<string, int> addCell = (txt, align) => tbl.AddCell(new PdfPCell(new Phrase(txt, _font)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = align, Padding = 2f });
            addCell("Printed by: " + (_user ?? ""), Element.ALIGN_LEFT);
            addCell("OBMS — Staff / Guard Issue Register", Element.ALIGN_CENTER);
            addCell("Page " + writer.PageNumber, Element.ALIGN_RIGHT);
            tbl.WriteSelectedRows(0, -1, doc.Left, doc.Bottom - 7f, cb);
        }
    }

</script>
