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
    // OBMS Purchase Return & Issue Return Report — PDF / Excel
    // Single-file inline C# (no code-behind) — same pattern as StockIssueListReport.aspx
    // Flow: Angular → ASPX → sp_PurchaseIssueReturnReport → PDF / Excel → Browser
    //
    // Combines: Purchase Return (goods returned to supplier)
    //           Issue Return    (items returned to warehouse)
    //
    // Columns: Return Type, Return No, Return Date, Ref Doc,
    //          Party (Supplier / Issued To), Warehouse,
    //          Item Code, Item Name, Category, UOM,
    //          Return Qty, Unit Cost, Total Value, Reason
    //
    // PDF: colour-coded by type
    //   Purchase Return = light blue | Issue Return = light green
    // ════════════════════════════════════════════════════════════════════════

    protected void Page_Load(object sender, EventArgs e)
    {
        // ── 1. Read query-string params ───────────────────────────────────
        string loginID      = (Request.QueryString["LoginID"]      ?? "").Trim();
        string warehouseStr = (Request.QueryString["WarehouseID"]  ?? "0").Trim();
        string returnType   = (Request.QueryString["ReturnType"]   ?? "ALL").Trim();
        string itemIDStr    = (Request.QueryString["ItemID"]       ?? "0").Trim();
        string startDateStr = (Request.QueryString["StartDate"]    ?? "").Trim();
        string endDateStr   = (Request.QueryString["EndDate"]      ?? "").Trim();
        string format       = (Request.QueryString["format"]       ?? "pdf").ToLower().Trim();

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
        if (string.IsNullOrWhiteSpace(returnType)) returnType = "ALL";

        // ── 3. Execute stored procedure ───────────────────────────────────
        DataTable dt;
        try   { dt = RunSP(warehouseID, returnType, itemID, startDate, endDate); }
        catch (Exception ex)
        {
            LogErr("sp_PurchaseIssueReturnReport", ex.Message);
            SendError("Unable to retrieve return data. Please try again.");
            return;
        }

        if (dt == null || dt.Rows.Count == 0)
        { SendError("No return data found for the selected filters."); return; }

        // ── 4. Resolve display names ──────────────────────────────────────
        string whName     = warehouseID > 0
            ? (Lookup("SELECT Name FROM WarehouseMaster WHERE ID=@p", warehouseID.ToString()) ?? warehouseStr)
            : "All Warehouses";
        string itmName    = itemID > 0
            ? (Lookup("SELECT Name FROM ItemMaster WHERE ID=@p", itemID.ToString()) ?? itemIDStr)
            : "All Items";
        string typeLabel  = returnType == "ALL"             ? "All Return Types"
                          : returnType == "PURCHASE_RETURN" ? "Purchase Return"
                          : returnType == "ISSUE_RETURN"    ? "Issue Return"
                          : returnType;
        string period     = startDate.ToString("dd MMM yyyy") + " to " + endDate.ToString("dd MMM yyyy");

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
                "attachment; filename=PurchaseIssueReturn_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls");
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
            "inline; filename=PurchaseIssueReturn_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
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
        BaseColor purRet    = new BaseColor(0xe0, 0xf0, 0xff);   // light blue  — Purchase Return
        BaseColor issRet    = new BaseColor(0xe6, 0xf4, 0xe6);   // light green — Issue Return
        BaseColor border    = new BaseColor(0xcc, 0xcc, 0xcc);
        BaseColor gold      = new BaseColor(0xD4, 0xA0, 0x17);
        BaseColor blueFont  = new BaseColor(0x00, 0x44, 0x88);
        BaseColor greenFont = new BaseColor(0x1a, 0x6e, 0x1a);

        // ── Fonts ──────────────────────────────────────────────────────────
        Font fTitle    = new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD,   navy);
        Font fFilter   = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, new BaseColor(0x44,0x44,0x44));
        Font fFilterB  = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   new BaseColor(0x44,0x44,0x44));
        Font fTH       = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   BaseColor.WHITE);
        Font fTD       = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.NORMAL, BaseColor.BLACK);
        Font fTDBlue   = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.BOLD,   blueFont);
        Font fTDGreen  = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.BOLD,   greenFont);
        Font fTDRed    = new Font(Font.FontFamily.HELVETICA, 7.5f,Font.BOLD,   new BaseColor(0xc6,0x28,0x28));
        Font fTotal    = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
        Font fFoot     = new Font(Font.FontFamily.HELVETICA,  7f, Font.NORMAL, BaseColor.GRAY);
        Font fLegend   = new Font(Font.FontFamily.HELVETICA, 7.5f, Font.NORMAL, new BaseColor(0x44,0x44,0x44));

        // ── Column definitions ─────────────────────────────────────────────
        var cols = new object[][]
        {
            new object[]{ "S.NO.",       "#",             1.2f,  true  },
            new object[]{ "Return Type", "ReturnType",    2.0f,  false },
            new object[]{ "Return No",   "ReturnNo",      1.8f,  false },
            new object[]{ "Return Date", "ReturnDate",    1.5f,  false },
            new object[]{ "Ref Doc",     "RefDocNo",      1.8f,  false },
            new object[]{ "Party",       "PartyName",     2.5f,  false },
            new object[]{ "Warehouse",   "WarehouseName", 2.0f,  false },
            new object[]{ "Item Code",   "ItemCode",      1.4f,  false },
            new object[]{ "Item Name",   "ItemName",      3.0f,  false },
            new object[]{ "Category",    "CategoryName",  1.6f,  false },
            new object[]{ "UOM",         "UOMName",       0.9f,  false },
            new object[]{ "Return Qty",  "ReturnQty",     1.3f,  true  },
            new object[]{ "Unit Cost",   "UnitCost",      1.6f,  true  },
            new object[]{ "Total Value", "TotalValue",    1.8f,  true  },
        };

        float[] widths = new float[cols.Length];
        for (int i = 0; i < cols.Length; i++) widths[i] = (float)cols[i][2];

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
            var doc    = new Document(PageSize.A4.Rotate(), 18f, 18f, 30f, 30f);
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

            P(doc, "PURCHASE RETURN & ISSUE RETURN REPORT", fTitle, Element.ALIGN_CENTER, 4f);

            // ── Filter summary ─────────────────────────────────────────────
            var ft = new PdfPTable(2) { WidthPercentage = 100f, SpacingAfter = 4f };
            ft.SetWidths(new float[] { 1f, 1f });
            ft.DefaultCell.Border = Rectangle.NO_BORDER;
            string[][] filterPairs = new string[][]
            {
                new[]{ "Warehouse:",   whName    },
                new[]{ "Return Type:", typeLabel },
                new[]{ "Item:",        itmName   },
                new[]{ "Period:",      period    }
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
            var legTable = new PdfPTable(3) { WidthPercentage = 60f, SpacingAfter = 6f };
            legTable.HorizontalAlignment = Element.ALIGN_LEFT;
            legTable.DefaultCell.Border  = Rectangle.NO_BORDER;
            legTable.AddCell(new PdfPCell(new Phrase("  Purchase Return (returned to Supplier)  ", fLegend))
                { BackgroundColor = purRet, Border = Rectangle.BOX, BorderColor = border, Padding = 3f });
            legTable.AddCell(new PdfPCell(new Phrase("  Issue Return (returned to Warehouse)  ", fLegend))
                { BackgroundColor = issRet, Border = Rectangle.BOX, BorderColor = border, Padding = 3f });
            legTable.AddCell(new PdfPCell(new Phrase("")) { Border = Rectangle.NO_BORDER });
            doc.Add(legTable);

            // ── Data table ─────────────────────────────────────────────────
            var tbl = new PdfPTable(cols.Length) { WidthPercentage = 100f, HeaderRows = 1 };
            tbl.SetWidths(widths);

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

            int     ri         = 0;
            decimal totalQty   = 0;
            decimal totalValue = 0;

            foreach (DataRow dr in dt.Rows)
            {
                ri++;
                string rtc  = dr["ReturnTypeCode"] != DBNull.Value ? dr["ReturnTypeCode"].ToString() : "";
                bool isPur  = rtc == "PURCHASE_RETURN";
                bool isIss  = rtc == "ISSUE_RETURN";
                BaseColor rowBg = isPur ? purRet : isIss ? issRet : (ri % 2 == 0 ? rowAlt : BaseColor.WHITE);

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

                        if (field == "ReturnType")
                            cf = isPur ? fTDBlue : isIss ? fTDGreen : fTD;
                        else if (isNum && raw != null && raw != DBNull.Value)
                        {
                            decimal dv;
                            if (decimal.TryParse(raw.ToString(), out dv) && dv < 0) cf = fTDRed;
                        }

                        if (raw != null && raw != DBNull.Value)
                        {
                            decimal v;
                            if (decimal.TryParse(raw.ToString(), out v))
                            {
                                if (field == "ReturnQty")  totalQty   += v;
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
                string txt   = field == "ReturnQty"  ? totalQty.ToString("N2")
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
            new[]{ "S.NO.",       "#"             },
            new[]{ "Return Type", "ReturnType"    },
            new[]{ "Return No",   "ReturnNo"      },
            new[]{ "Return Date", "ReturnDate"    },
            new[]{ "Ref Doc",     "RefDocNo"      },
            new[]{ "Party",       "PartyName"     },
            new[]{ "Warehouse",   "WarehouseName" },
            new[]{ "Item Code",   "ItemCode"      },
            new[]{ "Item Name",   "ItemName"      },
            new[]{ "Category",    "CategoryName"  },
            new[]{ "UOM",         "UOMName"       },
            new[]{ "Return Qty",  "ReturnQty"     },
            new[]{ "Unit Cost",   "UnitCost"      },
            new[]{ "Total Value", "TotalValue"    },
        };

        var numFlds = new System.Collections.Generic.HashSet<string>
            { "ReturnQty", "UnitCost", "TotalValue" };

        int span = cols.Length;
        var sb   = new System.Text.StringBuilder();

        sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" " +
                      "xmlns:x=\"urn:schemas-microsoft-com:office:excel\" " +
                      "xmlns=\"http://www.w3.org/TR/REC-html40\">");
        sb.AppendLine("<head><meta charset=\"UTF-8\"/>");
        sb.AppendLine("<!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets>" +
                      "<x:ExcelWorksheet><x:Name>Returns</x:Name>" +
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
        sb.AppendLine(".pur{background:#e0f0ff;}");
        sb.AppendLine(".iss{background:#e6f4e6;}");
        sb.AppendLine(".pur-n{background:#e0f0ff;text-align:right;padding:4px;border:1px solid #ddd;}");
        sb.AppendLine(".iss-n{background:#e6f4e6;text-align:right;padding:4px;border:1px solid #ddd;}");
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

        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"ttl\">PURCHASE RETURN &amp; ISSUE RETURN REPORT</td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:2px;\"></td></tr>\n", span);

        sb.AppendFormat("<tr><td colspan=\"{0}\">", span);
        sb.AppendFormat("<span class=\"fl\">Warehouse: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;",   H(whName));
        sb.AppendFormat("<span class=\"fl\">Return Type: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;", H(typeLabel));
        sb.AppendFormat("<span class=\"fl\">Item: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;",        H(itmName));
        sb.AppendFormat("<span class=\"fl\">Period: </span><span class=\"fv\">{0}</span>",                        H(period));
        sb.AppendLine("</td></tr>");

        // Legend
        sb.AppendFormat("<tr><td colspan=\"{0}\">", span);
        sb.Append("<span style=\"background:#e0f0ff;padding:2px 6px;border:1px solid #ccc;\">Purchase Return</span>&nbsp;");
        sb.Append("<span style=\"background:#e6f4e6;padding:2px 6px;border:1px solid #ccc;\">Issue Return</span>");
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
            string rtc  = dr["ReturnTypeCode"] != DBNull.Value ? dr["ReturnTypeCode"].ToString() : "";
            bool isPur   = rtc == "PURCHASE_RETURN";
            bool isIss   = rtc == "ISSUE_RETURN";
            string baseCls = isPur ? "pur" : isIss ? "iss" : (ri % 2 == 0 ? "alt" : "");

            sb.AppendLine("<tr>");
            foreach (var c in cols)
            {
                string fld = c[1];
                if (fld == "#")
                {
                    string nc = isPur ? "pur-n" : isIss ? "iss-n" : "tn";
                    sb.AppendFormat("<td class=\"{0}\">{1}</td>", nc, ri);
                    continue;
                }

                object raw = dt.Columns.Contains(fld) ? dr[fld] : null;
                string txt = raw != null && raw != DBNull.Value ? raw.ToString() : "";

                if (fld == "ReturnQty")  { decimal v; if (decimal.TryParse(txt, out v)) totalQty   += v; }
                if (fld == "TotalValue") { decimal v; if (decimal.TryParse(txt, out v)) totalValue += v; }

                if (numFlds.Contains(fld))
                {
                    decimal num; bool ok = decimal.TryParse(txt, out num);
                    string nc = isPur ? "pur-n" : isIss ? "iss-n" : "tn";
                    sb.AppendFormat("<td class=\"{0}\">{1}</td>", nc, ok ? num.ToString("N2") : "&mdash;");
                }
                else
                {
                    string tc2 = isPur ? "pur td" : isIss ? "iss td" : "td";
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
            string val   = fld == "ReturnQty"  ? totalQty.ToString("N2")
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

    private DataTable RunSP(int whID, string returnType, int itemID, DateTime from, DateTime to)
    {
        var dt = new DataTable();
        using (var conn = new SqlConnection(ConnStr()))
        using (var cmd  = new SqlCommand("sp_PurchaseIssueReturnReport", conn))
        {
            cmd.CommandType    = CommandType.StoredProcedure;
            cmd.CommandTimeout = 60;
            cmd.Parameters.AddWithValue("@WarehouseID", whID);
            cmd.Parameters.AddWithValue("@ReturnType",  returnType ?? "ALL");
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
            "<h3 style='color:#c62828'>Purchase Return &amp; Issue Return Report</h3>" +
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
