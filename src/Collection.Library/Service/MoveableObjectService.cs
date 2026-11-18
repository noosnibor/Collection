namespace Collection.Library.Service;

/// <summary>
/// Scoped "carry case" used to hand the username from the Login page to the
/// Change Password page when a first-time user must choose a new password.
/// </summary>
public class ChangePasswordMoveableObject
{
    #region Public Members

    /// <summary>User that must change their password (null when no change is pending).</summary>
    public string? fstrUsername { get; set; }

    /// <summary>Clears the carried username once the password flow has finished.</summary>
    public void Clear() => fstrUsername = null;

    #endregion
}
