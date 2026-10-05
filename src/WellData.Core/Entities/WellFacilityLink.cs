namespace WellData.Core.Entities;

/// <summary>Says which facility a well reports to, starting from a given date.</summary>
public class WellFacilityLink
{
    public int Id { get; set; }

    public int WellId { get; set; }

    public Well Well { get; set; } = null!;

    public int FacilityId { get; set; }

    public Facility Facility { get; set; } = null!;

    public DateOnly StartDate { get; set; }
}
