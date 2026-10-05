using System.ComponentModel.DataAnnotations;
using WellData.Core;
using WellData.Core.Entities;

namespace WellData.Api.Dtos;

public sealed class IssueFilter
{
    [StringLength(10)]
    public string? RuleCode { get; set; }

    public Severity? Severity { get; set; }

    public IssueStatus? Status { get; set; }

    /// <summary>Production month, YYYY-MM.</summary>
    public ProductionMonth? Month { get; set; }

    [StringLength(20)]
    public string? WellId { get; set; }
}

public sealed record IssueDto(
    long Id,
    string RuleCode,
    string RuleDescription,
    Severity Severity,
    IssueStatus Status,
    ProductionMonth ProductionMonth,
    string? FacilityId,
    string? WellId,
    string? Field,
    string? RawValue,
    string Message,
    int? RowNumber,
    DateTime DetectedAt,
    string? ResolutionNote,
    string? ModifiedBy,
    DateTime? ModifiedAt,
    byte[] RowVersion);

public sealed class AcknowledgeIssueRequest
{
    /// <summary>The RowVersion from the issue as last read. A stale value is rejected with 409.</summary>
    [Required]
    [Length(8, 8, ErrorMessage = "RowVersion is required and must be the 8-byte value returned by the API.")]
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ResolveIssueRequest
{
    /// <summary>What was found and how it was dealt with.</summary>
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Note { get; set; } = string.Empty;

    /// <summary>The RowVersion from the issue as last read. A stale value is rejected with 409.</summary>
    [Required]
    [Length(8, 8, ErrorMessage = "RowVersion is required and must be the 8-byte value returned by the API.")]
    public byte[] RowVersion { get; set; } = [];
}
