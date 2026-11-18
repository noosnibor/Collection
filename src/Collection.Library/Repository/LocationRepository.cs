using Dapper;
using Collection.Data;
using Collection.Library.DataTransferObject;
using Collection.Library.Model;
using System.Data;
using static Collection.Library.Repository.RepositoryHelper;

namespace Collection.Library.Repository;

#region Contract

/// <summary>Data access for church locations / campuses.</summary>
public interface ILocationRepository
{
    Task<ToastMessageModel> AddOrUpdateLocation(LocationDataTransferObject obj, bool insert = true, CancellationToken cancellationToken = default);
    Task<List<LocationModel>> SelectLocation(Guid? key = null, string? location = null, bool? active = null, CancellationToken cancellationToken = default);
}

#endregion

public class LocationRepository(IDataAccess dataAccess) : ILocationRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<LocationModel>> SelectLocation(Guid? key = null,
                                                          string? location = null,
                                                          bool? active = null,
                                                          CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@fstrLocation", location);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<LocationModel>("dbo.sp_SelectLocation", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateLocation(LocationDataTransferObject obj,
                                                             bool insert = true,
                                                             CancellationToken cancellationToken = default)
    {
        if (obj is null)
            return Invalid();

        try
        {
            // The duplicate lookup used to run outside the try/catch, so a database error
            // there crashed the page instead of showing a toast. It now lives inside the try.
            if (insert)
            {
                var existing = await SelectLocation(location: obj.fstrLocation?.Trim(), cancellationToken: cancellationToken);
                if (existing.Any(l => SameText(l.fstrLocation, obj.fstrLocation)))
                    return AlreadyExists("Location");
            }

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@fstrLocation", obj.fstrLocation?.Trim());
            parameters.Add("@fstrDescription", obj.fstrDescription?.Trim());
            parameters.Add("@fstrAddress", obj.fstrAddress?.Trim());
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);

            var procedure = insert ? "dbo.sp_AddLocation" : "dbo.sp_UpdateLocation";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Location");
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

    #endregion
}
