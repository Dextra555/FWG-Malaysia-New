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
    // OBMS Warehouse-wise Stock Report — PDF / Excel
    // Single-file inline C# (no code-behind) — same pattern as StockInGRNReport.aspx
    // Flow: Angular → ASPX → sp_WarehouseStockReport → PDF / Excel → Browser
    //
    // Columns: No, Warehouse, Category, Item Code, Item Name, Brand, UOM,
    //          Total In, Total Out, Closing Balance, Reorder Level,
    //          Last Unit Cost, Stock Value
    // Special: Warehouse group header row (shaded) when warehouse changes
    // ════════════════════════════════════════════════════════════════════════

    protected void Page_Load(object sender, EventArgs e)
    {
        // ── 1. Read query-string params ───────────────────────────────────
        string loginID        = (Request.QueryString["LoginID"]      ?? "").Trim();
        string warehouseIDStr = (Request.QueryString["WarehouseID"]  ?? "0").Trim();
        string categoryIDStr  = (Request.QueryString["CategoryID"]   ?? "0").Trim();
        string itemIDStr      = (Request.QueryString["ItemID"]       ?? "0").Trim();
        string asOfDateStr    = (Request.QueryString["AsOfDate"]     ?? "").Trim();
        string format         = (Request.QueryString["format"]       ?? "pdf").ToLower().Trim();

        // ── 2. Parse AsOfDate (default = today) ───────────────────────────
        DateTime asOfDate = DateTime.Today;
        if (!string.IsNullOrWhiteSpace(asOfDateStr))
            DateTime.TryParse(asOfDateStr, out asOfDate);

        int warehouseID = 0; int.TryParse(warehouseIDStr, out warehouseID);
        int categoryID  = 0; int.TryParse(categoryIDStr,  out categoryID);
        int itemID      = 0; int.TryParse(itemIDStr,      out itemID);

        // ── 3. Execute stored procedure ───────────────────────────────────
        DataTable dt;
        try   { dt = RunSP(warehouseID, categoryID, itemID, asOfDate); }
        catch (Exception ex)
        {
            LogErr("sp_WarehouseStockReport", ex.Message);
            SendError("Unable to retrieve warehouse stock data. Please try again.");
            return;
        }

        if (dt == null || dt.Rows.Count == 0)
        { SendError("No stock data found for the selected filters."); return; }

        // ── 4. Resolve display names ──────────────────────────────────────
        string whName  = warehouseID > 0
            ? (Lookup("SELECT Name FROM WarehouseMaster WHERE ID=@p", warehouseID.ToString()) ?? warehouseIDStr)
            : "All Warehouses";
        string catName = categoryID > 0
            ? (Lookup("SELECT Name FROM InventoryCategory WHERE ID=@p", categoryID.ToString()) ?? categoryIDStr)
            : "All Categories";
        string itmName = itemID > 0
            ? (Lookup("SELECT Name FROM ItemMaster WHERE ID=@p", itemID.ToString()) ?? itemIDStr)
            : "All Items";
        string asOf    = asOfDate.ToString("dd MMM yyyy");

        // ── 5. Build PDF or Excel ─────────────────────────────────────────
        if (format == "excel")
        {
            byte[] xlsx;
            try   { xlsx = BuildExcel(whName, catName, itmName, asOf, dt); }
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
                "attachment; filename=WarehouseStock_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls");
            Response.AddHeader("Content-Length", xlsx.Length.ToString());
            Response.AddHeader("Access-Control-Allow-Origin", "*");
            Response.BinaryWrite(xlsx);
            Response.Flush();
            Response.End();
            return;
        }

        // ── 5b. Build PDF (default) ───────────────────────────────────────
        byte[] pdf;
        try   { pdf = BuildPdf(loginID, whName, catName, itmName, asOf, dt); }
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
            "inline; filename=WarehouseStock_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
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
                            string itmName, string asOf, DataTable dt)
    {
        BaseColor navy      = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor headerBg  = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor rowAlt    = new BaseColor(0xf4, 0xf7, 0xfb);
        BaseColor totalBg   = new BaseColor(0xe8, 0xf0, 0xfe);
        BaseColor whGroupBg = new BaseColor(0xd0, 0xd9, 0xea);   // warehouse group header
        BaseColor lowBg     = new BaseColor(0xff, 0xf3, 0xcd);   // amber — below reorder
        BaseColor border    = new BaseColor(0xcc, 0xcc, 0xcc);
        BaseColor gold      = new BaseColor(0xD4, 0xA0, 0x17);

        Font fTitle    = new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD,   navy);
        Font fFilter   = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, new BaseColor(0x44,0x44,0x44));
        Font fFilterB  = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   new BaseColor(0x44,0x44,0x44));
        Font fTH       = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   BaseColor.WHITE);
        Font fTD       = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.NORMAL, BaseColor.BLACK);
        Font fTDRed    = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.BOLD,   new BaseColor(0xc6,0x28,0x28));
        Font fGroup    = new Font(Font.FontFamily.HELVETICA,  9f, Font.BOLD,   navy);
        Font fTotal    = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
        Font fFoot     = new Font(Font.FontFamily.HELVETICA,  7f, Font.NORMAL, BaseColor.GRAY);

        using (var ms = new MemoryStream())
        {
            var doc    = new Document(PageSize.A4.Rotate(), 25f, 25f, 30f, 30f);
            var writer = PdfWriter.GetInstance(doc, ms);
            writer.PageEvent = new FooterEvent(loginID, fFoot, navy);
            doc.Open();

            // ── Company header ────────────────────────────────────────────
            string logoPath = Server.MapPath("~/images/logo-white.png");
            Font fCoName = new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD,   navy);
            Font fCoReg  = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
            Font fCoSub  = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, BaseColor.DARK_GRAY);

            var hdrTbl = new PdfPTable(2) { WidthPercentage = 100f, SpacingAfter = 0f };
            hdrTbl.SetWidths(new float[] { 1.2f, 8.8f });
            hdrTbl.DefaultCell.Border = Rectangle.NO_BORDER;

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
            hdrTbl.AddCell(logoCell);

            var txtPhrase = new Phrase();
            txtPhrase.Add(new Chunk(AppSettings("CompanyName") + " ", fCoName));
            txtPhrase.Add(new Chunk(AppSettings("Registration") + "\n", fCoReg));
            txtPhrase.Add(new Chunk(AppSettings("Address1") + "\n", fCoSub));
            txtPhrase.Add(new Chunk(AppSettings("Address2") + "\n", fCoSub));
            txtPhrase.Add(new Chunk(AppSettings("PostCode") + " " + AppSettings("City") + "\n", fCoSub));
            txtPhrase.Add(new Chunk(AppSettings("State") + "\n", fCoSub));
            txtPhrase.Add(new Chunk(AppSettings("Phone"), fCoSub));
            hdrTbl.AddCell(new PdfPCell(txtPhrase)
            {
                Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_MIDDLE,
                HorizontalAlignment = Element.ALIGN_LEFT, Padding = 4f, PaddingLeft = 8f
            });
            doc.Add(hdrTbl);

            doc.Add(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(2.5f, 100f, gold, Element.ALIGN_CENTER, -2f)));
            doc.Add(Spacer(6f));

            // ── Report title ──────────────────────────────────────────────
            P(doc, "WAREHOUSE-WISE STOCK REPORT", fTitle, Element.ALIGN_CENTER, 6f);

            // ── Filter summary — 2 rows × 2 cols ─────────────────────────
            // Row 1: Warehouse | Category
            // Row 2: Item      | Period
            var ft = new PdfPTable(2) { WidthPercentage = 100f, SpacingAfter = 8f };
            ft.SetWidths(new float[] { 1f, 2f });
            ft.DefaultCell.Border = Rectangle.NO_BORDER;
            string[][] filterPairs = new string[][]
            {
                new[]{ "Warehouse:", whName  },
                new[]{ "Category:",  catName },
                new[]{ "Item:",      itmName },
                new[]{ "Period:",     asOf    }
            };
            foreach (var f in filterPairs)
            {
                var ph = new Phrase();
                ph.Add(new Chunk(f[0] + " ", fFilterB));
                ph.Add(new Chunk(f[1], fFilter));
                ft.AddCell(new PdfPCell(ph) { Border = Rectangle.NO_BORDER, Padding = 2f });
            }
            doc.Add(ft);

            // ── Data table ────────────────────────────────────────────────
            // header, fieldName, relWidth, isNumeric
            var cols = new object[][]
            {
                new object[]{ "S.NO.",          "#",               1.2f,  true  },
                new object[]{ "Warehouse",     "WarehouseName",   2.8f,  false },
                new object[]{ "Category",      "CategoryName",    2.0f,  false },
                new object[]{ "Item Code",     "ItemCode",        1.6f,  false },
                new object[]{ "Item Name",     "ItemName",        4.0f,  false },
                new object[]{ "Brand",         "BrandName",       1.8f,  false },
                new object[]{ "UOM",           "UOMName",         1.0f,  false },
                new object[]{ "Total In",      "TotalIn",         1.6f,  true  },
                new object[]{ "Total Out",     "TotalOut",        1.6f,  true  },
                new object[]{ "Closing Bal",   "ClosingBalance",  1.8f,  true  },
                new object[]{ "Reorder Lvl",   "ReorderLevel",    1.6f,  true  },
                new object[]{ "Unit Cost",     "LastUnitCost",    1.8f,  true  },
                new object[]{ "Stock Value",   "StockValue",      2.0f,  true  },
            };

            float[] widths = new float[cols.Length];
            for (int i = 0; i < cols.Length; i++) widths[i] = (float)cols[i][2];

            var tbl = new PdfPTable(cols.Length) { WidthPercentage = 100f, HeaderRows = 1 };
            tbl.SetWidths(widths);

            // Header row
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

            // Data rows — with warehouse group subtotals
            int     ri              = 0;
            decimal grandTotalIn    = 0;
            decimal grandTotalOut   = 0;
            decimal grandClosing    = 0;
            decimal grandStockValue = 0;

            decimal whTotalIn    = 0;
            decimal whTotalOut   = 0;
            decimal whClosing    = 0;
            decimal whStockValue = 0;
            string  prevWH       = null;

            foreach (DataRow dr in dt.Rows)
            {
                string currentWH = dr["WarehouseName"].ToString();

                // ── Warehouse subtotal row when warehouse changes ──────────
                if (prevWH != null && currentWH != prevWH)
                {
                    AddWarehouseSubtotalRow(tbl, prevWH, cols, whTotalIn, whTotalOut,
                                            whClosing, whStockValue, fTotal, whGroupBg, navy);
                    whTotalIn = whTotalOut = whClosing = whStockValue = 0;
                }
                prevWH = currentWH;

                ri++;
                // Amber background for items below reorder level
                decimal closBal  = GetDecimal(dr, "ClosingBalance");
                decimal reorder  = GetDecimal(dr, "ReorderLevel");
                bool    isLow    = reorder > 0 && closBal <= reorder;
                BaseColor rowBg  = isLow ? lowBg : ((ri % 2 == 0) ? rowAlt : BaseColor.WHITE);

                foreach (var c in cols)
                {
                    string field = (string)c[1];
                    bool   isNum = (bool)c[3];
                    Font   cf   = fTD;
                    string txt;

                    if (field == "#")
                    {
                        txt = ri.ToString();
                    }
                    else
                    {
                        object raw = dt.Columns.Contains(field) ? dr[field] : null;
                        txt = FmtCell(raw, isNum);

                        if (isNum && raw != null && raw != DBNull.Value)
                        {
                            decimal dv;
                            if (decimal.TryParse(raw.ToString(), out dv) && dv < 0)
                                cf = fTDRed;
                        }

                        // Accumulate warehouse subtotals
                        if (raw != null && raw != DBNull.Value)
                        {
                            decimal v;
                            if (decimal.TryParse(raw.ToString(), out v))
                            {
                                if (field == "TotalIn")       { whTotalIn    += v; grandTotalIn    += v; }
                                if (field == "TotalOut")      { whTotalOut   += v; grandTotalOut   += v; }
                                if (field == "ClosingBalance"){ whClosing    += v; grandClosing    += v; }
                                if (field == "StockValue")    { whStockValue += v; grandStockValue += v; }
                            }
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

            // Last warehouse subtotal
            if (prevWH != null)
            {
                AddWarehouseSubtotalRow(tbl, prevWH, cols, whTotalIn, whTotalOut,
                                        whClosing, whStockValue, fTotal, whGroupBg, navy);
            }

            // ── Grand totals row ──────────────────────────────────────────
            int labelSpan = 0;
            for (int i = 0; i < cols.Length; i++)
            {
                bool   isNum = (bool)cols[i][3];
                string fld   = (string)cols[i][1];
                if (!isNum || fld == "#") labelSpan++;
                else break;
            }
            if (labelSpan < 1) labelSpan = 1;

            tbl.AddCell(new PdfPCell(new Phrase("GRAND TOTAL", fTotal))
            {
                Colspan             = labelSpan,
                BackgroundColor     = totalBg,
                HorizontalAlignment = Element.ALIGN_LEFT,
                Padding = 5f, Border = Rectangle.BOX, BorderColor = navy, BorderWidth = 1.5f
            });
            for (int i = labelSpan; i < cols.Length; i++)
            {
                string field = (string)cols[i][1];
                bool   isNum = (bool)cols[i][3];
                string txt   = field == "TotalIn"        ? grandTotalIn.ToString("N2")
                             : field == "TotalOut"       ? grandTotalOut.ToString("N2")
                             : field == "ClosingBalance" ? grandClosing.ToString("N2")
                             : field == "StockValue"     ? grandStockValue.ToString("N2")
                             : "";
                tbl.AddCell(new PdfPCell(new Phrase(txt, fTotal))
                {
                    BackgroundColor     = totalBg,
                    HorizontalAlignment = isNum ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT,
                    Padding = 5f, Border = Rectangle.BOX, BorderColor = navy, BorderWidth = 1.5f
                });
            }

            doc.Add(tbl);
            doc.Close();
            return ms.ToArray();
        }
    }

    // Warehouse subtotal row helper
    private static void AddWarehouseSubtotalRow(PdfPTable tbl, string whLabel,
        object[][] cols,
        decimal totalIn, decimal totalOut, decimal closing, decimal stockValue,
        Font fTotal, BaseColor bg, BaseColor navy)
    {
        BaseColor border = new BaseColor(0xaa, 0xbb, 0xcc);

        // Merge all non-numeric leading cols + # into one label cell
        int lSpan = 0;
        for (int i = 0; i < cols.Length; i++)
        {
            bool   isNum = (bool)cols[i][3];
            string fld   = (string)cols[i][1];
            if (!isNum || fld == "#") lSpan++;
            else break;
        }
        if (lSpan < 1) lSpan = 1;

        tbl.AddCell(new PdfPCell(new Phrase(whLabel + " — Subtotal", fTotal))
        {
            Colspan             = lSpan,
            BackgroundColor     = bg,
            HorizontalAlignment = Element.ALIGN_LEFT,
            Padding = 4f, Border = Rectangle.BOX, BorderColor = border, BorderWidth = 1f
        });

        for (int i = lSpan; i < cols.Length; i++)
        {
            string field = (string)cols[i][1];
            bool   isNum = (bool)cols[i][3];
            string txt   = field == "TotalIn"        ? totalIn.ToString("N2")
                         : field == "TotalOut"       ? totalOut.ToString("N2")
                         : field == "ClosingBalance" ? closing.ToString("N2")
                         : field == "StockValue"     ? stockValue.ToString("N2")
                         : "";
            tbl.AddCell(new PdfPCell(new Phrase(txt, fTotal))
            {
                BackgroundColor     = bg,
                HorizontalAlignment = isNum ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT,
                Padding = 4f, Border = Rectangle.BOX, BorderColor = border, BorderWidth = 1f
            });
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // EXCEL BUILDER
    // ════════════════════════════════════════════════════════════════════════

    private byte[] BuildExcel(string whName, string catName, string itmName,
                              string asOf, DataTable dt)
    {
        var cols = new string[][]
        {
            new[]{ "No",           "#"              },
            new[]{ "Warehouse",    "WarehouseName"  },
            new[]{ "Category",     "CategoryName"   },
            new[]{ "Item Code",    "ItemCode"       },
            new[]{ "Item Name",    "ItemName"       },
            new[]{ "Brand",        "BrandName"      },
            new[]{ "UOM",          "UOMName"        },
            new[]{ "Total In",     "TotalIn"        },
            new[]{ "Total Out",    "TotalOut"       },
            new[]{ "Closing Bal",  "ClosingBalance" },
            new[]{ "Reorder Lvl",  "ReorderLevel"   },
            new[]{ "Unit Cost",    "LastUnitCost"   },
            new[]{ "Stock Value",  "StockValue"     },
        };

        var numFlds = new System.Collections.Generic.HashSet<string>
            { "TotalIn", "TotalOut", "ClosingBalance", "ReorderLevel", "LastUnitCost", "StockValue" };

        int span = cols.Length;
        var sb   = new System.Text.StringBuilder();

        sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" " +
                      "xmlns:x=\"urn:schemas-microsoft-com:office:excel\" " +
                      "xmlns=\"http://www.w3.org/TR/REC-html40\">");
        sb.AppendLine("<head><meta charset=\"UTF-8\"/>");
        sb.AppendLine("<!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets>" +
                      "<x:ExcelWorksheet><x:Name>Warehouse Stock</x:Name>" +
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
        sb.AppendLine(".grp{background:#d0d9ea;font-weight:bold;}");
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

        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"ttl\">WAREHOUSE-WISE STOCK REPORT</td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:2px;\"></td></tr>\n", span);

        sb.AppendFormat("<tr><td colspan=\"{0}\">", span);
        sb.AppendFormat("<span class=\"fl\">Warehouse: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;", H(whName));
        sb.AppendFormat("<span class=\"fl\">Category: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;",  H(catName));
        sb.AppendFormat("<span class=\"fl\">Item: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;",      H(itmName));
        sb.AppendFormat("<span class=\"fl\">Period: </span><span class=\"fv\">{0}</span>",                       H(asOf));
        sb.AppendLine("</td></tr>");
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:3px;\"></td></tr>\n", span);

        // Column headers
        sb.AppendLine("<tr>");
        foreach (var c in cols)
            sb.AppendFormat("<td class=\"th\">{0}</td>", H(c[0]));
        sb.AppendLine("</tr>");

        // Data rows with warehouse subtotals
        decimal grandIn = 0, grandOut = 0, grandClose = 0, grandVal = 0;
        decimal whIn = 0, whOut = 0, whClose = 0, whVal = 0;
        string  prevWH = null;
        int ri = 0;

        foreach (DataRow dr in dt.Rows)
        {
            string currWH = dr["WarehouseName"].ToString();

            // Warehouse subtotal row
            if (prevWH != null && currWH != prevWH)
            {
                sb.AppendFormat("<tr><td class=\"grp\" colspan=\"{0}\">{1} &mdash; Subtotal</td>", 7, H(prevWH));
                sb.AppendFormat("<td class=\"grp tn\">{0}</td>", whIn.ToString("N2"));
                sb.AppendFormat("<td class=\"grp tn\">{0}</td>", whOut.ToString("N2"));
                sb.AppendFormat("<td class=\"grp tn\">{0}</td>", whClose.ToString("N2"));
                sb.AppendFormat("<td class=\"grp tn\"></td>"); // ReorderLevel
                sb.AppendFormat("<td class=\"grp tn\"></td>"); // LastUnitCost
                sb.AppendFormat("<td class=\"grp tn\">{0}</td>", whVal.ToString("N2"));
                sb.AppendLine("</tr>");
                whIn = whOut = whClose = whVal = 0;
            }
            prevWH = currWH;

            ri++;
            decimal closBal = GetDecimal(dr, "ClosingBalance");
            decimal reorder = GetDecimal(dr, "ReorderLevel");
            bool    isLow   = reorder > 0 && closBal <= reorder;
            string  alt     = isLow ? " low" : (ri % 2 == 1 ? "" : " alt");

            sb.AppendLine("<tr>");
            foreach (var c in cols)
            {
                string fld = c[1];
                if (fld == "#") { sb.AppendFormat("<td class=\"tn{0}\">{1}</td>", alt, ri); continue; }

                object raw = dt.Columns.Contains(fld) ? dr[fld] : null;
                string txt = raw != null && raw != DBNull.Value ? raw.ToString() : "";

                if (fld == "TotalIn")       { decimal v; if (decimal.TryParse(txt, out v)) { whIn    += v; grandIn    += v; } }
                if (fld == "TotalOut")      { decimal v; if (decimal.TryParse(txt, out v)) { whOut   += v; grandOut   += v; } }
                if (fld == "ClosingBalance"){ decimal v; if (decimal.TryParse(txt, out v)) { whClose += v; grandClose += v; } }
                if (fld == "StockValue")    { decimal v; if (decimal.TryParse(txt, out v)) { whVal   += v; grandVal   += v; } }

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

        // Last warehouse subtotal
        if (prevWH != null)
        {
            sb.AppendFormat("<tr><td class=\"grp\" colspan=\"{0}\">{1} &mdash; Subtotal</td>", 7, H(prevWH));
            sb.AppendFormat("<td class=\"grp tn\">{0}</td>", whIn.ToString("N2"));
            sb.AppendFormat("<td class=\"grp tn\">{0}</td>", whOut.ToString("N2"));
            sb.AppendFormat("<td class=\"grp tn\">{0}</td>", whClose.ToString("N2"));
            sb.AppendFormat("<td class=\"grp tn\"></td>");
            sb.AppendFormat("<td class=\"grp tn\"></td>");
            sb.AppendFormat("<td class=\"grp tn\">{0}</td>", whVal.ToString("N2"));
            sb.AppendLine("</tr>");
        }

        // Grand total row
        sb.AppendLine("<tr>");
        int xlsLabelSpan = 0;
        for (int i = 0; i < cols.Length; i++)
        {
            if (!numFlds.Contains(cols[i][1])) xlsLabelSpan++;
            else break;
        }
        if (xlsLabelSpan < 1) xlsLabelSpan = 1;
        sb.AppendFormat("<td class=\"tot td\" colspan=\"{0}\"><b>GRAND TOTAL</b></td>", xlsLabelSpan);
        for (int i = xlsLabelSpan; i < cols.Length; i++)
        {
            string fld   = cols[i][1];
            bool   isNum = numFlds.Contains(fld);
            string val   = fld == "TotalIn"        ? grandIn.ToString("N2")
                         : fld == "TotalOut"       ? grandOut.ToString("N2")
                         : fld == "ClosingBalance" ? grandClose.ToString("N2")
                         : fld == "StockValue"     ? grandVal.ToString("N2") : "";
            string align = isNum ? " style=\"text-align:right;\"" : "";
            sb.AppendFormat("<td class=\"tot td\"{0}>{1}</td>", align, H(val));
        }
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

    private DataTable RunSP(int whID, int catID, int itemID, DateTime asOf)
    {
        var dt = new DataTable();
        using (var conn = new SqlConnection(ConnStr()))
        using (var cmd  = new SqlCommand("sp_WarehouseStockReport", conn))
        {
            cmd.CommandType    = CommandType.StoredProcedure;
            cmd.CommandTimeout = 60;
            cmd.Parameters.AddWithValue("@WarehouseID", whID);
            cmd.Parameters.AddWithValue("@CategoryID",  catID);
            cmd.Parameters.AddWithValue("@ItemID",      itemID);
            cmd.Parameters.AddWithValue("@AsOfDate",    asOf.Date);
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

    private static decimal GetDecimal(DataRow dr, string col)
    {
        if (dr == null || !dr.Table.Columns.Contains(col) || dr[col] == DBNull.Value) return 0m;
        decimal v; decimal.TryParse(dr[col].ToString(), out v); return v;
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
            "<h3 style='color:#c62828'>Warehouse-wise Stock Report</h3>" +
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
