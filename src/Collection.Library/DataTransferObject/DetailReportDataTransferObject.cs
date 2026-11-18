using System.ComponentModel.DataAnnotations;

namespace Collection.Library.DataTransferObject;

/// <summary>Search filters for the detail report. Every filter except the date range is optional.</summary>
public class DetailReportDataTransferObject : IValidatableObject
{
    #region Filters

    public string? fstrLocation { get; set; } = null;

    /// <summary>The viewer's own location: amounts of other locations are converted into its currency.</summary>
    public string? fstrReportLocation { get; set; }
    public string? fstrFirstname { get; set; } = null;
    public string? fstrLastname { get; set; } = null;

    [Required(ErrorMessage = "A transaction start date is required")]
    public DateTime? fdtmTransactionStartDate { get; set; } = null;

    [Required(ErrorMessage = "A transaction end date is required")]
    public DateTime? fdtmTransactionEndDate { get; set; } = null;

    public Guid? flngMemberType { get; set; } = null;
    public Guid? flngCollectionType { get; set; } = null;
    public Guid? flngPaymentType { get; set; } = null;
    public string? fstrCurrencyType { get; set; } = null;
    public decimal? fcurAmountFrom { get; set; } = null;
    public decimal? fcurAmountTo { get; set; } = null;
    public int? flngCollectionNumber { get; set; } = null;
    public string? fstrWho { get; set; } = null;

    #endregion

    #region Validation

    /// <summary>Cross-field rules: ranges must run forwards (new – they used to reach the database unchecked).</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (fdtmTransactionStartDate.HasValue && fdtmTransactionEndDate.HasValue &&
            fdtmTransactionEndDate.Value < fdtmTransactionStartDate.Value)
        {
            yield return new ValidationResult(
                "The end date cannot be earlier than the start date",
                [nameof(fdtmTransactionEndDate)]);
        }

        if (fcurAmountFrom.HasValue && fcurAmountTo.HasValue && fcurAmountTo.Value < fcurAmountFrom.Value)
        {
            yield return new ValidationResult(
                "The maximum amount cannot be less than the minimum amount",
                [nameof(fcurAmountTo)]);
        }
    }

    #endregion
}
