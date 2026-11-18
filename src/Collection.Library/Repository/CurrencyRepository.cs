using Dapper;
using Collection.Data;
using Collection.Library.DataTransferObject;
using Collection.Library.Model;
using System.Data;
using static Collection.Library.Repository.RepositoryHelper;

namespace Collection.Library.Repository;

#region Contract

/// <summary>Data access for currencies and their exchange rates (effective-dated).</summary>
public interface ICurrencyRepository
{
    Task<ToastMessageModel> AddOrUpdateCurrency(CurrencyDataTransferObject obj, bool insert = true, CancellationToken cancellationToken = default);
    Task<List<CurrencyModel>> SelectCurrency(Guid? key = null, string? currency = null, string? location = null, bool? active = null, CancellationToken cancellationToken = default);
}

#endregion

public class CurrencyRepository(IDataAccess dataAccess) : ICurrencyRepository
{
    #region Private Fields

    private readonly IDataAccess __dataAccess = dataAccess;

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<List<CurrencyModel>> SelectCurrency(Guid? key = null,
                                                          string? currency = null,
                                                          string? location = null,
                                                          bool? active = null,
                                                          CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@flngKey", key);
        parameters.Add("@fstrCurrency", currency);
        parameters.Add("@fstrLocation", location);
        parameters.Add("@fblnActive", active);

        var result = await WithCancellation(
            __dataAccess.QueryAsync<CurrencyModel>("dbo.sp_SelectCurrency", CommandType.StoredProcedure, parameters),
            cancellationToken);

        return AsSafeList(result);
    }

    #endregion

    #region Command Operations (Add / Update)

    /// <inheritdoc />
    public async Task<ToastMessageModel> AddOrUpdateCurrency(CurrencyDataTransferObject obj,
                                                             bool insert = true,
                                                             CancellationToken cancellationToken = default)
    {
        if (obj is null)
            return Invalid();

        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@flngKey", obj.flngKey);
            parameters.Add("@fstrCurrency", obj.fstrCurrency?.Trim().ToUpperInvariant()); // ISO codes are upper-case
            parameters.Add("@fstrLocation", obj.fstrLocation);
            parameters.Add("@fstrCurrencyType", obj.fstrCurrencyType?.Trim());
            parameters.Add("@fcurAmount", obj.fcurAmount);
            parameters.Add("@fblnActive", obj.fblnActive);
            parameters.Add("@fstrWho", obj.fstrWho);
            parameters.Add("@fdtmEffectiveFrom", obj.fdtmEffectiveFrom);
            parameters.Add("@fdtmEffectiveTo", obj.fdtmEffectiveTo);

            var procedure = insert ? "dbo.sp_AddCurrency" : "dbo.sp_UpdateCurrency";

            var rows = await WithCancellation(
                __dataAccess.ExecuteAsync(procedure, CommandType.StoredProcedure, parameters),
                cancellationToken);

            return FromRowCount(rows, insert, "Currency");
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
