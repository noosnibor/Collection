using Microsoft.JSInterop;

namespace Collection.Web.Components.Services;

#region Contract

/// <summary>Logs a message to the server log and to the browser console.</summary>
public interface ILogService<T>
{
    Task LogDebug(string message);
    Task LogError(string message);
    Task LogInfo(string message);
    Task LogWarning(string message);
}

#endregion

public class LogService<T>(ILogger<T> logger, IJSRuntime js) : ILogService<T>
{
    #region Private Fields

    private readonly ILogger<T> __logger = logger;
    private readonly IJSRuntime __js = js;

    #endregion

    #region Public Log Levels

    public Task LogInfo(string message) => LogAsync(message, LogLevel.Information);
    public Task LogWarning(string message) => LogAsync(message, LogLevel.Warning);
    public Task LogError(string message) => LogAsync(message, LogLevel.Error);
    public Task LogDebug(string message) => LogAsync(message, LogLevel.Debug);

    #endregion

    #region Private Helpers

    /// <summary>Central dual logger (server + browser console).</summary>
    private async Task LogAsync(string message, LogLevel level)
    {
        // Server log (structured – the message is never used as a format string).
        __logger.Log(level, "{Message}", message);

        // Browser console. The circuit may already be gone, so failures are ignored.
        try
        {
            var console = level switch
            {
                LogLevel.Warning => "console.warn",
                LogLevel.Error or LogLevel.Critical => "console.error",
                LogLevel.Debug => "console.debug",
                _ => "console.log"
            };

            await __js.InvokeVoidAsync(console, $"[{typeof(T).Name}] {message}");
        }
        catch (Exception ex) when (ex is JSDisconnectedException or InvalidOperationException or TaskCanceledException)
        {
            // Circuit disconnected / prerendering – nothing to do.
        }
    }

    #endregion
}
