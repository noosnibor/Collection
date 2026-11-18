namespace Collection.Library.Service;

#region Static Data Records

/// <summary>Collection number (1st, 2nd … collection of a service).</summary>
public record CollectionNumber(int flngCollectionNumberKey, string fstrCollectionNumberType, string fstrDecode);

/// <summary>Application role.</summary>
public record Roles(int Key, string Name);

/// <summary>A single permission that can be granted to a role.</summary>
public record Permission(int Key, string Name, string Description);

#endregion

/// <summary>
/// Lookup data that never changes at run time (roles, permissions, collection numbers).
/// The lists are built once and cached instead of being re-allocated on every call.
/// </summary>
public static class StaticDataService
{
    /// <summary>Role key of a System Admin. Only a System Admin may give this role to a user.</summary>
    public const int AdministratorRoleKey = 1;
    public const int SystemAdminRoleKey = 6;

    #region Cached Lists

    private static readonly CollectionNumber[] __collectionNumbers =
    [
        new(0, "Select a Collection Number", ""),
        new(1, "One", "1st"),
        new(2, "Two", "2nd"),
        new(3, "Three", "3rd"),
        new(4, "Four", "4th"),
        new(5, "Five", "5th"),
        new(6, "Six", "6th"),
        new(7, "Seven", "7th"),
        new(8, "Eight", "8th"),
        new(9, "Nine", "9th"),
        new(10, "Ten", "10th")
    ];

    private static readonly Roles[] __roles =
    [
        new(1, "Administrator"),
        new(2, "Manager"),
        new(3, "User"),
        new(4, "Data Entry"),
        new(5, "Guest"),
        new(6, "System Admin")
    ];

    private static readonly Permission[] __permissions =
    [
        // Data Entry Module
        new(1, "DataEntry.View", "Data Entry View"),
        new(2, "DataEntry.TransactionDate", "Data Entry Transaction Date Change"),
        new(3, "DataEntry.Save", "Data Entry Save"),
        new(4, "DataEntry.Update", "Data Entry Update"),

        // Location Module
        new(11, "Location.View", "Location View"),
        new(12, "Location.Add", "Location Add"),
        new(13, "Location.Update", "Location Update"),

        // Currency Module
        new(21, "Currency.View", "Currency View"),
        new(22, "Currency.Add", "Currency Add"),
        new(23, "Currency.Update", "Currency Update"),

        // Collection Type Module
        new(31, "CollectionType.View", "Collection Type View"),
        new(32, "CollectionType.Add", "Collection Type Add"),
        new(33, "CollectionType.Update", "Collection Type Update"),

        // Membership Type Module
        new(41, "Member.View", "Membership View"),
        new(42, "Member.Add", "Membership Add"),
        new(43, "Member.Update", "Membership Update"),

        // Payment Type Module
        new(51, "Payment.View", "Payment View"),
        new(52, "Payment.Add", "Payment Add"),
        new(53, "Payment.Update", "Payment Update"),

        // Report Module
        new(61, "Report.View", "Report View"),
        new(62, "Report.DetailSearch", "Report Detail Search"),
        new(63, "Report.SummarySearch", "Report Summary Search"),
        new(64, "Report.EODSearch", "Report EOD Search"),

        // User Module
        new(71, "User.View", "User View"),

        // Snapshot Module (key 81 is granted by Database/Scripts/Snapshot_GrantPermission.sql)
        new(81, "Snapshot.View", "Snapshot View"),

        // Financial Period Module (granted by Database/Scripts/Period_GrantPermission.sql)
        new(91, "Period.View", "Financial Period View"),
        new(92, "Period.Close", "Financial Period Close"),
        new(93, "Period.Reopen", "Financial Period Reopen"),
    ];

    #endregion

    #region Public Accessors

    /// <summary>Collection numbers; index 0 is the "Select …" placeholder for drop-downs.</summary>
    public static IReadOnlyList<CollectionNumber> GetCollectionNumber() => __collectionNumbers;

    /// <summary>All application roles.</summary>
    public static IReadOnlyList<Roles> GetRoles() => __roles;

    /// <summary>All permissions the system knows about (also used to register authorization policies).</summary>
    public static IReadOnlyList<Permission> GetPermissions() => __permissions;

    #endregion
}
