namespace Collection.Library.Model;

/// <summary>A type of member (e.g. Member, Visitor) for a location.</summary>
public class MemberTypeModel
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
