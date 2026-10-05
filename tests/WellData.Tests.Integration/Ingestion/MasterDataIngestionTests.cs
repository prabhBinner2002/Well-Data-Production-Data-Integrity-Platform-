using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WellData.Core.Entities;
using WellData.Infrastructure.Ingestion;
using WellData.Infrastructure.Ingestion.Master;
using WellData.Infrastructure.Persistence;
using WellData.Infrastructure.Petrinex;
using WellData.Tests.Integration.Infrastructure;

namespace WellData.Tests.Integration.Ingestion;

public class MasterDataIngestionTests(SqlServerFixture sqlServer) : DatabaseTest(sqlServer)
{
    private static readonly string[] Subset = ["0026", "0XZ4", "A04N"];

    [Fact]
    public async Task Only_the_operator_subset_is_loaded()
    {
        await IngestAsync(Subset);

        await using var context = Database.CreateContext();
        var operators = await context.BusinessAssociates
            .AsNoTracking()
            .OrderBy(ba => ba.BaId)
            .Select(ba => ba.BaId)
            .ToListAsync();

        Assert.Equal(Subset, operators);
        Assert.Equal(3, await context.Facilities.CountAsync());
        Assert.Equal(9, await context.Wells.CountAsync());
        Assert.Equal(9, await context.WellFacilityLinks.CountAsync());
    }

    [Fact]
    public async Task Facilities_and_links_are_joined_to_their_parents()
    {
        await IngestAsync(Subset);

        await using var context = Database.CreateContext();
        var battery = await context.Facilities
            .AsNoTracking()
            .Where(facility => facility.FacilityId == "ABBT0131180")
            .Select(facility => new
            {
                Operator = facility.Operator.BaId,
                Wells = facility.WellLinks.Select(link => link.Well.WellId).OrderBy(wellId => wellId).ToList(),
            })
            .SingleAsync();

        Assert.Equal("0XZ4", battery.Operator);
        Assert.Equal(5, battery.Wells.Count);
        Assert.Contains("ABWI100013604905W500", battery.Wells);
    }

    [Fact]
    public async Task Running_ingestion_twice_changes_nothing()
    {
        var first = await IngestAsync(Subset);
        var second = await IngestAsync(Subset);

        Assert.All(first, result => Assert.True(result.Merge.ChangedAnything));
        Assert.All(second, result => Assert.Equal(new MergeResult(0, 0), result.Merge));

        await using var context = Database.CreateContext();
        Assert.Equal(3, await context.BusinessAssociates.CountAsync());
        Assert.Equal(9, await context.Wells.CountAsync());
        Assert.Equal(9, await context.WellFacilityLinks.CountAsync());
        Assert.Equal(4, await context.SourceFiles.CountAsync());
    }

    [Fact]
    public async Task Changed_values_are_updated_in_place()
    {
        await IngestAsync(Subset);
        await using (var context = Database.CreateContext())
        {
            await context.Wells
                .Where(well => well.WellId == "ABWI100013604905W500")
                .ExecuteUpdateAsync(setters => setters.SetProperty(well => well.Name, "STALE NAME"));
        }

        var results = await IngestAsync(Subset);

        var wells = results.Single(result => result.FileType == SourceFileType.Well);
        Assert.Equal(new MergeResult(0, 1), wells.Merge);
        await using var verify = Database.CreateContext();
        var name = await verify.Wells
            .Where(well => well.WellId == "ABWI100013604905W500")
            .Select(well => well.Name)
            .SingleAsync();
        Assert.Equal("BONTERRA HZ PEMBINA 1-36-49-5", name);
    }

    [Fact]
    public async Task Each_source_file_is_recorded_with_its_hash_and_row_count()
    {
        await IngestAsync(Subset);

        await using var context = Database.CreateContext();
        var wellFile = await context.SourceFiles
            .AsNoTracking()
            .SingleAsync(file => file.FileType == SourceFileType.Well);

        Assert.Equal(SourceFileStatus.Loaded, wellFile.Status);
        Assert.Equal(9, wellFile.RowCount);
        Assert.Equal(64, wellFile.Sha256.Length);
        Assert.Null(wellFile.ProductionMonth);
        Assert.NotNull(wellFile.LoadedAt);
    }

    private async Task<IReadOnlyList<MasterFileResult>> IngestAsync(string[] operatorBaIds)
    {
        await using var context = Database.CreateContext();
        var service = new MasterDataIngestionService(
            new FixturePetrinexClient(),
            new SourceFileRegistry(context, TimeProvider.System),
            new MasterDataMerger(context),
            new OperatorSubset(operatorBaIds),
            context,
            TimeProvider.System,
            NullLogger<MasterDataIngestionService>.Instance);

        return await service.IngestAsync(CancellationToken.None);
    }
}
