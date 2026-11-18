using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Collection.Library.DataTransferObject;

/// <summary>Form payload for adding / editing a collection type (e.g. Tithes, Offering).</summary>
public class CollectionTypeDataTransferObject
{
    #region Properties

    [DisplayName("Key")]
    public Guid flngKey { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "A collection type is required"),
     StringLength(100, ErrorMessage = "The collection type must be 100 characters or fewer"),
     DisplayName("Collection Type")]
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
