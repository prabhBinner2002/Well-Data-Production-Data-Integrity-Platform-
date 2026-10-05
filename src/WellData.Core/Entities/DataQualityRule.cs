namespace WellData.Core.Entities;

/// <summary>Catalog entry for a validation rule. The code is the primary key and matches the rule's implementation.</summary>
public class DataQualityRule : IAuditable
{
    public required string Code { get; set; }

    public required string Description { get; set; }

    public Severity Severity { get; set; }

    public bool IsEnabled { get; set; } = true;
    public DateTime? CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Changes on every update. Used to detect that someone else saved the row first.</summary>
    public byte[] RowVersion { get; set; } = [];
}
