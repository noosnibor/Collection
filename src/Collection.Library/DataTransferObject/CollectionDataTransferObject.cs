using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Collection.Library.DataTransferObject;

/// <summary>Form payload for entering / editing a collection transaction.</summary>
public class CollectionDataTransferObject
{
    #region Identity

    [DisplayName("Key")]
    public Guid flngKey { get; set; } = Guid.NewGuid();

    #endregion

    #region Giver Details

    [InfoGroup("Member")]
    [Required(ErrorMessage = "A first name is required"), DisplayName("First Name")]
    public string? fstrFirstname { get; set; }

    [InfoGroup("Member")]
    [Required(ErrorMessage = "A last name is required"), DisplayName("Last Name")]
    public string? fstrLastname { get; set; }

    [InfoGroup("Member")]
    [EmailAddress(ErrorMessage = "Email Address format is incorrect"), DisplayName("Email Address")]
    public string? fstrEmailAddress { get; set; }

    #endregion

    #region Transaction Details

    [InfoGroup("Collection")]
    [Required(ErrorMessage = "A transaction date is required"), DisplayName("Transaction Date")]
    public DateTime? fdtmTransactionDate { get; set; } = DateTime.UtcNow.Date;

    [InfoGroup("Collection")]
    [Range(1, 100000000, ErrorMessage = "Collection number must be greater than zero"), DisplayName("Collection Number")]
    public int flngCollectionNumber { get; set; }

    // The three keys below are nullable so [Required] can detect an un-selected drop-down.
    [InfoGroup("Member")]
    [Required(ErrorMessage = "A member type is required"), DisplayName("Member Type")]
    public Guid? flngMemberTypeKey { get; set; }

    [InfoGroup("Collection")]
    [Required(ErrorMessage = "A collection type is required"), DisplayName("Collection Type")]
    public Guid? flngCollectionTypeKey { get; set; }

    [InfoGroup("Collection")]
    [Required(ErrorMessage = "A payment type is required"), DisplayName("Payment Type")]
    public Guid? flngPaymentTypeKey { get; set; }

    [InfoGroup("Collection")]
    [Range(0.01, 1000000000, ErrorMessage = "An amount is required"), DisplayName("Amount")]
    public decimal fcurAmount { get; set; }

    [DisplayName("Local Amount")]
    public decimal fcurLocalAmount { get; set; } = 0;

    [InfoGroup("Collection")]
    [Required(ErrorMessage = "A currency type is required"), DisplayName("Currency")]
    public string? fstrCurrencyKey { get; set; }

    #endregion

    #region Audit

    [InfoGroup("Collection")]
    [DisplayName("Active")]
    public bool fblnActive { get; set; } = true;

    [InfoGroup("Collection")]
    [DisplayName("Location")]
    public string? fstrLocationKey { get; set; }

    [DisplayName("Who")]
    public string? fstrWho { get; set; } = "system";

    [DisplayName("Collection Date")]
    public DateTime? fdtmWhen { get; set; } = DateTime.UtcNow;

    #endregion
}
