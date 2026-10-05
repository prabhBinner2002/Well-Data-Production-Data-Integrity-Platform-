using Microsoft.EntityFrameworkCore;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Persistence;

public class MigrationTests(SqlServerFixture sqlServer) : DatabaseTest(sqlServer)
{
    [Fact]
    public async Task Migrations_apply_with_none_pending()
    {
        await using var context = Database.CreateContext();

        var pending = await context.Database.GetPendingMigrationsAsync();

        Assert.Empty(pending);
    }

    [Fact]
    public async Task Activity_codes_are_seeded_with_the_negative_volume_flag()
    {
        await using var context = Database.CreateContext();

        var canBeNegative = await context.ActivityCodes
            .AsNoTracking()
            .Where(activity => activity.CanBeNegative)
            .Select(activity => activity.Code)
            .OrderBy(code => code)
            .ToListAsync();

        Assert.Equal(["DIFF", "FRAC", "INVADJ", "LDINVADJ", "PROC", "SHR"], canBeNegative);
        Assert.False(await context.ActivityCodes.AnyAsync(activity => activity.Code == "PROD" && activity.CanBeNegative));
    }

    [Fact]
    public async Task Product_codes_are_seeded()
    {
        await using var context = Database.CreateContext();

        var codes = await context.ProductCodes
            .AsNoTracking()
            .Select(product => product.Code)
            .ToListAsync();

        Assert.Contains("OIL", codes);
        Assert.Contains("GAS", codes);
        Assert.Contains("WATER", codes);
        Assert.Contains("COND", codes);
    }

    [Fact]
    public async Task Natural_keys_are_unique()
    {
        await using var context = Database.CreateContext();
        context.BusinessAssociates.Add(new() { BaId = "0026", LegalName = "First" });
        context.BusinessAssociates.Add(new() { BaId = "0026", LegalName = "Second" });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
