using Dapper;
using Collection.Data;
using Collection.Library.DataTransferObject;
using Collection.Library.Model;
using System.Data;
using static Collection.Library.Repository.RepositoryHelper;

namespace Collection.Library.Repository;

#region Contract

/// <summary>Data access for the detail, summary and end-of-day reports.</summary>
public interface IReportRepository
{
    /// <summary>Detail report – one row per collection transaction.</summary>
    Task<List<ReportModel>> SelectDailyReport(DetailReportDataTransferObject obj, CancellationToken cancellationToken = default);

    /// <summary>Summary report – collections grouped by date / type / payment / currency.</summary>
    Task<List<SummaryModel>> SelectCollectionReport(SummaryReportDataTransferObject obj, CancellationToken cancellationToken = default);

    /// <summary>End-of-day report – totals per collection per day.</summary>
    Task<List<EndOfDayModel>> SelectEndOfDayReport(EndOfDayReportDataTransferObject obj, CancellationToken cancellationToken = default);
}

#endregion

public class ReportRepository(IDataAccess dataAccess) : IReportRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Report Queries

    /// <inheritdoc />
    public async Task<List<ReportModel>> SelectDailyReport(DetailReportDataTransferObject obj,
                                                           CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(obj);

        var parameters = new DynamicParameters();
        parameters.Add("@fstrLocation", NullIfBlank(obj.fstrLocation));
        parameters.Add("@fstrFirstname", NullIfBlank(obj.fstrFirstname));
        parameters.Add("@fstrLastname", NullIfBlank(obj.fstrLastname));
        parameters.Add("@fdtmTransactionStartDate", obj.fdtmTransactionStartDate);
        parameters.Add("@fdtmTransactionEndDate", obj.fdtmTransactionEndDate);
        parameters.Add("@flngMemberType", obj.flngMemberType);
        parameters.Add("@flngCollectionType", obj.flngCollectionType);
        parameters.Add("@flngPaymentType", obj.flngPaymentType);
        parameters.Add("@fstrCurrencyType", NullIfBlank(obj.fstrCurrencyType));
        parameters.Add("@fcurAmountFrom", obj.fcurAmountFrom);
        parameters.Add("@fcurAmountTo", obj.fcurAmountTo);
        parameters.Add("@flngCollectionNumber", obj.flngCollectionNumber);
        parameters.Add("@fstrWho", NullIfBlank(obj.fstrWho));
        parameters.Add("@fstrReportLocation", NullIfBlank(obj.fstrReportLocation));

        var result = await WithCancellation(
            __dataAccess.QueryAsync<ReportModel>("dbo.sp_SelectDetailReport", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    /// <inheritdoc />
    public async Task<List<SummaryModel>> SelectCollectionReport(SummaryReportDataTransferObject obj,
                                                                 CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(obj);

        var parameters = new DynamicParameters();
        parameters.Add("@fstrLocation", NullIfBlank(obj.fstrLocation));
        parameters.Add("@fdtmTransactionStartDate", obj.fdtmTransactionStart);
        parameters.Add("@fdtmTransactionEndDate", obj.fdtmTransactionEnd);
        parameters.Add("@flngCollectionNumber", obj.flngCollectionNumber);
        parameters.Add("@fstrReportLocation", NullIfBlank(obj.fstrReportLocation));

        var result = await WithCancellation(
            __dataAccess.QueryAsync<SummaryModel>("dbo.sp_SelectCollectionReport", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    /// <summary>
    /// BUG FIX / RENAME: was misspelt <c>SelectEnOfDayReport</c>. The web project has been updated.
    /// </summary>
    public async Task<List<EndOfDayModel>> SelectEndOfDayReport(EndOfDayReportDataTransferObject obj,
                                                                CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(obj);

        var parameters = new DynamicParameters();
        parameters.Add("@fstrLocation", NullIfBlank(obj.fstrLocation));
        parameters.Add("@fdtmTransactionStartDate", obj.fdtmTransactionStart);
        parameters.Add("@fdtmTransactionEndDate", obj.fdtmTransactionEnd);
        parameters.Add("@fstrReportLocation", NullIfBlank(obj.fstrReportLocation));

        var result = await WithCancellation(
            __dataAccess.QueryAsync<EndOfDayModel>("dbo.sp_SelectEndOfDay", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Stored procedures treat NULL as "no filter" – an empty string would match nothing.
    /// Only the summary / EOD reports used to convert "" to null; now every filter does.
    /// </summary>
    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    #endregion
}
