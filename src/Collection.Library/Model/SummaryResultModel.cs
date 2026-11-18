namespace Collection.Library.Model;

/// <summary>One group (row) in a summary: its record count, measure totals and share of the whole.</summary>
public sealed class SummaryGroup
{
    #region Properties

    /// <summary>The dimension value this group represents.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Number of source rows in the group.</summary>
    public int Count { get; set; }

    /// <summary>Total per measure key.</summary>
    public Dictionary<string, decimal> Measures { get; set; } = [];

    /// <summary>Fraction (0–1) of the grand total of the primary measure.</summary>
    public double Share { get; set; }

    #endregion
}

/// <summary>Result of <c>ReportEngineService.Summarize</c>.</summary>
public sealed class SummaryResultModel
{
    #region Properties

    /// <summary>Header of the dimension that was grouped by.</summary>
    public string GroupedBy { get; set; } = string.Empty;

    /// <summary>Measure keys, in display order.</summary>
    public List<string> MeasureKeys { get; set; } = [];

    /// <summary>The groups, largest first.</summary>
    public List<SummaryGroup> Groups { get; set; } = [];

    /// <summary>Total per measure key across all groups.</summary>
    public Dictionary<string, decimal> GrandTotals { get; set; } = [];

    /// <summary>Total number of source rows.</summary>
    public int GrandCount { get; set; }

    #endregion
}
