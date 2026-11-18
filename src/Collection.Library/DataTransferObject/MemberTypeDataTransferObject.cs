using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Collection.Library.DataTransferObject;

/// <summary>Form payload for adding / editing a member type (e.g. Member, Visitor).</summary>
public class MemberTypeDataTransferObject
{
    #region Properties

    [DisplayName("Key")]
    public Guid flngKey { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "A member type is required"),
     StringLength(100, ErrorMessage = "The member type must be 100 characters or fewer"),
     DisplayName("Member Type")]
    public string? fstrType { get; set; }

    [DisplayName("Location")]
    public string? fstrLocation { get; set; }

    [DisplayName("Active")]
    public bool fblnActive { get; set; } = true;

    [DisplayName("Who")]
    public string? fstrWho { get; set; }

    [DisplayName("When")]
    public DateTime fdtmWhen { get; set; } = DateTime.UtcNow;

    #endregion
}
