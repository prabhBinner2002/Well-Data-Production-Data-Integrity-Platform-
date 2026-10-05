namespace WellData.Core.Entities;

/// <summary>A well event as identified by Petrinex (WellId is "ABWI" followed by the 16-character UWI).</summary>
public class Well
{
    public int Id { get; set; }

    public required string WellId { get; set; }

    /// <summary>The UWI in display format, for example 100/06-12-045-05W5/00.</summary>
    public required string Uwi { get; set; }

    public required string Name { get; set; }

    public string? LicenceNumber { get; set; }

    public string? LicenseeBaId { get; set; }

    public string? Field { get; set; }

    public string? StatusFluid { get; set; }

    public string? StatusMode { get; set; }

    public string? LicenceStatus { get; set; }

    public bool IsConfidential { get; set; }

    public ICollection<WellFacilityLink> FacilityLinks { get; } = [];
}
