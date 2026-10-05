using WellData.Core;
using WellData.Core.Entities;
using WellData.Infrastructure.Ingestion.Volumetric;

namespace WellData.Api.Dtos;

public sealed record ImportRunDto(
    int Id,
    ProductionMonth ProductionMonth,
    ImportRunStatus Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int RowsStaged,
    int RowsLoaded,
    int RowsRejected,
    int RowsSkipped,
    string? ErrorMessage,
    string SourceFileName,
    string SourceFileHash,
    SourceFileStatus SourceFileStatus);

public sealed class TriggerImportRequest
{
    /// <summary>Production month to import, YYYY-MM.</summary>
    public ProductionMonth Month { get; set; }
}

/// <param name="Run">Null when the month's file was unchanged and nothing was loaded.</param>
public sealed record ImportResultDto(ProductionMonth Month, ImportOutcome Outcome, ImportRunDto? Run);
