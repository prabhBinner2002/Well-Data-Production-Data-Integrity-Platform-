using WellData.Core.Entities;
using WellData.Core.Volumetric;
using WellData.Infrastructure.Ingestion.Volumetric;

namespace WellData.Tests.Unit.Volumetric;

public class VolumetricFileReaderTests
{
    [Fact]
    public async Task Every_data_row_is_read_with_its_file_line_number()
    {
        var rows = await ReadFixtureAsync();

        Assert.Equal(136, rows.Count);
        Assert.Equal(2, rows[0].RowNumber);
        Assert.Equal(137, rows[^1].RowNumber);
    }

    [Fact]
    public async Task Values_are_kept_as_raw_text_including_leading_zeros()
    {
        var rows = await ReadFixtureAsync();

        var row = rows.First(candidate => candidate is { ActivityId: "PROD", ProductId: "OIL", FromToId: "ABWI100013604905W500" });
        Assert.Equal("2026-06", row.ProductionMonth);
        Assert.Equal("0XZ4", row.OperatorBaId);
        Assert.Equal("ABBT0131180", row.ReportingFacilityId);
        Assert.Equal("38.6", row.Volume);
        Assert.Equal("720", row.Hours);
        Assert.Contains(rows, candidate => candidate.OperatorBaId == "0007");
    }

    [Fact]
    public async Task Proration_header_rows_are_recognised_separately_from_data_rows()
    {
        var rows = (await ReadFixtureAsync()).Select(row => new ParsedVolumetricRow(row)).ToList();

        var headers = rows.Where(row => row.IsProrationHeader).ToList();
        Assert.Equal(6, headers.Count);
        Assert.All(headers, header => Assert.NotNull(header.Staged.ProrationFactor));
        Assert.Contains(headers, header => header.Staged is { ProrationProduct: "OIL", ProrationFactor: "1.04224" });
    }

    [Fact]
    public async Task Masked_blank_and_valid_hours_are_told_apart()
    {
        var rows = (await ReadFixtureAsync()).Select(row => new ParsedVolumetricRow(row)).ToList();

        var confidentialWell = rows.First(row => row is { ActivityId: "PROD", ProductId: "OIL", FromToId: "ABWI100161207406W600" });
        var gasRow = rows.First(row => row is { ActivityId: "PROD", ProductId: "GAS", FromToId: "ABWI100013604905W500" });
        var oilRow = rows.First(row => row is { ActivityId: "PROD", ProductId: "OIL", FromToId: "ABWI100013604905W500" });

        Assert.Equal(FieldState.Masked, confidentialWell.Hours.State);
        Assert.True(confidentialWell.IsMasked);
        Assert.Equal(1164.8m, confidentialWell.Volume.Value);

        Assert.Equal(FieldState.Blank, gasRow.Hours.State);
        Assert.False(gasRow.IsMasked);

        Assert.Equal(720, oilRow.Hours.Value);
    }

    [Fact]
    public async Task Masked_from_to_id_is_masked_and_is_not_a_well()
    {
        var rows = (await ReadFixtureAsync()).Select(row => new ParsedVolumetricRow(row)).ToList();

        var maskedDisposition = rows.First(row => row is { ReportingFacilityId: "ABBT0170649", ActivityId: "DISP", ProductId: "OIL" });

        Assert.True(maskedDisposition.IsFromToMasked);
        Assert.True(maskedDisposition.IsMasked);
        Assert.Null(maskedDisposition.WellId);
        Assert.Equal(1884.5m, maskedDisposition.Volume.Value);
    }

    [Fact]
    public async Task Shut_in_rows_have_no_product_and_a_blank_volume()
    {
        var rows = (await ReadFixtureAsync()).Select(row => new ParsedVolumetricRow(row)).ToList();

        var shutIn = rows.Where(row => row.ActivityId == "SHUTIN").ToList();

        Assert.Equal(2, shutIn.Count);
        Assert.All(shutIn, row =>
        {
            Assert.False(row.IsProrationHeader);
            Assert.Equal(string.Empty, row.ProductId);
            Assert.Equal(FieldState.Blank, row.Volume.State);
            Assert.NotNull(row.WellId);
        });
    }

    [Fact]
    public async Task A_file_without_the_volumetric_columns_is_refused()
    {
        using var reader = FixtureFiles.Open(FixtureFiles.BusinessAssociates);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            await foreach (var _ in new VolumetricFileReader().ReadAsync(reader, CancellationToken.None))
            {
            }
        });

        Assert.Contains("ProductionMonth", exception.Message);
    }

    private static async Task<List<StagedVolumetricRow>> ReadFixtureAsync()
    {
        using var reader = FixtureFiles.Open(FixtureFiles.Volumetric);
        var rows = new List<StagedVolumetricRow>();
        await foreach (var row in new VolumetricFileReader().ReadAsync(reader, CancellationToken.None))
        {
            rows.Add(row);
        }

        return rows;
    }
}
