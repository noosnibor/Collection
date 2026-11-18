using System.ComponentModel.DataAnnotations;

namespace Collection.Library.DataTransferObject;

/// <summary>Grants one permission to one role.</summary>
public class PermissionDataTransferObject
{
    #region Properties

    [Required(ErrorMessage = "A permission key is required")]
    public int? flngPermissionKey { get; set; }

    [Required(ErrorMessage = "A role key is required")]
    public int? flngRoleKey { get; set; }

    #endregion
}
