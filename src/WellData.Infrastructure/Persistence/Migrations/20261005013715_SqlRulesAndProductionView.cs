using Microsoft.EntityFrameworkCore.Migrations;
using WellData.Infrastructure.Persistence;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WellData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SqlRulesAndProductionView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VolumetricRecords_Month_Facility",
                table: "VolumetricRecords");

            migrationBuilder.InsertData(
                table: "DataQualityRules",
                columns: new[] { "Code", "Description", "IsEnabled", "Severity" },
                values: new object[,]
                {
                    { "DUP-01", "Duplicate row for the same facility, activity, product, and From/To ID", true, "Error" },
                    { "GAP-01", "Well reappears after months with nothing reported", true, "Warning" },
                    { "OUT-01", "Production far from the well's trailing 12-month average", true, "Warning" },
                    { "RAT-01", "Water cut or gas-oil ratio outside bounds", true, "Warning" },
                    { "REF-01", "From/To well is not in master data", true, "Warning" },
                    { "SPK-01", "Month-over-month production change above the threshold", true, "Warning" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_VolumetricRecords_Month_Facility",
                table: "VolumetricRecords",
                columns: new[] { "ProductionMonth", "ReportingFacilityId" })
                .Annotation("SqlServer:Include", new[] { "ActivityId", "ProductId", "FromToId", "FromToIdType", "Volume", "IsMasked", "SourceRowNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_VolumetricRecords_Well_Month",
                table: "VolumetricRecords",
                columns: new[] { "FromToId", "ProductionMonth" })
                .Annotation("SqlServer:Include", new[] { "FromToIdType", "ActivityId", "ProductId", "Volume", "Hours", "ReportingFacilityId", "OperatorBaId", "IsMasked" });

            migrationBuilder.Sql(SqlScripts.Load("views/vw_MonthlyWellProduction.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_MonthlyWellProduction;");

            migrationBuilder.DropIndex(
                name: "IX_VolumetricRecords_Month_Facility",
                table: "VolumetricRecords");

            migrationBuilder.DropIndex(
                name: "IX_VolumetricRecords_Well_Month",
                table: "VolumetricRecords");

            migrationBuilder.DeleteData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "DUP-01");

            migrationBuilder.DeleteData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "GAP-01");

            migrationBuilder.DeleteData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "OUT-01");

            migrationBuilder.DeleteData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "RAT-01");

            migrationBuilder.DeleteData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "REF-01");

            migrationBuilder.DeleteData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "SPK-01");

            migrationBuilder.CreateIndex(
                name: "IX_VolumetricRecords_Month_Facility",
                table: "VolumetricRecords",
                columns: new[] { "ProductionMonth", "ReportingFacilityId" });
        }
    }
}
