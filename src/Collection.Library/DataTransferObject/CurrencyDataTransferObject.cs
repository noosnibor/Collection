using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Collection.Library.DataTransferObject;

/// <summary>Form payload for adding / editing a currency and its exchange rate.</summary>
public class CurrencyDataTransferObject : IValidatableObject
{
    #region Properties

    [DisplayName("Key")]
    public Guid flngKey { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Currency code is required"),
     DisplayName("Currency Code"),
     StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be exactly 3 letters"),
     RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "Currency code must be exactly 3 letters (e.g. JMD)")]
    public string? fstrCurrency { get; set; }

    [Required(ErrorMessage = "A location is required"),
     DisplayName("Location")]
    public string? fstrLocation { get; set; }

    [Required(ErrorMessage = "Currency type is required"),
     DisplayName("Currency Type")]
    public string? fstrCurrencyType { get; set; }

    /// <summary>
    /// BUG FIX: the range used to start at 1, which rejected every rate below 1
    /// (e.g. 0.0064 for a weaker-to-stronger currency conversion). Any positive rate is valid.
    /// </summary>
    [Range(typeof(decimal), "0.000001", "1000000000", ErrorMessage = "Rate must be greater than zero"),
     DisplayName("Rate")]
    public decimal fcurAmount { get; set; }

    [DisplayName("Active")]
    public bool fblnActive { get; set; } = true;

    [DisplayName("Who")]
    public string? fstrWho { get; set; }

    [Required(ErrorMessage = "An effective from date is required"), DisplayName("Effective From")]
    public DateTime? fdtmEffectiveFrom { get; set; } = DateTime.UtcNow;

    [Required(ErrorMessage = "An effective to date is required"), DisplayName("Effective To")]
    public DateTime? fdtmEffectiveTo { get; set; } = null;

    [DisplayName("When")]
    public DateTime fdtmWhen { get; set; } = DateTime.UtcNow;

    #endregion

    #region Validation

    /// <summary>Cross-field rule: the rate cannot end before it starts.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (fdtmEffectiveFrom.HasValue && fdtmEffectiveTo.HasValue &&
            fdtmEffectiveTo.Value < fdtmEffectiveFrom.Value)
        {
            yield return new ValidationResult(
                "Effective To cannot be earlier than Effective From",
                [nameof(fdtmEffectiveTo)]);
        }
    }

    #endregion
}
