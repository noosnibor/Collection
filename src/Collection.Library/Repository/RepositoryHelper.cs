using Collection.Library.Model;

namespace Collection.Library.Repository;

/// <summary>
/// Shared helpers used by every repository in the library.
/// Centralising the toast-message construction and cancellation handling removes
/// ~60 lines of copy/pasted code per repository and guarantees consistent behaviour.
/// </summary>
internal static class RepositoryHelper
{
    #region Toast Message Builders

    /// <summary>Returned when a caller passes a null / empty payload.</summary>
    internal static ToastMessageModel Invalid(string message = "Invalid") =>
        new() { ToastType = ToastTypeModel.Danger, Message = message };

    /// <summary>Returned when an insert would create a duplicate record.</summary>
    internal static ToastMessageModel AlreadyExists(string entity) =>
        new() { ToastType = ToastTypeModel.Danger, Message = $"{entity} already exist in the system" };

    /// <summary>
    /// Converts the "rows affected" value returned by a stored procedure into a
    /// user-friendly toast message (success when at least one row was affected).
    /// </summary>
    /// <param name="rowsAffected">Value returned by <c>ExecuteAsync</c>.</param>
    /// <param name="insert"><c>true</c> for an add, <c>false</c> for an update.</param>
    /// <param name="entity">Display name of the entity, e.g. "Location".</param>
    internal static ToastMessageModel FromRowCount(int rowsAffected, bool insert, string entity)
    {
        if (rowsAffected >= 1)
        {
            return new()
            {
                ToastType = ToastTypeModel.Success,
                Message = insert
                    ? $"{entity} was added successfully"
                    : $"{entity} was updated successfully"
            };
        }

        return new()
        {
            ToastType = ToastTypeModel.Warning,
            Message = insert
                ? $"Unable to add {entity} to the system"
                : $"Unable to update the {entity} in the system"
        };
    }

    /// <summary>Converts an unexpected exception into a danger toast.</summary>
    internal static ToastMessageModel FromException(Exception ex, string prefix = "") =>
        new() { ToastType = ToastTypeModel.Danger, Message = prefix + ex.Message };

    #endregion

    #region Cancellation & Result Helpers

    /// <summary>
    /// Lets the caller stop waiting for a data-access call as soon as the token is cancelled.
    /// <para>
    /// <c>IDataAccess</c> (the Data project) does not accept a <see cref="CancellationToken"/>,
    /// so <see cref="Task.WaitAsync(CancellationToken)"/> is used to honour it from here.
    /// If you later add a token parameter to <c>IDataAccess</c>, pass it through instead.
    /// </para>
    /// </summary>
    internal static async Task<T> WithCancellation<T>(Task<T> task, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Always returns a list (never null) from a Dapper result.</summary>
    internal static List<T> AsSafeList<T>(IEnumerable<T>? result) =>
        result is null ? [] : (result as List<T> ?? [.. result]);

    /// <summary>Case-insensitive, trim-tolerant string comparison used for duplicate checks.</summary>
    internal static bool SameText(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    #endregion
}
