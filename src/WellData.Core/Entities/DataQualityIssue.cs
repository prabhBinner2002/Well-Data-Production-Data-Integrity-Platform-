namespace WellData.Core.Entities;

/// <summary>One finding raised by a validation rule.</summary>
public class DataQualityIssue : IAuditable
{
    public long Id { get; set; }

    public required string RuleCode { get; set; }

    public DataQualityRule Rule { get; set; } = null!;

    public ProductionMonth ProductionMonth { get; set; }

    /// <summary>Set for issues found while validating a file. Null for issues found by SQL rules on loaded data.</summary>
    public int? SourceFileId { get; set; }

    /// <summary>Line number in the source file, when the issue is about a single row.</summary>
    public int? RowNumber { get; set; }

    public string? FacilityId { get; set; }

    public string? WellId { get; set; }

    public string? Field { get; set; }

    public string? RawValue { get; set; }

    public required string Message { get; set; }

    public IssueStatus Status { get; set; }

    public DateTime DetectedAt { get; set; }

    public string? ResolutionNote { get; set; }
    public DateTime? CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Changes on every update. Used to detect that someone else saved the row first.</summary>
    public byte[] RowVersion { get; set; } = [];
}
