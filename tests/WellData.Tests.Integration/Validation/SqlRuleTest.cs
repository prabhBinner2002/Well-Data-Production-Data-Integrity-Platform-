using Microsoft.EntityFrameworkCore;
using WellData.Core;
using WellData.Core.Entities;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Validation;

/// <summary>Shared arrange and assert helpers for the SQL rule tests.</summary>
public abstract class SqlRuleTest(SqlServerFixture sqlServer) : DatabaseTest(sqlServer)
{
    protected static readonly ProductionMonth June = new(2026, 6);

    protected IngestionHarness Harness { get; private set; } = null!;

    protected VolumetricSeeder Seed { get; private set; } = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Harness = new IngestionHarness(Database);
        Seed = new VolumetricSeeder(Database);
    }

    protected async Task<List<DataQualityIssue>> RunAndGetIssuesAsync(string ruleCode, ProductionMonth? month = null)
    {
        await Harness.RunSqlRulesAsync(month ?? June);
        return await GetIssuesAsync(ruleCode);
    }

    protected async Task<List<DataQualityIssue>> GetIssuesAsync(string ruleCode)
    {
        await using var context = Database.CreateContext();
        return await context.DataQualityIssues
            .AsNoTracking()
            .Where(issue => issue.RuleCode == ruleCode)
            .OrderBy(issue => issue.FacilityId)
            .ThenBy(issue => issue.WellId)
            .ThenBy(issue => issue.Id)
            .ToListAsync();
    }
}
