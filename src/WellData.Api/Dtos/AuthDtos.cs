using System.ComponentModel.DataAnnotations;

namespace WellData.Api.Dtos;

public sealed class TokenRequest
{
    [Required]
    [StringLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Password { get; set; } = string.Empty;
}

public sealed record TokenResponse(string AccessToken, DateTime ExpiresAt, string Role);
