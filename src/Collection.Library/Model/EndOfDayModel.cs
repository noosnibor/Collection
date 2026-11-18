namespace Collection.Library.Model;

/// <summary>One row of the end-of-day report: local-currency total per collection per day.</summary>
public class EndOfDayModel
{
    #region Properties

    public string? fstrLocationKey { get; set; }
    public string? fstrLocationName { get; set; }
    public string? fstrCollectionName { get; set; }
    public int? flngCollectionNumber { get; set; }
    public decimal fcurLocalAmount { get; set; }
    public bool fblnRateMissing { get; set; }
    public DateTime fdtmTransactionDate { get; set; }

    #endregion
}
