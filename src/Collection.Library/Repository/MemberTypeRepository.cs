using Dapper;
using Collection.Data;
using Collection.Library.DataTransferObject;
using Collection.Library.Model;
using System.Data;
using static Collection.Library.Repository.RepositoryHelper;

namespace Collection.Library.Repository;

#region Contract

/// <summary>Data access for member types (e.g. Member, Visitor, Friend of the Church).</summary>
public interface IMemberTypeRepository
{
    Task<ToastMessageModel> AddOrUpdateMemberType(MemberTypeDataTransferObject obj, bool insert = true, CancellationToken cancellationToken = default);
    Task<List<MemberTypeModel>> SelectMemberType(Guid? key = null, string? type = null, string? location = null, bool? active = null, CancellationToken cancellationToken = default);
}

#endregion

public class MemberTypeRepository(IDataAccess dataAccess) : IMemberTypeRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<MemberTypeModel>> SelectMemberType(Guid? key = null,
                                                                      string? type = null,
                                                                      string? location = null,
                                                                      bool? active = null,
                                                                      CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@fstrType", type);
        parameters.Add("@fstrLocation", location);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<MemberTypeModel>("dbo.sp_SelectMemberType", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateMemberType(MemberTypeDataTransferObject obj,
                                                                   bool insert = true,
                                                                   CancellationToken cancellationToken = default)
    {
        if (obj is null)
            return Invalid();

        try
        {
            // Duplicate check only matters for inserts (the original code also queried on updates).
            if (insert && await TypeAlreadyExists(obj, cancellationToken))
                return AlreadyExists("Member Type");

            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@fstrType", obj.fstrType?.Trim());
            parameters.Add("@fstrLocation", obj.fstrLocation);
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmWhen", obj.fdtmWhen);

            var procedure = insert ? "dbo.sp_AddMemberType" : "dbo.sp_UpdateMemberType";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Member Type");
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

    #region Private Helpers

    /// <summary>
    /// True when a member type with the same name already exists for the same location.
    /// The stored procedure may do a partial (LIKE) match, so the result is re-checked for an
    /// exact, case-insensitive name match before it is treated as a duplicate.
    /// </summary>
    private async Task<bool> TypeAlreadyExists(MemberTypeDataTransferObject obj, CancellationToken cancellationToken)
    {
        var matches = await SelectMemberType(type: obj.fstrType?.Trim(), cancellationToken: cancellationToken);

        return matches.Any(m => SameText(m.fstrType, obj.fstrType) &&
                                (string.IsNullOrWhiteSpace(obj.fstrLocation) ||
                                 string.IsNullOrWhiteSpace(m.fstrLocation) ||
                                 SameText(m.fstrLocation, obj.fstrLocation)));
    }

    #endregion
}
