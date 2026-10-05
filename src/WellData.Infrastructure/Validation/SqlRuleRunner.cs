using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WellData.Core;
using WellData.Core.Entities;
using WellData.Core.Validation;
using WellData.Infrastructure.Persistence;

namespace WellData.Infrastructure.Validation;

/// <summary>Runs the set-based SQL rules over a loaded month and records what they find as issues.</summary>
public sealed class SqlRuleRunner(WellDataDbContext context, IOptions<ValidationOptions> options, TimeProvider timeProvider)
{
    private static readonly string[] SqlRuleCodes = RuleCatalog.SqlRules.Select(rule => rule.Code).ToArray();

    private readonly ValidationOptions _options = options.Value;

    /// <returns>The number of issues raised.</returns>
    public async Task<int> RunAsync(ProductionMonth month, CancellationToken cancellationToken)
    {
        var enabledCodes = await context.DataQualityRules
            .AsNoTracking()
            .Where(rule => SqlRuleCodes.Contains(rule.Code) && rule.IsEnabled)
            .Select(rule => rule.Code)
            .ToListAsync(cancellationToken);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // Open issues are recomputed from scratch. Reviewed ones are kept and not raised a second time.
        await context.DataQualityIssues
            .Where(issue => issue.ProductionMonth == month)
            .Where(issue => SqlRuleCodes.Contains(issue.RuleCode))
            .Where(issue => issue.Status == IssueStatus.Open)
            .ExecuteDeleteAsync(cancellationToken);

        var reviewed = await context.DataQualityIssues
            .AsNoTracking()
            .Where(issue => issue.ProductionMonth == month)
            .Where(issue => SqlRuleCodes.Contains(issue.RuleCode))
            .Select(issue => new IssueKey(issue.RuleCode, issue.FacilityId, issue.WellId, issue.Field))
            .ToHashSetAsync(cancellationToken);

        var detectedAt = timeProvider.GetUtcNow().UtcDateTime;
        var raised = 0;

        foreach (var code in enabledCodes)
        {
            var findings = await context.Database
                .SqlQueryRaw<SqlRuleFinding>(SqlScripts.Load($"rules/{code}.sql"), CreateParameters(month))
                .ToListAsync(cancellationToken);

            var issues = findings
                .Where(finding => !reviewed.Contains(new IssueKey(code, finding.FacilityId, finding.WellId, finding.Field)))
                .Select(finding => new DataQualityIssue
                {
                    RuleCode = code,
                    ProductionMonth = month,
                    FacilityId = finding.FacilityId,
                    WellId = finding.WellId,
                    Field = finding.Field,
                    RawValue = finding.RawValue,
                    Message = finding.Message,
                    Status = IssueStatus.Open,
                    DetectedAt = detectedAt,
                })
                .ToList();

            context.DataQualityIssues.AddRange(issues);
            raised += issues.Count;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return raised;
    }

    /// <summary>Every script receives the same parameters and uses the ones it needs. Values are never concatenated into SQL.</summary>
    private object[] CreateParameters(ProductionMonth month) =>
    [
        new SqlParameter("@ProductionMonth", SqlDbType.Date) { Value = month.FirstDay.ToDateTime(TimeOnly.MinValue) },
        Decimal("@SpikeThresholdPercent", _options.SpikeThresholdPercent),
        Decimal("@MinimumVolume", _options.MinimumVolume),
        Decimal("@OutlierStandardDeviations", _options.OutlierStandardDeviations),
        new SqlParameter("@MinimumHistoryMonths", SqlDbType.Int) { Value = _options.MinimumHistoryMonths },
        Decimal("@MaxWaterCut", _options.MaxWaterCut),
        Decimal("@MaxGasOilRatio", _options.MaxGasOilRatio),
    ];

    private static SqlParameter Decimal(string name, decimal value) =>
        new(name, SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = value };

    private sealed record IssueKey(string RuleCode, string? FacilityId, string? WellId, string? Field);
}
