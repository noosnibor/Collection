using ClosedXML.Excel;
using Collection.Library.Model;
using Collection.Library.Service;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Data;
using System.Globalization;
using System.Text;

using IContainer = QuestPDF.Infrastructure.IContainer;

namespace Collection.Web.Components.Services;

#region Export Data Contracts

/// <summary>One labelled amount (a collection type inside a period, a row of the main table, ...).</summary>
public sealed record ExportLine(string Label, decimal Value);

/// <summary>A date-range or location bucket with an optional per-collection-type breakdown.</summary>
public sealed record ExportBucket(string Label, decimal Total, IReadOnlyList<ExportLine> Lines);

/// <summary>
/// Everything the Summary card needs to export. Amounts are ALREADY converted for display
/// (e.g. to USD), so the export always matches what the user sees on screen.
/// </summary>
public sealed class SummaryExportData
{
    public string Title { get; init; } = "Summary";
    public string? Eyebrow { get; init; }
    public string? Badge { get; init; }
    public string TotalLabel { get; init; } = "Total";
    public string CurrencyPrefix { get; init; } = string.Empty;
    public IReadOnlyList<ExportLine> Items { get; init; } = [];
    public decimal Total { get; init; }
    public string DateHeading { get; init; } = "By date range";
    public IReadOnlyList<ExportBucket> DateBuckets { get; init; } = [];
    public string LocationHeading { get; init; } = "By location";
    public IReadOnlyList<ExportBucket> LocationBuckets { get; init; } = [];
    public System.Globalization.CultureInfo Culture { get; init; } = System.Globalization.CultureInfo.InvariantCulture;
}

#endregion

/// <summary>
/// Builds every downloadable report file (CSV, Excel, PDF) in one place so the Razor pages
/// stay free of ClosedXML / QuestPDF code. All methods are pure: data in, bytes out.
/// </summary>
public sealed class ReportExportService
{
    #region Constants (palette follows the portal's Windows 11 style accent)

    private const string Accent = "#0F6CBD";
    private const string AccentSoft = "#EAF2FB";
    private const string AccentStrong = "#D5E6F7";
    private const string Line = "#E3E7EC";
    private const string Ink = "#16202C";
    private const string Muted = "#5B6B7B";
    private const string HeaderFill = "#F5F7F9";
    private const string ExcelNumberFormat = "#,##0.00";
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    #endregion

    #region Summary Card Exports (Excel / PDF)

    /// <summary>Content type used when downloading the .xlsx files created here.</summary>
    public static string XlsxContentType => ExcelContentType;

    /// <summary>Builds the multi-sheet Excel workbook for the Summary card.</summary>
    public byte[] SummaryToExcel(SummaryExportData d)
    {
        ArgumentNullException.ThrowIfNull(d);

        using var workbook = new XLWorkbook();
        var amountHeader = AmountHeader(d.CurrencyPrefix);

        // Sheet 1 – the main totals table.
        var sheet = workbook.Worksheets.Add("Summary");
        WriteHeader(sheet, ["Collection Type", amountHeader]);

        var row = 2;
        foreach (var item in d.Items)
        {
            SetText(sheet.Cell(row, 1), item.Label);
            SetAmount(sheet.Cell(row, 2), item.Value);
            row++;
        }

        SetText(sheet.Cell(row, 1), d.TotalLabel);
        SetAmount(sheet.Cell(row, 2), d.Total);
        StyleTotalRow(sheet.Range(row, 1, row, 2));
        sheet.Columns().AdjustToContents();

        // Optional sheets.
        AddBucketSheet(workbook, "By Date", "Period", d.DateBuckets, amountHeader);
        AddBucketSheet(workbook, "By Location", "Location", d.LocationBuckets, amountHeader);

        return Save(workbook);
    }

    /// <summary>Builds the PDF for the Summary card.</summary>
    public byte[] SummaryToPdf(SummaryExportData d)
    {
        ArgumentNullException.ThrowIfNull(d);

        string Money(decimal v) => $"{d.CurrencyPrefix}{v.ToString("#,##0.00", d.Culture)}";

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Margin(36);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Ink));

                page.Header().Column(h =>
                {
                    if (!string.IsNullOrEmpty(d.Eyebrow))
                        h.Item().Text(d.Eyebrow.ToUpperInvariant()).FontSize(8).FontColor(Muted);
                    h.Item().Text(d.Title).FontSize(16).SemiBold().FontColor(Accent);
                    if (!string.IsNullOrEmpty(d.Badge))
                        h.Item().Text(d.Badge).FontSize(9).FontColor(Muted);
                    h.Item().PaddingTop(6).LineHorizontal(1).LineColor(Line);
                });

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); });
                        table.Header(header =>
                        {
                            header.Cell().Element(HeadCell).Text("Collection Type");
                            header.Cell().Element(HeadCell).AlignRight().Text(AmountHeader(d.CurrencyPrefix));
                        });

                        foreach (var item in d.Items)
                        {
                            table.Cell().Element(BodyCell).Text(item.Label);
                            table.Cell().Element(BodyCell).AlignRight().Text(Money(item.Value));
                        }

                        table.Cell().Element(TotalCell).Text(d.TotalLabel);
                        table.Cell().Element(TotalCell).AlignRight().Text(Money(d.Total));
                    });

                    AddPdfBucketTable(col, d.DateHeading, "Period", d.DateBuckets, Money);
                    AddPdfBucketTable(col, d.LocationHeading, "Location", d.LocationBuckets, Money);
                });

                page.Footer().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Generated ").FontSize(8).FontColor(Muted);
                        t.Span(DateTime.UtcNow.ToString("d MMM yyyy", d.Culture)).FontSize(8).FontColor(Muted);
                    });
                    r.RelativeItem().AlignRight().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(8).FontColor(Muted));
                        t.Span("Page ");
                        t.CurrentPageNumber();
                        t.Span(" of ");
                        t.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }

    /// <summary>Adds a "bucket" sheet (period or location rows, one line per collection type).</summary>
    private static void AddBucketSheet(XLWorkbook workbook, string sheetName, string firstHeader,
                                       IReadOnlyList<ExportBucket> buckets, string amountHeader)
    {
        if (buckets.Count == 0) return;

        var sheet = workbook.Worksheets.Add(sheetName);
        WriteHeader(sheet, [firstHeader, "Collection Type", amountHeader]);

        var row = 2;
        foreach (var bucket in buckets)
        {
            if (bucket.Lines.Count > 0)
            {
                foreach (var line in bucket.Lines)
                {
                    SetText(sheet.Cell(row, 1), bucket.Label);
                    SetText(sheet.Cell(row, 2), line.Label);
                    SetAmount(sheet.Cell(row, 3), line.Value);
                    row++;
                }
            }
            else
            {
                SetText(sheet.Cell(row, 1), bucket.Label);
                SetAmount(sheet.Cell(row, 3), bucket.Total);
                row++;
            }
        }

        sheet.Columns().AdjustToContents();
    }

    /// <summary>Adds a titled period / location table to the PDF.</summary>
    private static void AddPdfBucketTable(ColumnDescriptor col, string heading, string firstHeader,
                                          IReadOnlyList<ExportBucket> buckets, Func<decimal, string> money)
    {
        if (buckets.Count == 0) return;

        col.Item().PaddingTop(18).Text(heading).FontSize(12).SemiBold();
        col.Item().PaddingTop(6).Table(table =>
        {
            table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); });
            table.Header(h =>
            {
                h.Cell().Element(HeadCell).Text(firstHeader);
                h.Cell().Element(HeadCell).AlignRight().Text("Amount");
            });

            foreach (var bucket in buckets)
            {
                table.Cell().Element(BodyCell).Text(bucket.Label);
                table.Cell().Element(BodyCell).AlignRight().Text(money(bucket.Total));
            }
        });
    }

    private static string AmountHeader(string prefix) =>
        string.IsNullOrEmpty(prefix) ? "Amount" : $"Amount ({prefix})";

    #endregion

    #region Detail Report Export (Excel)

    /// <summary>Builds the Excel file for the Detail report grid.</summary>
    public byte[] DetailToExcel(IEnumerable<ReportModel> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Detail Report");

        WriteHeader(sheet,
        [
            "Name", "Collection Number", "Member Type", "Collection Type",
            "Payment Type", "Currency", "Amount", "Local Amount", "Submitted By"
        ]);

        var collectionNumbers = StaticDataService.GetCollectionNumber();

        var row = 2;
        foreach (var item in rows)
        {
            var collectionNumberText = collectionNumbers
                .FirstOrDefault(m => m.flngCollectionNumberKey == item.flngCollectionNumber)
                ?.fstrCollectionNumberType ?? string.Empty;

            SetText(sheet.Cell(row, 1), $"{item.fstrFirstname} {item.fstrLastname}".Trim());
            SetText(sheet.Cell(row, 2), collectionNumberText);
            SetText(sheet.Cell(row, 3), item.fstrMemberTypeKey);
            SetText(sheet.Cell(row, 4), item.fstrCollectionTypeKey);
            SetText(sheet.Cell(row, 5), item.fstrPaymentTypeKey);
            SetText(sheet.Cell(row, 6), item.fstrCurrencyKey);
            SetAmount(sheet.Cell(row, 7), item.fcurAmount);
            SetAmount(sheet.Cell(row, 8), item.fcurLocalAmount);
            SetText(sheet.Cell(row, 9), item.fstrWho);
            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        return Save(workbook);
    }

    #endregion

    #region Shared Excel / PDF Helpers

    /// <summary>Writes a styled header row (row 1) to a worksheet.</summary>
    private static void WriteHeader(IXLWorksheet sheet, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var range = sheet.Range(1, 1, 1, headers.Count);
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml(Accent);
    }

    private static void StyleTotalRow(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        range.Style.Border.TopBorderColor = XLColor.FromHtml(Accent);
    }

    /// <summary>
    /// Writes text safely: a value starting with = + - @ would be run as a formula by Excel,
    /// so such values are forced to be plain text (CSV / formula-injection protection).
    /// </summary>
    private static void SetText(IXLCell cell, string? value)
    {
        value ??= string.Empty;
        if (value.Length > 0 && "=+-@".Contains(value[0]))
            cell.SetValue(value).Style.NumberFormat.Format = "@";
        else
            cell.SetValue(value);
    }

    private static void SetAmount(IXLCell cell, decimal value)
    {
        cell.SetValue(value);
        cell.Style.NumberFormat.Format = ExcelNumberFormat;
    }

    private static byte[] Save(XLWorkbook workbook)
    {
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static IContainer HeadCell(IContainer c) =>
        c.Background(Accent).PaddingVertical(6).PaddingHorizontal(8)
         .DefaultTextStyle(t => t.FontColor("#FFFFFF").SemiBold());

    private static IContainer BodyCell(IContainer c) =>
        c.BorderBottom(0.5f).BorderColor(Line).PaddingVertical(5).PaddingHorizontal(8);

    private static IContainer TotalCell(IContainer c) =>
        c.BorderTop(1).BorderColor(Accent).PaddingVertical(6).PaddingHorizontal(8)
         .DefaultTextStyle(t => t.SemiBold());

    #endregion

    #region Summary-Result Exports (CSV / Excel / PDF)

    // ---------- CSV ----------

    public byte[] ToCsv(SummaryResultModel r)
    {
        var sb = new StringBuilder();

        void WriteRow(IEnumerable<string> cells) =>
            sb.Append(string.Join(',', cells.Select(EscapeCsv))).Append("\r\n");

        WriteRow(new[] { r.GroupedBy, "Records" }.Concat(r.MeasureKeys));

        foreach (var g in r.Groups)
            WriteRow(new[] { g.Key, g.Count.ToString(CultureInfo.InvariantCulture) }
                .Concat(r.MeasureKeys.Select(k => g.Measures[k].ToString("0.00", CultureInfo.InvariantCulture))));

        WriteRow(new[] { "Grand total", r.GrandCount.ToString(CultureInfo.InvariantCulture) }
            .Concat(r.MeasureKeys.Select(k => r.GrandTotals[k].ToString("0.00", CultureInfo.InvariantCulture))));

        // UTF-8 BOM so Excel opens it with the right encoding.
        return Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();
    }

    /// <summary>Quotes a CSV cell and neutralises spreadsheet formulas (=, +, -, @).</summary>
    private static string EscapeCsv(string s)
    {
        s ??= string.Empty;
        if (s.Length > 0 && "=+-@".Contains(s[0]) && !decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            s = "'" + s;

        return s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r')
            ? $"\"{s.Replace("\"", "\"\"")}\""
            : s;
    }

    // ---------- Excel ----------

    public byte[] ToExcel(SummaryResultModel r, string title)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Summary");
        var lastCol = 2 + r.MeasureKeys.Count;

        ws.Cell(1, 1).Value = title;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, lastCol).Merge();

        ws.Cell(2, 1).Value = $"{r.GrandCount} records  ·  grouped by {r.GroupedBy}";
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml(Muted);
        ws.Range(2, 1, 2, lastCol).Merge();

        var headerRow = 4;
        ws.Cell(headerRow, 1).Value = r.GroupedBy;
        ws.Cell(headerRow, 2).Value = "Records";
        for (var i = 0; i < r.MeasureKeys.Count; i++)
            ws.Cell(headerRow, 3 + i).Value = r.MeasureKeys[i];

        var header = ws.Range(headerRow, 1, headerRow, lastCol);
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml(Accent);
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        var row = headerRow + 1;
        foreach (var g in r.Groups)
        {
            ws.Cell(row, 1).Value = g.Key;
            ws.Cell(row, 2).Value = g.Count;
            for (var i = 0; i < r.MeasureKeys.Count; i++)
            {
                var cell = ws.Cell(row, 3 + i);
                cell.Value = g.Measures[r.MeasureKeys[i]];
                cell.Style.NumberFormat.Format = ExcelNumberFormat;
            }
            row++;
        }

        ws.Cell(row, 1).Value = "Grand total";
        ws.Cell(row, 2).Value = r.GrandCount;
        for (var i = 0; i < r.MeasureKeys.Count; i++)
        {
            var cell = ws.Cell(row, 3 + i);
            cell.Value = r.GrandTotals[r.MeasureKeys[i]];
            cell.Style.NumberFormat.Format = ExcelNumberFormat;
        }

        var totalRange = ws.Range(row, 1, row, lastCol);
        totalRange.Style.Font.Bold = true;
        totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        totalRange.Style.Border.TopBorderColor = XLColor.FromHtml(Accent);

        // Right-align numeric columns across the data block.
        ws.Range(headerRow, 2, row, lastCol).Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Right;

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ---------- Pivot DataTable -> styled Excel ----------

    /// <summary>
    /// Exports a (pivot) <see cref="DataTable"/> to .xlsx using the same visual
    /// language as the on-screen PivotTable: humanized + wrapped headers, a tinted
    /// Total column, an accented Total row, right-aligned tabular numbers, hairline
    /// rules, and a frozen header row + first column.
    /// </summary>
    public byte[] PivotToExcel(
        DataTable table,
        string? title = null,
        string? subtitle = null,
        string totalRowLabel = "Total",
        string totalColumnName = "Total")
    {
        const string HairLine = "#E6EAEE";
        const string HairLine2 = "#D8DEE4";
        const string TotalColFill = AccentSoft;
        const string TotalColStrong = AccentStrong;
        const string NumFmt = ExcelNumberFormat;

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Report");
        var colCount = table.Columns.Count;
        var row = 1;

        if (!string.IsNullOrWhiteSpace(title))
        {
            ws.Cell(row, 1).Value = title;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 14;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml(Ink);
            ws.Range(row, 1, row, colCount).Merge();
            row++;
        }
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            ws.Cell(row, 1).Value = subtitle;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml(Muted);
            ws.Cell(row, 1).Style.Font.FontSize = 10;
            ws.Range(row, 1, row, colCount).Merge();
            row++;
        }
        if (row > 1) row++; // spacer

        var headerRow = row;
        for (var c = 0; c < colCount; c++)
        {
            var dc = table.Columns[c];
            var cell = ws.Cell(headerRow, c + 1);
            cell.Value = Humanize(dc.ColumnName);
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 10;
            cell.Style.Font.FontColor = XLColor.FromHtml(Muted);
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderFill);
            cell.Style.Alignment.WrapText = true;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Bottom;
            cell.Style.Alignment.Horizontal = IsNumeric(dc)
                ? XLAlignmentHorizontalValues.Right
                : XLAlignmentHorizontalValues.Left;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.BottomBorderColor = XLColor.FromHtml(HairLine2);

            if (dc.ColumnName == totalColumnName)
            {
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml(TotalColFill);
                cell.Style.Font.FontColor = XLColor.FromHtml(Accent);
            }
        }
        ws.Row(headerRow).Height = 30; // room for wrapped two-line date headers

        var firstDataRow = headerRow + 1;
        var rows = table.Rows.Cast<DataRow>().ToList();
        for (var i = 0; i < rows.Count; i++)
        {
            var dr = rows[i];
            var r = firstDataRow + i;
            var isTotal = string.Equals(dr[0]?.ToString(), totalRowLabel, StringComparison.OrdinalIgnoreCase)
                          || (i == rows.Count - 1 &&
                              string.Equals(dr[0]?.ToString(), "Total", StringComparison.OrdinalIgnoreCase));

            for (var c = 0; c < colCount; c++)
            {
                var dc = table.Columns[c];
                var cell = ws.Cell(r, c + 1);
                var numeric = IsNumeric(dc);

                if (numeric)
                {
                    cell.Value = dr.IsNull(c) ? 0m : Convert.ToDecimal(dr[c]);
                    cell.Style.NumberFormat.Format = NumFmt;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
                else
                {
                    cell.Value = dr.IsNull(c) ? "" : dr[c].ToString();
                    if (c == 0) cell.Style.Font.Bold = true;
                }

                cell.Style.Font.FontColor = XLColor.FromHtml(Ink);
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.BottomBorderColor = XLColor.FromHtml(HairLine);

                if (dc.ColumnName == totalColumnName)
                {
                    cell.Style.Fill.BackgroundColor =
                        XLColor.FromHtml(isTotal ? TotalColStrong : TotalColFill);
                    cell.Style.Font.Bold = true;
                }

                if (isTotal)
                {
                    if (dc.ColumnName != totalColumnName)
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderFill);
                    cell.Style.Font.Bold = true;
                    cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.TopBorderColor = XLColor.FromHtml(Accent);
                }
            }
        }

        ws.Columns().AdjustToContents();
        foreach (var col in ws.ColumnsUsed())
            if (col.Width > 28) col.Width = 28;     // keep wrapped headers from blowing out
        ws.Column(1).Width = Math.Max(ws.Column(1).Width, 18);

        ws.SheetView.FreezeRows(headerRow);
        ws.SheetView.FreezeColumns(1);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static bool IsNumeric(DataColumn c) =>
        c.DataType == typeof(decimal) || c.DataType == typeof(double) || c.DataType == typeof(float) ||
        c.DataType == typeof(int) || c.DataType == typeof(long) || c.DataType == typeof(short) ||
        c.DataType == typeof(byte);

    // Strip Hungarian prefixes (fstr/fdec/fdtm/…) and split camelCase; leave the
    // date/period headers (which have no such prefix) untouched.
    private static string Humanize(string name)
    {
        var m = System.Text.RegularExpressions.Regex.Match(
            name, "^(fstr|fdec|fdtm|flng|fint|fbln|fchr|fdbl)([A-Z].*)$");
        return m.Success
            ? System.Text.RegularExpressions.Regex.Replace(m.Groups[2].Value, "(?<=[a-z0-9])(?=[A-Z])", " ")
            : name;
    }

    // ---------- PDF ----------

    public byte[] ToPdf(
        SummaryResultModel r,
        string title,
        IReadOnlyDictionary<string, string> measureFormats,
        CultureInfo culture)
    {
        string Fmt(string key, decimal v) =>
            string.Format(culture, measureFormats.TryGetValue(key, out var f) ? f : "{0:N2}", v);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Ink));

                page.Header().Column(col =>
                {
                    col.Item().Text(title).FontSize(18).SemiBold().FontColor(Accent);
                    col.Item().Text($"{r.GrandCount} records  ·  grouped by {r.GroupedBy}")
                        .FontSize(9).FontColor(Muted);
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Line);
                });

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(1);
                        foreach (var _ in r.MeasureKeys) c.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(HeadCell).Text(r.GroupedBy);
                        h.Cell().Element(HeadCell).AlignRight().Text("Records");
                        foreach (var k in r.MeasureKeys)
                            h.Cell().Element(HeadCell).AlignRight().Text(k);
                    });

                    foreach (var g in r.Groups)
                    {
                        table.Cell().Element(BodyCell).Text(g.Key);
                        table.Cell().Element(BodyCell).AlignRight().Text(g.Count.ToString(culture));
                        foreach (var k in r.MeasureKeys)
                            table.Cell().Element(BodyCell).AlignRight().Text(Fmt(k, g.Measures[k]));
                    }

                    table.Cell().Element(TotalCell).Text("Grand total");
                    table.Cell().Element(TotalCell).AlignRight().Text(r.GrandCount.ToString(culture));
                    foreach (var k in r.MeasureKeys)
                        table.Cell().Element(TotalCell).AlignRight().Text(Fmt(k, r.GrandTotals[k]));
                });

                page.Footer().Row(fr =>
                {
                    fr.RelativeItem().Text($"Generated {DateTime.UtcNow.ToString("dd MMM yyyy HH:mm", culture)}")
                        .FontSize(8).FontColor(Muted);
                    fr.RelativeItem().AlignRight().Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(8).FontColor(Muted));
                        t.Span("Page ");
                        t.CurrentPageNumber();
                        t.Span(" of ");
                        t.TotalPages();
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    #endregion
}
