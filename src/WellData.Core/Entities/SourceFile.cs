namespace WellData.Core.Entities;

/// <summary>One version of a Petrinex file, identified by its SHA-256 hash.</summary>
public class SourceFile
{
    public int Id { get; set; }

    public required string Url { get; set; }

    public required string FileName { get; set; }

    public SourceFileType FileType { get; set; }

    /// <summary>Null for infrastructure files, which are not tied to a production month.</summary>
    public ProductionMonth? ProductionMonth { get; set; }

    public required string Sha256 { get; set; }

    public int RowCount { get; set; }

    public SourceFileStatus Status { get; set; }

    public DateTime DownloadedAt { get; set; }

    public DateTime? LoadedAt { get; set; }
}
