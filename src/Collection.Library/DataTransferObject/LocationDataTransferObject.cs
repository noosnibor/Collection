using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Collection.Library.DataTransferObject;

/// <summary>Form payload for adding / editing a church location.</summary>
public class LocationDataTransferObject
{
    #region Properties

    [DisplayName("Key")]
    public Guid flngKey { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "A location code is required"),
     DisplayName("Location Code"),
     MinLength(2, ErrorMessage = "Minimum length must be 2 characters")]
    public string? fstrLocation { get; set; }

    [Required(ErrorMessage = "A location description is required"),
     DisplayName("Description")]
    public string? fstrDescription { get; set; }

    [Required(ErrorMessage = "A location address is required"),
     DisplayName("Address")]
    public string? fstrAddress { get; set; }

    [DisplayName("Active")]
    public bool fblnActive { get; set; } = true;

    [DisplayName("Who")]
    public string? fstrWho { get; set; }

    [DisplayName("When")]
    public DateTime fdtmWhen { get; set; } = DateTime.UtcNow;

    #endregion
}
