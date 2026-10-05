using WellData.Core;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Ingestion.Volumetric;

public enum ImportOutcome
{
    Loaded,

    /// <summary>The file's hash matches the version already loaded, so nothing was done.</summary>
    Unchanged,

    Failed,

    /// <summary>The month has been closed, so its data may no longer be replaced.</summary>
    PeriodClosed,
}

/// <param name="Run">The audit record for this attempt. Null when the month was unchanged.</param>
public sealed record ImportResult(ProductionMonth Month, ImportOutcome Outcome, ImportRun? Run);
