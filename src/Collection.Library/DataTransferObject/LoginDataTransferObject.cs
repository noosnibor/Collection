using System.ComponentModel.DataAnnotations;

namespace Collection.Library.DataTransferObject;

/// <summary>Credentials posted by the login form.</summary>
public class LoginDataTransferObject
{
    #region Properties

    [Required(ErrorMessage = "Username is required")]
    public string? fstrUsername { get; set; }

    [Required(ErrorMessage = "Password is required")]
    public string? fstrPassword { get; set; }

    #endregion
}
