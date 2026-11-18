using Collection.Library.Model;
using System.Globalization;

namespace Collection.Web.Components.Services;

#region View models

/// <summary>One collection's share of a day or a week (drives a bar segment and a legend row).</summary>
public sealed record ChartSlice(string Name, decimal Amount, int ColorIndex);

/// <summary>One bar of the weekly chart: the week label plus its slices.</summary>
public sealed record ChartWeek(string Label, DateTime WeekStart, decimal Total, IReadOnlyList<ChartSlice> Slices, bool IsCurrent);

/// <summary>Everything the dashboard chart needs, already aggregated.</summary>
public sealed record GivingSummary(
    DateTime? LatestDay,
    bool LatestIsToday,
    decimal LatestTotal,
    IReadOnlyList<ChartSlice> LatestSlices,
    IReadOnlyList<ChartWeek> Weeks,
    IReadOnlyList<string> Collections);

#endregion

/// <summary>
/// Aggregates end-of-day rows into the figures shown on the dashboard:
/// the most recent day (today when there is data) and the last N weeks.
/// </summary>
/// <remarks>
/// Pure functions with no UI or database access, so they are easy to test.
/// Amounts are the local-currency totals already produced by the stored procedure.
/// Weeks start on Sunday, the main service day.
/// </remarks>
public static class GivingChartService
{
    #region Public API

    /// <summary>Builds the summary. <paramref name="weekCount"/> bars end with the week of <paramref name="today"/>.</summary>
    public static GivingSummary Build(IEnumerable<EndOfDayModel> rows, DateTime today, int weekCount = 8)
    {
        var data = rows.Where(r => r.fcurLocalAmount != 0 || r.fdtmTransactionDate != default).ToList();

        // One stable colour per collection, assigned alphabetically so colours never jump between loads.
        var collections = data
            .Select(r => CleanName(r.fstrCollectionName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int ColorOf(string name) => collections.FindIndex(c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase));

        List<ChartSlice> Slices(IEnumerable<EndOfDayModel> source) =>
            source.GroupBy(r => CleanName(r.fstrCollectionName), StringComparer.OrdinalIgnoreCase)
                  .Select(g => new ChartSlice(g.Key, g.Sum(r => r.fcurLocalAmount), ColorOf(g.Key)))
                  .OrderBy(s => s.ColorIndex)
                  .ToList();

        // ── Most recent day: today when it has data, otherwise the latest day with data ──
        DateTime? latestDay = data.Count == 0 ? null : data.Max(r => r.fdtmTransactionDate.Date);
        var latestRows = latestDay is null ? [] : data.Where(r => r.fdtmTransactionDate.Date == latestDay).ToList();
        var latestSlices = Slices(latestRows);

        // ── Weeks (Sunday start), oldest first ──
        var currentWeek = WeekStart(today);
        var weeks = new List<ChartWeek>();
        for (var i = weekCount - 1; i >= 0; i--)
        {
            var start = currentWeek.AddDays(-7 * i);
            var inWeek = data.Where(r => WeekStart(r.fdtmTransactionDate) == start).ToList();
            var slices = Slices(inWeek);

            weeks.Add(new ChartWeek(
                Label: start.ToString("MMM d", CultureInfo.CurrentCulture),
                WeekStart: start,
                Total: slices.Sum(s => s.Amount),
                Slices: slices,
                IsCurrent: i == 0));
        }

        return new GivingSummary(
            latestDay,
            latestDay?.Date == today.Date,
            latestSlices.Sum(s => s.Amount),
            latestSlices,
            weeks,
            collections);
    }

    /// <summary>The Sunday on or before <paramref name="date"/>.</summary>
    public static DateTime WeekStart(DateTime date) => date.Date.AddDays(-(int)date.DayOfWeek);

    /// <summary>Oldest date the dashboard query needs for <paramref name="weekCount"/> weeks.</summary>
    public static DateTime QueryStart(DateTime today, int weekCount = 8) =>
        WeekStart(today).AddDays(-7 * (weekCount - 1));

    #endregion

    #region Helpers

    private static string CleanName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "Other" : name.Trim();

    #endregion
}
