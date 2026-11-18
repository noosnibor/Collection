namespace Collection.Library.Service;

/// <summary>
/// Exposes the current application version.
/// The web project stores this value in the browser (localStorage) at login and compares it
/// on every page load: when the version changes, stale browser data is cleared and the user
/// is asked to sign in again.
/// </summary>
public class AppVersionService
{
    #region Constants

    /// <summary>Bump this value on every release that changes claims, permissions or cached data.</summary>
    private const string __version = "1.2.0";

    #endregion

    #region Public Members

    /// <summary>The semantic version of the running application.</summary>
    public string CurrentVersion => __version;

    #endregion
}
