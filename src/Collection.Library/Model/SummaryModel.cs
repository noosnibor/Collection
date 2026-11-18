namespace Collection.Library.Model;

/// <summary>One row of the summary report source data (local-currency amount per day / type).</summary>
public class SummaryModel
{
    #region Properties

    public DateTime fdtmTransactionDate { get; set; }
    public string? fstrCollectionTypeKey { get; set; }
    public string? fstrPaymentTypeKey { get; set; }
    public string? fstrCurrencyKey { get; set; }
    public decimal fcurLocalAmount { get; set; }
    public bool fblnRateMissing { get; set; }
    public string? fstrLocationKey { get; set; }

    #endregion
}
