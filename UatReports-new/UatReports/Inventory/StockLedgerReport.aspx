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
    // OBMS Stock Ledger PDF Report — Part 9
    // All logic inline (precompiled project — no CodeFile support).
    // Uses iTextSharp 5.5.13.x from bin/ (add via NuGet in VS then rebuild).
    // ════════════════════════════════════════════════════════════════════════

    protected void Page_Load(object sender, EventArgs e)
    {
        // ── 1. Read + validate query-string ──────────────────────────────
        string loginID         = (Request.QueryString["LoginID"]         ?? "").Trim();
        string startDateStr    = (Request.QueryString["StartDate"]       ?? "").Trim();
        string endDateStr      = (Request.QueryString["EndDate"]         ?? "").Trim();
        string warehouseIDStr  = (Request.QueryString["WarehouseID"]     ?? "0").Trim();
        string itemIDStr       = (Request.QueryString["ItemID"]          ?? "0").Trim();
        string transactionType = (Request.QueryString["TransactionType"] ?? "ALL").Trim();

        if (string.IsNullOrWhiteSpace(startDateStr) || string.IsNullOrWhiteSpace(endDateStr))
        { SendError("Please select a valid date range."); return; }

        DateTime startDate, endDate;
        if (!DateTime.TryParse(startDateStr, out startDate) ||
            !DateTime.TryParse(endDateStr,   out endDate))
        { SendError("Please select a valid date range."); return; }

        if (startDate.Date > endDate.Date)
        { SendError("Start date must be before end date."); return; }

        int warehouseID = 0;
        int.TryParse(warehouseIDStr, out warehouseID);
        if (warehouseID <= 0)
        { SendError("Please select a warehouse."); return; }

        int itemID = 0;
        int.TryParse(itemIDStr, out itemID);

        // ── 2. Execute stored procedure ───────────────────────────────────
        DataTable dt;
        try   { dt = RunSP(warehouseID, itemID, transactionType, startDate, endDate); }
        catch (Exception ex)
        {
            LogErr("sp_InventoryStockLedger", ex.Message);
            SendError("Unable to retrieve stock ledger data. Please try again.");
            return;
        }

        if (dt == null || dt.Rows.Count == 0)
        { SendError("No stock ledger data found for the selected filters."); return; }

        // ── 3. Resolve display names for filter header ────────────────────
        string whName    = Lookup("SELECT Name FROM WarehouseMaster WHERE ID=@p", warehouseID.ToString()) ?? warehouseIDStr;
        string itemName  = itemID > 0
            ? (Lookup("SELECT Name FROM ItemMaster WHERE ID=@p", itemID.ToString()) ?? itemIDStr)
            : "All Items";
        string typeLbl   = TypeLabel(transactionType);
        string period    = startDate.ToString("dd MMM yyyy") + " \u2013 " + endDate.ToString("dd MMM yyyy");

        // ── 4. Build PDF or Excel based on ?format= ──────────────────────
        string format = (Request.QueryString["format"] ?? "pdf").ToLower().Trim();

        if (format == "excel")
        {
            // ── Excel export ──────────────────────────────────────────────
            byte[] xlsx;
            try   { xlsx = BuildExcel(whName, itemName, typeLbl, period, dt); }
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
                "attachment; filename=StockLedger_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls");
            Response.AddHeader("Content-Length", xlsx.Length.ToString());
            Response.AddHeader("Access-Control-Allow-Origin", "*");
            Response.BinaryWrite(xlsx);
            Response.Flush();
            Response.End();
            return;
        }

        // ── 4b. Build PDF (default) ───────────────────────────────────────
        byte[] pdf;
        try   { pdf = BuildPdf(loginID, whName, itemName, typeLbl, period, dt); }
        catch (Exception ex)
        {
            LogErr("BuildPdf", ex.Message);
            SendError("Unable to generate PDF. Please try again.");
            return;
        }

        // ── 5. Stream PDF to browser ──────────────────────────────────────
        Response.Clear();
        Response.Buffer      = false;
        Response.ContentType = "application/pdf";
        Response.AddHeader("Content-Disposition",
            "inline; filename=StockLedger_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
        Response.AddHeader("Content-Length", pdf.Length.ToString());
        Response.AddHeader("Access-Control-Allow-Origin", "*");
        Response.BinaryWrite(pdf);
        Response.Flush();
        Response.End();
    }

    // ════════════════════════════════════════════════════════════════════════
    // PDF BUILDER
    // ════════════════════════════════════════════════════════════════════════

    private byte[] BuildPdf(string loginID, string whName, string itemName,
                            string typeLbl, string period, DataTable dt)
    {
        // Colours
        BaseColor navy      = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor headerBg  = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor rowAlt    = new BaseColor(0xf4, 0xf7, 0xfb);
        BaseColor totalBg   = new BaseColor(0xe8, 0xf0, 0xfe);
        BaseColor border    = new BaseColor(0xcc, 0xcc, 0xcc);

        // Fonts
        Font fTitle   = new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD,   navy);
        Font fFilter  = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, new BaseColor(0x44,0x44,0x44));
        Font fFilterB = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   new BaseColor(0x44,0x44,0x44));
        Font fTH      = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   BaseColor.WHITE);
        Font fTD      = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.NORMAL, BaseColor.BLACK);
        Font fTDRed   = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.BOLD,   new BaseColor(0xc6,0x28,0x28));
        Font fTotal   = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
        Font fFoot    = new Font(Font.FontFamily.HELVETICA,  7f, Font.NORMAL, BaseColor.GRAY);

        using (var ms = new MemoryStream())
        {
            var doc    = new Document(PageSize.A4.Rotate(), 25f, 25f, 30f, 30f);
            var writer = PdfWriter.GetInstance(doc, ms);

            // Page footer event
            writer.PageEvent = new FooterEvent(loginID, fFoot, navy);
            doc.Open();

            // ── Company header (logo left + company details right + gold line) ─
            string coName = AppSettings("CompanyName");
            string addr1  = AppSettings("Address1");
            string addr2  = AppSettings("Address2");
            string addr3  = AppSettings("PostCode") + " " + AppSettings("City");
            string state  = AppSettings("State");
            string phone  = AppSettings("Phone");
            string reg    = AppSettings("Registration");

            // Logo
            string logoPath = Server.MapPath("~/images/logo-white.png");

            // Header table: 2 columns — logo | company text
            var hdrTable = new PdfPTable(2) { WidthPercentage = 100f, SpacingAfter = 0f };
            hdrTable.SetWidths(new float[] { 1.2f, 8.8f });
            hdrTable.DefaultCell.Border = Rectangle.NO_BORDER;

            // Left cell — logo
            PdfPCell logoCell;
            if (System.IO.File.Exists(logoPath))
            {
                var logo = iTextSharp.text.Image.GetInstance(logoPath);
                logo.ScaleToFit(55f, 55f);
                logoCell = new PdfPCell(logo)
                {
                    Border              = Rectangle.NO_BORDER,
                    VerticalAlignment   = Element.ALIGN_MIDDLE,
                    HorizontalAlignment = Element.ALIGN_LEFT,
                    Padding             = 2f
                };
            }
            else
            {
                // Fallback if logo file missing
                logoCell = new PdfPCell(new Phrase("")) { Border = Rectangle.NO_BORDER };
            }
            hdrTable.AddCell(logoCell);

            // Right cell — company name + address stacked
            Font fCoNameHdr = new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD,   navy);
            Font fCoRegHdr  = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
            Font fCoSubHdr  = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, BaseColor.DARK_GRAY);

            var textPhrase = new Phrase();
            textPhrase.Add(new Chunk(coName + " ", fCoNameHdr));
            textPhrase.Add(new Chunk(reg + "\n",   fCoRegHdr));
            textPhrase.Add(new Chunk(addr1 + "\n", fCoSubHdr));
            textPhrase.Add(new Chunk(addr2 + "\n", fCoSubHdr));
            textPhrase.Add(new Chunk(addr3 + "\n", fCoSubHdr));
            textPhrase.Add(new Chunk(state + "\n", fCoSubHdr));
            textPhrase.Add(new Chunk(phone,         fCoSubHdr));

            var textCell = new PdfPCell(textPhrase)
            {
                Border              = Rectangle.NO_BORDER,
                VerticalAlignment   = Element.ALIGN_MIDDLE,
                HorizontalAlignment = Element.ALIGN_LEFT,
                Padding             = 4f,
                PaddingLeft         = 8f
            };
            hdrTable.AddCell(textCell);
            doc.Add(hdrTable);

            // Gold separator line (matches UI theme)
            BaseColor gold = new BaseColor(0xD4, 0xA0, 0x17);
            var goldSep = new iTextSharp.text.pdf.draw.LineSeparator(2.5f, 100f, gold, Element.ALIGN_CENTER, -2f);
            doc.Add(new Chunk(goldSep));
            doc.Add(Spacer(6f));

            // ── Report title ──────────────────────────────────────────────
            P(doc, "INVENTORY STOCK LEDGER REPORT", fTitle, Element.ALIGN_CENTER, 6f);

            // ── Filter summary (2-column table) ───────────────────────────
            var filters = new string[][]
            {
                new[]{ "Warehouse:",        whName   },
                new[]{ "Item:",             itemName },
                new[]{ "Transaction Type:", typeLbl  },
                new[]{ "Period:",           period   }
            };
            var ft = new PdfPTable(4) { WidthPercentage = 100f, SpacingAfter = 8f };
            ft.DefaultCell.Border = Rectangle.NO_BORDER;
            foreach (var f in filters)
            {
                var ph = new Phrase(); ph.Add(new Chunk(f[0] + " ", fFilterB)); ph.Add(new Chunk(f[1], fFilter));
                ft.AddCell(new PdfPCell(ph) { Border = Rectangle.NO_BORDER, Padding = 2f });
            }
            doc.Add(ft);

            // ── Data table ────────────────────────────────────────────────
            // Column definitions: header, fieldName, relWidth, isNumeric, isDate
            var cols = new object[][]
            {
                new object[]{ "Date",         "TransactionDate",      2.2f, false, true  },
                new object[]{ "Txn No",       "TransactionNo",        2.0f, false, false },
                new object[]{ "Type",         "TransactionTypeLabel", 2.8f, false, false },
                new object[]{ "Item Code",    "ItemCode",             2.0f, false, false },
                new object[]{ "Item Name",    "ItemName",             4.5f, false, false },
                new object[]{ "UOM",          "UOMName",              1.5f, false, false },
                new object[]{ "In Qty",       "InQty",                1.8f, true,  false },
                new object[]{ "Out Qty",      "OutQty",               1.8f, true,  false },
                new object[]{ "Balance",      "RunningBalance",       1.8f, true,  false },
                new object[]{ "Unit Cost",    "UnitCost",             1.8f, true,  false },
                new object[]{ "Ref No",       "ReferenceNo",          2.0f, false, false },
                new object[]{ "Remarks",      "Remarks",              3.0f, false, false },
            };

            float[] widths = new float[cols.Length];
            for (int i = 0; i < cols.Length; i++) widths[i] = (float)cols[i][2];

            var tbl = new PdfPTable(cols.Length) { WidthPercentage = 100f, HeaderRows = 1 };
            tbl.SetWidths(widths);

            // Header row
            foreach (var c in cols)
            {
                bool num = (bool)c[3];
                var cell = new PdfPCell(new Phrase((string)c[0], fTH))
                {
                    BackgroundColor     = headerBg,
                    HorizontalAlignment = num ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT,
                    Padding = 5f, Border = Rectangle.BOX, BorderColor = border
                };
                tbl.AddCell(cell);
            }

            // Data rows
            int ri = 0;
            decimal totalIn = 0, totalOut = 0;
            foreach (DataRow dr in dt.Rows)
            {
                var rowBg = (ri++ % 2 == 0) ? BaseColor.WHITE : rowAlt;
                foreach (var c in cols)
                {
                    string field  = (string)c[1];
                    bool   isNum  = (bool)c[3];
                    bool   isDate = (bool)c[4];
                    object raw    = dt.Columns.Contains(field) ? dr[field] : null;
                    string txt    = FmtCell(raw, isNum, isDate);

                    // Red for negative numbers
                    Font cf = fTD;
                    if (isNum && raw != null && raw != DBNull.Value)
                    { decimal dv; if (decimal.TryParse(raw.ToString(), out dv) && dv < 0) cf = fTDRed; }

                    // Accumulate totals
                    if (field == "InQty"  && raw != null && raw != DBNull.Value) { decimal v; if (decimal.TryParse(raw.ToString(), out v)) totalIn  += v; }
                    if (field == "OutQty" && raw != null && raw != DBNull.Value) { decimal v; if (decimal.TryParse(raw.ToString(), out v)) totalOut += v; }

                    tbl.AddCell(new PdfPCell(new Phrase(txt, cf))
                    {
                        BackgroundColor     = rowBg,
                        HorizontalAlignment = isNum ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT,
                        Padding = 4f, Border = Rectangle.BOX, BorderColor = border
                    });
                }
            }

            // Totals row
            for (int i = 0; i < cols.Length; i++)
            {
                string field = (string)cols[i][1];
                bool   isNum = (bool)cols[i][3];
                string txt   = i == 0 ? "TOTALS"
                             : field == "InQty"  ? totalIn.ToString("N2")
                             : field == "OutQty" ? totalOut.ToString("N2")
                             : "";
                int align    = (i == 0) ? Element.ALIGN_LEFT
                             : isNum   ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT;

                tbl.AddCell(new PdfPCell(new Phrase(txt, fTotal))
                {
                    BackgroundColor     = totalBg,
                    HorizontalAlignment = align,
                    Padding = 5f, Border = Rectangle.BOX,
                    BorderColor = navy, BorderWidth = 1.5f
                });
            }

            doc.Add(tbl);
            doc.Close();
            return ms.ToArray();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // DATA ACCESS
    // ════════════════════════════════════════════════════════════════════════
    // EXCEL BUILDER
    // Generates a simple .xlsx using HTML table — no extra library needed.
    // ASP.NET serves it as Excel via content type override.
    // Opens directly in Excel / LibreOffice Calc.
    // ════════════════════════════════════════════════════════════════════════

    private byte[] BuildExcel(string whName, string itemName,
                              string typeLbl, string period, DataTable dt)
    {
        var cols = new string[][]
        {
            new[]{ "Date",          "TransactionDate"      },
            new[]{ "Txn No",        "TransactionNo"        },
            new[]{ "Type",          "TransactionTypeLabel" },
            new[]{ "Item Code",     "ItemCode"             },
            new[]{ "Item Name",     "ItemName"             },
            new[]{ "UOM",           "UOMName"              },
            new[]{ "In Qty",        "InQty"                },
            new[]{ "Out Qty",       "OutQty"               },
            new[]{ "Balance",       "RunningBalance"       },
            new[]{ "Unit Cost",     "UnitCost"             },
            new[]{ "Ref No",        "ReferenceNo"          },
            new[]{ "Remarks",       "Remarks"              },
        };

        var numFlds = new System.Collections.Generic.HashSet<string>
            { "InQty", "OutQty", "RunningBalance", "UnitCost" };

        int span = cols.Length;
        var sb   = new System.Text.StringBuilder();

        // ── HTML header — Excel opens HTML tables with no errors ──────────
        sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" " +
                      "xmlns:x=\"urn:schemas-microsoft-com:office:excel\" " +
                      "xmlns=\"http://www.w3.org/TR/REC-html40\">");
        sb.AppendLine("<head><meta charset=\"UTF-8\"/>");
        sb.AppendLine("<!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets>" +
                      "<x:ExcelWorksheet><x:Name>Stock Ledger</x:Name>" +
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
        sb.AppendLine(".tnr{font-size:8.5pt;padding:4px;border:1px solid #ddd;text-align:right;color:#c62828;font-weight:bold;}");
        sb.AppendLine(".alt{background:#f4f7fb;}");
        sb.AppendLine(".tot{background:#e8f0fe;font-weight:bold;border-top:2px solid #1a3a5c;}");
        sb.AppendLine(".ftr{font-size:8pt;color:#888;}");
        sb.AppendLine(".gold{border-bottom:2px solid #D4A017;}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<table style=\"width:100%;border-collapse:collapse;\">");

        // ── Company header ────────────────────────────────────────────────
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

        // ── Title ─────────────────────────────────────────────────────────
        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"ttl\">INVENTORY STOCK LEDGER REPORT</td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:2px;\"></td></tr>\n", span);

        // ── Filters ───────────────────────────────────────────────────────
        sb.AppendFormat("<tr><td colspan=\"{0}\">", span);
        sb.AppendFormat("<span class=\"fl\">Warehouse: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;", H(whName));
        sb.AppendFormat("<span class=\"fl\">Item: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;", H(itemName));
        sb.AppendFormat("<span class=\"fl\">Type: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;", H(typeLbl));
        sb.AppendFormat("<span class=\"fl\">Period: </span><span class=\"fv\">{0}</span>", H(period));
        sb.AppendLine("</td></tr>");
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:3px;\"></td></tr>\n", span);

        // ── Column headers ────────────────────────────────────────────────
        sb.AppendLine("<tr>");
        foreach (var c in cols)
            sb.AppendFormat("<td class=\"th\">{0}</td>", H(c[0]));
        sb.AppendLine("</tr>");

        // ── Data rows ─────────────────────────────────────────────────────
        decimal totalIn = 0, totalOut = 0;
        int ri = 0;
        foreach (DataRow dr in dt.Rows)
        {
            string alt = (ri++ % 2 == 1) ? " alt" : "";
            sb.AppendLine("<tr>");
            foreach (var c in cols)
            {
                string fld = c[1];
                object raw = dt.Columns.Contains(fld) ? dr[fld] : null;
                string txt = "";
                if (raw != null && raw != DBNull.Value)
                {
                    if (fld == "TransactionDate")
                    { DateTime d; txt = DateTime.TryParse(raw.ToString(), out d) ? d.ToString("dd MMM yyyy") : raw.ToString(); }
                    else txt = raw.ToString();
                }
                if (fld == "InQty")  { decimal v; if (decimal.TryParse(txt, out v)) totalIn  += v; }
                if (fld == "OutQty") { decimal v; if (decimal.TryParse(txt, out v)) totalOut += v; }

                if (numFlds.Contains(fld))
                {
                    decimal num; bool ok = decimal.TryParse(txt, out num);
                    string cls = ok && num < 0 ? "tnr" + alt : "tn" + alt;
                    sb.AppendFormat("<td class=\"{0}\">{1}</td>", cls, ok ? num.ToString("N2") : "&mdash;");
                }
                else sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(txt));
            }
            sb.AppendLine("</tr>");
        }

        // ── Totals row ────────────────────────────────────────────────────
        sb.AppendLine("<tr>");
        foreach (var c in cols)
        {
            string fld   = c[1];
            bool   isNum = numFlds.Contains(fld);
            string val   = fld == "TransactionDate" ? "TOTALS"
                         : fld == "InQty"           ? totalIn.ToString("N2")
                         : fld == "OutQty"          ? totalOut.ToString("N2") : "";
            string align = isNum ? " style=\"text-align:right;\"" : "";
            sb.AppendFormat("<td class=\"tot td\"{0}>{1}</td>", align, H(val));
        }
        sb.AppendLine("</tr>");

        // ── Footer ────────────────────────────────────────────────────────
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:4px;\"></td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"ftr\">Generated: {1}</td></tr>\n",
            span, DateTime.Now.ToString("dd MMM yyyy hh:mm tt"));

        sb.AppendLine("</table></body></html>");
        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string H(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;");
    }

    // ════════════════════════════════════════════════════════════════════════

    private DataTable RunSP(int whID, int itemID, string txType, DateTime from, DateTime to)
    {
        var dt = new DataTable();
        using (var conn = new SqlConnection(ConnStr()))
        using (var cmd  = new SqlCommand("sp_InventoryStockLedger", conn))
        {
            cmd.CommandType    = CommandType.StoredProcedure;
            cmd.CommandTimeout = 60;
            cmd.Parameters.AddWithValue("@WarehouseID",     whID);
            cmd.Parameters.AddWithValue("@ItemID",          itemID);
            cmd.Parameters.AddWithValue("@TransactionType", txType ?? "ALL");
            cmd.Parameters.AddWithValue("@FromDate",        from.Date);
            cmd.Parameters.AddWithValue("@ToDate",          to.Date);
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

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════════════

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

    private static string FmtCell(object val, bool isNum, bool isDate)
    {
        if (val == null || val == DBNull.Value) return isNum ? "\u2014" : "";
        if (isDate)  { DateTime d; return DateTime.TryParse(val.ToString(), out d) ? d.ToString("dd MMM yyyy") : val.ToString(); }
        if (isNum)   { decimal  d; return decimal.TryParse(val.ToString(),  out d) ? d.ToString("N2")          : val.ToString(); }
        return val.ToString();
    }

    private static string TypeLabel(string code)
    {
        switch ((code ?? "ALL").ToUpper())
        {
            case "OPENING":      return "Opening Stock";
            case "STOCK_IN":     return "Stock In";
            case "ISSUE":        return "Stock Issue";
            case "ISSUE_RETURN": return "Issue Return";
            case "TRANSFER_IN":  return "Transfer In";
            case "TRANSFER_OUT": return "Transfer Out";
            case "ADJ_PLUS":     return "Adjustment (In)";
            case "ADJ_MINUS":    return "Adjustment (Out)";
            case "PURCHASE_RTN": return "Purchase Return";
            case "DAMAGE_LOSS":  return "Damage / Loss";
            default:             return "All Types";
        }
    }

    private void SendError(string msg)
    {
        Response.Clear(); Response.ContentType = "text/html";
        Response.Write("<html><body style='font-family:Arial;padding:30px'>" +
            "<h3 style='color:#c62828'>Stock Ledger Report</h3>" +
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

    // iTextSharp paragraph shortcut
    private static void P(Document doc, string text, Font font, int align, float spacingAfter)
    {
        doc.Add(new Paragraph(text, font) { Alignment = align, SpacingAfter = spacingAfter });
    }

    private static Paragraph Spacer(float h)
    {
        return new Paragraph(" ") { SpacingAfter = h };
    }

    // ════════════════════════════════════════════════════════════════════════
    // PAGE FOOTER EVENT (page number + generated-by)
    // ════════════════════════════════════════════════════════════════════════

    private class FooterEvent : PdfPageEventHelper
    {
        private readonly string    _user;
        private readonly Font      _font;
        private readonly BaseColor _navy;

        public FooterEvent(string user, Font font, BaseColor navy)
        { _user = user; _font = font; _navy = navy; }

        public override void OnEndPage(PdfWriter writer, Document doc)
        {
            var cb  = writer.DirectContent;
            var pg  = doc.PageSize;

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
