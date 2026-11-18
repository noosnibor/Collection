namespace Collection.Library.Model;

/// <summary>
/// One financial period (a month, a quarter or a year) for one location or for all locations together, with its lock state
/// and the totals of the transactions dated inside it.
/// </summary>
public class PeriodModel
{
    #region Identity

    /// <summary>A location code, or <see cref="PeriodScope.AllLocations"/> ("*") when the period applies to every location.</summary>
    public string? fstrLocationKey { get; set; }


    /// <summary>M = month, Q = quarter, Y = year (see <see cref="PeriodTypes"/>).</summary>
    public string? fstrPeriodType { get; set; }
    public int flngYear { get; set; }

    /// <summary>Month 1-12, quarter 1-4, or 1 for a year.</summary>
    public int flngNumber { get; set; }
    public DateTime fdtmStart { get; set; }
    public DateTime fdtmEnd { get; set; }

    #endregion

    #region Lock state

    /// <summary>True when this exact period has been closed.</summary>
    public bool fblnClosed { get; set; }
    public string? fstrClosedBy { get; set; }
    public DateTime? fdtmClosedOn { get; set; }
    public string? fstrReopenedBy { get; set; }
    public DateTime? fdtmReopenedOn { get; set; }

    /// <summary>The reason given the last time the period was reopened.</summary>
    public string? fstrReason { get; set; }

    /// <summary>True when every day is already covered by closed periods (e.g. all months of a quarter closed one by one).</summary>
    public bool fblnCovered { get; set; }

    /// <summary>Name of the bigger closed period (quarter / year) that locks this one, if any.</summary>
    public string? fstrLockedBy { get; set; }

    #endregion

    #region Totals (active records only, local currency)

    public int flngRecords { get; set; }
    public decimal fcurTotal { get; set; }

    /// <summary>Currency of <see cref="fcurTotal"/> (the currency used at the location whose money is shown).</summary>
    public string? fstrCurrencyKey { get; set; }

    /// <summary>Records that could not be converted because no exchange rate was found (shown unconverted).</summary>
    public int flngRatesMissing { get; set; }

    #endregion

    #region Helpers

    /// <summary>True when the period is locked, on its own or through a bigger closed period.</summary>
    public bool IsLocked => fblnClosed || fblnCovered || !string.IsNullOrEmpty(fstrLockedBy);

    /// <summary>Display name, e.g. "October 2026", "Q3 2026" or "2026".</summary>
    public string Name => PeriodTypes.Name(fstrPeriodType, flngYear, flngNumber);

    #endregion
}

/// <summary>One close / reopen event from the period history.</summary>
public class PeriodAuditModel
{
    public long flngId { get; set; }
    public string? fstrLocationKey { get; set; }
    public string? fstrPeriodType { get; set; }
    public int flngYear { get; set; }
    public int flngNumber { get; set; }

    /// <summary>CLOSE or REOPEN.</summary>
    public string? fstrAction { get; set; }
    public string? fstrWho { get; set; }
    public DateTime fdtmWhen { get; set; }
    public string? fstrReason { get; set; }

    /// <summary>True when the event applied to every location.</summary>
    public bool IsAllLocations => fstrLocationKey == PeriodScope.AllLocations;

    public string PeriodName => PeriodTypes.Name(fstrPeriodType, flngYear, flngNumber);
    public bool IsClose => string.Equals(fstrAction, "CLOSE", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Period type codes and naming rules shared by the page, the repository and the tests.</summary>
public static class PeriodTypes
{
    public const string Month = "M";
    public const string Quarter = "Q";
    public const string Year = "Y";

    /// <summary>"October 2026", "Q3 2026" or "2026".</summary>
    public static string Name(string? type, int year, int number) => type switch
    {
        Quarter => $"Q{number} {year}",
        Year => year.ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => $"{System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(Math.Clamp(number, 1, 12))} {year}"
    };
}

/// <summary>The scope a period is closed for.</summary>
public static class PeriodScope
{
    /// <summary>Stored in <c>fstrLocationKey</c> when a period is closed for every location at once.</summary>
    public const string AllLocations = "*";
}
