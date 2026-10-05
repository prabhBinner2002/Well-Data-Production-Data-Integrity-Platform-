namespace WellData.Api.Auth;

public static class Roles
{
    /// <summary>Can read everything and change nothing.</summary>
    public const string Viewer = "Viewer";

    /// <summary>Can also review issues, change rules, trigger imports, and close periods.</summary>
    public const string DataSteward = "DataSteward";
}
