using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Collection.Library.DataTransferObject;

/// <summary>Form payload for adding / editing a payment type (e.g. Cash, Cheque).</summary>
public class PaymentTypeDataTransferObject
{
    #region Properties

    [DisplayName("Key")]
    public Guid flngKey { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "A payment type is required"),
     StringLength(100, ErrorMessage = "The payment type must be 100 characters or fewer"),
     DisplayName("Payment Type")]
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
