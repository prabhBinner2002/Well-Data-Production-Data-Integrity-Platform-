namespace WellData.Core.Entities;

/// <summary>Audit record of one attempt to load a month's volumetric file.</summary>
public class ImportRun
{
    public int Id { get; set; }

    public int SourceFileId { get; set; }

    public SourceFile SourceFile { get; set; } = null!;

    public ProductionMonth ProductionMonth { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int RowsStaged { get; set; }

    public int RowsLoaded { get; set; }

    public int RowsRejected { get; set; }

    /// <summary>Facility proration header rows, which carry no volume and are not promoted.</summary>
    public int RowsSkipped { get; set; }

    public ImportRunStatus Status { get; set; }

    public string? ErrorMessage { get; set; }
}
