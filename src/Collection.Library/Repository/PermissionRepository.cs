using Dapper;
using Collection.Data;
using Collection.Library.DataTransferObject;
using Collection.Library.Model;
using System.Data;
using static Collection.Library.Repository.RepositoryHelper;

namespace Collection.Library.Repository;

#region Contract

/// <summary>Data access for role-based permissions.</summary>
public interface IPermissionRepository
{
    Task<ToastMessageModel> CreatePermission(List<PermissionDataTransferObject> model, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> DeletePermission(int flngPermissionKey, int flngRoleKey, CancellationToken cancellationToken = default);
    Task<List<PermissionModel>> SelectPermission(int? permissionKey = null, int? roleKey = null, CancellationToken cancellationToken = default);
    Task<List<string>> SelectPermissionByUsername(string username, string locationKey, CancellationToken cancellationToken = default);
}

#endregion

public class PermissionRepository(IDataAccess dataAccess) : IPermissionRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<PermissionModel>> SelectPermission(int? permissionKey = null,
                                                              int? roleKey = null,
                                                              CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngPermissionKey", permissionKey);
        parameters.Add("@flngRoleKey", roleKey);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<PermissionModel>("dbo.sp_SelectPermission", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    /// <summary>
    /// Returns the permission keys (as strings) granted to a user at a location.
    /// Used by the login process to build the user's permission claims.
    /// </summary>
    public async Task<List<string>> SelectPermissionByUsername(string username,
                                                               string locationKey,
                                                               CancellationToken cancellationToken = default)
    {
        // Nothing can be granted to an unknown user – avoid a pointless round-trip.
        if (string.IsNullOrWhiteSpace(username))
            return [];

        var parameters = new DynamicParameters();
        parameters.Add("@fstrUsername", username);
        parameters.Add("@fstrLocationKey", locationKey);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<string>("dbo.sp_SelectUserInPermission", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Create / Delete)

    /// <summary>Grants a batch of permissions to roles using a table-valued parameter.</summary>
    public async Task<ToastMessageModel> CreatePermission(List<PermissionDataTransferObject> model,
                                                          CancellationToken cancellationToken = default)
    {
        if (model is null || model.Count == 0)
            return Invalid();

        // Rows without both keys cannot be written to the table-valued parameter.
        if (model.Any(m => m.flngPermissionKey is null || m.flngRoleKey is null))
            return Invalid("Every permission row needs a permission key and a role key");

        try
        {
            var table = new DataTable();
            table.Columns.Add("flngPermissionKey", typeof(int));
            table.Columns.Add("flngRoleKey", typeof(int));

            foreach (var row in model)
                table.Rows.Add(row.flngPermissionKey, row.flngRoleKey);

            var parameters = new DynamicParameters();
            parameters.Add("@permission", table.AsTableValuedParameter("dbo.tblBulkPermission"));

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("dbo.sp_AddPermission", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return rows >= 1
                ? new() { ToastType = ToastTypeModel.Success, Message = "Permission was added successfully" }
                : new() { ToastType = ToastTypeModel.Warning, Message = "Unable to add permission" };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // BUG FIX: this used to return ToastType.Success when an exception occurred.
            return FromException(ex);
        }
    }

    /// <summary>Revokes one permission from one role.</summary>
    public async Task<ToastMessageModel> DeletePermission(int flngPermissionKey,
                                                          int flngRoleKey,
                                                          CancellationToken cancellationToken = default)
    {
        if (flngPermissionKey == 0 || flngRoleKey == 0)
            return Invalid();

        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngPermissionKey", flngPermissionKey);
            parameters.Add("@flngRoleKey", flngRoleKey);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("dbo.sp_DeletePermission", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return rows >= 1
                ? new() { ToastType = ToastTypeModel.Success, Message = "Permission was deleted successfully" }
                : new() { ToastType = ToastTypeModel.Warning, Message = "Unable to delete permission" };
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
