namespace WellData.Infrastructure.Validation;

/// <summary>The result shape every script in sql/rules returns.</summary>
public sealed class SqlRuleFinding
{
    public string? FacilityId { get; set; }

    public string? WellId { get; set; }

    public string? Field { get; set; }

    public string? RawValue { get; set; }

    public required string Message { get; set; }
}
