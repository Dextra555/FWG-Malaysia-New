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
    // OBMS Low Stock / Reorder Level Report — PDF / Excel
    // Single-file inline C# (no code-behind) — same pattern as StockIssueListReport.aspx
    // Flow: Angular → ASPX → sp_LowStockReport → PDF / Excel → Browser
    //
    // No date filter — shows current snapshot of stock vs reorder level.
    // Columns: No, Item Code, Item Name, Category, UOM, Warehouse,
    //          Current Stock, Reorder Level, Reorder Qty, Shortage Qty, Status
    //
    // PDF: rows colour-coded — "Out of Stock" = red background, "Low Stock" = amber.
    // ════════════════════════════════════════════════════════════════════════

    protected void Page_Load(object sender, EventArgs e)
    {
        // ── 1. Read query-string params ───────────────────────────────────
        string loginID       = (Request.QueryString["LoginID"]      ?? "").Trim();
        string warehouseIDStr= (Request.QueryString["WarehouseID"]  ?? "0").Trim();
        string categoryIDStr = (Request.QueryString["CategoryID"]   ?? "0").Trim();
        string format        = (Request.QueryString["format"]       ?? "pdf").ToLower().Trim();

        int warehouseID = 0; int.TryParse(warehouseIDStr, out warehouseID);
        int categoryID  = 0; int.TryParse(categoryIDStr,  out categoryID);

        // ── 2. Execute stored procedure ───────────────────────────────────
        DataTable dt;
        try   { dt = RunSP(warehouseID, categoryID); }
        catch (Exception ex)
        {
            LogErr("sp_LowStockReport", ex.Message);
            SendError("Unable to retrieve stock data. Please try again.");
            return;
        }

        if (dt == null || dt.Rows.Count == 0)
        { SendError("No low-stock items found for the selected filters. All items are above reorder level."); return; }

        // ── 3. Resolve display names for filter header ────────────────────
        string whName  = warehouseID > 0
            ? (Lookup("SELECT Name FROM WarehouseMaster WHERE ID=@p", warehouseID.ToString()) ?? warehouseIDStr)
            : "All Warehouses";
        string catName = categoryID > 0
            ? (Lookup("SELECT Name FROM InventoryCategory WHERE ID=@p", categoryID.ToString()) ?? categoryIDStr)
            : "All Categories";
        string asOf    = DateTime.Now.ToString("dd MMM yyyy");

        // ── 4. Build PDF or Excel ─────────────────────────────────────────
        if (format == "excel")
        {
            byte[] xlsx;
            try   { xlsx = BuildExcel(whName, catName, asOf, dt); }
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
                "attachment; filename=LowStockReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls");
            Response.AddHeader("Content-Length", xlsx.Length.ToString());
            Response.AddHeader("Access-Control-Allow-Origin", "*");
            Response.BinaryWrite(xlsx);
            Response.Flush();
            Response.End();
            return;
        }

        // ── 4b. Build PDF (default) ───────────────────────────────────────
        byte[] pdf;
        try   { pdf = BuildPdf(loginID, whName, catName, asOf, dt); }
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
            "inline; filename=LowStockReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
        Response.AddHeader("Content-Length", pdf.Length.ToString());
        Response.AddHeader("Access-Control-Allow-Origin", "*");
        Response.BinaryWrite(pdf);
        Response.Flush();
        Response.End();
    }

    // ════════════════════════════════════════════════════════════════════════
    // PDF BUILDER
    // ════════════════════════════════════════════════════════════════════════

    private byte[] BuildPdf(string loginID, string whName, string catName,
                            string asOf, DataTable dt)
    {
        // ── Colours ────────────────────────────────────────────────────────
        BaseColor navy      = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor headerBg  = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor rowAlt    = new BaseColor(0xf4, 0xf7, 0xfb);
        BaseColor outOfStock= new BaseColor(0xff, 0xe5, 0xe5);   // light red
        BaseColor lowStock  = new BaseColor(0xff, 0xf3, 0xcd);   // light amber
        BaseColor totalBg   = new BaseColor(0xe8, 0xf0, 0xfe);
        BaseColor border    = new BaseColor(0xcc, 0xcc, 0xcc);
        BaseColor gold      = new BaseColor(0xD4, 0xA0, 0x17);
        BaseColor red       = new BaseColor(0xc6, 0x28, 0x28);
        BaseColor amber     = new BaseColor(0x99, 0x66, 0x00);

        // ── Fonts ──────────────────────────────────────────────────────────
        Font fTitle   = new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD,   navy);
        Font fFilter  = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, new BaseColor(0x44,0x44,0x44));
        Font fFilterB = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   new BaseColor(0x44,0x44,0x44));
        Font fTH      = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   BaseColor.WHITE);
        Font fTD      = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.NORMAL, BaseColor.BLACK);
        Font fTDRed   = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.BOLD,   red);
        Font fTDAmber = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.BOLD,   amber);
        Font fTotal   = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
        Font fFoot    = new Font(Font.FontFamily.HELVETICA,  7f, Font.NORMAL, BaseColor.GRAY);
        Font fLegend  = new Font(Font.FontFamily.HELVETICA,  7.5f, Font.NORMAL, new BaseColor(0x44,0x44,0x44));

        // ── Column definitions ─────────────────────────────────────────────
        // header, fieldName, relWidth, isNumeric
        var cols = new object[][]
        {
            new object[]{ "S.NO.",         "#",               1.2f,  true  },
            new object[]{ "Item Code",     "ItemCode",        1.6f,  false },
            new object[]{ "Item Name",     "ItemName",        3.5f,  false },
            new object[]{ "Category",      "CategoryName",    2.0f,  false },
            new object[]{ "UOM",           "UOMName",         1.0f,  false },
            new object[]{ "Warehouse",     "WarehouseName",   2.2f,  false },
            new object[]{ "Current Stock", "CurrentStock",    1.8f,  true  },
            new object[]{ "Reorder Level", "ReorderLevel",    1.8f,  true  },
            new object[]{ "Reorder Qty",   "ReorderQuantity", 1.8f,  true  },
            new object[]{ "Shortage Qty",  "ShortageQty",     1.8f,  true  },
            new object[]{ "Status",        "StockStatus",     1.8f,  false },
        };

        float[] widths = new float[cols.Length];
        for (int i = 0; i < cols.Length; i++) widths[i] = (float)cols[i][2];

        using (var ms = new MemoryStream())
        {
            var doc    = new Document(PageSize.A4.Rotate(), 25f, 25f, 30f, 30f);
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

            // ── Report title ───────────────────────────────────────────────
            P(doc, "LOW STOCK / REORDER LEVEL REPORT", fTitle, Element.ALIGN_CENTER, 4f);

            // ── Filter summary ─────────────────────────────────────────────
            var ft = new PdfPTable(2) { WidthPercentage = 100f, SpacingAfter = 4f };
            ft.SetWidths(new float[] { 1f, 1f });
            ft.DefaultCell.Border = Rectangle.NO_BORDER;
            string[][] filterPairs = new string[][]
            {
                new[]{ "Warehouse:", whName  },
                new[]{ "Category:",  catName },
                new[]{ "Period:",     asOf    },
                new[]{ "Total Items:", dt.Rows.Count.ToString() + " item(s) below reorder level" }
            };
            foreach (var f in filterPairs)
            {
                var ph = new Phrase();
                ph.Add(new Chunk(f[0] + " ", fFilterB));
                ph.Add(new Chunk(f[1], fFilter));
                ft.AddCell(new PdfPCell(ph) { Border = Rectangle.NO_BORDER, Padding = 2f });
            }
            doc.Add(ft);

            // ── Legend ─────────────────────────────────────────────────────
            var legTable = new PdfPTable(3) { WidthPercentage = 60f, SpacingAfter = 6f };
            legTable.HorizontalAlignment = Element.ALIGN_LEFT;
            legTable.DefaultCell.Border = Rectangle.NO_BORDER;

            var legOOS = new PdfPCell(new Phrase("  Out of Stock (0 or negative balance)  ", fLegend))
            { BackgroundColor = outOfStock, Border = Rectangle.BOX, BorderColor = border, Padding = 3f };
            var legLS  = new PdfPCell(new Phrase("  Low Stock (above 0, at or below reorder level)  ", fLegend))
            { BackgroundColor = lowStock,  Border = Rectangle.BOX, BorderColor = border, Padding = 3f };
            var legBlank = new PdfPCell(new Phrase("")) { Border = Rectangle.NO_BORDER };

            legTable.AddCell(legOOS);
            legTable.AddCell(legLS);
            legTable.AddCell(legBlank);
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
            int     ri          = 0;
            decimal totalItems  = dt.Rows.Count;

            foreach (DataRow dr in dt.Rows)
            {
                ri++;
                string status = dr["StockStatus"] != DBNull.Value ? dr["StockStatus"].ToString() : "";
                BaseColor rowBg = status == "Out of Stock" ? outOfStock
                                : status == "Low Stock"    ? lowStock
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

                        // Colour-code status text and negative/zero stock
                        if (field == "StockStatus")
                        {
                            cf = txt == "Out of Stock" ? fTDRed : fTDAmber;
                        }
                        else if (isNum && raw != null && raw != DBNull.Value)
                        {
                            decimal dv;
                            if (decimal.TryParse(raw.ToString(), out dv) && dv <= 0)
                                cf = fTDRed;
                        }
                    }

                    tbl.AddCell(new PdfPCell(new Phrase(txt, cf))
                    {
                        BackgroundColor     = rowBg,
                        HorizontalAlignment = (isNum || field == "#") ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT,
                        Padding = 4f, Border = Rectangle.BOX, BorderColor = border
                    });
                }
            }

            doc.Add(tbl);
            doc.Close();
            return ms.ToArray();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // EXCEL BUILDER
    // ════════════════════════════════════════════════════════════════════════

    private byte[] BuildExcel(string whName, string catName, string asOf, DataTable dt)
    {
        var cols = new string[][]
        {
            new[]{ "S.NO.",         "#"               },
            new[]{ "Item Code",     "ItemCode"        },
            new[]{ "Item Name",     "ItemName"        },
            new[]{ "Category",      "CategoryName"    },
            new[]{ "UOM",           "UOMName"         },
            new[]{ "Warehouse",     "WarehouseName"   },
            new[]{ "Current Stock", "CurrentStock"    },
            new[]{ "Reorder Level", "ReorderLevel"    },
            new[]{ "Reorder Qty",   "ReorderQuantity" },
            new[]{ "Shortage Qty",  "ShortageQty"     },
            new[]{ "Status",        "StockStatus"     },
        };

        var numFlds = new System.Collections.Generic.HashSet<string>
            { "CurrentStock", "ReorderLevel", "ReorderQuantity", "ShortageQty" };

        int span = cols.Length;
        var sb   = new System.Text.StringBuilder();

        sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" " +
                      "xmlns:x=\"urn:schemas-microsoft-com:office:excel\" " +
                      "xmlns=\"http://www.w3.org/TR/REC-html40\">");
        sb.AppendLine("<head><meta charset=\"UTF-8\"/>");
        sb.AppendLine("<!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets>" +
                      "<x:ExcelWorksheet><x:Name>Low Stock</x:Name>" +
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
        sb.AppendLine(".oos{background:#ffe5e5;font-weight:bold;color:#c62828;}");
        sb.AppendLine(".low{background:#fff3cd;font-weight:bold;color:#996600;}");
        sb.AppendLine(".oos-n{background:#ffe5e5;text-align:right;font-weight:bold;color:#c62828;padding:4px;border:1px solid #ddd;}");
        sb.AppendLine(".low-n{background:#fff3cd;text-align:right;font-weight:bold;color:#996600;padding:4px;border:1px solid #ddd;}");
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

        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"ttl\">LOW STOCK / REORDER LEVEL REPORT</td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:2px;\"></td></tr>\n", span);

        sb.AppendFormat("<tr><td colspan=\"{0}\">", span);
        sb.AppendFormat("<span class=\"fl\">Warehouse: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;", H(whName));
        sb.AppendFormat("<span class=\"fl\">Category: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;",  H(catName));
        sb.AppendFormat("<span class=\"fl\">Period: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;",     H(asOf));
        sb.AppendFormat("<span class=\"fl\">Total Items: </span><span class=\"fv\">{0} item(s) below reorder level</span>", dt.Rows.Count);
        sb.AppendLine("</td></tr>");
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:3px;\"></td></tr>\n", span);

        // Legend row
        sb.AppendFormat("<tr><td colspan=\"{0}\">", span);
        sb.Append("<span style=\"background:#ffe5e5;color:#c62828;font-weight:bold;padding:2px 6px;border:1px solid #ccc;\">Out of Stock</span>&nbsp;&nbsp;");
        sb.Append("<span style=\"background:#fff3cd;color:#996600;font-weight:bold;padding:2px 6px;border:1px solid #ccc;\">Low Stock</span>");
        sb.AppendLine("</td></tr>");
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:3px;\"></td></tr>\n", span);

        // Column headers
        sb.AppendLine("<tr>");
        foreach (var c in cols) sb.AppendFormat("<td class=\"th\">{0}</td>", H(c[0]));
        sb.AppendLine("</tr>");

        // Data rows
        int ri = 0;
        foreach (DataRow dr in dt.Rows)
        {
            ri++;
            string status = dr["StockStatus"] != DBNull.Value ? dr["StockStatus"].ToString() : "";
            bool   isOOS  = status == "Out of Stock";
            bool   isLow  = status == "Low Stock";

            sb.AppendLine("<tr>");
            foreach (var c in cols)
            {
                string fld = c[1];
                if (fld == "#")
                {
                    string cls = isOOS ? "oos-n" : isLow ? "low-n" : "tn";
                    sb.AppendFormat("<td class=\"{0}\">{1}</td>", cls, ri);
                    continue;
                }

                object raw = dt.Columns.Contains(fld) ? dr[fld] : null;
                string txt = raw != null && raw != DBNull.Value ? raw.ToString() : "";

                if (numFlds.Contains(fld))
                {
                    decimal num; bool ok = decimal.TryParse(txt, out num);
                    string cls = isOOS ? "oos-n" : isLow ? "low-n" : "tn";
                    sb.AppendFormat("<td class=\"{0}\">{1}</td>", cls, ok ? num.ToString("N2") : "&mdash;");
                }
                else
                {
                    string cls = isOOS ? "oos td" : isLow ? "low td" : "td";
                    sb.AppendFormat("<td class=\"{0}\">{1}</td>", cls, H(txt));
                }
            }
            sb.AppendLine("</tr>");
        }

        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:4px;\"></td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"ftr\">Generated: {1}</td></tr>\n",
            span, DateTime.Now.ToString("dd MMM yyyy hh:mm tt"));
        sb.AppendLine("</table></body></html>");
        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ════════════════════════════════════════════════════════════════════════
    // DATA ACCESS
    // ════════════════════════════════════════════════════════════════════════

    private DataTable RunSP(int whID, int catID)
    {
        var dt = new DataTable();
        using (var conn = new SqlConnection(ConnStr()))
        using (var cmd  = new SqlCommand("sp_LowStockReport", conn))
        {
            cmd.CommandType    = CommandType.StoredProcedure;
            cmd.CommandTimeout = 60;
            cmd.Parameters.AddWithValue("@WarehouseID", whID);
            cmd.Parameters.AddWithValue("@CategoryID",  catID);
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
            "<h3 style='color:#c62828'>Low Stock / Reorder Level Report</h3>" +
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

    // ════════════════════════════════════════════════════════════════════════
    // PAGE FOOTER EVENT
    // ════════════════════════════════════════════════════════════════════════

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
