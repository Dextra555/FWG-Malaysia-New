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
    // OBMS Item-wise & Category-wise Stock Report — PDF / Excel
    // Single-file inline C# (no code-behind) — same pattern as StockIssueListReport.aspx
    // Flow: Angular → ASPX → sp_ItemCategoryStockReport → PDF / Excel → Browser
    //
    // Columns: No, Category, Item Code, Item Name, UOM, Warehouse,
    //          Total In, Total Out, Closing Balance, Avg Unit Cost, Closing Value
    //
    // PDF is grouped by Category with a sub-total row per category.
    // ════════════════════════════════════════════════════════════════════════

    protected void Page_Load(object sender, EventArgs e)
    {
        // ── 1. Read query-string params ───────────────────────────────────
        string loginID       = (Request.QueryString["LoginID"]      ?? "").Trim();
        string warehouseIDStr= (Request.QueryString["WarehouseID"]  ?? "0").Trim();
        string categoryIDStr = (Request.QueryString["CategoryID"]   ?? "0").Trim();
        string itemIDStr     = (Request.QueryString["ItemID"]       ?? "0").Trim();
        string asOfDateStr   = (Request.QueryString["AsOfDate"]     ?? "").Trim();
        string format        = (Request.QueryString["format"]       ?? "pdf").ToLower().Trim();

        // ── 2. Validate date ──────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(asOfDateStr))
            asOfDateStr = DateTime.Today.ToString("yyyy-MM-dd");

        DateTime asOfDate;
        if (!DateTime.TryParse(asOfDateStr, out asOfDate))
        { SendError("Please select a valid date."); return; }

        int warehouseID = 0; int.TryParse(warehouseIDStr, out warehouseID);
        int categoryID  = 0; int.TryParse(categoryIDStr,  out categoryID);
        int itemID      = 0; int.TryParse(itemIDStr,      out itemID);

        // ── 3. Execute stored procedure ───────────────────────────────────
        DataTable dt;
        try   { dt = RunSP(warehouseID, categoryID, itemID, asOfDate); }
        catch (Exception ex)
        {
            LogErr("sp_ItemCategoryStockReport", ex.Message);
            SendError("Unable to retrieve stock data. Please try again.");
            return;
        }

        if (dt == null || dt.Rows.Count == 0)
        { SendError("No stock data found for the selected filters."); return; }

        // ── 4. Resolve display names for filter header ────────────────────
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
                "attachment; filename=ItemCategoryStock_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls");
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
            "inline; filename=ItemCategoryStock_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
        Response.AddHeader("Content-Length", pdf.Length.ToString());
        Response.AddHeader("Access-Control-Allow-Origin", "*");
        Response.BinaryWrite(pdf);
        Response.Flush();
        Response.End();
    }

    // ════════════════════════════════════════════════════════════════════════
    // PDF BUILDER  (grouped by Category, sub-total per category)
    // ════════════════════════════════════════════════════════════════════════

    private byte[] BuildPdf(string loginID, string whName, string catName,
                            string itmName, string asOf, DataTable dt)
    {
        // ── Colours ────────────────────────────────────────────────────────
        BaseColor navy     = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor headerBg = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor rowAlt   = new BaseColor(0xf4, 0xf7, 0xfb);
        BaseColor subTotBg = new BaseColor(0xd0, 0xe4, 0xff);
        BaseColor totalBg  = new BaseColor(0xe8, 0xf0, 0xfe);
        BaseColor catHdrBg = new BaseColor(0x2e, 0x5c, 0x99);
        BaseColor border   = new BaseColor(0xcc, 0xcc, 0xcc);
        BaseColor gold     = new BaseColor(0xD4, 0xA0, 0x17);

        // ── Fonts ──────────────────────────────────────────────────────────
        Font fTitle   = new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD,   navy);
        Font fFilter  = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, new BaseColor(0x44,0x44,0x44));
        Font fFilterB = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   new BaseColor(0x44,0x44,0x44));
        Font fTH      = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   BaseColor.WHITE);
        Font fCatHdr  = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   BaseColor.WHITE);
        Font fTD      = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.NORMAL, BaseColor.BLACK);
        Font fTDRed   = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.BOLD,   new BaseColor(0xc6,0x28,0x28));
        Font fSubTot  = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
        Font fTotal   = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
        Font fFoot    = new Font(Font.FontFamily.HELVETICA,  7f, Font.NORMAL, BaseColor.GRAY);

        // ── Column definitions: header, fieldName, relWidth, isNumeric ─────
        var cols = new object[][]
        {
            new object[]{ "S.NO.",             "#",              1.0f,  true  },
            new object[]{ "Item Code",      "ItemCode",       1.6f,  false },
            new object[]{ "Item Name",      "ItemName",       3.8f,  false },
            new object[]{ "UOM",            "UOMName",        1.0f,  false },
            new object[]{ "Warehouse",      "WarehouseName",  2.2f,  false },
            new object[]{ "Total In",       "TotalIn",        1.6f,  true  },
            new object[]{ "Total Out",      "TotalOut",       1.6f,  true  },
            new object[]{ "Closing Bal.",   "ClosingBalance", 1.6f,  true  },
            new object[]{ "Avg Cost",       "AvgUnitCost",    1.8f,  true  },
            new object[]{ "Closing Value",  "ClosingValue",   2.0f,  true  },
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
            P(doc, "ITEM-WISE & CATEGORY-WISE STOCK REPORT", fTitle, Element.ALIGN_CENTER, 6f);

            // ── Filter summary ─────────────────────────────────────────────
            var ft = new PdfPTable(2) { WidthPercentage = 100f, SpacingAfter = 8f };
            ft.SetWidths(new float[] { 1f, 1f });
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

            // ── Data rows grouped by Category ──────────────────────────────
            int     riGlobal       = 0;
            decimal grandTotalIn   = 0;
            decimal grandTotalOut  = 0;
            decimal grandBalance   = 0;
            decimal grandValue     = 0;
            string  currentCat     = null;
            int     catRowStart    = 0;
            decimal catTotalIn     = 0;
            decimal catTotalOut    = 0;
            decimal catBalance     = 0;
            decimal catValue       = 0;

            // Count label columns (leading non-numeric) for subtotal colspan
            int labelSpan = 0;
            for (int i = 0; i < cols.Length; i++)
            {
                bool   isNum = (bool)cols[i][3];
                string fld   = (string)cols[i][1];
                if (!isNum || fld == "#") labelSpan++;
                else break;
            }
            if (labelSpan < 1) labelSpan = 1;

            foreach (DataRow dr in dt.Rows)
            {
                string thisCat = dr["CategoryName"] != DBNull.Value ? dr["CategoryName"].ToString() : "Uncategorised";

                // ── Category header row when category changes ──────────────
                if (currentCat != null && currentCat != thisCat)
                {
                    // Sub-total row for the previous category
                    AddSubTotalRow(tbl, cols, labelSpan,
                        "Sub-Total: " + currentCat,
                        catTotalIn, catTotalOut, catBalance, catValue,
                        subTotBg, fSubTot, border, navy);
                    catTotalIn = catTotalOut = catBalance = catValue = 0;
                }

                if (currentCat != thisCat)
                {
                    // Category header spanning full width
                    tbl.AddCell(new PdfPCell(new Phrase(thisCat, fCatHdr))
                    {
                        Colspan             = cols.Length,
                        BackgroundColor     = catHdrBg,
                        HorizontalAlignment = Element.ALIGN_LEFT,
                        Padding = 4f, Border = Rectangle.BOX, BorderColor = border
                    });
                    currentCat  = thisCat;
                    catRowStart = riGlobal + 1;
                }

                riGlobal++;
                BaseColor rowBg = (riGlobal % 2 == 0) ? rowAlt : BaseColor.WHITE;

                foreach (var c in cols)
                {
                    string field = (string)c[1];
                    bool   isNum = (bool)c[3];
                    Font   cf    = fTD;
                    string txt;

                    if (field == "#")
                    {
                        txt = riGlobal.ToString();
                    }
                    else
                    {
                        object raw = dt.Columns.Contains(field) ? dr[field] : null;
                        txt = FmtCell(raw, isNum);

                        if (isNum && raw != null && raw != DBNull.Value)
                        {
                            decimal dv;
                            if (decimal.TryParse(raw.ToString(), out dv) && dv < 0) cf = fTDRed;
                        }

                        // Accumulate
                        if (raw != null && raw != DBNull.Value)
                        {
                            decimal v;
                            if (decimal.TryParse(raw.ToString(), out v))
                            {
                                if (field == "TotalIn")       { catTotalIn  += v; grandTotalIn  += v; }
                                if (field == "TotalOut")      { catTotalOut += v; grandTotalOut += v; }
                                if (field == "ClosingBalance"){ catBalance  += v; grandBalance  += v; }
                                if (field == "ClosingValue")  { catValue    += v; grandValue    += v; }
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

            // Sub-total row for the last category
            if (currentCat != null)
            {
                AddSubTotalRow(tbl, cols, labelSpan,
                    "Sub-Total: " + currentCat,
                    catTotalIn, catTotalOut, catBalance, catValue,
                    subTotBg, fSubTot, border, navy);
            }

            // ── Grand total row ────────────────────────────────────────────
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
                             : field == "ClosingBalance" ? grandBalance.ToString("N2")
                             : field == "ClosingValue"   ? grandValue.ToString("N2")
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

    // Helper: adds a sub-total row for a category
    private static void AddSubTotalRow(
        PdfPTable tbl, object[][] cols, int labelSpan,
        string label,
        decimal totalIn, decimal totalOut, decimal balance, decimal value,
        BaseColor bg, Font font, BaseColor border, BaseColor navy)
    {
        tbl.AddCell(new PdfPCell(new Phrase(label, font))
        {
            Colspan             = labelSpan,
            BackgroundColor     = bg,
            HorizontalAlignment = Element.ALIGN_LEFT,
            Padding = 4f, Border = Rectangle.BOX, BorderColor = navy, BorderWidth = 1f
        });
        for (int i = labelSpan; i < cols.Length; i++)
        {
            string field = (string)cols[i][1];
            bool   isNum = (bool)cols[i][3];
            string txt   = field == "TotalIn"        ? totalIn.ToString("N2")
                         : field == "TotalOut"       ? totalOut.ToString("N2")
                         : field == "ClosingBalance" ? balance.ToString("N2")
                         : field == "ClosingValue"   ? value.ToString("N2")
                         : "";
            tbl.AddCell(new PdfPCell(new Phrase(txt, font))
            {
                BackgroundColor     = bg,
                HorizontalAlignment = isNum ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT,
                Padding = 4f, Border = Rectangle.BOX, BorderColor = navy, BorderWidth = 1f
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
            new[]{ "S.NO.",            "#"              },
            new[]{ "Category",      "CategoryName"   },
            new[]{ "Item Code",     "ItemCode"       },
            new[]{ "Item Name",     "ItemName"       },
            new[]{ "UOM",           "UOMName"        },
            new[]{ "Warehouse",     "WarehouseName"  },
            new[]{ "Total In",      "TotalIn"        },
            new[]{ "Total Out",     "TotalOut"       },
            new[]{ "Closing Bal.",  "ClosingBalance" },
            new[]{ "Avg Cost",      "AvgUnitCost"    },
            new[]{ "Closing Value", "ClosingValue"   },
        };

        var numFlds = new System.Collections.Generic.HashSet<string>
            { "TotalIn", "TotalOut", "ClosingBalance", "AvgUnitCost", "ClosingValue" };

        int span = cols.Length;
        var sb   = new System.Text.StringBuilder();

        sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" " +
                      "xmlns:x=\"urn:schemas-microsoft-com:office:excel\" " +
                      "xmlns=\"http://www.w3.org/TR/REC-html40\">");
        sb.AppendLine("<head><meta charset=\"UTF-8\"/>");
        sb.AppendLine("<!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets>" +
                      "<x:ExcelWorksheet><x:Name>Item Category Stock</x:Name>" +
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
        sb.AppendLine(".cath{background:#2e5c99;color:#fff;font-weight:bold;font-size:9pt;padding:4px;border:1px solid #ccc;}");
        sb.AppendLine(".td{font-size:8.5pt;padding:4px;border:1px solid #ddd;}");
        sb.AppendLine(".tn{font-size:8.5pt;padding:4px;border:1px solid #ddd;text-align:right;}");
        sb.AppendLine(".tnr{font-size:8.5pt;padding:4px;border:1px solid #ddd;text-align:right;color:#c62828;font-weight:bold;}");
        sb.AppendLine(".alt{background:#f4f7fb;}");
        sb.AppendLine(".stot{background:#d0e4ff;font-weight:bold;border-top:1px solid #1a3a5c;}");
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

        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"ttl\">ITEM-WISE &amp; CATEGORY-WISE STOCK REPORT</td></tr>\n", span);
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

        // Data rows grouped by category
        string  curCat     = null;
        int     ri         = 0;
        decimal grandIn    = 0, grandOut = 0, grandBal = 0, grandVal = 0;
        decimal catIn      = 0, catOut   = 0, catBal   = 0, catVal   = 0;

        // Label span for subtotal (non-numeric leading cols including #)
        int xlsLabel = 0;
        for (int i = 0; i < cols.Length; i++)
        {
            if (!numFlds.Contains(cols[i][1])) xlsLabel++;
            else break;
        }
        if (xlsLabel < 1) xlsLabel = 1;

        foreach (DataRow dr in dt.Rows)
        {
            string thisCat = dr["CategoryName"] != DBNull.Value ? dr["CategoryName"].ToString() : "Uncategorised";

            if (curCat != null && curCat != thisCat)
            {
                // Sub-total row
                sb.AppendLine("<tr>");
                sb.AppendFormat("<td class=\"stot td\" colspan=\"{0}\"><b>Sub-Total: {1}</b></td>", xlsLabel, H(curCat));
                for (int i = xlsLabel; i < cols.Length; i++)
                {
                    string fld = cols[i][1];
                    string v   = fld == "TotalIn"        ? catIn.ToString("N2")
                               : fld == "TotalOut"       ? catOut.ToString("N2")
                               : fld == "ClosingBalance" ? catBal.ToString("N2")
                               : fld == "ClosingValue"   ? catVal.ToString("N2") : "";
                    sb.AppendFormat("<td class=\"stot tn\">{0}</td>", H(v));
                }
                sb.AppendLine("</tr>");
                catIn = catOut = catBal = catVal = 0;
            }

            if (curCat != thisCat)
            {
                // Category header row
                sb.AppendFormat("<tr><td class=\"cath\" colspan=\"{0}\">{1}</td></tr>\n", span, H(thisCat));
                curCat = thisCat;
            }

            ri++;
            string alt = (ri % 2 == 1) ? "" : " alt";
            sb.AppendLine("<tr>");
            foreach (var c in cols)
            {
                string fld = c[1];
                if (fld == "#") { sb.AppendFormat("<td class=\"tn{0}\">{1}</td>", alt, ri); continue; }

                object raw = dt.Columns.Contains(fld) ? dr[fld] : null;
                string txt = raw != null && raw != DBNull.Value ? raw.ToString() : "";

                if (fld == "TotalIn")        { decimal v; if (decimal.TryParse(txt, out v)) { catIn  += v; grandIn  += v; } }
                if (fld == "TotalOut")       { decimal v; if (decimal.TryParse(txt, out v)) { catOut += v; grandOut += v; } }
                if (fld == "ClosingBalance") { decimal v; if (decimal.TryParse(txt, out v)) { catBal += v; grandBal += v; } }
                if (fld == "ClosingValue")   { decimal v; if (decimal.TryParse(txt, out v)) { catVal += v; grandVal += v; } }

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

        // Last category sub-total
        if (curCat != null)
        {
            sb.AppendLine("<tr>");
            sb.AppendFormat("<td class=\"stot td\" colspan=\"{0}\"><b>Sub-Total: {1}</b></td>", xlsLabel, H(curCat));
            for (int i = xlsLabel; i < cols.Length; i++)
            {
                string fld = cols[i][1];
                string v   = fld == "TotalIn"        ? catIn.ToString("N2")
                           : fld == "TotalOut"       ? catOut.ToString("N2")
                           : fld == "ClosingBalance" ? catBal.ToString("N2")
                           : fld == "ClosingValue"   ? catVal.ToString("N2") : "";
                sb.AppendFormat("<td class=\"stot tn\">{0}</td>", H(v));
            }
            sb.AppendLine("</tr>");
        }

        // Grand total
        sb.AppendLine("<tr>");
        sb.AppendFormat("<td class=\"tot td\" colspan=\"{0}\"><b>GRAND TOTAL</b></td>", xlsLabel);
        for (int i = xlsLabel; i < cols.Length; i++)
        {
            string fld   = cols[i][1];
            bool   isNum = numFlds.Contains(fld);
            string val   = fld == "TotalIn"        ? grandIn.ToString("N2")
                         : fld == "TotalOut"       ? grandOut.ToString("N2")
                         : fld == "ClosingBalance" ? grandBal.ToString("N2")
                         : fld == "ClosingValue"   ? grandVal.ToString("N2") : "";
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
        using (var cmd  = new SqlCommand("sp_ItemCategoryStockReport", conn))
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

    private static string H(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;");
    }

    private void SendError(string msg)
    {
        Response.Clear(); Response.ContentType = "text/html";
        Response.Write("<html><body style='font-family:Arial;padding:30px'>" +
            "<h3 style='color:#c62828'>Item-wise &amp; Category-wise Stock Report</h3>" +
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
