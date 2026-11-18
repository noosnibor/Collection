using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Collection.Library.DataTransferObject;

/// <summary>Form payload for adding / editing an application user.</summary>
public class UserDataTransferObject : IValidatableObject
{
    #region Identity & Credentials

    [DisplayName("Key")]
    public Guid flngKey { get; set; } = Guid.NewGuid();

    [InfoGroup("Identity")]
    [Required(ErrorMessage = "Username field is required"), DisplayName("User Name")]
    public string fstrUsername { get; set; } = string.Empty;

    #endregion

    #region Profile

    [InfoGroup("Identity")]
    [Required(ErrorMessage = "Firstname field is required"), DisplayName("First Name")]
    public string fstrFirstname { get; set; } = string.Empty;

    [InfoGroup("Identity")]
    [Required(ErrorMessage = "Lastname field is required"), DisplayName("Last Name")]
    public string fstrLastname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email Address field is required"),
     EmailAddress(ErrorMessage = "Invalid email format"), DisplayName("Email Address")]
    [InfoGroup("Identity")]
    public string fstrEmailAddress { get; set; } = string.Empty;

    [InfoGroup("Access")]
    [Required(ErrorMessage = "A location is required"), DisplayName("Location")]
    public string fstrLocationKey { get; set; } = string.Empty;

    [InfoGroup("Access")]
    [Required(ErrorMessage = "A role is required"), DisplayName("Role")]
    public int? flngRoleKey { get; set; }

    [InfoGroup("Access")]
    [Required(ErrorMessage = "A currency is required"), DisplayName("Currency")]
    public string? fstrCurrencyKey { get; set; } = string.Empty;

    #endregion

    #region Account Validity

    [InfoGroup("Account")]
    [DisplayName("Active")]
    public bool fblnActive { get; set; } = true;

    [InfoGroup("Account")]
    [Required(ErrorMessage = "A start date is required"), DisplayName("Start Date")]
    public DateTime? fdtmStart { get; set; } = null;

    [InfoGroup("Account")]
    [Required(ErrorMessage = "An end date is required"), DisplayName("End Date")]
    public DateTime? fdtmEnd { get; set; } = null;

    /// <summary>
    /// Temporary password given to a new user. They must replace it at first login
    /// (see <see cref="fblnPasswordChanged"/>).
    /// </summary>
    [InfoGroup("Account")]
    [Required(ErrorMessage = "Password field is required"), DisplayName("Password")]
    public string fstrPassword { get; set; } = "password";

    /// <summary>True once the user has chosen their own password.</summary>
    [InfoGroup("Account")]
    [DisplayName("Password Changed")]
    public bool fblnPasswordChanged { get; set; } = false;

    #endregion

    #region Audit

    [DisplayName("Who")]
    public string fstrWho { get; set; } = "Batch";

    [DisplayName("When")]
    public DateTime fdtmWhen { get; set; } = DateTime.UtcNow;

    #endregion

    #region Validation

    /// <summary>Cross-field rule: the account cannot end before it starts.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (fdtmStart.HasValue && fdtmEnd.HasValue && fdtmEnd < fdtmStart)
        {
            yield return new ValidationResult(
                "End Date cannot be earlier than Start Date",
                [nameof(fdtmEnd)]);
        }
    }

    #endregion
}
