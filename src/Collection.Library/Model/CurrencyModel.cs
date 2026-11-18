namespace Collection.Library.Model;

/// <summary>A currency and its exchange rate, valid between two dates.</summary>
public class CurrencyModel
{
    #region Properties

    public Guid flngKey { get; set; }

    /// <summary>Currency code, e.g. JMD, USD.</summary>
    public string? fstrCurrency { get; set; }
    public string? fstrLocation { get; set; }

    /// <summary>Display name, e.g. "Jamaica Dollar".</summary>
    public string? fstrCurrencyType { get; set; }

    /// <summary>Exchange rate to the local currency.</summary>
    public decimal fcurAmount { get; set; }
    public bool fblnActive { get; set; }
    public string? fstrWho { get; set; }
    public DateTime fdtmEffectiveFrom { get; set; }
    public DateTime fdtmEffectiveTo { get; set; }
    public DateTime fdtmWhen { get; set; }

    #endregion
}
