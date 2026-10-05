namespace WellData.Api.Auth;

/// <summary>The local accounts that may request a token. They come from user-secrets or environment variables.</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public List<UserAccount> Users { get; set; } = [];
}

public sealed class UserAccount
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = Roles.Viewer;
}
