using Microsoft.Extensions.Logging;
using WellData.Core.Entities;
using WellData.Infrastructure.Persistence;
using WellData.Infrastructure.Petrinex;

namespace WellData.Infrastructure.Ingestion.Master;

public sealed record MasterFileResult(SourceFileType FileType, int RowsInSubset, MergeResult Merge);

/// <summary>Loads the four Petrinex infrastructure files for the configured operator subset.</summary>
public sealed class MasterDataIngestionService(
    IPetrinexClient petrinexClient,
    SourceFileRegistry sourceFiles,
    MasterDataMerger merger,
    OperatorSubset subset,
    WellDataDbContext context,
    TimeProvider timeProvider,
    ILogger<MasterDataIngestionService> logger)
{
    /// <summary>
    /// Files load in dependency order: facilities need their operator, links need both the well and the facility.
    /// The merges only touch rows that differ, so running this again with unchanged files changes nothing.
    /// </summary>
    public async Task<IReadOnlyList<MasterFileResult>> IngestAsync(CancellationToken cancellationToken)
    {
        return
        [
            await IngestFileAsync(
                SourceFileType.BusinessAssociate,
                new BusinessAssociateFileParser(),
                row => subset.Includes(row.BaId),
                merger.MergeBusinessAssociatesAsync,
                cancellationToken),
            await IngestFileAsync(
                SourceFileType.Facility,
                new FacilityFileParser(),
                row => subset.Includes(row.OperatorBaId),
                merger.MergeFacilitiesAsync,
                cancellationToken),
            await IngestFileAsync(
                SourceFileType.Well,
                new WellFileParser(),
                row => subset.Includes(row.LinkedFacilityOperatorBaId) || subset.Includes(row.LicenseeBaId),
                merger.MergeWellsAsync,
                cancellationToken),
            await IngestFileAsync(
                SourceFileType.WellFacilityLink,
                new WellFacilityLinkFileParser(),
                row => subset.Includes(row.LinkedFacilityOperatorBaId),
                merger.MergeWellFacilityLinksAsync,
                cancellationToken),
        ];
    }

    private async Task<MasterFileResult> IngestFileAsync<TRow>(
        SourceFileType fileType,
        IMasterFileParser<TRow> parser,
        Func<TRow, bool> isInSubset,
        Func<IReadOnlyCollection<TRow>, CancellationToken, Task<MergeResult>> mergeAsync,
        CancellationToken cancellationToken)
    {
        var file = await petrinexClient.GetInfrastructureFileAsync(fileType, cancellationToken);
        var sourceFile = await sourceFiles.GetOrRegisterAsync(file, cancellationToken);

        var rows = new List<TRow>();
        using (var reader = new StreamReader(file.LocalPath))
        {
            await foreach (var row in parser.ParseAsync(reader, cancellationToken))
            {
                if (isInSubset(row))
                {
                    rows.Add(row);
                }
            }
        }

        var merge = await mergeAsync(rows, cancellationToken);

        sourceFile.RowCount = rows.Count;
        sourceFile.Status = SourceFileStatus.Loaded;
        sourceFile.LoadedAt ??= timeProvider.GetUtcNow().UtcDateTime;
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{FileType}: {Rows} rows in subset, {Inserted} inserted, {Updated} updated",
            fileType,
            rows.Count,
            merge.Inserted,
            merge.Updated);

        return new MasterFileResult(fileType, rows.Count, merge);
    }
}
