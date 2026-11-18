namespace Collection.Library.Model;

/// <summary>One row of the detail report (a single collection transaction with names resolved).</summary>
public class ReportModel
{
    #region Giver Details

    public string? fstrFirstname { get; set; }
    public string? fstrLastname { get; set; }
    public string? fstrEmailAddress { get; set; }

    #endregion

    #region Transaction Details

    public DateTime fdtmTransactionDate { get; set; }
    public int flngCollectionNumber { get; set; }
    public string? fstrMemberTypeKey { get; set; }
    public string? fstrCollectionTypeKey { get; set; }
    public string? fstrPaymentTypeKey { get; set; }
    public string? fstrCurrencyKey { get; set; }
    public decimal fcurAmount { get; set; }
    /// <summary>Amount in the VIEWER's currency (converted when the row belongs to another location).</summary>
    public decimal fcurLocalAmount { get; set; }

    /// <summary>True when no exchange rate was found to convert this row, so it is shown unconverted.</summary>
    public bool fblnRateMissing { get; set; }

    #endregion

    #region Audit

    public bool fblnActive { get; set; }
    public string? fstrLocationKey { get; set; }
    public string? fstrWho { get; set; }
    public DateTime fdtmWhen { get; set; }

    #endregion
}
