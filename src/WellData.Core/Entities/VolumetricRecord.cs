namespace WellData.Core.Entities;

/// <summary>A validated, typed volumetric row: one activity for one product at a facility in a month.</summary>
public class VolumetricRecord
{
    public long Id { get; set; }

    public ProductionMonth ProductionMonth { get; set; }

    public required string OperatorBaId { get; set; }

    public required string ReportingFacilityId { get; set; }

    public required string ActivityId { get; set; }

    /// <summary>Null for status-only activities such as SHUTIN.</summary>
    public string? ProductId { get; set; }

    /// <summary>The well or facility on the other side of the movement. "***" when masked, null when not applicable.</summary>
    public string? FromToId { get; set; }

    public string? FromToIdType { get; set; }

    /// <summary>m3 for liquids, e3m3 for gas. Null when masked or not reported.</summary>
    public decimal? Volume { get; set; }

    /// <summary>GJ.</summary>
    public decimal? Energy { get; set; }

    public int? Hours { get; set; }

    /// <summary>True when Petrinex replaced Hours, Volume, or the From/To ID with "***" for confidentiality.</summary>
    public bool IsMasked { get; set; }

    /// <summary>True when a Warning rule flagged this row. The row is loaded but should be reviewed.</summary>
    public bool HasWarning { get; set; }

    public int SourceFileId { get; set; }

    public SourceFile SourceFile { get; set; } = null!;

    public int SourceRowNumber { get; set; }
}
