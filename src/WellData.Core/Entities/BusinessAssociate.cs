namespace WellData.Core.Entities;

/// <summary>A company registered with Petrinex (operator or licensee).</summary>
public class BusinessAssociate
{
    public int Id { get; set; }

    public required string BaId { get; set; }

    public required string LegalName { get; set; }

    public string? AbbreviatedName { get; set; }

    public string? CorporateStatus { get; set; }

    public ICollection<Facility> Facilities { get; } = [];
}
