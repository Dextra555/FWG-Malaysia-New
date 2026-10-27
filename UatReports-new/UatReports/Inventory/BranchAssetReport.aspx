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
    // OBMS Branch-wise Asset Report — PDF / Excel
    // Single-file inline C# (no code-behind) — same pattern as StockIssueListReport.aspx
    // Flow: Angular → ASPX → sp_BranchAssetReport → PDF / Excel → Browser
    //
    // Current status snapshot (no date filter).
    // Columns: No, Allocation No, Alloc Date, Asset Code, Asset Name,
    //          Asset Type, Serial No, Brand, Model, Purchase Date,
    //          Purchase Value, Status, Condition, Location
    //
    // PDF: grouped by Branch with a branch header row + count + value sub-total
    // ════════════════════════════════════════════════════════════════════════

    protected void Page_Load(object sender, EventArgs e)
    {
        // ── 1. Read query-string params ───────────────────────────────────
        string loginID       = (Request.QueryString["LoginID"]      ?? "").Trim();
        string branchStr     = (Request.QueryString["BranchID"]     ?? "0").Trim();
        string assetStatus   = (Request.QueryString["AssetStatus"]  ?? "ALL").Trim();
        string assetType     = (Request.QueryString["AssetType"]    ?? "ALL").Trim();
        string format        = (Request.QueryString["format"]       ?? "pdf").ToLower().Trim();

        int branchID = 0; int.TryParse(branchStr, out branchID);
        if (string.IsNullOrWhiteSpace(assetStatus)) assetStatus = "ALL";
        if (string.IsNullOrWhiteSpace(assetType))   assetType   = "ALL";

        // ── 2. Execute stored procedure ───────────────────────────────────
        DataTable dt;
        try   { dt = RunSP(branchID, assetStatus, assetType); }
        catch (Exception ex)
        {
            LogErr("sp_BranchAssetReport", ex.Message);
            SendError("Unable to retrieve asset data. Please try again.");
            return;
        }

        if (dt == null || dt.Rows.Count == 0)
        { SendError("No asset data found for the selected filters."); return; }

        // ── 3. Resolve display names ──────────────────────────────────────
        string brName    = branchID > 0
            ? (Lookup("SELECT Name FROM BranchMaster WHERE ID=@p", branchID.ToString()) ?? branchStr)
            : "All Branches";
        string stLabel   = assetStatus == "ALL" ? "All Statuses" : assetStatus;
        string typeLabel = assetType   == "ALL" ? "All Types"    : assetType;
        string asOf      = DateTime.Now.ToString("dd MMM yyyy");

        // ── 4. Build PDF or Excel ─────────────────────────────────────────
        if (format == "excel")
        {
            byte[] xlsx;
            try   { xlsx = BuildExcel(brName, stLabel, typeLabel, asOf, dt); }
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
                "attachment; filename=BranchAsset_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls");
            Response.AddHeader("Content-Length", xlsx.Length.ToString());
            Response.AddHeader("Access-Control-Allow-Origin", "*");
            Response.BinaryWrite(xlsx);
            Response.Flush();
            Response.End();
            return;
        }

        // ── 4b. Build PDF ─────────────────────────────────────────────────
        byte[] pdf;
        try   { pdf = BuildPdf(loginID, brName, stLabel, typeLabel, asOf, dt); }
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
            "inline; filename=BranchAsset_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
        Response.AddHeader("Content-Length", pdf.Length.ToString());
        Response.AddHeader("Access-Control-Allow-Origin", "*");
        Response.BinaryWrite(pdf);
        Response.Flush();
        Response.End();
    }

    // ════════════════════════════════════════════════════════════════════════
    // PDF BUILDER  (grouped by Branch)
    // ════════════════════════════════════════════════════════════════════════

    private byte[] BuildPdf(string loginID, string brName, string stLabel,
                            string typeLabel, string asOf, DataTable dt)
    {
        // ── Colours ────────────────────────────────────────────────────────
        BaseColor navy      = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor headerBg  = new BaseColor(0x1a, 0x3a, 0x5c);
        BaseColor brHdrBg   = new BaseColor(0x2e, 0x5c, 0x99);
        BaseColor rowAlt    = new BaseColor(0xf4, 0xf7, 0xfb);
        BaseColor subTotBg  = new BaseColor(0xd0, 0xe4, 0xff);
        BaseColor totalBg   = new BaseColor(0xe8, 0xf0, 0xfe);
        BaseColor border    = new BaseColor(0xcc, 0xcc, 0xcc);
        BaseColor gold      = new BaseColor(0xD4, 0xA0, 0x17);
        // Status colours
        BaseColor dispBg    = new BaseColor(0xff, 0xe5, 0xe5);   // light red — Disposed/Damaged
        BaseColor maintBg   = new BaseColor(0xff, 0xf3, 0xcd);   // amber — Under_Maintenance

        // ── Fonts ──────────────────────────────────────────────────────────
        Font fTitle   = new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD,   navy);
        Font fFilter  = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, new BaseColor(0x44,0x44,0x44));
        Font fFilterB = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   new BaseColor(0x44,0x44,0x44));
        Font fTH      = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   BaseColor.WHITE);
        Font fBrHdr   = new Font(Font.FontFamily.HELVETICA,  9f, Font.BOLD,   BaseColor.WHITE);
        Font fTD      = new Font(Font.FontFamily.HELVETICA, 7f, Font.NORMAL, BaseColor.BLACK);
        Font fTDRed   = new Font(Font.FontFamily.HELVETICA, 7f, Font.BOLD,   new BaseColor(0xc6,0x28,0x28));
        Font fSubTot  = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
        Font fTotal   = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   navy);
        Font fFoot    = new Font(Font.FontFamily.HELVETICA,  7f, Font.NORMAL, BaseColor.GRAY);

        // ── Column definitions ─────────────────────────────────────────────
        var cols = new object[][]
        {
            new object[]{ "S.NO.",            "#",               1.0f,  true  },
            new object[]{ "Alloc No",      "AllocationNo",    1.6f,  false },
            new object[]{ "Alloc Date",    "AllocationDate",  1.5f,  false },
            new object[]{ "Asset Code",    "AssetCode",       1.4f,  false },
            new object[]{ "Asset Name",    "AssetName",       2.8f,  false },
            new object[]{ "Asset Type",    "AssetType",       1.6f,  false },
            new object[]{ "Serial No",     "SerialNumber",    1.8f,  false },
            new object[]{ "Brand",         "BrandName",       1.5f,  false },
            new object[]{ "Model",         "ModelNo",         1.5f,  false },
            new object[]{ "Purchase Date", "PurchaseDate",    1.4f,  false },
            new object[]{ "Purch Value",   "PurchaseValue",   1.6f,  true  },
            new object[]{ "Status",        "AssetStatus",     1.5f,  false },
            new object[]{ "Condition",     "AssetCondition",  1.3f,  false },
            new object[]{ "Location",      "CurrentLocation", 1.8f,  false },
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
            var doc    = new Document(PageSize.A4.Rotate(), 15f, 15f, 30f, 30f);
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

            P(doc, "BRANCH-WISE ASSET REPORT", fTitle, Element.ALIGN_CENTER, 4f);

            // ── Filter summary ─────────────────────────────────────────────
            var ft = new PdfPTable(2) { WidthPercentage = 100f, SpacingAfter = 6f };
            ft.SetWidths(new float[] { 1f, 1f });
            ft.DefaultCell.Border = Rectangle.NO_BORDER;
            string[][] filterPairs = new string[][]
            {
                new[]{ "Branch:",       brName    },
                new[]{ "Asset Status:", stLabel   },
                new[]{ "Asset Type:",   typeLabel },
                new[]{ "Period:",        asOf      }
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

            // ── Grouped by Branch ──────────────────────────────────────────
            int     riGlobal   = 0;
            decimal grandVal   = 0;  int grandCount  = 0;
            decimal brVal      = 0;  int brCount     = 0;
            string  currentBr  = null;

            foreach (DataRow dr in dt.Rows)
            {
                string thisBr = dr["BranchName"] != DBNull.Value
                    ? dr["BranchName"].ToString() : "Unallocated";
                string thisBrCode = dr["BranchCode"] != DBNull.Value
                    ? dr["BranchCode"].ToString() : "";

                // Branch change → emit sub-total
                if (currentBr != null && currentBr != thisBr)
                {
                    AddBranchSubTotalRow(tbl, cols, labelSpan,
                        "Sub-Total: " + currentBr + "   (" + brCount + " asset" + (brCount != 1 ? "s" : "") + ")",
                        brVal, subTotBg, fSubTot, border, navy);
                    brVal = 0; brCount = 0;
                }

                // Branch header
                if (currentBr != thisBr)
                {
                    string brLabel = thisBr;
                    if (!string.IsNullOrEmpty(thisBrCode)) brLabel += "  (" + thisBrCode + ")";
                    tbl.AddCell(new PdfPCell(new Phrase(brLabel, fBrHdr))
                    {
                        Colspan             = cols.Length,
                        BackgroundColor     = brHdrBg,
                        HorizontalAlignment = Element.ALIGN_LEFT,
                        Padding = 5f, Border = Rectangle.BOX, BorderColor = border
                    });
                    currentBr = thisBr;
                }

                riGlobal++; brCount++; grandCount++;
                string status = dr["AssetStatus"] != DBNull.Value ? dr["AssetStatus"].ToString() : "";
                BaseColor rowBg = (status == "Disposed" || status == "Damaged") ? dispBg
                                : status == "Under_Maintenance"                  ? maintBg
                                : (riGlobal % 2 == 0 ? rowAlt : BaseColor.WHITE);

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

                        if (field == "AssetStatus")
                        {
                            if (status == "Disposed" || status == "Damaged") cf = fTDRed;
                        }
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
                                if (field == "PurchaseValue") { brVal  += v; grandVal  += v; }
                            }
                        }
                    }

                    tbl.AddCell(new PdfPCell(new Phrase(txt, cf))
                    {
                        BackgroundColor     = rowBg,
                        HorizontalAlignment = (isNum || field == "#") ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT,
                        Padding = 3f, Border = Rectangle.BOX, BorderColor = border
                    });
                }
            }

            // Sub-total for last branch
            if (currentBr != null)
            {
                AddBranchSubTotalRow(tbl, cols, labelSpan,
                    "Sub-Total: " + currentBr + "   (" + brCount + " asset" + (brCount != 1 ? "s" : "") + ")",
                    brVal, subTotBg, fSubTot, border, navy);
            }

            // ── Grand Total ────────────────────────────────────────────────
            tbl.AddCell(new PdfPCell(new Phrase("GRAND TOTAL   (" + grandCount + " assets)", fTotal))
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
                string txt   = field == "PurchaseValue" ? grandVal.ToString("N2") : "";
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

    private static void AddBranchSubTotalRow(
        PdfPTable tbl, object[][] cols, int labelSpan,
        string label, decimal val,
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
            string txt   = field == "PurchaseValue" ? val.ToString("N2") : "";
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

    private byte[] BuildExcel(string brName, string stLabel, string typeLabel,
                              string asOf, DataTable dt)
    {
        var cols = new string[][]
        {
            new[]{ "S.NO.",            "#"               },
            new[]{ "Branch",        "BranchName"      },
            new[]{ "Branch Code",   "BranchCode"      },
            new[]{ "Alloc No",      "AllocationNo"    },
            new[]{ "Alloc Date",    "AllocationDate"  },
            new[]{ "Asset Code",    "AssetCode"       },
            new[]{ "Asset Name",    "AssetName"       },
            new[]{ "Asset Type",    "AssetType"       },
            new[]{ "Serial No",     "SerialNumber"    },
            new[]{ "Brand",         "BrandName"       },
            new[]{ "Model",         "ModelNo"         },
            new[]{ "Purchase Date", "PurchaseDate"    },
            new[]{ "Purch Value",   "PurchaseValue"   },
            new[]{ "Status",        "AssetStatus"     },
            new[]{ "Condition",     "AssetCondition"  },
            new[]{ "Location",      "CurrentLocation" },
        };

        var numFlds = new System.Collections.Generic.HashSet<string> { "PurchaseValue" };

        int span = cols.Length;
        var sb   = new System.Text.StringBuilder();

        sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" " +
                      "xmlns:x=\"urn:schemas-microsoft-com:office:excel\" " +
                      "xmlns=\"http://www.w3.org/TR/REC-html40\">");
        sb.AppendLine("<head><meta charset=\"UTF-8\"/>");
        sb.AppendLine("<!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets>" +
                      "<x:ExcelWorksheet><x:Name>Branch Assets</x:Name>" +
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
        sb.AppendLine(".brh{background:#2e5c99;color:#fff;font-weight:bold;font-size:9pt;padding:4px;border:1px solid #ccc;}");
        sb.AppendLine(".td{font-size:8.5pt;padding:4px;border:1px solid #ddd;}");
        sb.AppendLine(".tn{font-size:8.5pt;padding:4px;border:1px solid #ddd;text-align:right;}");
        sb.AppendLine(".alt{background:#f4f7fb;}");
        sb.AppendLine(".disp{background:#ffe5e5;color:#c62828;}");
        sb.AppendLine(".maint{background:#fff3cd;}");
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

        sb.AppendFormat("<tr><td colspan=\"{0}\" class=\"ttl\">BRANCH-WISE ASSET REPORT</td></tr>\n", span);
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:2px;\"></td></tr>\n", span);

        sb.AppendFormat("<tr><td colspan=\"{0}\">", span);
        sb.AppendFormat("<span class=\"fl\">Branch: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;",       H(brName));
        sb.AppendFormat("<span class=\"fl\">Asset Status: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;", H(stLabel));
        sb.AppendFormat("<span class=\"fl\">Asset Type: </span><span class=\"fv\">{0}</span>&nbsp;&nbsp;&nbsp;",   H(typeLabel));
        sb.AppendFormat("<span class=\"fl\">Period: </span><span class=\"fv\">{0}</span>",                          H(asOf));
        sb.AppendLine("</td></tr>");
        sb.AppendFormat("<tr><td colspan=\"{0}\" style=\"padding:3px;\"></td></tr>\n", span);

        // Column headers
        sb.AppendLine("<tr>");
        foreach (var c in cols) sb.AppendFormat("<td class=\"th\">{0}</td>", H(c[0]));
        sb.AppendLine("</tr>");

        // Data rows
        string  curBr     = null;
        int     ri        = 0;
        decimal grandVal  = 0; int grandCnt = 0;
        decimal brVal     = 0; int brCnt    = 0;

        int xlsLabel = 0;
        for (int i = 0; i < cols.Length; i++)
        {
            if (!numFlds.Contains(cols[i][1])) xlsLabel++;
            else break;
        }
        if (xlsLabel < 1) xlsLabel = 1;

        foreach (DataRow dr in dt.Rows)
        {
            string thisBr     = dr["BranchName"] != DBNull.Value ? dr["BranchName"].ToString() : "Unallocated";
            string thisBrCode = dr["BranchCode"] != DBNull.Value ? dr["BranchCode"].ToString()  : "";
            string status     = dr["AssetStatus"]!= DBNull.Value ? dr["AssetStatus"].ToString()  : "";

            if (curBr != null && curBr != thisBr)
            {
                sb.AppendLine("<tr>");
                sb.AppendFormat("<td class=\"stot td\" colspan=\"{0}\"><b>Sub-Total: {1} ({2} asset{3})</b></td>",
                    xlsLabel, H(curBr), brCnt, brCnt != 1 ? "s" : "");
                for (int i = xlsLabel; i < cols.Length; i++)
                {
                    string fld = cols[i][1];
                    string v   = fld == "PurchaseValue" ? brVal.ToString("N2") : "";
                    sb.AppendFormat("<td class=\"stot tn\">{0}</td>", H(v));
                }
                sb.AppendLine("</tr>");
                brVal = 0; brCnt = 0;
            }

            if (curBr != thisBr)
            {
                string brLabel = thisBr;
                if (!string.IsNullOrEmpty(thisBrCode)) brLabel += "  (" + thisBrCode + ")";
                sb.AppendFormat("<tr><td class=\"brh\" colspan=\"{0}\">{1}</td></tr>\n", span, H(brLabel));
                curBr = thisBr;
            }

            ri++; brCnt++; grandCnt++;
            bool isDisp  = status == "Disposed" || status == "Damaged";
            bool isMaint = status == "Under_Maintenance";
            string alt   = (ri % 2 == 1) ? "" : " alt";

            sb.AppendLine("<tr>");
            foreach (var c in cols)
            {
                string fld = c[1];
                if (fld == "#") { sb.AppendFormat("<td class=\"tn{0}\">{1}</td>", alt, ri); continue; }

                object raw = dt.Columns.Contains(fld) ? dr[fld] : null;
                string txt = raw != null && raw != DBNull.Value ? raw.ToString() : "";

                if (fld == "PurchaseValue") { decimal v; if (decimal.TryParse(txt, out v)) { brVal += v; grandVal += v; } }

                if (numFlds.Contains(fld))
                {
                    decimal num; bool ok = decimal.TryParse(txt, out num);
                    string cls = isDisp ? "disp tn" : isMaint ? "maint tn" : "tn" + alt;
                    sb.AppendFormat("<td class=\"{0}\">{1}</td>", cls, ok ? num.ToString("N2") : "&mdash;");
                }
                else
                {
                    string cls = isDisp ? "disp td" : isMaint ? "maint td" : "td" + alt;
                    sb.AppendFormat("<td class=\"{0}\">{1}</td>", cls, H(txt));
                }
            }
            sb.AppendLine("</tr>");
        }

        // Last branch sub-total
        if (curBr != null)
        {
            sb.AppendLine("<tr>");
            sb.AppendFormat("<td class=\"stot td\" colspan=\"{0}\"><b>Sub-Total: {1} ({2} asset{3})</b></td>",
                xlsLabel, H(curBr), brCnt, brCnt != 1 ? "s" : "");
            for (int i = xlsLabel; i < cols.Length; i++)
            {
                string fld = cols[i][1];
                string v   = fld == "PurchaseValue" ? brVal.ToString("N2") : "";
                sb.AppendFormat("<td class=\"stot tn\">{0}</td>", H(v));
            }
            sb.AppendLine("</tr>");
        }

        // Grand total
        sb.AppendLine("<tr>");
        sb.AppendFormat("<td class=\"tot td\" colspan=\"{0}\"><b>GRAND TOTAL ({1} assets)</b></td>", xlsLabel, grandCnt);
        for (int i = xlsLabel; i < cols.Length; i++)
        {
            string fld   = cols[i][1];
            bool   isNum = numFlds.Contains(fld);
            string val   = fld == "PurchaseValue" ? grandVal.ToString("N2") : "";
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

    private DataTable RunSP(int branchID, string assetStatus, string assetType)
    {
        var dt = new DataTable();
        using (var conn = new SqlConnection(ConnStr()))
        using (var cmd  = new SqlCommand("sp_BranchAssetReport", conn))
        {
            cmd.CommandType    = CommandType.StoredProcedure;
            cmd.CommandTimeout = 60;
            cmd.Parameters.AddWithValue("@BranchID",    branchID);
            cmd.Parameters.AddWithValue("@AssetStatus", assetStatus ?? "ALL");
            cmd.Parameters.AddWithValue("@AssetType",   assetType   ?? "ALL");
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
            "<h3 style='color:#c62828'>Branch-wise Asset Report</h3>" +
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
