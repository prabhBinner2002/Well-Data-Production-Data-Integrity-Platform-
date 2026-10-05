using Microsoft.EntityFrameworkCore;
using WellData.Api.Dtos;
using WellData.Core.Entities;
using WellData.Infrastructure.Persistence;

namespace WellData.Api.Services;

public sealed class RuleService(WellDataDbContext context)
{
    public Task<List<RuleDto>> ListAsync(CancellationToken cancellationToken) =>
        context.DataQualityRules
            .AsNoTracking()
            .OrderBy(rule => rule.Code)
            .Select(rule => new RuleDto(
                rule.Code,
                rule.Description,
                rule.Severity,
                rule.IsEnabled,
                rule.ModifiedBy,
                rule.ModifiedAt,
                rule.RowVersion))
            .ToListAsync(cancellationToken);

    public async Task<UpdateResult<RuleDto>> SetEnabledAsync(string code, bool isEnabled, byte[] rowVersion, CancellationToken cancellationToken)
    {
        var rule = await context.DataQualityRules.SingleOrDefaultAsync(candidate => candidate.Code == code, cancellationToken);

        if (rule is null)
        {
            return new UpdateResult<RuleDto>(UpdateOutcome.NotFound);
        }

        context.Entry(rule).Property(candidate => candidate.RowVersion).OriginalValue = rowVersion;
        rule.IsEnabled = isEnabled;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new UpdateResult<RuleDto>(UpdateOutcome.ConcurrencyConflict);
        }

        return new UpdateResult<RuleDto>(UpdateOutcome.Updated, ToDto(rule));
    }

    private static RuleDto ToDto(DataQualityRule rule) =>
        new(rule.Code, rule.Description, rule.Severity, rule.IsEnabled, rule.ModifiedBy, rule.ModifiedAt, rule.RowVersion);
}
