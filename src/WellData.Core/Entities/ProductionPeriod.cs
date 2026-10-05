namespace WellData.Core.Entities;

/// <summary>The close state of a production month. A month with no row here is open.</summary>
public class ProductionPeriod : IAuditable
{
    public int Id { get; set; }

    public ProductionMonth ProductionMonth { get; set; }

    public PeriodStatus Status { get; set; }

    public string? ClosedBy { get; set; }

    public DateTime? ClosedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public string? ModifiedBy { get; set; }
}
