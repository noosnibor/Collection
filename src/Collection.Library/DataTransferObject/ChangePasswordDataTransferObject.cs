using System.ComponentModel.DataAnnotations;

namespace Collection.Library.DataTransferObject;

/// <summary>Form payload for choosing a new password.</summary>
public class ChangePasswordDataTransferObject
{
    #region Properties

    [Required(ErrorMessage = "Password field is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    public string? fstrPassword { get; set; }

    [Required(ErrorMessage = "Password Confirmed field is required")]
    [Compare(nameof(fstrPassword), ErrorMessage = "Password does not match")]
    public string? fstrConfirmPassword { get; set; }

    #endregion
}
