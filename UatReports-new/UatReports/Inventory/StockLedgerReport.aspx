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

<<<<<<< HEAD
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
=======
        // ── 3. Resolve display names for filter header ────────────────────
        string whName    = Lookup("SELECT Name FROM WarehouseMaster WHERE ID=@p", warehouseID.ToString()) ?? warehouseIDStr;
        string itemName  = itemID > 0
            ? (Lookup("SELECT Name FROM ItemMaster WHERE ID=@p", itemID.ToString()) ?? itemIDStr)
            : "All Items";
        string typeLbl   = TypeLabel(transactionType);
        string period    = startDate.ToString("dd MMM yyyy") + " to " + endDate.ToString("dd MMM yyyy");

        // ── 4. Build PDF or Excel based on ?format= ──────────────────────
        string format = (Request.QueryString["format"] ?? "html").ToLower().Trim();
>>>>>>> 754e8b5 (Updated payslip changes)

        if (format == "excel")
        {
            // ── Excel export ──────────────────────────────────────────────
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

<<<<<<< HEAD
        byte[] pdf;
        try   { pdf = BuildPdf(loginID, whName, itemName, catName, sizeLbl, typeLbl, period, dt); }
        catch (Exception ex)
=======
        if (format == "pdf")
>>>>>>> 754e8b5 (Updated payslip changes)
        {
            // ── PDF export ────────────────────────────────────────────────
            byte[] pdf;
            try   { pdf = BuildPdf(loginID, whName, itemName, typeLbl, period, dt); }
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
            return;
        }

        // ── 4c. Default: HTML table (clickable Ref No links work in iframe) ──
        string appBaseUrl = (AppSettings("AppBaseUrl") ?? "http://localhost:4200").TrimEnd('/');
        string htmlOut = BuildHtmlTable(whName, itemName, typeLbl, period, dt, appBaseUrl);
        Response.Clear();
        Response.Buffer      = false;
        Response.ContentType = "text/html; charset=utf-8";
        Response.AddHeader("Access-Control-Allow-Origin", "*");
        Response.Write(htmlOut);
        Response.Flush();
        Response.End();
    }

    // ════════════════════════════════════════════════════════════════════════
    // PDF BUILDER
    // ════════════════════════════════════════════════════════════════════════

    private byte[] BuildPdf(string loginID, string whName, string itemName,
                            string catName, string sizeLbl,
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

<<<<<<< HEAD
            // Company text cell
            Font fCoName = new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD,   BaseColor.DARK_GRAY);
            Font fCoReg  = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
            Font fCoSub  = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, BaseColor.DARK_GRAY);
=======
            // Right cell — company name + address stacked
            Font fCoNameHdr = new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD,   navy);
            Font fCoRegHdr  = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
            Font fCoSubHdr  = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, BaseColor.DARK_GRAY);
>>>>>>> 754e8b5 (Updated payslip changes)

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
                new[]{ "Category:",         catName  },
                new[]{ "Size:",             sizeLbl  },
                new[]{ "Transaction Type:", typeLbl  },
                new[]{ "Period:",           period   }
            };
<<<<<<< HEAD
            var ft = new PdfPTable(3) { WidthPercentage = 100f, SpacingAfter = 8f };
            ft.SetWidths(new float[] { 1.5f, 3.5f, 5.0f });
=======
            var ft = new PdfPTable(4) { WidthPercentage = 100f, SpacingAfter = 8f };
            ft.SetWidths(new float[] { 1.2f, 2.3f, 1.2f, 2.3f });
>>>>>>> 754e8b5 (Updated payslip changes)
            ft.DefaultCell.Border = Rectangle.NO_BORDER;
            foreach (var f in filters)
            {
                var ph = new Phrase(); ph.Add(new Chunk(f[0] + " ", fFilterB)); ph.Add(new Chunk(f[1], fFilter));
                ft.AddCell(new PdfPCell(ph) { Border = Rectangle.NO_BORDER, Padding = 2f });
            }
            // pad to complete the last row if needed (6 items → 2 rows of 3)
            doc.Add(ft);

            // ── Data table ────────────────────────────────────────────────
<<<<<<< HEAD
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
=======
            // Column definitions: header, fieldName, relWidth, isNumeric, isDate
            var cols = new object[][]
            {
                new object[]{ "Date",         "TransactionDate",      2.2f, false, true  },
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
>>>>>>> 754e8b5 (Updated payslip changes)
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
<<<<<<< HEAD
            string pdfBaseUrl = GetAppBaseUrl();
=======
            string appBaseUrl = (AppSettings("AppBaseUrl") ?? "").TrimEnd('/');
>>>>>>> 754e8b5 (Updated payslip changes)
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

<<<<<<< HEAD
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
=======
                    // Red for negative numbers
                    Font cf = fTD;
                    if (isNum && raw != null && raw != DBNull.Value)
                    { decimal dv; if (decimal.TryParse(raw.ToString(), out dv) && dv < 0) cf = fTDRed; }

                    // Accumulate totals
                    if (field == "InQty"  && raw != null && raw != DBNull.Value) { decimal v; if (decimal.TryParse(raw.ToString(), out v)) totalIn  += v; }
                    if (field == "OutQty" && raw != null && raw != DBNull.Value) { decimal v; if (decimal.TryParse(raw.ToString(), out v)) totalOut += v; }

                    // "Ref No" column: clickable link → navigates to the transaction's detail page
                    if (field == "ReferenceNo" && !string.IsNullOrEmpty(txt))
                    {
                        string txType = dt.Columns.Contains("TransactionType")
                            ? (dr["TransactionType"] == DBNull.Value ? "" : dr["TransactionType"].ToString().ToUpper())
                            : "";
                        object refIdRaw = dt.Columns.Contains("ReferenceID") ? dr["ReferenceID"] : null;
                        string refId    = (refIdRaw != null && refIdRaw != DBNull.Value) ? refIdRaw.ToString() : "";

                        // Use appBaseUrl from web.config; fall back to relative hash-route if missing
                        string base64 = string.IsNullOrEmpty(appBaseUrl) ? "http://localhost:4200" : appBaseUrl;
                        string navUrl = BuildNavUrl(base64, txType, refId);
                        if (!string.IsNullOrEmpty(navUrl))
                        {
                            Font fLink     = new Font(Font.FontFamily.HELVETICA, 7.5f, Font.UNDERLINE, new BaseColor(0x0d, 0x6e, 0xfd));
                            var chunk      = new Chunk(txt, fLink);
                            chunk.SetAnchor(new System.Uri(navUrl));
                            var linkPhrase = new Phrase();
                            linkPhrase.Add(chunk);
                            tbl.AddCell(new PdfPCell(linkPhrase)
                            {
                                BackgroundColor     = rowBg,
                                HorizontalAlignment = Element.ALIGN_LEFT,
                                Padding = 4f, Border = Rectangle.BOX, BorderColor = border
                            });
                            continue;
                        }
                    }

                    tbl.AddCell(new PdfPCell(new Phrase(txt, cf))
>>>>>>> 754e8b5 (Updated payslip changes)
                    {
                        BackgroundColor     = rowBg,
                        HorizontalAlignment = isNum ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT,
                        Padding = 4f, Border = Rectangle.BOX, BorderColor = border
                    });
                }
                ri++;
            }

<<<<<<< HEAD
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
=======
            // Totals row
            for (int i = 0; i < cols.Length; i++)
>>>>>>> 754e8b5 (Updated payslip changes)
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
    // HTML TABLE BUILDER
    // Renders the stock ledger as a styled HTML page.
    // Ref No cells use <a href target="_parent"> so clicking inside the iframe
    // navigates the parent Angular window to the correct transaction page.
    // ════════════════════════════════════════════════════════════════════════

    private string BuildHtmlTable(string whName, string itemName,
                                  string typeLbl, string period,
                                  DataTable dt, string baseUrl)
    {
        var numFlds = new System.Collections.Generic.HashSet<string>
            { "InQty", "OutQty", "RunningBalance", "UnitCost" };

        var cols = new string[][]
        {
            new[]{ "Date",       "TransactionDate"       },
            new[]{ "Type",       "TransactionTypeLabel"  },
            new[]{ "Item Code",  "ItemCode"              },
            new[]{ "Item Name",  "ItemName"              },
            new[]{ "UOM",        "UOMName"               },
            new[]{ "In Qty",     "InQty"                 },
            new[]{ "Out Qty",    "OutQty"                },
            new[]{ "Balance",    "RunningBalance"        },
            new[]{ "Unit Cost",  "UnitCost"              },
            new[]{ "Ref No",     "ReferenceNo"           },
            new[]{ "Remarks",    "Remarks"               },
        };

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='UTF-8'>");
        sb.AppendLine("<style>");
        sb.AppendLine("*{box-sizing:border-box;margin:0;padding:0;}");
        sb.AppendLine("body{font-family:Arial,sans-serif;font-size:12px;background:#fff;padding:16px;}");
        sb.AppendLine(".co-name{font-size:15px;font-weight:bold;color:#1a3a5c;}");
        sb.AppendLine(".co-sub{font-size:11px;color:#555;margin-top:2px;}");
        sb.AppendLine(".gold-line{border:none;border-top:2.5px solid #D4A017;margin:8px 0;}");
        sb.AppendLine(".report-title{text-align:center;font-size:14px;font-weight:bold;color:#1a3a5c;margin:10px 0 6px;}");
        sb.AppendLine(".filters{font-size:11px;margin-bottom:10px;color:#444;}");
        sb.AppendLine(".filters span{margin-right:20px;}");
        sb.AppendLine(".filters b{color:#1a3a5c;}");
        sb.AppendLine("table{width:100%;border-collapse:collapse;font-size:11px;}");
        sb.AppendLine("thead th{background:#1a3a5c;color:#fff;padding:6px 5px;text-align:left;border:1px solid #ccc;}");
        sb.AppendLine("thead th.num{text-align:right;}");
        sb.AppendLine("tbody tr:nth-child(even){background:#f4f7fb;}");
        sb.AppendLine("tbody tr:hover{background:#e3eaf6;}");
        sb.AppendLine("tbody td{padding:4px 5px;border:1px solid #ddd;vertical-align:middle;}");
        sb.AppendLine("tbody td.num{text-align:right;}");
        sb.AppendLine("tbody td.neg{text-align:right;color:#c62828;font-weight:bold;}");
        sb.AppendLine("tfoot td{background:#e8f0fe;font-weight:bold;padding:5px;border:1.5px solid #1a3a5c;}");
        sb.AppendLine("tfoot td.num{text-align:right;}");
        sb.AppendLine("a.ref-link{color:#0d6efd;text-decoration:underline;cursor:pointer;font-weight:500;}");
        sb.AppendLine("a.ref-link:hover{color:#0a58ca;}");
        sb.AppendLine("</style></head><body>");

        // ── Company header ─────────────────────────────────────────────────
        string logoPath = Server.MapPath("~/images/logo-white.png");
        string logoUrl  = ResolveUrl("~/images/logo-white.png");
        sb.AppendLine("<table style='width:100%;border:none;margin-bottom:4px;'><tr>");
        sb.AppendLine("<td style='width:60px;vertical-align:middle;border:none;'>");
        if (System.IO.File.Exists(logoPath))
            sb.AppendFormat("<img src='{0}' style='height:50px;'/>", logoUrl);
        sb.AppendLine("</td>");
        sb.AppendLine("<td style='vertical-align:middle;border:none;padding-left:10px;'>");
        sb.AppendFormat("<div class='co-name'>{0} {1}</div>", H(AppSettings("CompanyName")), H(AppSettings("Registration")));
        sb.AppendFormat("<div class='co-sub'>{0} {1}</div>", H(AppSettings("Address1")), H(AppSettings("Address2")));
        sb.AppendFormat("<div class='co-sub'>{0} {1}, {2}</div>", H(AppSettings("PostCode")), H(AppSettings("City")), H(AppSettings("State")));
        sb.AppendFormat("<div class='co-sub'>{0}</div>", H(AppSettings("Phone")));
        sb.AppendLine("</td></tr></table>");
        sb.AppendLine("<hr class='gold-line'>");

        // ── Report title ───────────────────────────────────────────────────
        sb.AppendLine("<div class='report-title'>INVENTORY STOCK LEDGER REPORT</div>");

        // ── Filter summary ─────────────────────────────────────────────────
        sb.AppendLine("<div class='filters'>");
        sb.AppendFormat("<span><b>Warehouse:</b> {0}</span>", H(whName));
        sb.AppendFormat("<span><b>Item:</b> {0}</span>", H(itemName));
        sb.AppendFormat("<span><b>Transaction Type:</b> {0}</span>", H(typeLbl));
        sb.AppendFormat("<span><b>Period:</b> {0}</span>", H(period));
        sb.AppendLine("</div>");

        // ── Data table ─────────────────────────────────────────────────────
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr>");
        foreach (var c in cols)
        {
            string cls = numFlds.Contains(c[1]) ? " class='num'" : "";
            sb.AppendFormat("<th{0}>{1}</th>", cls, H(c[0]));
        }
        sb.AppendLine("</tr></thead>");

        sb.AppendLine("<tbody>");
        decimal totalIn = 0, totalOut = 0;
        foreach (DataRow dr in dt.Rows)
        {
            // Get TransactionType + ReferenceID once per row
            string rowTxType = dt.Columns.Contains("TransactionType")
                ? (dr["TransactionType"] == DBNull.Value ? "" : dr["TransactionType"].ToString().ToUpper())
                : "";
            object rowRefIdRaw = dt.Columns.Contains("ReferenceID") ? dr["ReferenceID"] : null;
            string rowRefId    = (rowRefIdRaw != null && rowRefIdRaw != DBNull.Value) ? rowRefIdRaw.ToString() : "";

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
                    else if (numFlds.Contains(fld))
                    { decimal v; txt = decimal.TryParse(raw.ToString(), out v) ? v.ToString("N2") : raw.ToString(); }
                    else txt = raw.ToString();
                }

                if (fld == "InQty")  { decimal v; if (decimal.TryParse(raw != null && raw != DBNull.Value ? raw.ToString() : "", out v)) totalIn  += v; }
                if (fld == "OutQty") { decimal v; if (decimal.TryParse(raw != null && raw != DBNull.Value ? raw.ToString() : "", out v)) totalOut += v; }

                // Ref No: clickable link using target="_parent" to navigate Angular app
                if (fld == "ReferenceNo" && !string.IsNullOrEmpty(txt))
                {
                    string navUrl = BuildNavUrl(baseUrl, rowTxType, rowRefId);
                    if (!string.IsNullOrEmpty(navUrl))
                    {
                        sb.AppendFormat("<td><a class='ref-link' href='{0}' target='_parent'>{1}</a></td>",
                            H(navUrl), H(txt));
                        continue;
                    }
                }

                if (numFlds.Contains(fld))
                {
                    decimal dv; bool isNeg = decimal.TryParse(raw != null && raw != DBNull.Value ? raw.ToString() : "", out dv) && dv < 0;
                    sb.AppendFormat("<td class='{0}'>{1}</td>", isNeg ? "neg" : "num", string.IsNullOrEmpty(txt) ? "&mdash;" : H(txt));
                }
                else
                {
                    sb.AppendFormat("<td>{0}</td>", H(txt));
                }
            }
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</tbody>");

        // ── Totals row ─────────────────────────────────────────────────────
        sb.AppendLine("<tfoot><tr>");
        foreach (var c in cols)
        {
            string fld = c[1];
            string val = fld == "TransactionDate" ? "TOTALS"
                       : fld == "InQty"           ? totalIn.ToString("N2")
                       : fld == "OutQty"          ? totalOut.ToString("N2")
                       : "";
            string cls = numFlds.Contains(fld) ? " class='num'" : "";
            sb.AppendFormat("<td{0}>{1}</td>", cls, H(val));
        }
        sb.AppendLine("</tr></tfoot>");
        sb.AppendLine("</table>");

        // ── Footer ─────────────────────────────────────────────────────────
        sb.AppendFormat("<p style='font-size:10px;color:#888;margin-top:10px;'>Generated: {0}</p>",
            DateTime.Now.ToString("dd MMM yyyy hh:mm tt"));

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    // ════════════════════════════════════════════════════════════════════════
    // EXCEL BUILDER
    // Generates a simple .xlsx using HTML table — no extra library needed.
    // ASP.NET serves it as Excel via content type override.
    // Opens directly in Excel / LibreOffice Calc.
    // ════════════════════════════════════════════════════════════════════════

    private byte[] BuildExcel(string whName, string itemName,
                              string catName, string sizeLbl,
                              string typeLbl, string period, DataTable dt)
    {
        var cols = new string[][]
        {
<<<<<<< HEAD
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
=======
            new[]{ "Date",          "TransactionDate"      },
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
>>>>>>> 754e8b5 (Updated payslip changes)
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
        sb.AppendFormat("<span class=\"fl\">Category: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;", H(catName));
        sb.AppendFormat("<span class=\"fl\">Size: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;", H(sizeLbl));
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
<<<<<<< HEAD
            string alt = (ri % 2 == 1) ? " alt" : "";

=======
            string alt = (ri++ % 2 == 1) ? " alt" : "";
            string rowTxType = dt.Columns.Contains("TransactionType")
                ? (dr["TransactionType"] == DBNull.Value ? "" : dr["TransactionType"].ToString().ToUpper())
                : "";
            object rowRefIdRaw = dt.Columns.Contains("ReferenceID") ? dr["ReferenceID"] : null;
            string rowRefId    = (rowRefIdRaw != null && rowRefIdRaw != DBNull.Value) ? rowRefIdRaw.ToString() : "";
>>>>>>> 754e8b5 (Updated payslip changes)
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

<<<<<<< HEAD
                // Ref No — plain text only in Excel (no hyperlink)
=======
                // Ref No column: render as hyperlink
                if (fld == "ReferenceNo" && !string.IsNullOrEmpty(txt) && !string.IsNullOrEmpty(xlsBaseUrl))
                {
                    string navUrl = BuildNavUrl(xlsBaseUrl, rowTxType, rowRefId);
                    if (!string.IsNullOrEmpty(navUrl))
                    {
                        sb.AppendFormat("<td class=\"td{0}\"><a href=\"{1}\" style=\"color:#0d6efd;text-decoration:underline;\">{2}</a></td>",
                            alt, H(navUrl), H(txt));
                        continue;
                    }
                }

>>>>>>> 754e8b5 (Updated payslip changes)
                if (numFlds.Contains(fld))
                {
                    decimal num; bool ok = decimal.TryParse(txt, out num);
                    string cls = ok && num < 0 ? "tnr" + alt : "tn" + alt;
                    sb.AppendFormat("<td class=\"{0}\">{1}</td>", cls, ok ? num.ToString("N2") : "&mdash;");
                }
                else sb.AppendFormat("<td class=\"td{0}\">{1}</td>", alt, H(txt));
            }
            sb.AppendLine("</tr>");
            ri++;
        }

        // ── Totals row ────────────────────────────────────────────────────
        sb.AppendLine("<tr>");
<<<<<<< HEAD
        sb.AppendFormat("<td class=\"tot td\" colspan=\"8\">TOTALS</td>");
        for (int i = 5; i < cols.Length; i++)
=======
        foreach (var c in cols)
>>>>>>> 754e8b5 (Updated payslip changes)
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

<<<<<<< HEAD
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
=======
    // ── Navigation URL builder ────────────────────────────────────────────
    // Maps TransactionType code → Angular route with optional :ID segment.
    // Returns empty string if no navigation is defined for that type.
>>>>>>> 754e8b5 (Updated payslip changes)
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

        // If ReferenceID available, go to detail page directly
        if (!string.IsNullOrEmpty(refId))
            return baseUrl + "/" + seg + "/" + refId;

        // Fallback: go to list page for that transaction type
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
        private readonly string                _user;
        private readonly iTextSharp.text.Font  _font;
        private readonly BaseColor             _navy;

        public FooterEvent(string user, iTextSharp.text.Font font, BaseColor navy)
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
