namespace Collection.Library.Model;

/// <summary>A single collection transaction (offering, tithe …) as stored in the database.</summary>
public class CollectionModel
{
    #region Identity

    public Guid flngKey { get; set; }

    #endregion

    #region Giver Details

    public string? fstrFirstname { get; set; }
    public string? fstrLastname { get; set; }
    public string? fstrEmailAddress { get; set; }

    #endregion

    #region Transaction Details

    public DateTime fdtmTransactionDate { get; set; }
    public int flngCollectionNumber { get; set; }
    public Guid flngMemberTypeKey { get; set; }
    public Guid flngCollectionTypeKey { get; set; }
    public Guid flngPaymentTypeKey { get; set; }

    /// <summary>Amount in the currency the giver paid in.</summary>
    public decimal fcurAmount { get; set; }

    /// <summary>Amount converted to the location's local currency.</summary>
    public decimal fcurLocalAmount { get; set; }
    public string? fstrCurrencyKey { get; set; }

    #endregion

    #region Audit

    public bool fblnActive { get; set; }
    public string? fstrLocationKey { get; set; }
    public string? fstrWho { get; set; }
    public DateTime fdtmWhen { get; set; }

    #endregion
}
