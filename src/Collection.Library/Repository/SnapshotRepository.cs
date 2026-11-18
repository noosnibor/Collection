using Dapper;
using Collection.Data;
using Collection.Library.DataTransferObject;
using Collection.Library.Model;
using System.Data;
using static Collection.Library.Repository.RepositoryHelper;

namespace Collection.Library.Repository;

#region Contract

/// <summary>Data access for Snapshot (session replay) recordings.</summary>
public interface ISnapshotRepository
{
    Task<ToastMessageModel> AddSnapshotSession(SnapshotSessionDataTransferObject obj, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> AddSnapshotChunk(SnapshotChunkDataTransferObject obj, CancellationToken cancellationToken = default);
    Task<List<SnapshotUserModel>> SelectSnapshotUser(DateTime fromUtc, CancellationToken cancellationToken = default);
    Task<List<SnapshotSessionModel>> SelectSnapshotSession(string username, CancellationToken cancellationToken = default);
    Task<List<SnapshotMarkerModel>> SelectSnapshotMarker(Guid sessionKey, CancellationToken cancellationToken = default);
    Task<List<SnapshotChunkModel>> SelectSnapshotChunk(Guid sessionKey, CancellationToken cancellationToken = default);
    Task<int> DeleteSnapshotOlderThan(DateTime beforeUtc, CancellationToken cancellationToken = default);
}

#endregion

public class SnapshotRepository(IDataAccess dataAccess) : ISnapshotRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    /// <summary>A normal 5 second chunk is well under 200 KB compressed; anything above this is refused.</summary>
    private const int MaxChunkBytes = 2 * 1024 * 1024;

    /// <summary>A 5 second chunk never holds this many clicks; protects the table from a runaway client.</summary>
    private const int MaxMarkersPerChunk = 500;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<SnapshotUserModel>> SelectSnapshotUser(DateTime fromUtc,
                                                                  CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fdtmFrom", fromUtc);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<SnapshotUserModel>("dbo.sp_SelectSnapshotUser", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    /// <inheritdoc />
    public async Task<List<SnapshotSessionModel>> SelectSnapshotSession(string username,
                                                                        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fstrUsername", username);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<SnapshotSessionModel>("dbo.sp_SelectSnapshotSession", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    /// <inheritdoc />
    public async Task<List<SnapshotMarkerModel>> SelectSnapshotMarker(Guid sessionKey,
                                                                      CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngSessionKey", sessionKey);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<SnapshotMarkerModel>("dbo.sp_SelectSnapshotMarker", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    /// <inheritdoc />
    public async Task<List<SnapshotChunkModel>> SelectSnapshotChunk(Guid sessionKey,
                                                                    CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngSessionKey", sessionKey);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<SnapshotChunkModel>("dbo.sp_SelectSnapshotChunk", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Delete)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddSnapshotSession(SnapshotSessionDataTransferObject obj,
                                                            CancellationToken cancellationToken = default)
    {
        if (obj is null || string.IsNullOrWhiteSpace(obj.fstrUsername))
            return Invalid();

        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@fstrUsername", Clip(obj.fstrUsername, 50));
            parameters.Add("@fstrFullName", Clip(obj.fstrFullName, 100));
            parameters.Add("@fstrLocationKey", Clip(obj.fstrLocationKey, 10));
            parameters.Add("@fstrRole", Clip(obj.fstrRole, 50));
            parameters.Add("@fstrEnvironment", Clip(obj.fstrEnvironment, 30));
            parameters.Add("@fstrEntryUrl", Clip(obj.fstrEntryUrl, 500));
            parameters.Add("@flngViewportW", obj.flngViewportW);
            parameters.Add("@flngViewportH", obj.flngViewportH);
            parameters.Add("@fstrUserAgent", Clip(obj.fstrUserAgent, 500));
            parameters.Add("@fstrWho", Clip(obj.fstrWho ?? obj.fstrUsername, 50));

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("dbo.sp_AddSnapshotSession", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert: true, "Snapshot session");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return FromException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddSnapshotChunk(SnapshotChunkDataTransferObject obj,
                                                          CancellationToken cancellationToken = default)
    {
        if (obj is null || obj.flngSessionKey == Guid.Empty || string.IsNullOrWhiteSpace(obj.fstrWho)
            || obj.fbytData.Length == 0 || obj.fbytData.Length > MaxChunkBytes
            || obj.flstMarkers.Count > MaxMarkersPerChunk)
            return Invalid();

        try
        {
            // The markers travel with the chunk in one table-valued parameter so both are saved together.
            var table = new DataTable();
            table.Columns.Add("flngTimestampMs", typeof(long));
            table.Columns.Add("fstrKind", typeof(string));
            table.Columns.Add("fstrLabel", typeof(string));
            table.Columns.Add("fstrUrl", typeof(string));

            foreach (var marker in obj.flstMarkers)
                table.Rows.Add(marker.flngTimestampMs,
                               Clip(marker.fstrKind, 20),
                               Clip(marker.fstrLabel, 200),
                               string.IsNullOrWhiteSpace(marker.fstrUrl) ? (object)DBNull.Value : Clip(marker.fstrUrl, 500));

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@flngSessionKey", obj.flngSessionKey);
            parameters.Add("@flngSequence", obj.flngSequence);
            parameters.Add("@flngEventCount", obj.flngEventCount);
            parameters.Add("@flngFirstEventMs", obj.flngFirstEventMs);
            parameters.Add("@flngLastEventMs", obj.flngLastEventMs);
            parameters.Add("@fbytData", obj.fbytData, DbType.Binary);
            parameters.Add("@marker", table.AsTableValuedParameter("dbo.tblBulkSnapshotMarker"));
            parameters.Add("@fstrWho", Clip(obj.fstrWho, 50));

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("dbo.sp_AddSnapshotChunk", CommandType.StoredProcedure, parameters),
                cancellationToken);

            // The procedure does nothing (rows = -1) when the recording does not belong to this user.
            return FromRowCount(rows, insert: true, "Snapshot chunk");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return FromException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> DeleteSnapshotOlderThan(DateTime beforeUtc,
                                                   CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fdtmBefore", beforeUtc);

        return await WithCancellation(
            __dataAccess.ExecuteAsync("dbo.sp_DeleteSnapshotOlderThan", CommandType.StoredProcedure, parameters),
            cancellationToken);
    }

    #endregion

    #region Helpers

    /// <summary>Trims and shortens text so it always fits its column.</summary>
    private static string Clip(string? value, int max)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length <= max ? text : text[..max];
    }

    #endregion
}
