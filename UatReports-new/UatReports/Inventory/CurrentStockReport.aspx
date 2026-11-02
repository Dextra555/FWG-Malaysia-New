<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Data.SqlClient" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="iTextSharp.text" %>
<%@ Import Namespace="iTextSharp.text.pdf" %>
<%@ Import Namespace="System.IO" %>
<script runat="server">

    protected void Page_Load(object sender, EventArgs e)
    {
        string loginID = (Request.QueryString["LoginID"] ?? "").Trim();
        string warehouseIDStr = (Request.QueryString["WarehouseID"] ?? "0").Trim();
        string itemIDStr = (Request.QueryString["ItemID"] ?? "0").Trim();
        string format = (Request.QueryString["format"] ?? "pdf").ToLower().Trim();

        int warehouseID = 0;
        int.TryParse(warehouseIDStr, out warehouseID);

        int itemID = 0;
        int.TryParse(itemIDStr, out itemID);

        DataTable dt;

        try
        {
            dt = RunSP(warehouseID, itemID);
        }
        catch (Exception ex)
        {
            LogErr("sp_CurrentStockReport", ex.Message);
            SendError("Unable to retrieve current stock data. Please try again.");
            return;
        }

        if (dt == null || dt.Rows.Count == 0)
        {
            SendError("No stock data found for the selected filters.");
            return;
        }

        string whName = warehouseID > 0
            ? (Lookup("SELECT Name FROM WarehouseMaster WHERE ID=@p",
                warehouseID.ToString()) ?? warehouseIDStr)
            : "All Warehouses";

        string itemName = itemID > 0
            ? (Lookup("SELECT Name FROM ItemMaster WHERE ID=@p",
                itemID.ToString()) ?? itemIDStr)
            : "All Items";

        // Default period: first day of this month through today.
        // StartDate and EndDate can override the displayed period.
        DateTime today = DateTime.Today;
        DateTime startDate, endDate;

        if (!DateTime.TryParse(Request.QueryString["StartDate"], out startDate))
            startDate = new DateTime(today.Year, today.Month, 1);

        if (!DateTime.TryParse(Request.QueryString["EndDate"], out endDate))
            endDate = today;

        // Use a Unicode en dash in the report period.
        string period = startDate.ToString("dd MMM yyyy")
                      + " to "
                      + endDate.ToString("dd MMM yyyy");

        if (format == "excel")
        {
            byte[] xlsx;

            try
            {
                xlsx = BuildExcel(whName, itemName, period, dt);
            }
            catch (Exception ex)
            {
                LogErr("BuildExcel", ex.Message);
                SendError("Unable to generate Excel. Please try again.");
                return;
            }

            Response.Clear();
            Response.Buffer = false;
            Response.ContentType = "application/vnd.ms-excel";
            Response.AddHeader(
                "Content-Disposition",
                "attachment; filename=CurrentStock_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls");

            Response.AddHeader("Content-Length", xlsx.Length.ToString());
            Response.AddHeader("Access-Control-Allow-Origin", "*");
            Response.BinaryWrite(xlsx);
            Response.Flush();
            Response.End();
            return;
        }

        byte[] pdf;

        try
        {
            pdf = BuildPdf(loginID, whName, itemName, period, dt);
        }
        catch (Exception ex)
        {
            LogErr("BuildPdf", ex.Message);
            SendError("Unable to generate PDF. Please try again.");
            return;
        }

        Response.Clear();
        Response.Buffer = false;
        Response.ContentType = "application/pdf";
        Response.AddHeader(
            "Content-Disposition",
            "inline; filename=CurrentStock_" +
            DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");

        Response.AddHeader("Content-Length", pdf.Length.ToString());
        Response.AddHeader("Access-Control-Allow-Origin", "*");
        Response.BinaryWrite(pdf);
        Response.Flush();
        Response.End();
    }

    // ============================================================
    // PDF BUILDER
    // ============================================================

    private byte[] BuildPdf(
        string loginID,
        string whName,
        string itemName,
        string period,
        DataTable dt)
    {
        BaseColor navy = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor headerBg = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor rowAlt = new BaseColor(0xf4, 0xf7, 0xfb);
        BaseColor totalBg = new BaseColor(0xe8, 0xf0, 0xfe);
        BaseColor lowBg = new BaseColor(0xff, 0xf3, 0xcd);
        BaseColor border = new BaseColor(0xcc, 0xcc, 0xcc);
        BaseColor gold = new BaseColor(0xD4, 0xA0, 0x17);

        Font fTitle = new Font(
            Font.FontFamily.HELVETICA, 11f, Font.BOLD, navy);

        Font fFilter = new Font(
            Font.FontFamily.HELVETICA, 8f, Font.NORMAL,
            new BaseColor(0x44, 0x44, 0x44));

        Font fFilterB = new Font(
            Font.FontFamily.HELVETICA, 8f, Font.BOLD,
            new BaseColor(0x44, 0x44, 0x44));

        Font fTH = new Font(
            Font.FontFamily.HELVETICA, 8f, Font.BOLD, BaseColor.WHITE);

        Font fTD = new Font(
            Font.FontFamily.HELVETICA, 7.5f, Font.NORMAL, BaseColor.BLACK);

        Font fTDRed = new Font(
            Font.FontFamily.HELVETICA, 7.5f, Font.BOLD,
            new BaseColor(0xc6, 0x28, 0x28));

        Font fTDAmber = new Font(
            Font.FontFamily.HELVETICA, 7.5f, Font.BOLD,
            new BaseColor(0x85, 0x60, 0x00));

        Font fTotal = new Font(
            Font.FontFamily.HELVETICA, 8f, Font.BOLD, navy);

        Font fFoot = new Font(
            Font.FontFamily.HELVETICA, 7f, Font.NORMAL, BaseColor.GRAY);

        using (var ms = new MemoryStream())
        {
            var doc = new Document(
                PageSize.A4.Rotate(), 25f, 25f, 30f, 30f);

            var writer = PdfWriter.GetInstance(doc, ms);
            writer.PageEvent = new FooterEvent(loginID, fFoot, navy);
            doc.Open();

            // Company header
            string coName = AppSettings("CompanyName");
            string addr1 = AppSettings("Address1");
            string addr2 = AppSettings("Address2");
            string addr3 = AppSettings("PostCode") + " " +
                           AppSettings("City");
            string state = AppSettings("State");
            string phone = AppSettings("Phone");
            string reg = AppSettings("Registration");

            string logoPath = Server.MapPath("~/images/logo-white.png");

            var hdrTable = new PdfPTable(2)
            {
                WidthPercentage = 100f,
                SpacingAfter = 0f
            };

            hdrTable.SetWidths(new float[] { 1.2f, 8.8f });
            hdrTable.DefaultCell.Border = Rectangle.NO_BORDER;

            PdfPCell logoCell;

            if (System.IO.File.Exists(logoPath))
            {
                var logo = iTextSharp.text.Image.GetInstance(logoPath);
                logo.ScaleToFit(55f, 55f);

                logoCell = new PdfPCell(logo)
                {
                    Border = Rectangle.NO_BORDER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    HorizontalAlignment = Element.ALIGN_LEFT,
                    Padding = 2f
                };
            }
            else
            {
                logoCell = new PdfPCell(new Phrase(""))
                {
                    Border = Rectangle.NO_BORDER
                };
            }

            hdrTable.AddCell(logoCell);

            Font fCoName = new Font(
                Font.FontFamily.HELVETICA, 11f, Font.BOLD, navy);

            Font fCoReg = new Font(
                Font.FontFamily.HELVETICA, 8f, Font.BOLD, navy);

            Font fCoSub = new Font(
                Font.FontFamily.HELVETICA, 8f, Font.NORMAL,
                BaseColor.DARK_GRAY);

            var textPhrase = new Phrase();
            textPhrase.Add(new Chunk(coName + " ", fCoName));
            textPhrase.Add(new Chunk(reg + "\n", fCoReg));
            textPhrase.Add(new Chunk(addr1 + "\n", fCoSub));
            textPhrase.Add(new Chunk(addr2 + "\n", fCoSub));
            textPhrase.Add(new Chunk(addr3 + "\n", fCoSub));
            textPhrase.Add(new Chunk(state + "\n", fCoSub));
            textPhrase.Add(new Chunk(phone, fCoSub));

            var textCell = new PdfPCell(textPhrase)
            {
                Border = Rectangle.NO_BORDER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                HorizontalAlignment = Element.ALIGN_LEFT,
                Padding = 4f,
                PaddingLeft = 8f
            };

            hdrTable.AddCell(textCell);
            doc.Add(hdrTable);

            var goldSep =
                new iTextSharp.text.pdf.draw.LineSeparator(
                    2.5f, 100f, gold, Element.ALIGN_CENTER, -2f);

            doc.Add(new Chunk(goldSep));
            doc.Add(Spacer(6f));

            // Report title
            P(doc, "CURRENT STOCK REPORT",
                fTitle, Element.ALIGN_CENTER, 6f);

            // Filter summary
            var filters = new string[][]
            {
                new[] { "Warehouse:", whName },
                new[] { "Item:", itemName },
                new[] { "Period:", period }
            };

            var ft = new PdfPTable(3)
            {
                WidthPercentage = 100f,
                SpacingAfter = 8f
            };

            ft.SetWidths(new float[] { 3.2f, 2.8f, 4.0f });
            ft.DefaultCell.Border = Rectangle.NO_BORDER;

            foreach (var f in filters)
            {
                var ph = new Phrase();
                ph.Add(new Chunk(f[0] + " ", fFilterB));
                ph.Add(new Chunk(f[1], fFilter));

                ft.AddCell(new PdfPCell(ph)
                {
                    Border = Rectangle.NO_BORDER,
                    Padding = 2f,
                    NoWrap = true
                });
            }

            doc.Add(ft);

            // Data table columns
            // Header, field name, relative width, numerics
            var cols = new object[][]
            {
                new object[] { "S.NO.",            "#",                1.2f, true  },
                new object[] { "Warehouse",     "WarehouseName",     2.8f, false },
                new object[] { "Item Code",     "ItemCode",          1.8f, false },
                new object[] { "Item Name",     "ItemName",          4.5f, false },
                new object[] { "Category",      "CategoryName",      2.2f, false },
                new object[] { "Brand",         "BrandName",        1.8f, false },
                new object[] { "UOM",           "UOMName",          1.2f, false },
                new object[] { "Current Stock", "CurrentStock",     2.0f, true  },
                new object[] { "Reorder Lvl",   "ReorderLevel",     2.0f, true  },
                new object[] { "Unit Cost",     "LastUnitCost",     2.0f, true  },
                new object[] { "Stock Value",   "StockValue",       2.2f, true  },
                new object[] { "Status",        "BelowReorderLevel",2.0f, false }
            };

            float[] widths = new float[cols.Length];

            for (int i = 0; i < cols.Length; i++)
                widths[i] = (float)cols[i][2];

            var tbl = new PdfPTable(cols.Length)
            {
                WidthPercentage = 100f,
                HeaderRows = 1
            };

            tbl.SetWidths(widths);

            // Header row
            foreach (var c in cols)
            {
                bool isNum = (bool)c[3];

                tbl.AddCell(new PdfPCell(
                    new Phrase((string)c[0], fTH))
                {
                    BackgroundColor = headerBg,
                    HorizontalAlignment = isNum
                        ? Element.ALIGN_RIGHT
                        : Element.ALIGN_LEFT,
                    Padding = 5f,
                    Border = Rectangle.BOX,
                    BorderColor = border
                });
            }

            int ri = 0;
            decimal totalStock = 0;
            decimal totalValue = 0;

            // Data rows
            foreach (DataRow dr in dt.Rows)
            {
                ri++;

                object belowRaw =
                    dt.Columns.Contains("BelowReorderLevel")
                    ? dr["BelowReorderLevel"] : null;

                bool isLowStock =
                    belowRaw != null &&
                    belowRaw != DBNull.Value &&
                    (belowRaw.ToString() == "1" ||
                     belowRaw.ToString().ToLower() == "true");

                BaseColor rowBg = isLowStock
                    ? lowBg
                    : ((ri % 2 == 0) ? rowAlt : BaseColor.WHITE);

                foreach (var c in cols)
                {
                    string field = (string)c[1];
                    bool isNum = (bool)c[3];

                    string txt;
                    Font cf = isLowStock ? fTDAmber : fTD;

                    if (field == "#")
                    {
                        txt = ri.ToString();
                    }
                    else if (field == "BelowReorderLevel")
                    {
                        txt = isLowStock ? "Low Stock" : "OK";

                        cf = isLowStock
                            ? new Font(Font.FontFamily.HELVETICA,
                                7.5f, Font.BOLD,
                                new BaseColor(0xc6, 0x28, 0x28))
                            : new Font(Font.FontFamily.HELVETICA,
                                7.5f, Font.BOLD,
                                new BaseColor(0x2e, 0x7d, 0x32));
                    }
                    else
                    {
                        object raw =
                            dt.Columns.Contains(field) ? dr[field] : null;

                        txt = FmtCell(raw, isNum);

                        if (isNum && raw != null && raw != DBNull.Value)
                        {
                            decimal dv;

                            if (decimal.TryParse(raw.ToString(), out dv) &&
                                dv < 0)
                            {
                                cf = fTDRed;
                            }
                        }

                        if (field == "CurrentStock" &&
                            raw != null && raw != DBNull.Value)
                        {
                            decimal v;
                            if (decimal.TryParse(raw.ToString(), out v))
                                totalStock += v;
                        }

                        if (field == "StockValue" &&
                            raw != null && raw != DBNull.Value)
                        {
                            decimal v;
                            if (decimal.TryParse(raw.ToString(), out v))
                                totalValue += v;
                        }
                    }

                    tbl.AddCell(new PdfPCell(new Phrase(txt, cf))
                    {
                        BackgroundColor = rowBg,
                        HorizontalAlignment =
                            (isNum || field == "#")
                            ? Element.ALIGN_RIGHT
                            : Element.ALIGN_LEFT,
                        Padding = 4f,
                        Border = Rectangle.BOX,
                        BorderColor = border
                    });
                }
            }

            // TOTALS: merge first seven columns horizontally
            tbl.AddCell(new PdfPCell(new Phrase("TOTALS", fTotal))
            {
                Colspan = 7,
                BackgroundColor = totalBg,
                HorizontalAlignment = Element.ALIGN_LEFT,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 5f,
                Border = Rectangle.BOX,
                BorderColor = navy,
                BorderWidth = 1.5f,
                NoWrap = true
            });

            for (int i = 7; i < cols.Length; i++)
            {
                string field = (string)cols[i][1];

                string txt =
                    field == "CurrentStock"
                        ? totalStock.ToString("N2")
                    : field == "StockValue"
                        ? totalValue.ToString("N2")
                    : "";

                tbl.AddCell(new PdfPCell(new Phrase(txt, fTotal))
                {
                    BackgroundColor = totalBg,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Padding = 5f,
                    Border = Rectangle.BOX,
                    BorderColor = navy,
                    BorderWidth = 1.5f
                });
            }

            doc.Add(tbl);
            doc.Close();

            return ms.ToArray();
        }
    }

    // ============================================================
    // EXCEL BUILDER
    // ============================================================

    private byte[] BuildExcel(
        string whName,
        string itemName,
        string period,
        DataTable dt)
    {
        var cols = new string[][]
        {
            new[] { "S.NO.",            "#" },
            new[] { "Warehouse",     "WarehouseName" },
            new[] { "Item Code",     "ItemCode" },
            new[] { "Item Name",     "ItemName" },
            new[] { "Category",      "CategoryName" },
            new[] { "Brand",         "BrandName" },
            new[] { "UOM",           "UOMName" },
            new[] { "Current Stock", "CurrentStock" },
            new[] { "Reorder Level", "ReorderLevel" },
            new[] { "Unit Cost",     "LastUnitCost" },
            new[] { "Stock Value",   "StockValue" },
            new[] { "Status",        "BelowReorderLevel" }
        };

        var numFlds = new HashSet<string>
        {
            "CurrentStock",
            "ReorderLevel",
            "LastUnitCost",
            "StockValue"
        };

        int span = cols.Length;
        var sb = new System.Text.StringBuilder();

        sb.AppendLine(
            "<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" " +
            "xmlns:x=\"urn:schemas-microsoft-com:office:excel\" " +
            "xmlns=\"http://www.w3.org/TR/REC-html40\">");

        sb.AppendLine("<head><meta charset=\"UTF-8\"/>");

        sb.AppendLine(
            "<!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets>" +
            "<x:ExcelWorksheet><x:Name>Current Stock</x:Name>" +
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
        sb.AppendLine(".low{background:#fff3cd;}");
        sb.AppendLine(".tot{background:#e8f0fe;font-weight:bold;border-top:2px solid #1a3a5c;}");
        sb.AppendLine(".ok{color:#2e7d32;font-weight:bold;}");
        sb.AppendLine(".ls{color:#c62828;font-weight:bold;}");
        sb.AppendLine(".ftr{font-size:8pt;color:#888;}");
        sb.AppendLine(".gold{border-bottom:2px solid #D4A017;}");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine(
            "<table style=\"width:100%;border-collapse:collapse;\">");

        // Company header
        sb.AppendFormat(
            "<tr><td colspan=\"{0}\" class=\"co\">{1} " +
            "<span style=\"font-size:9pt\">{2}</span></td></tr>\n",
            span,
            H(AppSettings("CompanyName")),
            H(AppSettings("Registration")));

        sb.AppendFormat(
            "<tr><td colspan=\"{0}\" class=\"sub\">{1} {2}</td></tr>\n",
            span,
            H(AppSettings("Address1")),
            H(AppSettings("Address2")));

        sb.AppendFormat(
            "<tr><td colspan=\"{0}\" class=\"sub\">{1} {2}, {3}</td></tr>\n",
            span,
            H(AppSettings("PostCode")),
            H(AppSettings("City")),
            H(AppSettings("State")));

        sb.AppendFormat(
            "<tr><td colspan=\"{0}\" class=\"sub\">{1}</td></tr>\n",
            span,
            H(AppSettings("Phone")));

        sb.AppendFormat(
            "<tr><td colspan=\"{0}\" class=\"gold\" style=\"padding:2px;\"></td></tr>\n",
            span);

        sb.AppendFormat(
            "<tr><td colspan=\"{0}\" style=\"padding:3px;\"></td></tr>\n",
            span);

        // Title
        sb.AppendFormat(
            "<tr><td colspan=\"{0}\" class=\"ttl\">CURRENT STOCK REPORT</td></tr>\n",
            span);

        sb.AppendFormat(
            "<tr><td colspan=\"{0}\" style=\"padding:2px;\"></td></tr>\n",
            span);

        // Filters: Period uses HTML numeric entity for the en dash.
        sb.AppendFormat("<tr><td colspan=\"{0}\">", span);

        sb.AppendFormat(
            "<span class=\"fl\">Warehouse: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;",
            H(whName));

        sb.AppendFormat(
            "<span class=\"fl\">Item: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;",
            H(itemName));

        sb.AppendFormat(
            "<span class=\"fl\">Period: </span><span class=\"fv\">{0}</span>",
            H(period));

        sb.AppendLine("</td></tr>");

        sb.AppendFormat(
            "<tr><td colspan=\"{0}\" style=\"padding:3px;\"></td></tr>\n",
            span);

        // Column headers
        sb.AppendLine("<tr>");

        foreach (var c in cols)
            sb.AppendFormat("<td class=\"th\">{0}</td>", H(c[0]));

        sb.AppendLine("</tr>");

        decimal totalStock = 0;
        decimal totalValue = 0;
        int ri = 0;

        // Data rows
        foreach (DataRow dr in dt.Rows)
        {
            ri++;

            object belowRaw =
                dt.Columns.Contains("BelowReorderLevel")
                ? dr["BelowReorderLevel"] : null;

            bool isLowStock =
                belowRaw != null &&
                belowRaw != DBNull.Value &&
                (belowRaw.ToString() == "1" ||
                 belowRaw.ToString().ToLower() == "true");

            string rowCls = isLowStock
                ? " low"
                : (ri % 2 == 1 ? "" : " alt");

            sb.AppendLine("<tr>");

            foreach (var c in cols)
            {
                string fld = c[1];

                if (fld == "#")
                {
                    sb.AppendFormat(
                        "<td class=\"tn{0}\">{1}</td>",
                        rowCls, ri);
                    continue;
                }

                if (fld == "BelowReorderLevel")
                {
                    string statusCls = isLowStock ? "ls" : "ok";
                    string statusTxt = isLowStock ? "Low Stock" : "OK";

                    sb.AppendFormat(
                        "<td class=\"td{0}\"><span class=\"{1}\">{2}</span></td>",
                        rowCls, statusCls, statusTxt);

                    continue;
                }

                object raw = dt.Columns.Contains(fld) ? dr[fld] : null;

                string txt =
                    raw != null && raw != DBNull.Value
                    ? raw.ToString()
                    : "";

                if (fld == "CurrentStock")
                {
                    decimal v;
                    if (decimal.TryParse(txt, out v))
                        totalStock += v;
                }

                if (fld == "StockValue")
                {
                    decimal v;
                    if (decimal.TryParse(txt, out v))
                        totalValue += v;
                }

                if (numFlds.Contains(fld))
                {
                    decimal num;
                    bool ok = decimal.TryParse(txt, out num);

                    string cls = ok && num < 0
                        ? "tnr" + rowCls
                        : "tn" + rowCls;

                    sb.AppendFormat(
                        "<td class=\"{0}\">{1}</td>",
                        cls,
                        ok ? num.ToString("N2") : "&mdash;");
                }
                else
                {
                    sb.AppendFormat(
                        "<td class=\"td{0}\">{1}</td>",
                        rowCls, H(txt));
                }
            }

            sb.AppendLine("</tr>");
        }

        // TOTALS: merge first seven columns into one horizontal cell.
        sb.AppendLine("<tr>");
        sb.AppendLine(
            "<td class=\"tot td\" colspan=\"7\">TOTALS</td>");

        for (int i = 7; i < cols.Length; i++)
        {
            string fld = cols[i][1];

            string val =
                fld == "CurrentStock"
                    ? totalStock.ToString("N2")
                : fld == "StockValue"
                    ? totalValue.ToString("N2")
                : "";

            sb.AppendFormat(
                "<td class=\"tot td\" style=\"text-align:right;\">{0}</td>",
                H(val));
        }

        sb.AppendLine("</tr>");

        // Footer
        sb.AppendFormat(
            "<tr><td colspan=\"{0}\" style=\"padding:4px;\"></td></tr>\n",
            span);

        sb.AppendFormat(
            "<tr><td colspan=\"{0}\" class=\"ftr\">Generated: {1}</td></tr>\n",
            span,
            DateTime.Now.ToString("dd MMM yyyy hh:mm tt"));

        sb.AppendLine("</table></body></html>");

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ============================================================
    // DATA ACCESS
    // ============================================================

    private DataTable RunSP(int whID, int itemID)
    {
        var dt = new DataTable();

        using (var conn = new SqlConnection(ConnStr()))
        using (var cmd = new SqlCommand("sp_CurrentStockReport", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 60;

            cmd.Parameters.AddWithValue("@WarehouseID", whID);
            cmd.Parameters.AddWithValue("@ItemID", itemID);

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
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@p", paramValue);

                conn.Open();
                object r = cmd.ExecuteScalar();

                return (r != null && r != DBNull.Value)
                    ? r.ToString()
                    : null;
            }
        }
        catch
        {
            return null;
        }
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private static string ConnStr()
    {
        return string.Format(
            "Server={0};Database={1};User Id={2};Password={3};",
            ConfigurationManager.AppSettings["Server"] ?? ".",
            ConfigurationManager.AppSettings["Database"] ?? "",
            ConfigurationManager.AppSettings["UserID"] ?? "",
            ConfigurationManager.AppSettings["Password"] ?? "");
    }

    private static string AppSettings(string key)
    {
        return ConfigurationManager.AppSettings[key] ?? "";
    }

    private static string FmtCell(object val, bool isNum)
    {
        if (val == null || val == DBNull.Value)
            return isNum ? "\u2014" : "";

        if (isNum)
        {
            decimal d;

            return decimal.TryParse(val.ToString(), out d)
                ? d.ToString("N2")
                : val.ToString();
        }

        return val.ToString();
    }

    private static string H(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";

        return s.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
    }

    private void SendError(string msg)
    {
        Response.Clear();
        Response.ContentType = "text/html";

        Response.Write(
            "<html><body style='font-family:Arial;padding:30px'>" +
            "<h3 style='color:#c62828'>Current Stock Report</h3>" +
            "<p style='color:#333'>" +
            Server.HtmlEncode(msg) +
            "</p></body></html>");

        Response.End();
    }

    private void LogErr(string src, string msg)
    {
        try
        {
            string path = Server.MapPath("~/ReportParamLog.txt");

            string entry = string.Format(
                "[{0}] ERROR in {1}: {2}{3}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                src,
                msg,
                Environment.NewLine);

            System.IO.File.AppendAllText(path, entry);
        }
        catch
        {
        }
    }

    private static void P(
        Document doc,
        string text,
        Font font,
        int align,
        float spacingAfter)
    {
        doc.Add(new Paragraph(text, font)
        {
            Alignment = align,
            SpacingAfter = spacingAfter
        });
    }

    private static Paragraph Spacer(float h)
    {
        return new Paragraph(" ")
        {
            SpacingAfter = h
        };
    }

    // ============================================================
    // PAGE FOOTER EVENT
    // ============================================================

    private class FooterEvent : PdfPageEventHelper
    {
        private readonly string _user;
        private readonly iTextSharp.text.Font _font;
        private readonly BaseColor _navy;

        public FooterEvent(
            string user,
            iTextSharp.text.Font font,
            BaseColor navy)
        {
            _user = user;
            _font = font;
            _navy = navy;
        }

        public override void OnEndPage(PdfWriter writer, Document doc)
        {
            var cb = writer.DirectContent;
            var pg = doc.PageSize;

            cb.SetLineWidth(0.5f);
            cb.SetColorStroke(new BaseColor(0xcc, 0xcc, 0xcc));

            cb.MoveTo(doc.LeftMargin, doc.BottomMargin - 5f);
            cb.LineTo(
                pg.Width - doc.RightMargin,
                doc.BottomMargin - 5f);
            cb.Stroke();

            string left = "Generated by: " + _user + "   |   " +
                DateTime.Now.ToString("dd MMM yyyy hh:mm tt");

            ColumnText.ShowTextAligned(
                cb,
                Element.ALIGN_LEFT,
                new Phrase(left, _font),
                doc.LeftMargin,
                doc.BottomMargin - 15f,
                0);

            ColumnText.ShowTextAligned(
                cb,
                Element.ALIGN_RIGHT,
                new Phrase("Page " + writer.PageNumber, _font),
                pg.Width - doc.RightMargin,
                doc.BottomMargin - 15f,
                0);
        }
    }

</script>