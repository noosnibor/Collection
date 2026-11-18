using Dapper;
using Collection.Data;
using Collection.Library.DataTransferObject;
using Collection.Library.Model;
using System.Data;
using static Collection.Library.Repository.RepositoryHelper;

namespace Collection.Library.Repository;

#region Contract

/// <summary>Data access for church collection (offering / tithe) transactions.</summary>
public interface ICollectionRepository
{
    /// <summary>Adds (<paramref name="insert"/> = true) or updates a collection transaction.</summary>
    Task<ToastMessageModel> AddOrUpdateCollection(CollectionDataTransferObject model, bool insert = true, CancellationToken cancellationToken = default);

    /// <summary>Selects collections, optionally filtered by location, key and transaction date.</summary>
    Task<List<CollectionModel>> SelectCollection(string? LocationKey = null, Guid? key = null, DateTime? transactionDate = null, CancellationToken cancellationToken = default);
}

#endregion

public class CollectionRepository(IDataAccess dataAccess) : ICollectionRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<CollectionModel>> SelectCollection(string? LocationKey = null,
                                                              Guid? key = null,
                                                              DateTime? transactionDate = null,
                                                              CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@fstrLocationKey", LocationKey);
        parameters.Add("@flngKey", key);
        parameters.Add("@fdtmTransactionDate", transactionDate);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<CollectionModel>("dbo.sp_SelectCollection", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateCollection(CollectionDataTransferObject model,
                                                               bool insert = true,
                                                               CancellationToken cancellationToken = default)
    {
        if (model is null)
            return Invalid();

        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", model.flngKey);
            parameters.Add("@fstrFirstname", model.fstrFirstname?.Trim());
            parameters.Add("@fstrLastname", model.fstrLastname?.Trim());
            parameters.Add("@fstrEmailAddress", model.fstrEmailAddress?.Trim());
            parameters.Add("@fdtmTransactionDate", model.fdtmTransactionDate);
            parameters.Add("@flngCollectionNumber", model.flngCollectionNumber);
            parameters.Add("@flngMemberTypeKey", model.flngMemberTypeKey);
            parameters.Add("@flngCollectionTypeKey", model.flngCollectionTypeKey);
            parameters.Add("@flngPaymentTypeKey", model.flngPaymentTypeKey);
            parameters.Add("@fcurAmount", model.fcurAmount);
            parameters.Add("@fcurLocalAmount", model.fcurLocalAmount);
            parameters.Add("@fstrCurrencyKey", model.fstrCurrencyKey);
            parameters.Add("@fstrLocationKey", model.fstrLocationKey);
            parameters.Add("@fblnActive", model.fblnActive);
            parameters.Add("@fstrWho", model.fstrWho);
            parameters.Add("@fdtmWhen", model.fdtmWhen);

            var procedure = insert ? "dbo.sp_AddCollection" : "dbo.sp_UpdateCollection";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Collection");
        }
        catch (OperationCanceledException)
        {
            // Never swallow cancellation – the caller asked us to stop.
            throw;
        }
        catch (Exception ex)
        {
            return FromException(ex);
        }
    }

    #endregion
}
