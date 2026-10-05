namespace WellData.Core.Entities;

/// <summary>A Petrinex product (OIL, GAS, WATER, ...). The code is the primary key.</summary>
public class ProductCode
{
    public required string Code { get; set; }

    public required string Description { get; set; }
}
