using Microsoft.JSInterop;

namespace Collection.Web.Components.Services;

/// <summary>
/// Converts the UTC times stored in the database into the viewer's own local time.
/// </summary>
/// <remarks>
/// The database always stores UTC, so staff in the US, Canada, London or Jamaica all write the same
/// kind of value and nothing breaks when daylight saving changes. The browser tells us which time
/// zone the person is in (<c>wwwroot/js/localTime.js</c>) and every screen converts on the way out.
/// </remarks>
public class LocalTimeService
{
    #region Private Fields

    private readonly IJSRuntime __js;
    private TimeZoneInfo __zone = TimeZoneInfo.Utc;
    private bool __initialized;

    #endregion

    #region Constructor

    public LocalTimeService(IJSRuntime js) => __js = js;

    #endregion

    #region Properties

    /// <summary>The viewer's time zone (UTC until <see cref="InitializeAsync"/> has run).</summary>
    public TimeZoneInfo Zone => __zone;

    /// <summary>The current date and time for the viewer.</summary>
    public DateTime Now => ToLocal(DateTime.UtcNow);

    /// <summary>Today's date for the viewer (not the server's date).</summary>
    public DateTime Today => Now.Date;

    #endregion

    #region Initialisation

    /// <summary>Asks the browser for its time zone. Safe to call more than once; failures fall back to UTC.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (__initialized) return;

        try
        {
            var id = await __js.InvokeAsync<string>("portalLocalTime.zone", cancellationToken);
            if (!string.IsNullOrWhiteSpace(id) && TryFindZone(id, out var zone))
            {
                __zone = zone;
            }
            else
            {
                // The server does not know that IANA id – use the browser's current offset instead.
                var minutes = await __js.InvokeAsync<int>("portalLocalTime.offsetMinutes", cancellationToken);
                __zone = TimeZoneInfo.CreateCustomTimeZone("browser", TimeSpan.FromMinutes(minutes), "Local", "Local");
            }
            __initialized = true;
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or TaskCanceledException or JSDisconnectedException)
        {
            // Keep UTC; the next call (after the circuit is ready) will try again.
        }
    }

    private static bool TryFindZone(string id, out TimeZoneInfo zone)
    {
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(id); return true; }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            zone = TimeZoneInfo.Utc;
            return false;
        }
    }

    #endregion

    #region Conversion

    /// <summary>Converts a stored UTC time (Kind may be Unspecified, as Dapper returns it) to the viewer's local time.</summary>
    public DateTime ToLocal(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), __zone);

    /// <summary>Short zone label such as "EST" – best effort, empty when not meaningful.</summary>
    public string ZoneAbbreviation => __zone.Id == "browser" ? "" : (__zone.IsDaylightSavingTime(DateTime.UtcNow) ? __zone.DaylightName : __zone.StandardName);

    #endregion
}
