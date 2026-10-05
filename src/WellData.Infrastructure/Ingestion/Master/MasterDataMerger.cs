using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WellData.Infrastructure.Persistence;

namespace WellData.Infrastructure.Ingestion.Master;

/// <summary>Upserts master data by bulk loading rows into a temp staging table and running a SQL MERGE.</summary>
public sealed class MasterDataMerger(WellDataDbContext context)
{
    public Task<MergeResult> MergeBusinessAssociatesAsync(IReadOnlyCollection<BusinessAssociateRow> rows, CancellationToken cancellationToken)
    {
        var table = CreateTable("BaId", "LegalName", "AbbreviatedName", "CorporateStatus");
        foreach (var row in rows)
        {
            table.Rows.Add(table.Rows.Count + 1, row.BaId, row.LegalName, row.AbbreviatedName, row.CorporateStatus);
        }

        return MergeAsync("BusinessAssociates", table, cancellationToken);
    }

    public Task<MergeResult> MergeFacilitiesAsync(IReadOnlyCollection<FacilityRow> rows, CancellationToken cancellationToken)
    {
        var table = CreateTable("FacilityId", "FacilityType", "Name", "SubType", "SubTypeDescription", "Location", "OperationalStatus", "OperatorBaId");
        foreach (var row in rows)
        {
            table.Rows.Add(
                table.Rows.Count + 1,
                row.FacilityId,
                row.FacilityType,
                row.Name,
                row.SubType,
                row.SubTypeDescription,
                row.Location,
                row.OperationalStatus,
                row.OperatorBaId);
        }

        return MergeAsync("Facilities", table, cancellationToken);
    }

    public Task<MergeResult> MergeWellsAsync(IReadOnlyCollection<WellRow> rows, CancellationToken cancellationToken)
    {
        var table = CreateTable("WellId", "Uwi", "Name", "LicenceNumber", "LicenseeBaId", "Field", "StatusFluid", "StatusMode", "LicenceStatus");
        table.Columns.Add("IsConfidential", typeof(bool));
        foreach (var row in rows)
        {
            table.Rows.Add(
                table.Rows.Count + 1,
                row.WellId,
                row.Uwi,
                row.Name,
                row.LicenceNumber,
                row.LicenseeBaId,
                row.Field,
                row.StatusFluid,
                row.StatusMode,
                row.LicenceStatus,
                row.IsConfidential);
        }

        return MergeAsync("Wells", table, cancellationToken);
    }

    public Task<MergeResult> MergeWellFacilityLinksAsync(IReadOnlyCollection<WellFacilityLinkRow> rows, CancellationToken cancellationToken)
    {
        var table = CreateTable("WellId", "FacilityId");
        table.Columns.Add("StartDate", typeof(DateTime));
        foreach (var row in rows)
        {
            table.Rows.Add(table.Rows.Count + 1, row.WellId, row.FacilityId, row.StartDate.ToDateTime(TimeOnly.MinValue));
        }

        return MergeAsync("WellFacilityLinks", table, cancellationToken);
    }

    private static DataTable CreateTable(params string[] textColumns)
    {
        var table = new DataTable();
        table.Columns.Add("RowNumber", typeof(int));
        foreach (var column in textColumns)
        {
            table.Columns.Add(column, typeof(string));
        }

        return table;
    }

    /// <summary>
    /// Runs sql/staging/Staging{name}.sql, bulk copies the rows in, then runs sql/merge/Merge{name}.sql.
    /// Everything shares one open connection because the temp table only exists for that session.
    /// </summary>
    private async Task<MergeResult> MergeAsync(string name, DataTable rows, CancellationToken cancellationToken)
    {
        var stagingTable = $"#Staging{name}";
        await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            var connection = (SqlConnection)context.Database.GetDbConnection();

            await using (var createStaging = connection.CreateCommand())
            {
                createStaging.CommandText = SqlScripts.Load($"staging/Staging{name}.sql");
                await createStaging.ExecuteNonQueryAsync(cancellationToken);
            }

            using (var bulkCopy = new SqlBulkCopy(connection) { DestinationTableName = stagingTable })
            {
                foreach (DataColumn column in rows.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }

                await bulkCopy.WriteToServerAsync(rows, cancellationToken);
            }

            await using var merge = connection.CreateCommand();
            merge.CommandText = SqlScripts.Load($"merge/Merge{name}.sql");
            merge.CommandTimeout = 300;
            await using var reader = await merge.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            return new MergeResult(reader.GetInt32(0), reader.GetInt32(1));
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
