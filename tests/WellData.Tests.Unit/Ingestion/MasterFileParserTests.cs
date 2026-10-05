using WellData.Infrastructure.Ingestion.Master;

namespace WellData.Tests.Unit.Ingestion;

public class MasterFileParserTests
{
    [Fact]
    public async Task Business_associate_ids_keep_their_leading_zeros()
    {
        var rows = await ParseAsync(new BusinessAssociateFileParser(), FixtureFiles.BusinessAssociates);

        Assert.Equal(7, rows.Count);
        Assert.Contains(rows, row => row is { BaId: "0026", LegalName: "OVINTIV CANADA ULC", CorporateStatus: "ACTIVE" });
        Assert.Contains(rows, row => row.BaId == "0001");
    }

    [Fact]
    public async Task Blank_optional_business_associate_fields_become_null()
    {
        var rows = await ParseAsync(new BusinessAssociateFileParser(), FixtureFiles.BusinessAssociates);

        var unregistered = Assert.Single(rows, row => row.BaId == "0003");
        Assert.Null(unregistered.AbbreviatedName);
        Assert.Equal("MCGAHA CHARLES", unregistered.LegalName);
    }

    [Fact]
    public async Task Facility_rows_carry_type_location_and_operator()
    {
        var rows = await ParseAsync(new FacilityFileParser(), FixtureFiles.Facilities);

        Assert.Equal(9, rows.Count);
        var battery = Assert.Single(rows, row => row.FacilityId == "ABBT0131180");
        Assert.Equal("BT", battery.FacilityType);
        Assert.Equal("PEMBINA KEYSTONE 03-01-050-05W5", battery.Name);
        Assert.Equal("322", battery.SubType);
        Assert.Equal("03-01-050-05W5", battery.Location);
        Assert.Equal("ACTIVE", battery.OperationalStatus);
        Assert.Equal("0XZ4", battery.OperatorBaId);
    }

    [Fact]
    public async Task Well_rows_get_a_formatted_UWI_and_keep_licence_leading_zeros()
    {
        var rows = await ParseAsync(new WellFileParser(), FixtureFiles.Wells);

        Assert.Equal(18, rows.Count);
        var well = Assert.Single(rows, row => row.WellId == "ABWI100013604905W500");
        Assert.Equal("100/01-36-049-05W5/00", well.Uwi);
        Assert.Equal("BONTERRA HZ PEMBINA 1-36-49-5", well.Name);
        Assert.Equal("0479080", well.LicenceNumber);
        Assert.Equal("0XZ4", well.LicenseeBaId);
        Assert.Equal("0XZ4", well.LinkedFacilityOperatorBaId);
        Assert.Equal("CR-OIL", well.StatusFluid);
    }

    [Fact]
    public async Task Confidential_wells_are_flagged()
    {
        var rows = await ParseAsync(new WellFileParser(), FixtureFiles.Wells);

        Assert.True(rows.Single(row => row.WellId == "ABWI100161207406W600").IsConfidential);
        Assert.False(rows.Single(row => row.WellId == "ABWI100013604905W500").IsConfidential);
    }

    [Fact]
    public async Task Wells_without_a_linked_facility_have_no_linked_operator()
    {
        var rows = await ParseAsync(new WellFileParser(), FixtureFiles.Wells);

        Assert.Null(rows.Single(row => row.WellId == "ABWI100010100115W400").LinkedFacilityOperatorBaId);
    }

    [Fact]
    public async Task Link_rows_parse_the_start_date()
    {
        var rows = await ParseAsync(new WellFacilityLinkFileParser(), FixtureFiles.WellFacilityLinks);

        Assert.Equal(18, rows.Count);
        var link = Assert.Single(rows, row => row.WellId == "ABWI100163606223W400");
        Assert.Equal("ABBT0175610", link.FacilityId);
        Assert.Equal(new DateOnly(2026, 3, 4), link.StartDate);
        Assert.Equal("A04N", link.LinkedFacilityOperatorBaId);
    }

    [Fact]
    public async Task Rows_without_a_key_or_with_an_unreadable_date_are_dropped()
    {
        const string csv = """
            WellID,LinkedFacilityID,LinkedStartDate,LinkedFacilityOperatorBAID
            ,ABBT0131180,2016-04-09,0XZ4
            ABWI100013604905W500,,2016-04-09,0XZ4
            ABWI100013604905W500,ABBT0131180,09/04/2016,0XZ4
            ABWI100013604905W500,ABBT0131180,2016-04-09,0XZ4
            """;

        var rows = new List<WellFacilityLinkRow>();
        await foreach (var row in new WellFacilityLinkFileParser().ParseAsync(new StringReader(csv), CancellationToken.None))
        {
            rows.Add(row);
        }

        Assert.Single(rows);
    }

    private static async Task<List<TRow>> ParseAsync<TRow>(IMasterFileParser<TRow> parser, string fixture)
    {
        using var reader = FixtureFiles.Open(fixture);
        var rows = new List<TRow>();
        await foreach (var row in parser.ParseAsync(reader, CancellationToken.None))
        {
            rows.Add(row);
        }

        return rows;
    }
}
