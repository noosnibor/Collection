using Dapper;
using Collection.Data;
using Collection.Library.Model;
using System.Data;
using static Collection.Library.Repository.RepositoryHelper;

namespace Collection.Library.Repository;

#region Contract

/// <summary>Data access for closing and reopening financial periods.</summary>
public interface IPeriodRepository
{
    /// <summary>
    /// Every month / quarter / year of <paramref name="year"/> with its lock state and totals.
    /// <paramref name="locationKey"/> is a location code or "*" (all locations);
    /// <paramref name="moneyLocationKey"/> is the viewer's location: "All locations" is added up in its currency,
    /// and <paramref name="viewerCurrency"/> also shows a single location in that currency.
    /// </summary>
    Task<List<PeriodModel>> SelectPeriods(string locationKey, string moneyLocationKey, int year, string periodType, bool viewerCurrency = false, CancellationToken cancellationToken = default);

    /// <summary>The latest close / reopen events (optionally only one year).</summary>
    Task<List<PeriodAuditModel>> SelectAudit(string locationKey, int? year = null, int top = 20, CancellationToken cancellationToken = default);

    /// <summary>Closes a period so no transaction can be added to or changed inside it.</summary>
    Task<ToastMessageModel> ClosePeriod(PeriodModel period, string who, bool systemAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>Reopens a closed period. A reason is required and is kept in the history.</summary>
    Task<ToastMessageModel> ReopenPeriod(PeriodModel period, string reason, string who, bool systemAdmin = false, CancellationToken cancellationToken = default);
}

#endregion

public class PeriodRepository(IDataAccess dataAccess) : IPeriodRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<PeriodModel>> SelectPeriods(string locationKey, string moneyLocationKey, int year, string periodType, bool viewerCurrency = false,
                                                       CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fstrLocationKey", locationKey);
        parameters.Add("@fstrMoneyLocation", moneyLocationKey);
        parameters.Add("@flngYear", year);
        parameters.Add("@fstrPeriodType", periodType);
        parameters.Add("@fblnViewerCurrency", viewerCurrency);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<PeriodModel>("dbo.sp_SelectPeriod", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    /// <inheritdoc />
    public async Task<List<PeriodAuditModel>> SelectAudit(string locationKey, int? year = null, int top = 20,
                                                          CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fstrLocationKey", locationKey);
        parameters.Add("@flngYear", year);
        parameters.Add("@flngTop", top);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<PeriodAuditModel>("dbo.sp_SelectPeriodAudit", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations

    /// <inheritdoc />
    public async Task<ToastMessageModel> ClosePeriod(PeriodModel period, string who, bool systemAdmin = false, CancellationToken cancellationToken = default)
    {
        if (period is null || string.IsNullOrWhiteSpace(who)) return Invalid();

        try
        {
            var parameters = Identity(period);
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", DateTime.UtcNow);
            parameters.Add("@fblnSystemAdmin", systemAdmin);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("dbo.sp_ClosePeriod", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return rows >= 1
                ? new() { ToastType = ToastTypeModel.Success, Message = $"{period.Name} is now closed{ScopeText(period)}" }
                : new() { ToastType = ToastTypeModel.Warning, Message = $"Unable to close {period.Name}" };
        }
        catch (OperationCanceledException) { throw; }
        // The procedure raises readable messages (not an administrator, earlier period still open, …).
        catch (Exception ex) { return FromException(ex); }
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> ReopenPeriod(PeriodModel period, string reason, string who,
                                                      bool systemAdmin = false, CancellationToken cancellationToken = default)
    {
        if (period is null || string.IsNullOrWhiteSpace(who)) return Invalid();

        try
        {
            var parameters = Identity(period);
            parameters.Add("@fstrReason", reason?.Trim());
            parameters.Add("@fstrWho", who);
            parameters.Add("@fdtmWhen", DateTime.UtcNow);
            parameters.Add("@fblnSystemAdmin", systemAdmin);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("dbo.sp_ReopenPeriod", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return rows >= 1
                ? new() { ToastType = ToastTypeModel.Success, Message = $"{period.Name} has been reopened{ScopeText(period)}" }
                : new() { ToastType = ToastTypeModel.Warning, Message = $"Unable to reopen {period.Name}" };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return FromException(ex); }
    }

    #endregion

    #region Private Helpers

    private static string ScopeText(PeriodModel period) =>
        period.fstrLocationKey == PeriodScope.AllLocations ? " for all locations" : $" for {period.fstrLocationKey}";

    private static DynamicParameters Identity(PeriodModel period)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fstrLocationKey", period.fstrLocationKey);
        parameters.Add("@fstrPeriodType", period.fstrPeriodType);
        parameters.Add("@flngYear", period.flngYear);
        parameters.Add("@flngNumber", period.flngNumber);
        return parameters;
    }

    #endregion
}
