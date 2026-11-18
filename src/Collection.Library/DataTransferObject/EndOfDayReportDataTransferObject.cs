using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Collection.Library.DataTransferObject;

/// <summary>Search filters for the end-of-day report.</summary>
public class EndOfDayReportDataTransferObject : IValidatableObject
{
    #region Filters

    [DisplayName("Location")]
    public string? fstrLocation { get; set; } = null;

    /// <summary>The viewer's own location: amounts of other locations are converted into its currency.</summary>
    public string? fstrReportLocation { get; set; }

    [DisplayName("Start")]
    public DateTime? fdtmTransactionStart { get; set; } = default;

    [DisplayName("End")]
    public DateTime? fdtmTransactionEnd { get; set; } = default;

    #endregion

    #region Validation

    /// <summary>The end date cannot be earlier than the start date.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (fdtmTransactionStart.HasValue && fdtmTransactionEnd.HasValue &&
            fdtmTransactionEnd.Value < fdtmTransactionStart.Value)
        {
            yield return new ValidationResult(
                "The end date cannot be earlier than the start date",
                [nameof(fdtmTransactionEnd)]);
        }
    }

    #endregion
}
