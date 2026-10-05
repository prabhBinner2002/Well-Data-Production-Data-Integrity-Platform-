using Microsoft.EntityFrameworkCore;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Validation;

public class MonthlyWellProductionViewTests(SqlServerFixture sqlServer) : DatabaseTest(sqlServer)
{
    [Fact]
    public async Task View_pivots_a_wells_products_into_one_row()
    {
        await new IngestionHarness(Database).IngestFixtureMonthAsync();

        await using var context = Database.CreateContext();
        var row = await context.MonthlyWellProduction
            .AsNoTracking()
            .SingleAsync(production => production.WellId == "ABWI100013604905W500");

        Assert.Equal(IngestionHarness.FixtureMonth, row.ProductionMonth);
        Assert.Equal("ABBT0131180", row.FacilityId);
        Assert.Equal("0XZ4", row.OperatorBaId);
        Assert.Equal(38.6m, row.OilVolume);
        Assert.Equal(9.8m, row.GasVolume);
        Assert.Equal(1.0m, row.WaterVolume);
        Assert.Null(row.CondensateVolume);
        Assert.Equal(720, row.ProducingHours);
        Assert.False(row.IsMasked);
    }

    [Fact]
    public async Task View_has_one_row_per_producing_well_and_excludes_non_production_activities()
    {
        await new IngestionHarness(Database).IngestFixtureMonthAsync();

        await using var context = Database.CreateContext();
        var rows = await context.MonthlyWellProduction.AsNoTracking().ToListAsync();

        // 14 wells appear in the fixture; two of them only have a SHUTIN row.
        Assert.Equal(12, rows.Count);
        Assert.Equal(rows.Count, rows.Select(row => row.WellId).Distinct().Count());
    }

    [Fact]
    public async Task Confidential_well_keeps_its_volumes_but_hours_are_unknown()
    {
        await new IngestionHarness(Database).IngestFixtureMonthAsync();

        await using var context = Database.CreateContext();
        var row = await context.MonthlyWellProduction
            .AsNoTracking()
            .SingleAsync(production => production.WellId == "ABWI100163606223W400");

        Assert.True(row.IsMasked);
        Assert.Null(row.ProducingHours);
        Assert.Equal(206.9m, row.OilVolume);
        Assert.Equal(445.1m, row.WaterVolume);
    }
}
