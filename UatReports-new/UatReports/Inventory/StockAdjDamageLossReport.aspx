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
    // OBMS Stock Adjustment / Damage / Loss Report — PDF / Excel
    // Single-file inline C# (no code-behind) — same pattern as StockIssueListReport.aspx
    // Flow: Angular → ASPX → sp_StockAdjDamageLossReport → PDF / Excel → Browser
    //
    // Combines: Adjustment(+), Adjustment(-), Damage, Loss
    // Columns: No, Type, Doc No, Doc Date, Warehouse,
    //          Item Code, Item Name, Category, UOM,
    //          Qty, Unit Cost, Total Value, Reason
    //
    // PDF: Transaction Type colour-coded:
    //   Adjustment(+) = green tint | Adjustment(-) = amber tint
    //   Damage/Loss   = red tint
    // ════════════════════════════════════════════════════════════════════════

    protected void Page_Load(object sender, EventArgs e)
    {
        // ── 1. Read query-string params ───────────────────────────────────
        string loginID       = (Request.QueryString["LoginID"]      ?? "").Trim();
        string warehouseStr  = (Request.QueryString["WarehouseID"]  ?? "0").Trim();
        string transType     = (Request.QueryString["TransType"]    ?? "ALL").Trim();
        string itemIDStr     = (Request.QueryString["ItemID"]       ?? "0").Trim();
        string startDateStr  = (Request.QueryString["StartDate"]    ?? "").Trim();
        string endDateStr    = (Request.QueryString["EndDate"]      ?? "").Trim();
        string format        = (Request.QueryString["format"]       ?? "pdf").ToLower().Trim();

        // ── 2. Validate dates ─────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(startDateStr) || string.IsNullOrWhiteSpace(endDateStr))
        { SendError("Please select a valid date range."); return; }

        DateTime startDate, endDate;
        if (!DateTime.TryParse(startDateStr, out startDate) ||
            !DateTime.TryParse(endDateStr,   out endDate))
        { SendError("Please select a valid date range."); return; }

        if (startDate.Date > endDate.Date)
        { SendError("Start date must be before end date."); return; }

        int warehouseID = 0; int.TryParse(warehouseStr, out warehouseID);
        int itemID      = 0; int.TryParse(itemIDStr,    out itemID);
        if (string.IsNullOrWhiteSpace(transType)) transType = "ALL";

        // ── 3. Execute stored procedure ───────────────────────────────────
        DataTable dt;
        try   { dt = RunSP(warehouseID, transType, itemID, startDate, endDate); }
        catch (Exception ex)
        {
            LogErr("sp_StockAdjDamageLossReport", ex.Message);
            SendError("Unable to retrieve data. Please try again.");
            return;
        }

        if (dt == null || dt.Rows.Count == 0)
        { SendError("No data found for the selected filters."); return; }

        // ── 4. Resolve display names ──────────────────────────────────────
        string whName    = warehouseID > 0
            ? (Lookup("SELECT Name FROM WarehouseMaster WHERE ID=@p", warehouseID.ToString()) ?? warehouseStr)
            : "All Warehouses";
        string itmName   = itemID > 0
            ? (Lookup("SELECT Name FROM ItemMaster WHERE ID=@p", itemID.ToString()) ?? itemIDStr)
            : "All Items";
        string typeLabel = transType == "ALL"       ? "All Types"
                         : transType == "ADJ_PLUS"  ? "Adjustment (+)"
                         : transType == "ADJ_MINUS" ? "Adjustment (-)"
                         : transType == "DAMAGE"    ? "Damage"
                         : transType == "LOSS"      ? "Loss"
                         : transType;
        string period    = startDate.ToString("dd MMM yyyy") + " to " + endDate.ToString("dd MMM yyyy");

        // ── 5. Build PDF or Excel ─────────────────────────────────────────
        if (format == "excel")
        {
            byte[] xlsx;
            try   { xlsx = BuildExcel(whName, typeLabel, itmName, period, dt); }
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
                "attachment; filename=StockAdjDamageLoss_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls");
            Response.AddHeader("Content-Length", xlsx.Length.ToString());
            Response.AddHeader("Access-Control-Allow-Origin", "*");
            Response.BinaryWrite(xlsx);
            Response.Flush();
            Response.End();
            return;
        }

        // ── 5b. Build PDF ─────────────────────────────────────────────────
        byte[] pdf;
        try   { pdf = BuildPdf(loginID, whName, typeLabel, itmName, period, dt); }
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
            "inline; filename=StockAdjDamageLoss_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
        Response.AddHeader("Content-Length", pdf.Length.ToString());
        Response.AddHeader("Access-Control-Allow-Origin", "*");
        Response.BinaryWrite(pdf);
        Response.Flush();
        Response.End();
    }

    // ════════════════════════════════════════════════════════════════════════
    // PDF BUILDER
    // ════════════════════════════════════════════════════════════════════════

    private byte[] BuildPdf(string loginID, string whName, string typeLabel,
                            string itmName, string period, DataTable dt)
    {
        // ── Colours ────────────────────────────────────────────────────────
        BaseColor navy      = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor headerBg  = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor rowAlt    = new BaseColor(0xf4, 0xf7, 0xfb);
        BaseColor totalBg   = new BaseColor(0xe8, 0xf0, 0xfe);
        BaseColor border    = new BaseColor(0xcc, 0xcc, 0xcc);
        BaseColor gold      = new BaseColor(0xD4, 0xA0, 0x17);
        BaseColor adjPlus   = new BaseColor(0xe6, 0xf4, 0xe6);   // light green
        BaseColor adjMinus  = new BaseColor(0xff, 0xf3, 0xcd);   // light amber
        BaseColor dmgLoss   = new BaseColor(0xff, 0xe5, 0xe5);   // light red

        // ── Fonts ──────────────────────────────────────────────────────────
        Font fTitle    = new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD,   navy);
        Font fFilter   = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, new BaseColor(0x44,0x44,0x44));
        Font fFilterB  = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   new BaseColor(0x44,0x44,0x44));
        Font fTH       = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   BaseColor.WHITE);
        Font fTD       = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.NORMAL, BaseColor.BLACK);
        Font fTDGreen  = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.BOLD,   new BaseColor(0x1a, 0x6e, 0x1a));
        Font fTDAmber  = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.BOLD,   new BaseColor(0x99, 0x66, 0x00));
        Font fTDRed    = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.BOLD,   new BaseColor(0xc6, 0x28, 0x28));
        Font fTotal    = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
        Font fFoot     = new Font(Font.FontFamily.HELVETICA,  7f, Font.NORMAL, BaseColor.GRAY);
        Font fLegend   = new Font(Font.FontFamily.HELVETICA, 7.5f, Font.NORMAL, new BaseColor(0x44,0x44,0x44));

        // ── Column definitions ─────────────────────────────────────────────
        var cols = new object[][]
        {
            new object[]{ "S.NO.",          "#",               1.2f,  true  },
            new object[]{ "Type",        "TransactionType", 1.8f,  false },
            new object[]{ "Doc No",      "DocNo",           1.8f,  false },
            new object[]{ "Date",        "DocDate",         1.5f,  false },
            new object[]{ "Warehouse",   "WarehouseName",   2.2f,  false },
            new object[]{ "Item Code",   "ItemCode",        1.5f,  false },
            new object[]{ "Item Name",   "ItemName",        3.2f,  false },
            new object[]{ "Category",    "CategoryName",    1.8f,  false },
            new object[]{ "UOM",         "UOMName",         0.9f,  false },
            new object[]{ "Qty",         "Quantity",        1.3f,  true  },
            new object[]{ "Unit Cost",   "UnitCost",        1.6f,  true  },
            new object[]{ "Total Value", "TotalValue",      1.8f,  true  },
            new object[]{ "Reason",      "Reason",          2.5f,  false },
        };

        float[] widths = new float[cols.Length];
        for (int i = 0; i < cols.Length; i++) widths[i] = (float)cols[i][2];

        // Label span for totals row
        int labelSpan = 0;
        for (int i = 0; i < cols.Length; i++)
        {
            bool isNum = (bool)cols[i][3];
            string fld = (string)cols[i][1];
            if (!isNum || fld == "#") labelSpan++;
            else break;
        }
        if (labelSpan < 1) labelSpan = 1;

        using (var ms = new MemoryStream())
        {
            var doc    = new Document(PageSize.A4.Rotate(), 20f, 20f, 30f, 30f);
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
            P(doc, "STOCK ADJUSTMENT / DAMAGE / LOSS REPORT", fTitle, Element.ALIGN_CENTER, 4f);

            // ── Filter summary ─────────────────────────────────────────────
            var ft = new PdfPTable(2) { WidthPercentage = 100f, SpacingAfter = 4f };
            ft.SetWidths(new float[] { 1f, 2f });
            ft.DefaultCell.Border = Rectangle.NO_BORDER;
            string[][] filterPairs = new string[][]
            {
                new[]{ "Warehouse:",        whName    },
                new[]{ "Transaction Type:", typeLabel },
                new[]{ "Item:",             itmName   },
                new[]{ "Period:",           period    }
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
            var legTable = new PdfPTable(4) { WidthPercentage = 75f, SpacingAfter = 6f };
            legTable.HorizontalAlignment = Element.ALIGN_LEFT;
            legTable.DefaultCell.Border  = Rectangle.NO_BORDER;
            legTable.AddCell(new PdfPCell(new Phrase("  Adjustment (+)  ", fLegend))
                { BackgroundColor = adjPlus,  Border = Rectangle.BOX, BorderColor = border, Padding = 3f });
            legTable.AddCell(new PdfPCell(new Phrase("  Adjustment (-)  ", fLegend))
                { BackgroundColor = adjMinus, Border = Rectangle.BOX, BorderColor = border, Padding = 3f });
            legTable.AddCell(new PdfPCell(new Phrase("  Damage / Loss  ", fLegend))
                { BackgroundColor = dmgLoss,  Border = Rectangle.BOX, BorderColor = border, Padding = 3f });
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
            int     ri         = 0;
            decimal totalQty   = 0;
            decimal totalValue = 0;

            foreach (DataRow dr in dt.Rows)
            {
                ri++;
                string tc = dr["TransTypeCode"] != DBNull.Value ? dr["TransTypeCode"].ToString() : "";
                BaseColor rowBg = tc == "PLUS"
                    ? adjPlus
                    : (tc == "MINUS" ? adjMinus
                    : (tc == "Damage" || tc == "Loss" ? dmgLoss
                    : (ri % 2 == 0 ? rowAlt : BaseColor.WHITE)));

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

                        // Colour font by type
                        if (field == "TransactionType")
                        {
                            cf = tc == "PLUS"   ? fTDGreen
                               : tc == "MINUS"  ? fTDAmber
                               : fTDRed;
                        }
                        else if (isNum && raw != null && raw != DBNull.Value)
                        {
                            decimal dv;
                            if (decimal.TryParse(raw.ToString(), out dv) && dv < 0) cf = fTDRed;
                        }

                        // Accumulate totals
                        if (raw != null && raw != DBNull.Value)
                        {
                            decimal v;
                            if (decimal.TryParse(raw.ToString(), out v))
                            {
                                if (field == "Quantity")   totalQty   += v;
                                if (field == "TotalValue") totalValue += v;
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

            // ── Totals row ─────────────────────────────────────────────────
            tbl.AddCell(new PdfPCell(new Phrase("TOTALS", fTotal))
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
                string txt   = field == "Quantity"   ? totalQty.ToString("N2")
                             : field == "TotalValue" ? totalValue.ToString("N2")
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

    // ════════════════════════════════════════════════════════════════════════
    // EXCEL BUILDER
    // ════════════════════════════════════════════════════════════════════════

    private byte[] BuildExcel(string whName, string typeLabel, string itmName,
                              string period, DataTable dt)
    {
        var cols = new string[][]
        {
            new[]{ "S.NO.",          "#"               },
            new[]{ "Type",        "TransactionType" },
            new[]{ "Doc No",      "DocNo"           },
            new[]{ "Date",        "DocDate"         },
            new[]{ "Warehouse",   "WarehouseName"   },
            new[]{ "Item Code",   "ItemCode"        },
            new[]{ "Item Name",   "ItemName"        },
            new[]{ "Category",    "CategoryName"    },
            new[]{ "UOM",         "UOMName"         },
            new[]{ "Qty",         "Quantity"        },
            new[]{ "Unit Cost",   "UnitCost"        },
            new[]{ "Total Value", "TotalValue"      },
            new[]{ "Reason",      "Reason"          },
        };

        var numFlds = new System.Collections.Generic.HashSet<string>
            { "Quantity", "UnitCost", "TotalValue" };

        int span = cols.Length;
        var sb   = new System.Text.StringBuilder();

        sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" " +
                      "xmlns:x=\"urn:schemas-microsoft-com:office:excel\" " +
                      "xmlns=\"http://www.w3.org/TR/REC-html40\">");
        sb.AppendLine("<head><meta charset=\"UTF-8\"/>");
        sb.AppendLine("<!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets>" +
                      "<x:ExcelWorksheet><x:Name>Adj Damage Loss</x:Name>" +
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
        sb.AppendLine(".adj-plus{background:#e6f4e6;}");
        sb.AppendLine(".adj-minus{background:#fff3cd;}");
        sb.AppendLine(".dmg{background:#ffe5e5;}");
        sb.AppendLine(".adj-plus-n{background:#e6f4e6;text-align:right;padding:4px;border:1px solid #ddd;}");
        sb.AppendLine(".adj-minus-n{background:#fff3cd;text-align:right;padding:4px;border:1px solid #ddd;}");
        sb.AppendLine(".dmg-n{background:#ffe5e5;text-align:right;padding:4px;border:1px solid #ddd;}");
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

        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"ttl\">STOCK ADJUSTMENT / DAMAGE / LOSS REPORT</td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:2px;\"></td></tr>\n", span);

        sb.AppendFormat("<tr><td colspan=\"{0}\">", span);
        sb.AppendFormat("<span class=\"fl\">Warehouse: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;",        H(whName));
        sb.AppendFormat("<span class=\"fl\">Transaction Type: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;", H(typeLabel));
        sb.AppendFormat("<span class=\"fl\">Item: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;",             H(itmName));
        sb.AppendFormat("<span class=\"fl\">Period: </span><span class=\"fv\">{0}</span>",                             H(period));
        sb.AppendLine("</td></tr>");

        // Legend
        sb.AppendFormat("<tr><td colspan=\"{0}\">", span);
        sb.Append("<span style=\"background:#e6f4e6;padding:2px 6px;border:1px solid #ccc;\">Adjustment (+)</span>&nbsp;");
        sb.Append("<span style=\"background:#fff3cd;padding:2px 6px;border:1px solid #ccc;\">Adjustment (-)</span>&nbsp;");
        sb.Append("<span style=\"background:#ffe5e5;padding:2px 6px;border:1px solid #ccc;\">Damage / Loss</span>");
        sb.AppendLine("</td></tr>");
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:3px;\"></td></tr>\n", span);

        // Column headers
        sb.AppendLine("<tr>");
        foreach (var c in cols) sb.AppendFormat("<td class=\"th\">{0}</td>", H(c[0]));
        sb.AppendLine("</tr>");

        // Data rows
        decimal totalQty = 0, totalValue = 0;
        int ri = 0;
        foreach (DataRow dr in dt.Rows)
        {
            ri++;
            string tc  = dr["TransTypeCode"] != DBNull.Value ? dr["TransTypeCode"].ToString() : "";
            bool isPlus  = tc == "PLUS";
            bool isMinus = tc == "MINUS";
            bool isDmg   = tc == "Damage" || tc == "Loss";
            string baseCls = isPlus ? "adj-plus" : isMinus ? "adj-minus" : isDmg ? "dmg" : (ri % 2 == 0 ? "alt" : "");

            sb.AppendLine("<tr>");
            foreach (var c in cols)
            {
                string fld = c[1];
                if (fld == "#")
                {
                    string nc = isPlus ? "adj-plus-n" : isMinus ? "adj-minus-n" : isDmg ? "dmg-n" : "tn";
                    sb.AppendFormat("<td class=\"{0}\">{1}</td>", nc, ri);
                    continue;
                }

                object raw = dt.Columns.Contains(fld) ? dr[fld] : null;
                string txt = raw != null && raw != DBNull.Value ? raw.ToString() : "";

                if (fld == "Quantity")   { decimal v; if (decimal.TryParse(txt, out v)) totalQty   += v; }
                if (fld == "TotalValue") { decimal v; if (decimal.TryParse(txt, out v)) totalValue += v; }

                if (numFlds.Contains(fld))
                {
                    decimal num; bool ok = decimal.TryParse(txt, out num);
                    string nc = isPlus ? "adj-plus-n" : isMinus ? "adj-minus-n" : isDmg ? "dmg-n" : "tn";
                    sb.AppendFormat("<td class=\"{0}\">{1}</td>", nc, ok ? num.ToString("N2") : "&mdash;");
                }
                else
                {
                    string tc2 = isPlus ? "adj-plus td" : isMinus ? "adj-minus td" : isDmg ? "dmg td" : "td";
                    sb.AppendFormat("<td class=\"{0}\">{1}</td>", tc2, H(txt));
                }
            }
            sb.AppendLine("</tr>");
        }

        // Totals row
        int xlsLabel = 0;
        for (int i = 0; i < cols.Length; i++)
        {
            if (!numFlds.Contains(cols[i][1])) xlsLabel++;
            else break;
        }
        if (xlsLabel < 1) xlsLabel = 1;

        sb.AppendLine("<tr>");
        sb.AppendFormat("<td class=\"tot td\" colspan=\"{0}\"><b>TOTALS</b></td>", xlsLabel);
        for (int i = xlsLabel; i < cols.Length; i++)
        {
            string fld   = cols[i][1];
            bool   isNum = numFlds.Contains(fld);
            string val   = fld == "Quantity"   ? totalQty.ToString("N2")
                         : fld == "TotalValue" ? totalValue.ToString("N2") : "";
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

    private DataTable RunSP(int whID, string transType, int itemID, DateTime from, DateTime to)
    {
        var dt = new DataTable();
        using (var conn = new SqlConnection(ConnStr()))
        using (var cmd  = new SqlCommand("sp_StockAdjDamageLossReport", conn))
        {
            cmd.CommandType    = CommandType.StoredProcedure;
            cmd.CommandTimeout = 60;
            cmd.Parameters.AddWithValue("@WarehouseID", whID);
            cmd.Parameters.AddWithValue("@TransType",   transType ?? "ALL");
            cmd.Parameters.AddWithValue("@ItemID",      itemID);
            cmd.Parameters.AddWithValue("@FromDate",    from.Date);
            cmd.Parameters.AddWithValue("@ToDate",      to.Date);
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
            "<h3 style='color:#c62828'>Stock Adjustment / Damage / Loss Report</h3>" +
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
