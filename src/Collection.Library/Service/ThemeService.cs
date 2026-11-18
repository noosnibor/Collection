namespace Collection.Library.Service;

/// <summary>The theme the user has chosen. <see cref="System"/> follows the operating system (like Windows 11).</summary>
public enum ThemeMode
{
    System,
    Light,
    Dark
}

/// <summary>
/// Holds the user's theme choice for the current circuit and notifies the UI when it changes.
/// The actual colours live in CSS custom properties (see <c>wwwroot/app.css</c>); this service
/// only tracks <em>which</em> palette is active and exposes the CSS variable references that
/// older components still pass in as inline-style parameters.
/// </summary>
public class ThemeService
{
    #region Private Fields

    private ThemeMode __mode = ThemeMode.System;

    #endregion

    #region Events

    /// <summary>Raised after <see cref="Mode"/> changes so layouts can re-render.</summary>
    public event Action? OnChange;

    #endregion

    #region Theme Mode

    /// <summary>The currently selected theme mode.</summary>
    public ThemeMode Mode => __mode;

    /// <summary>Changes the theme and notifies subscribers (no-op when unchanged).</summary>
    public void SetMode(ThemeMode mode)
    {
        if (__mode == mode) return;
        __mode = mode;
        OnChange?.Invoke();
    }

    /// <summary>Parses a stored value ("light" / "dark" / "system"); unknown values fall back to System.</summary>
    public static ThemeMode Parse(string? value) =>
        Enum.TryParse<ThemeMode>(value, ignoreCase: true, out var mode) ? mode : ThemeMode.System;

    #endregion

    #region Legacy Style Tokens

    // These used to be hard-coded hex colours. They now point at CSS variables so every
    // existing component that binds them (Banner, Tabs, Table …) follows light / dark mode.

    /// <summary>Accent used by tab headers and bars.</summary>
    public string HeaderColor { get; set; } = "var(--accent)";

    /// <summary>Background of table header rows.</summary>
    public string TableHeaderColor { get; set; } = "var(--table-head-bg)";

    /// <summary>
    /// Former remote (Unsplash) banner photo. Banners are now a theme-aware gradient, which
    /// removes an external dependency and works offline.
    /// </summary>
    public string ImageBackground { get; set; } = string.Empty;

    #endregion
}
