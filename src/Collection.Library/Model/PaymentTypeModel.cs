namespace Collection.Library.Model;

/// <summary>A way a collection can be paid (e.g. Cash, Cheque) for a location.</summary>
public class PaymentTypeModel
{
    #region Properties

    public Guid flngKey { get; set; }
    public string? fstrType { get; set; }
    public string? fstrLocation { get; set; }
    public bool fblnActive { get; set; }
    public string? fstrWho { get; set; }
    public DateTime fdtmWhen { get; set; }

    #endregion
}
