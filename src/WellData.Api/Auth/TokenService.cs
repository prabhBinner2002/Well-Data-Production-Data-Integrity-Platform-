using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace WellData.Api.Auth;

public sealed record IssuedToken(string AccessToken, DateTime ExpiresAt, string Role);

/// <summary>Checks a user name and password against the configured accounts and issues a signed JWT.</summary>
public sealed class TokenService(IOptions<JwtOptions> jwtOptions, IOptions<AuthOptions> authOptions, TimeProvider timeProvider)
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    /// <returns>A token, or null when the credentials do not match an account.</returns>
    public IssuedToken? Issue(string userName, string password)
    {
        var account = authOptions.Value.Users.FirstOrDefault(user =>
            string.Equals(user.UserName, userName, StringComparison.OrdinalIgnoreCase));

        // Compare against an empty password when the user is unknown so both cases take the same time.
        var passwordMatches = FixedTimeEquals(password, account?.Password ?? string.Empty);
        if (account is null || account.Password.Length == 0 || !passwordMatches)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_jwt.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, account.UserName),
                new Claim(ClaimTypes.Name, account.UserName),
                new Claim(ClaimTypes.Role, account.Role),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };

        return new IssuedToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt, account.Role);
    }

    private static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(left)),
            SHA256.HashData(Encoding.UTF8.GetBytes(right)));
}
