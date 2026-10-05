namespace WellData.Core.Entities;

/// <summary>A reporting facility such as a battery (BT), gathering system (GS), or gas plant (GP).</summary>
public class Facility
{
    public int Id { get; set; }

    public required string FacilityId { get; set; }

    public required string FacilityType { get; set; }

    public required string Name { get; set; }

    public string? SubType { get; set; }

    public string? SubTypeDescription { get; set; }

    public string? Location { get; set; }

    public string? OperationalStatus { get; set; }

    public int OperatorId { get; set; }

    public BusinessAssociate Operator { get; set; } = null!;

    public ICollection<WellFacilityLink> WellLinks { get; } = [];
}
