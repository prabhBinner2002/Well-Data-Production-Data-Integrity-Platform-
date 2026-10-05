using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WellData.Core.Entities;
using WellData.Infrastructure.Persistence;

namespace WellData.Infrastructure.Ingestion.Volumetric;

/// <summary>Bulk inserts typed volumetric records inside the caller's EF transaction.</summary>
public sealed class VolumetricRecordBulkWriter(WellDataDbContext context)
{
    private static readonly (string Column, Type Type, Func<VolumetricRecord, object?> Value)[] Columns =
    [
        (nameof(VolumetricRecord.ProductionMonth), typeof(DateTime), record => record.ProductionMonth.FirstDay.ToDateTime(TimeOnly.MinValue)),
        (nameof(VolumetricRecord.OperatorBaId), typeof(string), record => record.OperatorBaId),
        (nameof(VolumetricRecord.ReportingFacilityId), typeof(string), record => record.ReportingFacilityId),
        (nameof(VolumetricRecord.ActivityId), typeof(string), record => record.ActivityId),
        (nameof(VolumetricRecord.ProductId), typeof(string), record => record.ProductId),
        (nameof(VolumetricRecord.FromToId), typeof(string), record => record.FromToId),
        (nameof(VolumetricRecord.FromToIdType), typeof(string), record => record.FromToIdType),
        (nameof(VolumetricRecord.Volume), typeof(decimal), record => record.Volume),
        (nameof(VolumetricRecord.Energy), typeof(decimal), record => record.Energy),
        (nameof(VolumetricRecord.Hours), typeof(int), record => record.Hours),
        (nameof(VolumetricRecord.IsMasked), typeof(bool), record => record.IsMasked),
        (nameof(VolumetricRecord.HasWarning), typeof(bool), record => record.HasWarning),
        (nameof(VolumetricRecord.SourceFileId), typeof(int), record => record.SourceFileId),
        (nameof(VolumetricRecord.SourceRowNumber), typeof(int), record => record.SourceRowNumber),
    ];

    public async Task WriteAsync(IReadOnlyCollection<VolumetricRecord> records, CancellationToken cancellationToken)
    {
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Volumetric records must be written inside a transaction.");

        using var table = new DataTable();
        foreach (var (column, type, _) in Columns)
        {
            table.Columns.Add(column, type);
        }

        foreach (var record in records)
        {
            table.Rows.Add(Columns.Select(column => column.Value(record) ?? DBNull.Value).ToArray());
        }

        // CheckConstraints makes the bulk insert enforce foreign keys, which SqlBulkCopy skips by default.
        using var bulkCopy = new SqlBulkCopy(
            (SqlConnection)context.Database.GetDbConnection(),
            SqlBulkCopyOptions.CheckConstraints,
            (SqlTransaction)transaction.GetDbTransaction())
        {
            DestinationTableName = "dbo.VolumetricRecords",
            BulkCopyTimeout = 300,
        };

        foreach (var (column, _, _) in Columns)
        {
            bulkCopy.ColumnMappings.Add(column, column);
        }

        await bulkCopy.WriteToServerAsync(table, cancellationToken);
    }
}
