using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WellData.Core;
using WellData.Core.Entities;
using WellData.Infrastructure.Auditing;
using WellData.Infrastructure.Periods;
using WellData.Infrastructure.Persistence;

namespace WellData.Infrastructure.Erp;

public enum ErpExportOutcome
{
    Exported,

    /// <summary>Only an approved, locked month may be sent to the ERP.</summary>
    PeriodNotClosed,
}

public sealed record ErpExportResult(ErpExportOutcome Outcome, ErpExport? Export = null);

/// <summary>
/// Writes a closed month's well production as a CSV in the ERP import layout:
/// PERIOD,COST_CENTRE,WELL_ID,PRODUCT,QUANTITY,UOM with one line per well and product.
/// </summary>
public sealed class ErpExportService(
    WellDataDbContext context,
    PeriodGuard periodGuard,
    IOptions<ErpExportOptions> options,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public const string Header = "PERIOD,COST_CENTRE,WELL_ID,PRODUCT,QUANTITY,UOM";

    /// <summary>Cost centre written for wells without a mapping, so their volumes are still accounted for.</summary>
    public const string UnmappedCostCentre = "UNMAPPED";

    public async Task<ErpExportResult> ExportAsync(ProductionMonth month, CancellationToken cancellationToken)
    {
        if (!await periodGuard.IsClosedAsync(month, cancellationToken))
        {
            return new ErpExportResult(ErpExportOutcome.PeriodNotClosed);
        }

        var wells = await context.MonthlyWellProduction
            .AsNoTracking()
            .Where(production => production.ProductionMonth == month)
            .OrderBy(production => production.WellId)
            .Select(production => new WellVolumes(
                production.WellId,
                context.ErpCostCentreMappings
                    .Where(mapping => mapping.WellId == production.WellId)
                    .Select(mapping => mapping.CostCentre)
                    .FirstOrDefault(),
                production.OilVolume,
                production.GasVolume,
                production.WaterVolume,
                production.CondensateVolume))
            .ToListAsync(cancellationToken);

        var lines = wells.SelectMany(well => ToLines(month, well)).ToList();
        var exportedAt = timeProvider.GetUtcNow().UtcDateTime;
        var fileName = $"erp_production_{month}_{exportedAt:yyyyMMddHHmmssfff}.csv";

        Directory.CreateDirectory(options.Value.Directory);
        await File.WriteAllLinesAsync(
            Path.Combine(options.Value.Directory, fileName),
            lines.Select(line => line.Text).Prepend(Header),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken);

        var export = new ErpExport
        {
            ProductionMonth = month,
            FileName = fileName,
            RowCount = lines.Count,
            OilVolume = Total(lines, "OIL"),
            GasVolume = Total(lines, "GAS"),
            WaterVolume = Total(lines, "WATER"),
            CondensateVolume = Total(lines, "COND"),
            UnmappedWellCount = wells.Count(well => well.CostCentre is null),
            ExportedAt = exportedAt,
            ExportedBy = currentUser.Name,
        };

        context.ErpExports.Add(export);
        await context.SaveChangesAsync(cancellationToken);

        return new ErpExportResult(ErpExportOutcome.Exported, export);
    }

    private static IEnumerable<ExportLine> ToLines(ProductionMonth month, WellVolumes well)
    {
        var costCentre = well.CostCentre ?? UnmappedCostCentre;

        // Gas is reported in thousands of cubic metres, liquids in cubic metres.
        (string Product, decimal? Volume, string Unit)[] products =
        [
            ("OIL", well.Oil, "M3"),
            ("GAS", well.Gas, "E3M3"),
            ("WATER", well.Water, "M3"),
            ("COND", well.Condensate, "M3"),
        ];

        foreach (var (product, volume, unit) in products)
        {
            if (volume is { } quantity)
            {
                var text = string.Create(
                    CultureInfo.InvariantCulture,
                    $"{month},{costCentre},{well.WellId},{product},{quantity:F3},{unit}");
                yield return new ExportLine(product, quantity, text);
            }
        }
    }

    private static decimal Total(IEnumerable<ExportLine> lines, string product) =>
        lines.Where(line => line.Product == product).Sum(line => line.Quantity);

    private sealed record WellVolumes(string WellId, string? CostCentre, decimal? Oil, decimal? Gas, decimal? Water, decimal? Condensate);

    private sealed record ExportLine(string Product, decimal Quantity, string Text);
}
