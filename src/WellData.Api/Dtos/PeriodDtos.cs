using WellData.Core;
using WellData.Core.Entities;

namespace WellData.Api.Dtos;

public sealed record PeriodDto(ProductionMonth ProductionMonth, PeriodStatus Status, string? ClosedBy, DateTime? ClosedAt);
