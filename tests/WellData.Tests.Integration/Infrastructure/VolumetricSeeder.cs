using WellData.Core;
using WellData.Core.Entities;

namespace WellData.Tests.Integration.Infrastructure;

/// <summary>Inserts exact VolumetricRecord scenarios for SQL rule tests, without going through file ingestion.</summary>
public sealed class VolumetricSeeder(TestDatabase database)
{
    public const string Facility = "ABBT0131180";
    public const string WellA = "ABWI100013604905W500";
    public const string WellB = "ABWI100023604905W500";

    private readonly List<VolumetricRecord> _records = [];

    public VolumetricSeeder Record(
        ProductionMonth month,
        string activity,
        string? product,
        decimal? volume,
        string? well = null,
        int? hours = null,
        bool isMasked = false,
        string facility = Facility,
        string? fromToId = null,
        string? fromToIdType = null)
    {
        _records.Add(new VolumetricRecord
        {
            ProductionMonth = month,
            OperatorBaId = "0XZ4",
            ReportingFacilityId = facility,
            ActivityId = activity,
            ProductId = product,
            FromToId = well ?? fromToId,
            FromToIdType = well is null ? fromToIdType : "WI",
            Volume = volume,
            Hours = hours,
            IsMasked = isMasked,
            SourceRowNumber = _records.Count + 2,
        });

        return this;
    }

    /// <summary>Adds a well's PROD rows for a month, with hours on the oil row as Petrinex reports them.</summary>
    public VolumetricSeeder Production(ProductionMonth month, string well, decimal? oil = null, decimal? gas = null, decimal? water = null, int? hours = 720)
    {
        if (oil is not null)
        {
            Record(month, "PROD", "OIL", oil, well, hours);
        }

        if (gas is not null)
        {
            Record(month, "PROD", "GAS", gas, well);
        }

        if (water is not null)
        {
            Record(month, "PROD", "WATER", water, well);
        }

        return this;
    }

    public async Task SaveAsync()
    {
        await using var context = database.CreateContext();
        var sourceFile = new SourceFile
        {
            Url = "https://petrinex.test/seeded",
            FileName = "seeded.csv",
            FileType = SourceFileType.Volumetric,
            Sha256 = Convert.ToHexStringLower(Guid.NewGuid().ToByteArray()).PadRight(64, '0'),
            Status = SourceFileStatus.Loaded,
        };

        foreach (var record in _records)
        {
            record.SourceFile = sourceFile;
        }

        context.VolumetricRecords.AddRange(_records);
        await context.SaveChangesAsync();
        _records.Clear();
    }
}
