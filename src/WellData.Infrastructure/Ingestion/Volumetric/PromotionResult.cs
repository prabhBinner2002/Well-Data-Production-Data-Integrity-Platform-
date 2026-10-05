namespace WellData.Infrastructure.Ingestion.Volumetric;

/// <param name="RowsLoaded">Rows promoted to VolumetricRecords.</param>
/// <param name="RowsRejected">Rows kept out because they failed validation.</param>
/// <param name="RowsSkipped">Facility proration header rows.</param>
public sealed record PromotionResult(int RowsLoaded, int RowsRejected, int RowsSkipped);
