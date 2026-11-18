using Collection.Library.Repository;

namespace Collection.Web.Components.Services;

/// <summary>
/// Deletes old Snapshot recordings once a day so the database does not grow without limit.
/// Keeps <c>Snapshot:RetentionDays</c> days (default 14). Does nothing while Snapshot is switched off.
/// </summary>
public class SnapshotRetentionService(IServiceScopeFactory scopeFactory,
                                      IConfiguration configuration,
                                      ILogger<SnapshotRetentionService> logger) : BackgroundService
{
    #region Private Fields

    private readonly IServiceScopeFactory __scopeFactory = scopeFactory;
    private readonly IConfiguration __configuration = configuration;
    private readonly ILogger<SnapshotRetentionService> __logger = logger;

    #endregion

    #region Background Loop

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the application finish starting before the first clean-up.
        try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanAsync(stoppingToken);

            try { await Task.Delay(TimeSpan.FromHours(24), stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task CleanAsync(CancellationToken cancellationToken)
    {
        if (!__configuration.GetValue("Snapshot:Enabled", false)) return;

        try
        {
            var days = Math.Max(1, __configuration.GetValue("Snapshot:RetentionDays", 14));

            using var scope = __scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ISnapshotRepository>();

            await repository.DeleteSnapshotOlderThan(DateTime.UtcNow.AddDays(-days), cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            __logger.LogWarning(ex, "Snapshot clean-up failed; it will try again tomorrow");
        }
    }

    #endregion
}
