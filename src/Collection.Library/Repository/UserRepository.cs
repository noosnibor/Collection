using Dapper;
using Collection.Data;
using Collection.Library.DataTransferObject;
using Collection.Library.Model;
using System.Data;
using static Collection.Library.Repository.RepositoryHelper;

namespace Collection.Library.Repository;

#region Contract

/// <summary>Data access for application users and their credentials.</summary>
public interface IUserRepository
{
    Task<ToastMessageModel> AddOrUpdateUser(UserDataTransferObject model, bool insert = true, CancellationToken cancellationToken = default);
    Task<List<UserModel>> SelectUser(string? username = null, Guid? key = null, string? LocationKey = null, bool? active = null, CancellationToken cancellationToken = default);
    Task<ToastMessageModel> UpdatePasswordAsync(string username, string password, CancellationToken cancellationToken = default);
}

#endregion

public class UserRepository(IDataAccess dataAccess) : IUserRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<UserModel>> SelectUser(string? username = null,
                                                  Guid? key = null,
                                                  string? LocationKey = null,
                                                  bool? active = null,
                                                  CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fstrUsername", username);
        parameters.Add("@fstrLocationKey", LocationKey);
        parameters.Add("@fblnActive", active);
        parameters.Add("@flngKey", key);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<UserModel>("dbo.sp_SelectUser", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateUser(UserDataTransferObject model,
                                                         bool insert = true,
                                                         CancellationToken cancellationToken = default)
    {
        if (model is null)
            return Invalid();

        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", model.flngKey);
            parameters.Add("@fstrUsername", model.fstrUsername?.Trim());
            parameters.Add("@fstrPassword", model.fstrPassword);
            parameters.Add("@fstrFirstname", model.fstrFirstname?.Trim());
            parameters.Add("@fstrLastname", model.fstrLastname?.Trim());
            parameters.Add("@fstrEmailAddress", model.fstrEmailAddress?.Trim());
            parameters.Add("@fstrLocationKey", model.fstrLocationKey);
            parameters.Add("@flngRoleKey", model.flngRoleKey);
            parameters.Add("@fstrCurrencyKey", model.fstrCurrencyKey);
            parameters.Add("@fblnActive", model.fblnActive);
            parameters.Add("@fblnPasswordChanged", model.fblnPasswordChanged);
            parameters.Add("@fdtmStart", model.fdtmStart);
            parameters.Add("@fdtmEnd", model.fdtmEnd);
            parameters.Add("@fstrWho", model.fstrWho);
            parameters.Add("@fdtmWhen", model.fdtmWhen);

            var procedure = insert ? "dbo.sp_AddUsers" : "dbo.sp_UpdateUser";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "User");
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

    /// <summary>
    /// Stores a new (already hashed) password and flags the account as "password changed".
    /// </summary>
    /// <remarks>
    /// BUG FIX: the failure and exception paths used to return <c>ToastType.Success</c>,
    /// so the UI showed a green "success" toast even when the password was not updated.
    /// </remarks>
    public async Task<ToastMessageModel> UpdatePasswordAsync(string username,
                                                             string password,
                                                             CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return Invalid();

        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@fstrUsername", username);
            parameters.Add("@fstrPassword", password);
            parameters.Add("@fblnPasswordChanged", true);

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync("dbo.sp_UpdatePassword", CommandType.StoredProcedure, parameters),
                cancellationToken);

            return rows >= 1
                ? new() { ToastType = ToastTypeModel.Success, Message = "User password was updated successfully" }
                : new() { ToastType = ToastTypeModel.Warning, Message = "An error has occurred while updating the password" };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return FromException(ex, "Unexpected error: ");
        }
    }

    #endregion
}
