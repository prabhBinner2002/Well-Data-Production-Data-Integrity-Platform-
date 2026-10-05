namespace WellData.Core.Entities;

/// <summary>One row of a volumetric file exactly as Petrinex wrote it. Every value is still text.</summary>
public class StagedVolumetricRow
{
    public long Id { get; set; }

    public int SourceFileId { get; set; }

    /// <summary>Line number in the source file, where the header is line 1.</summary>
    public int RowNumber { get; set; }

    public string? ProductionMonth { get; set; }

    public string? OperatorBaId { get; set; }

    public string? OperatorName { get; set; }

    public string? ReportingFacilityId { get; set; }

    public string? ReportingFacilityType { get; set; }

    public string? ReportingFacilityName { get; set; }

    public string? SubmissionDate { get; set; }

    public string? ActivityId { get; set; }

    public string? ProductId { get; set; }

    public string? FromToId { get; set; }

    public string? FromToIdType { get; set; }

    public string? Volume { get; set; }

    public string? Energy { get; set; }

    public string? Hours { get; set; }

    public string? ProrationProduct { get; set; }

    public string? ProrationFactor { get; set; }
}
