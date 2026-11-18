namespace Collection.Library.Service;

/// <summary>Small, stateless helpers shared by the whole application.</summary>
public static class GlobalService
{
    #region Timing Helpers

    /// <summary>
    /// Asynchronously waits for the given time. Used to give the UI a moment to render
    /// (spinners, toasts) before continuing with a long-running operation.
    /// </summary>
    /// <param name="milliseconds">Delay in milliseconds (negative values are treated as zero).</param>
    /// <param name="cancellationToken">Token used to abort the wait.</param>
    public static Task DelayAsync(int milliseconds = 200, CancellationToken cancellationToken = default) =>
        Task.Delay(Math.Max(0, milliseconds), cancellationToken);

    #endregion
}
