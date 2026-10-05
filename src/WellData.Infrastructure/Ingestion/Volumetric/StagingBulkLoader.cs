using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WellData.Core.Entities;
using WellData.Infrastructure.Persistence;

namespace WellData.Infrastructure.Ingestion.Volumetric;

/// <summary>Loads raw rows into the staging table with SqlBulkCopy, replacing any earlier rows for the same file.</summary>
public sealed class StagingBulkLoader(WellDataDbContext context)
{
    private const int BatchSize = 20_000;

    private static readonly (string Column, Func<StagedVolumetricRow, object?> Value)[] Columns =
    [
        (nameof(StagedVolumetricRow.SourceFileId), row => row.SourceFileId),
        (nameof(StagedVolumetricRow.RowNumber), row => row.RowNumber),
        (nameof(StagedVolumetricRow.ProductionMonth), row => row.ProductionMonth),
        (nameof(StagedVolumetricRow.OperatorBaId), row => row.OperatorBaId),
        (nameof(StagedVolumetricRow.OperatorName), row => row.OperatorName),
        (nameof(StagedVolumetricRow.ReportingFacilityId), row => row.ReportingFacilityId),
        (nameof(StagedVolumetricRow.ReportingFacilityType), row => row.ReportingFacilityType),
        (nameof(StagedVolumetricRow.ReportingFacilityName), row => row.ReportingFacilityName),
        (nameof(StagedVolumetricRow.SubmissionDate), row => row.SubmissionDate),
        (nameof(StagedVolumetricRow.ActivityId), row => row.ActivityId),
        (nameof(StagedVolumetricRow.ProductId), row => row.ProductId),
        (nameof(StagedVolumetricRow.FromToId), row => row.FromToId),
        (nameof(StagedVolumetricRow.FromToIdType), row => row.FromToIdType),
        (nameof(StagedVolumetricRow.Volume), row => row.Volume),
        (nameof(StagedVolumetricRow.Energy), row => row.Energy),
        (nameof(StagedVolumetricRow.Hours), row => row.Hours),
        (nameof(StagedVolumetricRow.ProrationProduct), row => row.ProrationProduct),
        (nameof(StagedVolumetricRow.ProrationFactor), row => row.ProrationFactor),
    ];

    /// <returns>The number of rows staged.</returns>
    public async Task<int> LoadAsync(int sourceFileId, IAsyncEnumerable<StagedVolumetricRow> rows, CancellationToken cancellationToken)
    {
        await context.StagedVolumetricRows
            .Where(row => row.SourceFileId == sourceFileId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            using var bulkCopy = new SqlBulkCopy((SqlConnection)context.Database.GetDbConnection())
            {
                DestinationTableName = "dbo.StagingVolumetricRows",
                BulkCopyTimeout = 300,
            };

            using var batch = new DataTable();
            foreach (var (column, _) in Columns)
            {
                batch.Columns.Add(column, typeof(object));
                bulkCopy.ColumnMappings.Add(column, column);
            }

            var staged = 0;
            await foreach (var row in rows.WithCancellation(cancellationToken))
            {
                row.SourceFileId = sourceFileId;
                batch.Rows.Add(Columns.Select(column => column.Value(row) ?? DBNull.Value).ToArray());

                if (batch.Rows.Count == BatchSize)
                {
                    staged += await FlushAsync(bulkCopy, batch, cancellationToken);
                }
            }

            return staged + await FlushAsync(bulkCopy, batch, cancellationToken);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static async Task<int> FlushAsync(SqlBulkCopy bulkCopy, DataTable batch, CancellationToken cancellationToken)
    {
        var count = batch.Rows.Count;
        if (count > 0)
        {
            await bulkCopy.WriteToServerAsync(batch, cancellationToken);
            batch.Clear();
        }

        return count;
    }
}
