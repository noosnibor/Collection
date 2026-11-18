using Collection.Library.Model;

namespace Collection.Library.Service;

/// <summary>
/// Generic group-by / aggregate engine behind the summary report.
/// One dimension is chosen to group by; any number of numeric measures are totalled per group.
/// </summary>
public static class ReportEngineService
{
    #region Summarise

    /// <summary>
    /// Groups <paramref name="data"/> by <paramref name="groupBy"/> and totals every measure.
    /// </summary>
    /// <typeparam name="T">Row type.</typeparam>
    /// <param name="data">Rows to summarise (enumerated once).</param>
    /// <param name="groupBy">The dimension column to group by.</param>
    /// <param name="measures">The measure columns to total (the first one drives share % and ordering).</param>
    /// <returns>Groups ordered largest-first, with grand totals.</returns>
    public static SummaryResultModel Summarize<T>(
        IEnumerable<T> data,
        ReportColumnModel<T> groupBy,
        IReadOnlyList<ReportColumnModel<T>> measures)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(groupBy);
        ArgumentNullException.ThrowIfNull(measures);

        // Two measures sharing a key would crash ToDictionary below – fail early with a clear message.
        var duplicate = measures.GroupBy(m => m.Key).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new ArgumentException($"Measure key '{duplicate.Key}' is used more than once.", nameof(measures));

        var rows = data as IReadOnlyCollection<T> ?? data.ToList();
        var measureKeys = measures.Select(m => m.Key).ToList();

        var result = new SummaryResultModel
        {
            GroupedBy = groupBy.Header,
            MeasureKeys = measureKeys,
            GrandCount = rows.Count
        };

        // Build one SummaryGroup per distinct dimension value ("—" stands in for blanks / nulls).
        var groups = rows
            .GroupBy(r => groupBy.Selector(r)?.ToString() ?? "—")
            .Select(g => new SummaryGroup
            {
                Key = g.Key,
                Count = g.Count(),
                Measures = measures.ToDictionary(m => m.Key, m => g.Sum(m.Value))
            })
            .ToList();

        foreach (var m in measures)
            result.GrandTotals[m.Key] = groups.Sum(g => g.Measures[m.Key]);

        // Share is based on the first measure (the headline number).
        var primary = measureKeys.FirstOrDefault();
        if (primary is not null && result.GrandTotals.TryGetValue(primary, out var total) && total != 0)
        {
            foreach (var g in groups)
                g.Share = (double)(g.Measures[primary] / total);
        }

        // Largest contributor first (ties broken alphabetically so the order is stable);
        // fall back to record count when there are no measures.
        result.Groups = primary is not null
            ? [.. groups.OrderByDescending(g => g.Measures[primary]).ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)]
            : [.. groups.OrderByDescending(g => g.Count).ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)];

        return result;
    }

    #endregion
}
