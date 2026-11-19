using System;
using System.Data;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

/// <summary>
/// OBMS Reusable PDF Report Helper — Part 9
/// ==========================================
/// Generates professional A4 PDF reports using iTextSharp 5.5.13.3.
///
/// DESIGNED FOR REUSE — future inventory reports (Current Stock,
/// Purchase, GRN, Stock Issue, Transfer, Adjustment, Damage/Loss,
/// Employee Issue, etc.) call the same helper with their own
/// column definitions and DataTable.
///
/// Usage pattern:
///   var helper = new PdfReportHelper();
///   helper.SetCompanyInfo(name, address1, address2, address3, address4, reg, phone);
///   helper.SetReportTitle("INVENTORY STOCK LEDGER REPORT");
///   helper.AddFilterLine("Warehouse", warehouseName);
///   helper.AddFilterLine("Period", "01 Jan 2026 – 07 Oct 2026");
///   helper.SetColumns(columnDefs);      // List of PdfColumnDef
///   helper.SetDataTable(dt);
///   helper.SetGeneratedBy(loginID);
///   byte[] pdf = helper.GeneratePdf();
/// </summary>
public class PdfReportHelper
{
    // ── Company info ─────────────────────────────────────────────────────────
    private string _companyName     = string.Empty;
    private string _companyAddress1 = string.Empty;
    private string _companyAddress2 = string.Empty;
    private string _companyAddress3 = string.Empty;  // PostCode + City
    private string _companyAddress4 = string.Empty;  // State
    private string _companyReg      = string.Empty;
    private string _companyPhone    = string.Empty;

    // ── Report metadata ───────────────────────────────────────────────────────
    private string _reportTitle  = string.Empty;
    private string _generatedBy  = string.Empty;

    // ── Filter lines shown below the title ───────────────────────────────────
    private readonly System.Collections.Generic.List<string> _filterLines
        = new System.Collections.Generic.List<string>();

    // ── Data ─────────────────────────────────────────────────────────────────
    private System.Collections.Generic.List<PdfColumnDef> _columns
        = new System.Collections.Generic.List<PdfColumnDef>();
    private DataTable _dataTable = new DataTable();

    // ── Totals footer (optional) ──────────────────────────────────────────────
    private readonly System.Collections.Generic.List<PdfTotalDef> _totals
        = new System.Collections.Generic.List<PdfTotalDef>();

    // ────────────────────────────────────────────────────────────────────────
    // COLOURS  (OBMS navy + light header shading)
    // ────────────────────────────────────────────────────────────────────────
    private static readonly BaseColor ColourNavy       = new BaseColor(0x1a, 0x3a, 0x5c);
    private static readonly BaseColor ColourHeaderBg   = new BaseColor(0x1a, 0x3a, 0x5c);
    private static readonly BaseColor ColourHeaderFg   = BaseColor.WHITE;
    private static readonly BaseColor ColourRowAlt     = new BaseColor(0xf4, 0xf7, 0xfb);
    private static readonly BaseColor ColourRowNormal  = BaseColor.WHITE;
    private static readonly BaseColor ColourTotalBg    = new BaseColor(0xe8, 0xf0, 0xfe);
    private static readonly BaseColor ColourBorder     = new BaseColor(0xcc, 0xcc, 0xcc);
    private static readonly BaseColor ColourNegative   = new BaseColor(0xc6, 0x28, 0x28);
    private static readonly BaseColor ColourFilterText = new BaseColor(0x44, 0x44, 0x44);

    // ────────────────────────────────────────────────────────────────────────
    // FONTS
    // ────────────────────────────────────────────────────────────────────────
    private static readonly Font FontCompanyName  = new Font(Font.FontFamily.HELVETICA, 13f, Font.BOLD,   ColourNavy);
    private static readonly Font FontCompanySub   = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, BaseColor.DARK_GRAY);
    private static readonly Font FontTitle        = new Font(Font.FontFamily.HELVETICA, 11f, Font.BOLD,   ColourNavy);
    private static readonly Font FontFilter       = new Font(Font.FontFamily.HELVETICA,  8f, Font.NORMAL, ColourFilterText);
    private static readonly Font FontFilterLabel  = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   ColourFilterText);
    private static readonly Font FontTableHeader  = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   ColourHeaderFg);
    private static readonly Font FontTableCell    = new Font(Font.FontFamily.HELVETICA,  7.5f, Font.NORMAL, BaseColor.BLACK);
    private static readonly Font FontTableCellRed = new Font(Font.FontFamily.HELVETICA,  7.5f, Font.BOLD,   ColourNegative);
    private static readonly Font FontTotal        = new Font(Font.FontFamily.HELVETICA,  8f, Font.BOLD,   ColourNavy);
    private static readonly Font FontFooter       = new Font(Font.FontFamily.HELVETICA,  7f, Font.NORMAL, BaseColor.GRAY);

    // ════════════════════════════════════════════════════════════════════════
    // PUBLIC API
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Set company header info from Web.config AppSettings.</summary>
    public void SetCompanyInfo(string name, string addr1, string addr2,
                               string addr3, string addr4, string reg, string phone)
    {
        _companyName     = name  ?? string.Empty;
        _companyAddress1 = addr1 ?? string.Empty;
        _companyAddress2 = addr2 ?? string.Empty;
        _companyAddress3 = addr3 ?? string.Empty;
        _companyAddress4 = addr4 ?? string.Empty;
        _companyReg      = reg   ?? string.Empty;
        _companyPhone    = phone ?? string.Empty;
    }

    /// <summary>Report title shown below the company header.</summary>
    public void SetReportTitle(string title)
    {
        _reportTitle = title ?? string.Empty;
    }

    /// <summary>
    /// Add one filter line to the header summary.
    /// Example: AddFilterLine("Warehouse", "Main Store")
    ///          AddFilterLine("Period", "01 Jan 2026 – 07 Oct 2026")
    /// </summary>
    public void AddFilterLine(string label, string value)
    {
        _filterLines.Add(string.Format("{0}: {1}", label, value ?? "All"));
    }

    /// <summary>Column definitions — controls header labels, data fields, widths, alignment.</summary>
    public void SetColumns(System.Collections.Generic.List<PdfColumnDef> columns)
    {
        _columns = columns ?? new System.Collections.Generic.List<PdfColumnDef>();
    }

    /// <summary>DataTable returned from the stored procedure.</summary>
    public void SetDataTable(DataTable dt)
    {
        _dataTable = dt ?? new DataTable();
    }

    /// <summary>User login for the "Generated by" footer line.</summary>
    public void SetGeneratedBy(string loginID)
    {
        _generatedBy = loginID ?? string.Empty;
    }

    /// <summary>
    /// Optional: Add a totals row at the bottom of the table.
    /// Example: AddTotal("InQty", "Total In")
    /// </summary>
    public void AddTotal(string fieldName, string label)
    {
        _totals.Add(new PdfTotalDef { FieldName = fieldName, Label = label });
    }

    /// <summary>
    /// Generate the PDF and return it as a byte array.
    /// Throws InvalidOperationException if columns or DataTable not set.
    /// </summary>
    public byte[] GeneratePdf()
    {
        if (_columns == null || _columns.Count == 0)
            throw new InvalidOperationException("PdfReportHelper: No columns defined.");

        using (var ms = new MemoryStream())
        {
            // A4 landscape
            var pageSize = PageSize.A4.Rotate();
            var doc = new Document(pageSize, 25f, 25f, 30f, 30f);
            var writer = PdfWriter.GetInstance(doc, ms);

            // Page event for page number + footer
            var pageEvent = new PdfPageEvent(_generatedBy, _reportTitle);
            writer.PageEvent = pageEvent;

            doc.Open();

            // ── Company header ────────────────────────────────────────────
            AddCompanyHeader(doc);

            // ── Report title ──────────────────────────────────────────────
            AddReportTitle(doc);

            // ── Filter summary ────────────────────────────────────────────
            if (_filterLines.Count > 0)
                AddFilterSummary(doc);

            doc.Add(new Paragraph(" ") { SpacingAfter = 2f });

            // ── Data table ────────────────────────────────────────────────
            if (_dataTable == null || _dataTable.Rows.Count == 0)
            {
                var noData = new Paragraph("No data found for the selected filters.",
                    new Font(Font.FontFamily.HELVETICA, 9f, Font.ITALIC, BaseColor.GRAY));
                noData.Alignment = Element.ALIGN_CENTER;
                noData.SpacingBefore = 10f;
                doc.Add(noData);
            }
            else
            {
                AddDataTable(doc);
            }

            doc.Close();
            return ms.ToArray();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // PRIVATE BUILDERS
    // ════════════════════════════════════════════════════════════════════════

    private void AddCompanyHeader(Document doc)
    {
        // Company name — centred, large bold
        var pName = new Paragraph(_companyName, FontCompanyName)
        {
            Alignment    = Element.ALIGN_CENTER,
            SpacingAfter = 2f
        };
        doc.Add(pName);

        // Address lines
        foreach (var line in new[] { _companyAddress1, _companyAddress2,
                                     _companyAddress3 + (_companyAddress4.Length > 0 ? ", " + _companyAddress4 : "") })
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                var p = new Paragraph(line, FontCompanySub) { Alignment = Element.ALIGN_CENTER };
                doc.Add(p);
            }
        }

        // Phone + Reg on same line
        if (!string.IsNullOrWhiteSpace(_companyPhone) || !string.IsNullOrWhiteSpace(_companyReg))
        {
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(_companyPhone)) parts.Add(_companyPhone);
            if (!string.IsNullOrWhiteSpace(_companyReg))   parts.Add("Reg: " + _companyReg);
            var pContact = new Paragraph(string.Join("   |   ", parts), FontCompanySub)
            {
                Alignment    = Element.ALIGN_CENTER,
                SpacingAfter = 6f
            };
            doc.Add(pContact);
        }

        // Separator line
        var separator = new iTextSharp.text.pdf.draw.LineSeparator(1f, 100f, ColourNavy,
            Element.ALIGN_CENTER, -2f);
        doc.Add(new Chunk(separator));
        doc.Add(new Paragraph(" ") { SpacingAfter = 2f });
    }

    private void AddReportTitle(Document doc)
    {
        var pTitle = new Paragraph(_reportTitle, FontTitle)
        {
            Alignment    = Element.ALIGN_CENTER,
            SpacingBefore = 4f,
            SpacingAfter  = 6f
        };
        doc.Add(pTitle);
    }

    private void AddFilterSummary(Document doc)
    {
        // Build filter lines as one paragraph, 2 per row separated by spaces
        var filterTable = new PdfPTable(_filterLines.Count > 3 ? 3 : _filterLines.Count)
        {
            WidthPercentage = 100f,
            SpacingAfter    = 6f
        };
        filterTable.DefaultCell.Border    = Rectangle.NO_BORDER;
        filterTable.DefaultCell.Padding   = 2f;

        foreach (var line in _filterLines)
        {
            var idx   = line.IndexOf(':');
            var label = idx >= 0 ? line.Substring(0, idx + 1) : line;
            var value = idx >= 0 ? line.Substring(idx + 1).Trim() : string.Empty;

            var phrase = new Phrase();
            phrase.Add(new Chunk(label + " ", FontFilterLabel));
            phrase.Add(new Chunk(value, FontFilter));

            var cell = new PdfPCell(phrase)
            {
                Border  = Rectangle.NO_BORDER,
                Padding = 2f
            };
            filterTable.AddCell(cell);
        }

        // Pad to complete the last row if needed
        int rem = _filterLines.Count % (filterTable.NumberOfColumns);
        if (rem != 0)
        {
            for (int i = 0; i < filterTable.NumberOfColumns - rem; i++)
                filterTable.AddCell(new PdfPCell(new Phrase("")) { Border = Rectangle.NO_BORDER });
        }

        doc.Add(filterTable);
    }

    private void AddDataTable(Document doc)
    {
        // Build relative widths array from PdfColumnDef
        float[] widths = new float[_columns.Count];
        for (int i = 0; i < _columns.Count; i++)
            widths[i] = _columns[i].RelativeWidth;

        var table = new PdfPTable(_columns.Count)
        {
            WidthPercentage = 100f,
            HeaderRows       = 1      // first row repeats on every page
        };
        table.SetWidths(widths);

        // ── Header row ────────────────────────────────────────────────────
        foreach (var col in _columns)
        {
            var cell = new PdfPCell(new Phrase(col.Header, FontTableHeader))
            {
                BackgroundColor  = ColourHeaderBg,
                HorizontalAlignment = col.IsNumeric ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT,
                Padding          = 5f,
                Border           = Rectangle.BOX,
                BorderColor      = ColourBorder
            };
            table.AddCell(cell);
        }

        // ── Data rows ─────────────────────────────────────────────────────
        int rowIndex = 0;
        foreach (DataRow dr in _dataTable.Rows)
        {
            var rowBg = (rowIndex % 2 == 0) ? ColourRowNormal : ColourRowAlt;
            rowIndex++;

            foreach (var col in _columns)
            {
                var rawVal = dr.Table.Columns.Contains(col.FieldName)
                    ? dr[col.FieldName]
                    : null;

                string displayText = FormatCellValue(rawVal, col);

                // Choose font — negative numbers in red
                Font cellFont = FontTableCell;
                if (col.IsNumeric && rawVal != null && rawVal != DBNull.Value)
                {
                    double d;
                    if (double.TryParse(rawVal.ToString(), out d) && d < 0)
                        cellFont = FontTableCellRed;
                }

                var cell = new PdfPCell(new Phrase(displayText, cellFont))
                {
                    BackgroundColor     = rowBg,
                    HorizontalAlignment = col.IsNumeric ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT,
                    Padding             = 4f,
                    Border              = Rectangle.BOX,
                    BorderColor         = ColourBorder
                };
                table.AddCell(cell);
            }
        }

        // ── Totals row ────────────────────────────────────────────────────
        if (_totals.Count > 0)
        {
            // Pre-calculate totals
            var totalValues = new System.Collections.Generic.Dictionary<string, decimal>();
            foreach (var tot in _totals)
            {
                decimal sum = 0;
                if (_dataTable.Columns.Contains(tot.FieldName))
                {
                    foreach (DataRow dr in _dataTable.Rows)
                    {
                        if (dr[tot.FieldName] != DBNull.Value)
                            sum += Convert.ToDecimal(dr[tot.FieldName]);
                    }
                }
                totalValues[tot.FieldName] = sum;
            }

            // Build a lookup: columnIndex → total label/value
            for (int i = 0; i < _columns.Count; i++)
            {
                var col   = _columns[i];
                var match = _totals.Find(t => t.FieldName == col.FieldName);

                string cellText;
                Font   cellFont;
                int    align;

                if (i == 0 && _totals.Count > 0)
                {
                    // First cell always shows "TOTALS" label
                    cellText = "TOTALS";
                    cellFont = FontTotal;
                    align    = Element.ALIGN_LEFT;
                }
                else if (match != null)
                {
                    decimal val = totalValues.ContainsKey(match.FieldName)
                        ? totalValues[match.FieldName] : 0;
                    cellText = val.ToString("N2");
                    cellFont = FontTotal;
                    align    = Element.ALIGN_RIGHT;
                }
                else
                {
                    cellText = string.Empty;
                    cellFont = FontTotal;
                    align    = Element.ALIGN_LEFT;
                }

                var cell = new PdfPCell(new Phrase(cellText, cellFont))
                {
                    BackgroundColor     = ColourTotalBg,
                    HorizontalAlignment = align,
                    Padding             = 5f,
                    Border              = Rectangle.BOX,
                    BorderColor         = ColourNavy,
                    BorderWidth         = 1.5f
                };
                table.AddCell(cell);
            }
        }

        doc.Add(table);
    }

    private static string FormatCellValue(object val, PdfColumnDef col)
    {
        if (val == null || val == DBNull.Value)
            return col.IsNumeric ? "—" : string.Empty;

        if (col.IsDate)
        {
            DateTime dt;
            if (DateTime.TryParse(val.ToString(), out dt))
                return dt.ToString("dd MMM yyyy");
            return val.ToString();
        }

        if (col.IsNumeric)
        {
            decimal d;
            if (decimal.TryParse(val.ToString(), out d))
                return d.ToString("N2");
            return val.ToString();
        }

        return val.ToString();
    }
}

// ════════════════════════════════════════════════════════════════════════
// SUPPORT CLASSES
// ════════════════════════════════════════════════════════════════════════

/// <summary>
/// Defines one column in the PDF table.
/// </summary>
public class PdfColumnDef
{
    /// <summary>Column heading displayed in the PDF header row.</summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>DataTable column name to read the value from.</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>Relative width — used to proportion column widths across the page.</summary>
    public float RelativeWidth { get; set; } = 1f;

    /// <summary>True = right-aligned and formatted as N2 decimal. Negative values shown in red.</summary>
    public bool IsNumeric { get; set; } = false;

    /// <summary>True = formatted as "dd MMM yyyy".</summary>
    public bool IsDate { get; set; } = false;
}

/// <summary>
/// Defines one totals row entry — summed at the bottom of the table.
/// </summary>
public class PdfTotalDef
{
    public string FieldName { get; set; } = string.Empty;
    public string Label     { get; set; } = string.Empty;
}

/// <summary>
/// iTextSharp page event — draws page number and footer on every page.
/// </summary>
internal class PdfPageEvent : PdfPageEventHelper
{
    private readonly string _generatedBy;
    private readonly string _reportTitle;

    private static readonly Font FontFooter = new Font(
        Font.FontFamily.HELVETICA, 7f, Font.NORMAL, BaseColor.GRAY);
    private static readonly BaseColor ColourNavy = new BaseColor(0x1a, 0x3a, 0x5c);

    public PdfPageEvent(string generatedBy, string reportTitle)
    {
        _generatedBy = generatedBy ?? string.Empty;
        _reportTitle = reportTitle ?? string.Empty;
    }

    public override void OnEndPage(PdfWriter writer, Document document)
    {
        var cb   = writer.DirectContent;
        var page = document.PageSize;

        // Footer separator line
        cb.SetLineWidth(0.5f);
        cb.SetColorStroke(new BaseColor(0xcc, 0xcc, 0xcc));
        cb.MoveTo(document.LeftMargin, document.BottomMargin - 5f);
        cb.LineTo(page.Width - document.RightMargin, document.BottomMargin - 5f);
        cb.Stroke();

        // Left: Generated by
        var leftText  = string.Format("Generated by: {0}   |   {1}",
            _generatedBy,
            DateTime.Now.ToString("dd MMM yyyy hh:mm tt"));
        var leftPhrase = new Phrase(leftText, FontFooter);
        ColumnText.ShowTextAligned(cb, Element.ALIGN_LEFT, leftPhrase,
            document.LeftMargin, document.BottomMargin - 15f, 0);

        // Right: Page number
        var rightText  = string.Format("Page {0}", writer.PageNumber);
        var rightPhrase = new Phrase(rightText, FontFooter);
        ColumnText.ShowTextAligned(cb, Element.ALIGN_RIGHT, rightPhrase,
            page.Width - document.RightMargin, document.BottomMargin - 15f, 0);
    }
}
