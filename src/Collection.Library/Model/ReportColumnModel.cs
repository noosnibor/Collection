namespace Collection.Library.Model;

/// <summary>Whether a report column is grouped by (Dimension) or totalled (Measure).</summary>
public enum ColumnRole
{
    Dimension,
    Measure
}

/// <summary>Describes one column of a generic report: how to read it from a row and how to show it.</summary>
/// <typeparam name="T">Row type.</typeparam>
public sealed class ReportColumnModel<T>
{
    #region Properties

    /// <summary>Unique key (used as dictionary key in results).</summary>
    public required string Key { get; init; }

    /// <summary>Column header shown to the user.</summary>
    public required string Header { get; init; }

    public required ColumnRole Role { get; init; }

    /// <summary>Reads the grouping value (Dimension columns).</summary>
    public Func<T, object?> Selector { get; init; } = _ => null;

    /// <summary>Reads the numeric value (Measure columns).</summary>
    public Func<T, decimal> Value { get; init; } = _ => 0m;

    /// <summary>Composite format string used to display measure values.</summary>
    public string Format { get; init; } = "{0:N2}";

    #endregion
}

/// <summary>Factory helpers that keep report definitions short and readable.</summary>
public static class Report
{
    #region Factories

    /// <summary>Creates a dimension (group-by) column.</summary>
    public static ReportColumnModel<T> Dimension<T>(string header, Func<T, object?> selector, string? key = null)
        => new()
        {
            Key = key ?? header,
            Header = header,
            Role = ColumnRole.Dimension,
            Selector = selector
        };

    /// <summary>Creates a measure (total) column.</summary>
    public static ReportColumnModel<T> Measure<T>(string header, Func<T, decimal> value, string format = "{0:N2}", string? key = null)
        => new()
        {
            Key = key ?? header,
            Header = header,
            Role = ColumnRole.Measure,
            Value = value,
            Format = format
        };

    #endregion
}
