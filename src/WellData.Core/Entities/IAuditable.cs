namespace WellData.Core.Entities;

/// <summary>Entities that record who created and last changed them. The values are set when changes are saved.</summary>
public interface IAuditable
{
    /// <summary>Null for rows that were seeded by a migration rather than created by a user or a load.</summary>
    DateTime? CreatedAt { get; set; }

    string? CreatedBy { get; set; }

    DateTime? ModifiedAt { get; set; }

    string? ModifiedBy { get; set; }
}
