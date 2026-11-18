namespace Collection.Library.Model;

/// <summary>A type of collection (e.g. Tithes, Offering, Building Fund) for a location.</summary>
public class CollectionTypeModel
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
