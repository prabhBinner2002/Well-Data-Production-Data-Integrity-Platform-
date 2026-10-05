using System.ComponentModel.DataAnnotations;

namespace WellData.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 signing key. Comes from user-secrets or an environment variable, never from appsettings.</summary>
    [Required]
    [MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters. Run scripts/setup.ps1 to generate one.")]
    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "WellData.Api";

    public string Audience { get; set; } = "WellData.Api";

    [Range(1, 1440)]
    public int ExpiryMinutes { get; set; } = 60;
}
