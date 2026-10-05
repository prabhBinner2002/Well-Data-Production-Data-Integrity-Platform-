namespace WellData.Core.Entities;

/// <summary>Record of one ERP export file, with the totals needed to reconcile it against the database.</summary>
public class ErpExport
{
    public int Id { get; set; }

    public ProductionMonth ProductionMonth { get; set; }

    public required string FileName { get; set; }

    public int RowCount { get; set; }

    /// <summary>m3.</summary>
    public decimal OilVolume { get; set; }

    /// <summary>e3m3.</summary>
    public decimal GasVolume { get; set; }

    /// <summary>m3.</summary>
    public decimal WaterVolume { get; set; }

    /// <summary>m3.</summary>
    public decimal CondensateVolume { get; set; }

    /// <summary>Wells exported under the placeholder cost centre because they have no mapping.</summary>
    public int UnmappedWellCount { get; set; }

    public DateTime ExportedAt { get; set; }

    public required string ExportedBy { get; set; }
}
