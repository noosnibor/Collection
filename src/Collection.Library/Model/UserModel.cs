namespace Collection.Library.Model;

/// <summary>An application user as stored in the database.</summary>
public class UserModel
{
    #region Identity & Credentials

    public Guid flngKey { get; set; }
    public string? fstrUsername { get; set; }

    /// <summary>BCrypt hash (or the temporary password until the user changes it).</summary>
    public string? fstrPassword { get; set; }

    /// <summary>False until the user replaces the temporary password.</summary>
    public bool fblnPasswordChanged { get; set; }

    #endregion

    #region Profile

    public string? fstrFirstname { get; set; }
    public string? fstrLastname { get; set; }
    public string? fstrEmailAddress { get; set; }
    public string? fstrLocationKey { get; set; }
    public int flngRoleKey { get; set; }
    public string? fstrCurrencyKey { get; set; }

    #endregion

    #region Account Validity

    public bool fblnActive { get; set; }
    public DateTime fdtmStart { get; set; }
    public DateTime fdtmEnd { get; set; }

    #endregion

    #region Audit

    public string? fstrWho { get; set; }
    public DateTime fdtmWhen { get; set; }

    #endregion
}
