namespace WellData.Core.Entities;

/// <summary>Says which ERP cost centre a well's volumes are booked to.</summary>
public class ErpCostCentreMapping : IAuditable
{
    public int Id { get; set; }

    public required string WellId { get; set; }

    public required string CostCentre { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public string? ModifiedBy { get; set; }
}
