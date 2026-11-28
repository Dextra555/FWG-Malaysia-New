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
    // OBMS Inventory Stock Ledger PDF Report
    // Style: matches CurrentStockReport.aspx (company header, gold line, footer)
    // Columns: Date | Type | Item Code | Item Name | UOM |
    //          In Qty | Out Qty | Balance | Unit Cost | Ref No | Remarks
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
        string categoryIDStr   = (Request.QueryString["CategoryID"]      ?? "0").Trim();
        string sizeFilter      = (Request.QueryString["Size"]            ?? "").Trim();
        string format          = (Request.QueryString["format"]          ?? "pdf").ToLower().Trim();

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

        int categoryID = 0;
        int.TryParse(categoryIDStr, out categoryID);

        // ── 2. Execute stored procedure ───────────────────────────────────
        DataTable dt;
        try   { dt = RunSP(warehouseID, itemID, transactionType, startDate, endDate, categoryID, sizeFilter); }
        catch (Exception ex)
        {
            LogErr("sp_InventoryStockLedger", ex.Message);
            SendError("Unable to retrieve stock ledger data. Please try again.");
            return;
        }

        if (dt == null || dt.Rows.Count == 0)
        { SendError("No stock ledger data found for the selected filters."); return; }

        // ── 3. Resolve display names ──────────────────────────────────────
        string whName      = Lookup("SELECT Name FROM WarehouseMaster WHERE ID=@p", warehouseID.ToString()) ?? warehouseIDStr;
        string itemName    = itemID > 0
            ? (Lookup("SELECT Name FROM ItemMaster WHERE ID=@p", itemID.ToString()) ?? itemIDStr)
            : "All Items";
        string catName     = categoryID > 0
            ? (Lookup("SELECT Name FROM InventoryCategory WHERE ID=@p", categoryID.ToString()) ?? categoryIDStr)
            : "All Categories";
        string typeLbl     = TypeLabel(transactionType);
        string sizeLbl     = string.IsNullOrEmpty(sizeFilter) ? "All Sizes" : sizeFilter;
        string period      = startDate.ToString("dd MMM yyyy") + " to " + endDate.ToString("dd MMM yyyy");

        // ── 4. Excel or PDF ───────────────────────────────────────────────
        if (format == "excel")
        {
            byte[] xlsx;
            try   { xlsx = BuildExcel(whName, itemName, catName, sizeLbl, typeLbl, period, dt); }
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

        byte[] pdf;
        try   { pdf = BuildPdf(loginID, whName, itemName, catName, sizeLbl, typeLbl, period, dt); }
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
            "inline; filename=StockLedger_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
        Response.AddHeader("Content-Length", pdf.Length.ToString());
        Response.AddHeader("Access-Control-Allow-Origin", "*");
        Response.BinaryWrite(pdf);
        Response.Flush();
        Response.End();
    }

    // ════════════════════════════════════════════════════════════════════════
    // PDF BUILDER  —  CurrentStockReport.aspx style
    // ════════════════════════════════════════════════════════════════════════

    private byte[] BuildPdf(string loginID, string whName, string itemName,
                            string catName, string sizeLbl,
                            string typeLbl, string period, DataTable dt)
    {
        // ── Colours (identical to CurrentStockReport) ────────────────────
        BaseColor navy     = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor headerBg = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor rowAlt   = new BaseColor(0xf4, 0xf7, 0xfb);
        BaseColor totalBg  = new BaseColor(0xe8, 0xf0, 0xfe);
        BaseColor border   = new BaseColor(0xcc, 0xcc, 0xcc);
        BaseColor gold     = new BaseColor(0xD4, 0xA0, 0x17);

        // ── Fonts ─────────────────────────────────────────────────────────
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
            writer.PageEvent = new FooterEvent(loginID, fFoot, navy);
            doc.Open();

            // ── Company header ────────────────────────────────────────────
            string coName = AppSettings("CompanyName");
            string addr1  = AppSettings("Address1");
            string addr2  = AppSettings("Address2");
            string addr3  = AppSettings("PostCode") + " " + AppSettings("City");
            string state  = AppSettings("State");
            string phone  = AppSettings("Phone");
            string reg    = AppSettings("Registration");

            string logoPath = Server.MapPath("~/images/logo-white.png");

            var hdrTable = new PdfPTable(2) { WidthPercentage = 100f, SpacingAfter = 0f };
            hdrTable.SetWidths(new float[] { 1.2f, 8.8f });
            hdrTable.DefaultCell.Border = Rectangle.NO_BORDER;

            // Logo cell
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
                logoCell = new PdfPCell(new Phrase("")) { Border = Rectangle.NO_BORDER };
            }
            hdrTable.AddCell(logoCell);

            // Company text cell
            Font fCoName = new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD,   BaseColor.DARK_GRAY);
            Font fCoReg  = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
            Font fCoSub  = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, BaseColor.DARK_GRAY);

            var textPhrase = new Phrase();
            textPhrase.Add(new Chunk(coName + " ", fCoName));
            textPhrase.Add(new Chunk(reg    + "\n", fCoReg));
            textPhrase.Add(new Chunk(addr1  + "\n", fCoSub));
            textPhrase.Add(new Chunk(addr2  + "\n", fCoSub));
            textPhrase.Add(new Chunk(addr3  + "\n", fCoSub));
            textPhrase.Add(new Chunk(state  + "\n", fCoSub));
            textPhrase.Add(new Chunk(phone,          fCoSub));

            hdrTable.AddCell(new PdfPCell(textPhrase)
            {
                Border              = Rectangle.NO_BORDER,
                VerticalAlignment   = Element.ALIGN_MIDDLE,
                HorizontalAlignment = Element.ALIGN_LEFT,
                Padding             = 4f,
                PaddingLeft         = 8f
            });
            doc.Add(hdrTable);

            // ── Gold separator line ───────────────────────────────────────
            var goldSep = new iTextSharp.text.pdf.draw.LineSeparator(2.5f, 100f, gold, Element.ALIGN_CENTER, -2f);
            doc.Add(new Chunk(goldSep));
            doc.Add(Spacer(6f));

            // ── Report title ──────────────────────────────────────────────
            P(doc, "INVENTORY STOCK LEDGER REPORT", fTitle, Element.ALIGN_CENTER, 6f);

            // ── Filter summary ────────────────────────────────────────────
            var filters = new string[][]
            {
                new[]{ "Warehouse:",        whName   },
                new[]{ "Item:",             itemName },
                new[]{ "Category:",         catName  },
                new[]{ "Size:",             sizeLbl  },
                new[]{ "Transaction Type:", typeLbl  },
                new[]{ "Period:",           period   }
            };
            var ft = new PdfPTable(3) { WidthPercentage = 100f, SpacingAfter = 8f };
            ft.SetWidths(new float[] { 1.5f, 3.5f, 5.0f });
            ft.DefaultCell.Border = Rectangle.NO_BORDER;
            foreach (var f in filters)
            {
                var ph = new Phrase();
                ph.Add(new Chunk(f[0] + " ", fFilterB));
                ph.Add(new Chunk(f[1], fFilter));
                ft.AddCell(new PdfPCell(ph) { Border = Rectangle.NO_BORDER, Padding = 2f, NoWrap = true });
            }
            // pad to complete the last row if needed (6 items → 2 rows of 3)
            doc.Add(ft);

            // ── Data table ────────────────────────────────────────────────
            // header text | field name | rel width | isNumeric | isDate
            // S.No is generated in the loop (not a DB field) — handled specially
            var cols = new object[][]
            {
                new object[]{ "S.No",       "SERNO",               0.8f, false, false },
                new object[]{ "Ref No",     "ReferenceNo",          1.8f, false, false },
                new object[]{ "Date",       "TransactionDate",      1.8f, false, true  },
                new object[]{ "Type",       "TransactionTypeLabel", 2.2f, false, false },
                new object[]{ "Category",   "CategoryName",         2.0f, false, false },
                new object[]{ "Item Code",  "ItemCode",             1.6f, false, false },
                new object[]{ "Item Name",  "ItemName",             3.5f, false, false },
                new object[]{ "Size",       "Size",                 1.0f, false, false },
                new object[]{ "UOM",        "UOMName",              1.0f, false, false },
                new object[]{ "In Qty",     "InQty",                1.5f, true,  false },
                new object[]{ "Out Qty",    "OutQty",               1.5f, true,  false },
                new object[]{ "Balance",    "RunningBalance",       1.5f, true,  false },
                new object[]{ "Unit Cost",  "UnitCost",             1.5f, true,  false },
                new object[]{ "Remarks",    "Remarks",              2.6f, false, false },
            };

            float[] widths = new float[cols.Length];
            for (int i = 0; i < cols.Length; i++) widths[i] = (float)cols[i][2];

            var tbl = new PdfPTable(cols.Length) { WidthPercentage = 100f, HeaderRows = 1 };
            tbl.SetWidths(widths);

            // Header row
            foreach (var c in cols)
            {
                bool num = (bool)c[3];
                tbl.AddCell(new PdfPCell(new Phrase((string)c[0], fTH))
                {
                    BackgroundColor     = headerBg,
                    HorizontalAlignment = num ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT,
                    Padding = 5f, Border = Rectangle.BOX, BorderColor = border
                });
            }

            // Data rows
            int ri = 0;
            decimal totalIn = 0, totalOut = 0;
            string pdfBaseUrl = GetAppBaseUrl();
            foreach (DataRow dr in dt.Rows)
            {
                var rowBg = (ri % 2 == 0) ? BaseColor.WHITE : rowAlt;

                // Per-row values needed for Ref No hyperlink
                string rowTxType = dt.Columns.Contains("TransactionType") && dr["TransactionType"] != DBNull.Value
                    ? dr["TransactionType"].ToString().ToUpper() : "";
                string rowRefId  = dt.Columns.Contains("ReferenceID") && dr["ReferenceID"] != DBNull.Value
                    ? dr["ReferenceID"].ToString() : "";

                foreach (var c in cols)
                {
                    string field  = (string)c[1];
                    bool   isNum  = (bool)c[3];
                    bool   isDate = (bool)c[4];

                    // S.No — generated, not a DB field
                    string txt;
                    if (field == "SERNO")
                    {
                        txt = (ri + 1).ToString();
                    }
                    else
                    {
                        object raw = dt.Columns.Contains(field) ? dr[field] : null;
                        txt = FmtCell(raw, isNum, isDate);

                        // Ref No → clickable hyperlink in PDF
                        if (field == "ReferenceNo" && !string.IsNullOrEmpty(txt) && !string.IsNullOrEmpty(pdfBaseUrl))
                        {
                            string navUrl = BuildNavUrl(pdfBaseUrl, rowTxType, rowRefId);
                            if (!string.IsNullOrEmpty(navUrl))
                            {
                                Font fLink = new Font(Font.FontFamily.HELVETICA, 7.5f, Font.UNDERLINE,
                                                      new BaseColor(0x0d, 0x6e, 0xfd));
                                var anchor = new Anchor(txt, fLink) { Reference = navUrl };
                                tbl.AddCell(new PdfPCell(anchor)
                                {
                                    BackgroundColor     = rowBg,
                                    HorizontalAlignment = Element.ALIGN_LEFT,
                                    Padding = 4f, Border = Rectangle.BOX, BorderColor = border
                                });
                                continue;
                            }
                        }

                        if (isNum && raw != null && raw != DBNull.Value)
                        { decimal dv; if (decimal.TryParse(raw.ToString(), out dv) && dv < 0)
                            { tbl.AddCell(new PdfPCell(new Phrase(txt, fTDRed))
                              { BackgroundColor = rowBg, HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 4f, Border = Rectangle.BOX, BorderColor = border }); continue; } }
                        if (field == "InQty"  && raw != null && raw != DBNull.Value) { decimal v; if (decimal.TryParse(raw.ToString(), out v)) totalIn  += v; }
                        if (field == "OutQty" && raw != null && raw != DBNull.Value) { decimal v; if (decimal.TryParse(raw.ToString(), out v)) totalOut += v; }
                    }

                    tbl.AddCell(new PdfPCell(new Phrase(txt, fTD))
                    {
                        BackgroundColor     = rowBg,
                        HorizontalAlignment = isNum ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT,
                        Padding = 4f, Border = Rectangle.BOX, BorderColor = border
                    });
                }
                ri++;
            }

            // Totals row — first 8 cols merged, then In Qty, Out Qty, rest blank
            tbl.AddCell(new PdfPCell(new Phrase("TOTALS", fTotal))
            {
                Colspan             = 8,
                BackgroundColor     = totalBg,
                HorizontalAlignment = Element.ALIGN_LEFT,
                VerticalAlignment   = Element.ALIGN_MIDDLE,
                Padding = 5f, Border = Rectangle.BOX, BorderColor = navy, BorderWidth = 1.5f,
                NoWrap  = true
            });
            for (int i = 8; i < cols.Length; i++)
            {
                string field = (string)cols[i][1];
                string txt   = field == "InQty"  ? totalIn.ToString("N2")
                             : field == "OutQty" ? totalOut.ToString("N2") : "";
                bool   isNum = (bool)cols[i][3];
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

    // ════════════════════════════════════════════════════════════════════════
    // EXCEL BUILDER
    // ════════════════════════════════════════════════════════════════════════

    private byte[] BuildExcel(string whName, string itemName,
                              string catName, string sizeLbl,
                              string typeLbl, string period, DataTable dt)
    {
        var cols = new string[][]
        {
            new[]{ "S.No",        "SERNO"                },
            new[]{ "Ref No",      "ReferenceNo"          },
            new[]{ "Date",        "TransactionDate"      },
            new[]{ "Type",        "TransactionTypeLabel" },
            new[]{ "Category",    "CategoryName"         },
            new[]{ "Item Code",   "ItemCode"             },
            new[]{ "Item Name",   "ItemName"             },
            new[]{ "Size",        "Size"                 },
            new[]{ "UOM",         "UOMName"              },
            new[]{ "In Qty",      "InQty"                },
            new[]{ "Out Qty",     "OutQty"               },
            new[]{ "Balance",     "RunningBalance"       },
            new[]{ "Unit Cost",   "UnitCost"             },
            new[]{ "Remarks",     "Remarks"              },
        };

        var numFlds = new HashSet<string> { "InQty", "OutQty", "RunningBalance", "UnitCost" };

        int span = cols.Length;
        var sb   = new System.Text.StringBuilder();

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
        sb.AppendLine(".co{font-size:13pt;font-weight:bold;color:#404040;}");
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

        // Title
        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"ttl\">INVENTORY STOCK LEDGER REPORT</td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:2px;\"></td></tr>\n", span);

        // Filters
        sb.AppendFormat("<tr><td colspan=\"{0}\">", span);
        sb.AppendFormat("<span class=\"fl\">Warehouse: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;", H(whName));
        sb.AppendFormat("<span class=\"fl\">Item: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;", H(itemName));
        sb.AppendFormat("<span class=\"fl\">Category: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;", H(catName));
        sb.AppendFormat("<span class=\"fl\">Size: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;", H(sizeLbl));
        sb.AppendFormat("<span class=\"fl\">Type: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;", H(typeLbl));
        sb.AppendFormat("<span class=\"fl\">Period: </span><span class=\"fv\">{0}</span>", H(period));
        sb.AppendLine("</td></tr>");
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:3px;\"></td></tr>\n", span);

        // Column headers
        sb.AppendLine("<tr>");
        foreach (var c in cols)
            sb.AppendFormat("<td class=\"th\">{0}</td>", H(c[0]));
        sb.AppendLine("</tr>");

        // Data rows
        decimal totalIn = 0, totalOut = 0;
        int ri = 0;
        foreach (DataRow dr in dt.Rows)
        {
            string alt = (ri % 2 == 1) ? " alt" : "";

            sb.AppendLine("<tr>");
            foreach (var c in cols)
            {
                string fld = c[1];

                // S.No — sequential counter, not a DB field
                if (fld == "SERNO")
                {
                    sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, ri + 1);
                    continue;
                }

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

                // Ref No — plain text only in Excel (no hyperlink)
                if (numFlds.Contains(fld))
                {
                    decimal num; bool ok = decimal.TryParse(txt, out num);
                    string cls = ok && num < 0 ? "tnr" + alt : "tn" + alt;
                    sb.AppendFormat("<td class=\"{0}\">{1}</td>", cls, ok ? num.ToString("N2") : "&mdash;");
                }
                else
                {
                    sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(txt));
                }
            }
            sb.AppendLine("</tr>");
            ri++;
        }

        // Totals row
        sb.AppendLine("<tr>");
        sb.AppendFormat("<td class=\"tot td\" colspan=\"8\">TOTALS</td>");
        for (int i = 5; i < cols.Length; i++)
        {
            string fld   = cols[i][1];
            bool   isNum = numFlds.Contains(fld);
            string val   = fld == "InQty"  ? totalIn.ToString("N2")
                         : fld == "OutQty" ? totalOut.ToString("N2") : "";
            string align = isNum ? " style=\"text-align:right;\"" : "";
            sb.AppendFormat("<td class=\"tot td\"{0}>{1}</td>", align, H(val));
        }
        sb.AppendLine("</tr>");

        // Footer
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:4px;\"></td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"ftr\">Generated: {1}</td></tr>\n",
            span, DateTime.Now.ToString("dd MMM yyyy hh:mm tt"));
        sb.AppendLine("</table></body></html>");

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ════════════════════════════════════════════════════════════════════════
    // DATA ACCESS
    // ════════════════════════════════════════════════════════════════════════

    private DataTable RunSP(int whID, int itemID, string txType,
                            DateTime from, DateTime to,
                            int categoryID, string sizeFilter)
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
            cmd.Parameters.AddWithValue("@CategoryID",      categoryID);
            cmd.Parameters.AddWithValue("@Size",
                string.IsNullOrWhiteSpace(sizeFilter) ? "" : sizeFilter.Trim());
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

    private static string H(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;");
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

    // ── Dynamic base URL ──────────────────────────────────────────────────
    // Derives the Angular UI URL from the current HTTP request:
    //   scheme + host (no IP config needed) + AppUIPort from Web.config
    // Web.config only needs the port number, not the IP address.
    // Example Web.config:
    //   <add key="AppUIPort" value="8644"/>   ← UAT
    //   <add key="AppUIPort" value="80"/>     ← Live
    private string GetAppBaseUrl()
    {
        string port = (ConfigurationManager.AppSettings["AppUIPort"] ?? "").Trim();
        if (string.IsNullOrEmpty(port)) return "";

        // Request.Url gives the current scheme + host (IP or domain) automatically
        var uri     = Request.Url;
        string host = uri.Host;   // e.g. 124.217.236.37 or obms.company.com
        string scheme = uri.Scheme; // http or https

        return string.Format("{0}://{1}:{2}", scheme, host, port).TrimEnd('/');
    }

    // Navigation URL: TransactionType → Angular route
    private static string BuildNavUrl(string baseUrl, string txType, string refId)
    {
        string seg = null;
        switch ((txType ?? "").ToUpper())
        {
            case "OPENING":      seg = "inventory/opening-stock/new-opening-stock";           break;
            case "STOCK_IN":     seg = "inventory/goods-receipt/new-grn";                     break;
            case "ISSUE":        seg = "inventory/stock-issue/new-stock-issue";               break;
            case "ISSUE_RETURN": seg = "inventory/stock-issue-return/new-stock-issue-return"; break;
            case "TRANSFER_IN":
            case "TRANSFER_OUT": seg = "inventory/stock-transfer/new-stock-transfer";         break;
            case "ADJ_PLUS":
            case "ADJ_MINUS":    seg = "inventory/stock-adjustment/new-stock-adjustment";     break;
            case "PURCHASE_RTN": seg = "inventory/purchase-return/new-return";                break;
            case "DAMAGE_LOSS":  seg = "inventory/stock-damage-loss/new-stock-damage-loss";   break;
        }
        if (seg == null) return "";
        if (!string.IsNullOrEmpty(refId)) return baseUrl + "/" + seg + "/" + refId;
        string listSeg = seg.Contains("/") ? seg.Substring(0, seg.LastIndexOf('/')) : seg;
        return baseUrl + "/" + listSeg;
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

    private static void P(Document doc, string text, Font font, int align, float spacingAfter)
    {
        doc.Add(new Paragraph(text, font) { Alignment = align, SpacingAfter = spacingAfter });
    }

    private static Paragraph Spacer(float h)
    {
        return new Paragraph(" ") { SpacingAfter = h };
    }

    // ════════════════════════════════════════════════════════════════════════
    // PAGE FOOTER EVENT  —  identical to CurrentStockReport
    // ════════════════════════════════════════════════════════════════════════

    private class FooterEvent : PdfPageEventHelper
    {
        private readonly string    _user;
        private readonly Font      _font;
        private readonly BaseColor _navy;

        public FooterEvent(string user, Font font, BaseColor navy)
        {
            _user = user;
            _font = font;
            _navy = navy;
        }

        public override void OnEndPage(PdfWriter writer, Document doc)
        {
            var cb = writer.DirectContent;
            var pg = doc.PageSize;

            // Footer separator line
            cb.SetLineWidth(0.5f);
            cb.SetColorStroke(new BaseColor(0xcc, 0xcc, 0xcc));
            cb.MoveTo(doc.LeftMargin, doc.BottomMargin - 5f);
            cb.LineTo(pg.Width - doc.RightMargin, doc.BottomMargin - 5f);
            cb.Stroke();

            // Left: generated by + date-time
            string left = "Generated by: " + _user + "   |   " +
                          DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
            ColumnText.ShowTextAligned(cb, Element.ALIGN_LEFT,
                new Phrase(left, _font), doc.LeftMargin, doc.BottomMargin - 15f, 0);

            // Right: page number
            string right = "Page " + writer.PageNumber;
            ColumnText.ShowTextAligned(cb, Element.ALIGN_RIGHT,
                new Phrase(right, _font), pg.Width - doc.RightMargin, doc.BottomMargin - 15f, 0);
        }
    }

</script>
<!DOCTYPE html>
<html>
<head><title>Stock Ledger Report</title></head>
<body></body>
</html>
