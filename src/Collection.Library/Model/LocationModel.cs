namespace Collection.Library.Model;

/// <summary>A church location / campus.</summary>
public class LocationModel
{
    #region Properties

    public Guid flngKey { get; set; }

    /// <summary>Row version.</summary>
    public int flngVer { get; set; }

    /// <summary>Short location code, e.g. HQ.</summary>
    public string? fstrLocation { get; set; }
    public string? fstrDescription { get; set; }
    public string? fstrAddress { get; set; }
    public bool fblnActive { get; set; }
    public string? fstrWho { get; set; }
    public DateTime fdtmWhen { get; set; }

    #endregion
}
