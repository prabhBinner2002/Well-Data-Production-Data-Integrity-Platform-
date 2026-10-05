using System.ComponentModel.DataAnnotations;
using WellData.Core;

namespace WellData.Api.Dtos;

public sealed class ProductionFilter : IValidatableObject
{
    [StringLength(20)]
    public string? WellId { get; set; }

    [StringLength(4)]
    public string? OperatorBaId { get; set; }

    /// <summary>First month to include, YYYY-MM.</summary>
    public ProductionMonth? FromMonth { get; set; }

    /// <summary>Last month to include, YYYY-MM.</summary>
    public ProductionMonth? ToMonth { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FromMonth is { } from && ToMonth is { } to && from.CompareTo(to) > 0)
        {
            yield return new ValidationResult("FromMonth must not be after ToMonth.", [nameof(FromMonth), nameof(ToMonth)]);
        }
    }
}
