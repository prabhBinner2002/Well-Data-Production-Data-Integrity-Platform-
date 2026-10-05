using System.ComponentModel.DataAnnotations;

namespace WellData.Api.Dtos;

public sealed class FacilityFilter
{
    [StringLength(4)]
    public string? OperatorBaId { get; set; }

    /// <summary>Two-letter facility type, for example BT for battery.</summary>
    [StringLength(2)]
    public string? FacilityType { get; set; }
}

public sealed record FacilitySummaryDto(
    string FacilityId,
    string FacilityType,
    string Name,
    string? SubTypeDescription,
    string? Location,
    string? OperationalStatus,
    string OperatorBaId,
    string OperatorName);

public sealed record FacilityDetailDto(
    string FacilityId,
    string FacilityType,
    string Name,
    string? SubType,
    string? SubTypeDescription,
    string? Location,
    string? OperationalStatus,
    string OperatorBaId,
    string OperatorName,
    IReadOnlyList<LinkedWellDto> Wells);

public sealed record LinkedWellDto(string WellId, string Uwi, string Name, DateOnly StartDate);
