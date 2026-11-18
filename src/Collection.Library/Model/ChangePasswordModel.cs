namespace Collection.Library.Model;

/// <summary>Read model for the change-password form.</summary>
public class ChangePasswordModel
{
    #region Properties

    /// <summary>The new password (plain text, hashed before it is stored).</summary>
    public string? fstrPassword { get; set; }

    /// <summary>Repeat of the new password.</summary>
    public string? fstrConfirmPassword { get; set; }

    #endregion
}
