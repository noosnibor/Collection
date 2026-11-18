using Collection.Library.Model;
using Collection.Library.Service;
using System.Data;
using System.Globalization;

namespace Collection.Web.Components.Services;

/// <summary>
/// Builds the end-of-day pivot (collections as rows, collection dates/numbers as columns)
/// from the flat <see cref="EndOfDayModel"/> rows returned by the repository.
/// </summary>
/// <remarks>
/// Replaces the old index-based <c>PivotData</c> in Report.razor, which looked columns up by
/// position (3/5/2/4), built a <c>DataTable.Select</c> filter that broke on apostrophes, and
/// re-decoded the collection number for every cell. This version works on typed properties.
/// </remarks>
public static class EndOfDayPivotService
{
    #region Constants

    /// <summary>Label of the totals row and the totals column.</summary>
    public const string TotalLabel = "Total";

    /// <summary>Header of the first (row-label) column.</summary>
    public const string CollectionColumn = "fstrCollectionName";

    #endregion

    #region Public API

    /// <summary>Distinct location names, sorted – feeds the location buttons.</summary>
    public static List<string> GetLocations(IEnumerable<EndOfDayModel> rows) =>
        rows.Select(r => r.fstrLocationName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// Pivots the rows of one location. Columns are "MMM-dd-yyyy\r\n( 1st )" ordered by
    /// date then collection number; a Total row and a Total column are appended.
    /// </summary>
    public static DataTable Build(IEnumerable<EndOfDayModel> rows)
    {
        var data = rows.ToList();
        var result = new DataTable("EndOfDay");
        result.Columns.Add(CollectionColumn, typeof(string));

        // ── Columns: one per distinct (date, collection number) ─────────────
        var decodes = BuildDecodeMap();
        var columnKeys = data
            .Select(r => (Date: r.fdtmTransactionDate.Date, Number: r.flngCollectionNumber ?? 0))
            .Distinct()
            .OrderBy(k => k.Date).ThenBy(k => k.Number)
            .ToList();

        var columnNames = new Dictionary<(DateTime, int), string>();
        foreach (var key in columnKeys)
        {
            var decode = decodes.TryGetValue(key.Number, out var d) ? d : key.Number.ToString(CultureInfo.InvariantCulture);
            var name = $"{key.Date.ToString("MMM-dd-yyyy", CultureInfo.InvariantCulture)}\r\n( {decode} )";
            columnNames[key] = name;
            result.Columns.Add(name, typeof(decimal));
        }
        result.Columns.Add(TotalLabel, typeof(decimal));

        // ── Rows: one per collection name, amounts summed per column ────────
        var collections = data
            .Select(r => r.fstrCollectionName ?? "(Unnamed)")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);

        foreach (var collection in collections)
        {
            var row = result.NewRow();
            row[CollectionColumn] = collection;

            foreach (var column in columnNames.Values)
                row[column] = 0m;

            foreach (var item in data.Where(r => string.Equals(r.fstrCollectionName ?? "(Unnamed)", collection, StringComparison.OrdinalIgnoreCase)))
            {
                var column = columnNames[(item.fdtmTransactionDate.Date, item.flngCollectionNumber ?? 0)];
                row[column] = (decimal)row[column] + item.fcurLocalAmount;
            }

            row[TotalLabel] = columnNames.Values.Sum(c => (decimal)row[c]);
            result.Rows.Add(row);
        }

        // ── Totals row ──────────────────────────────────────────────────────
        var totals = result.NewRow();
        totals[CollectionColumn] = TotalLabel;
        foreach (DataColumn column in result.Columns.Cast<DataColumn>().Skip(1))
            totals[column] = result.AsEnumerable().Sum(r => r.Field<decimal>(column));
        result.Rows.Add(totals);

        return result;
    }

    #endregion

    #region Helpers

    /// <summary>Collection number key → display text (1st, 2nd …).</summary>
    private static Dictionary<int, string> BuildDecodeMap() =>
        (StaticDataService.GetCollectionNumber() ?? [])
            .GroupBy(c => c.flngCollectionNumberKey)
            .ToDictionary(g => g.Key, g => g.First().fstrDecode ?? g.Key.ToString(CultureInfo.InvariantCulture));

    #endregion
}
