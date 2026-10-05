using System.ComponentModel.DataAnnotations;
using WellData.Core.Entities;

namespace WellData.Api.Dtos;

public sealed record RuleDto(
    string Code,
    string Description,
    Severity Severity,
    bool IsEnabled,
    string? ModifiedBy,
    DateTime? ModifiedAt,
    byte[] RowVersion);

public sealed class SetRuleEnabledRequest
{
    public bool IsEnabled { get; set; }

    /// <summary>The RowVersion from the rule as last read. A stale value is rejected with 409.</summary>
    [Required]
    [Length(8, 8, ErrorMessage = "RowVersion is required and must be the 8-byte value returned by the API.")]
    public byte[] RowVersion { get; set; } = [];
}
