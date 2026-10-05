using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WellData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PeriodCloseAndErpExport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErpCostCentreMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WellId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CostCentre = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpCostCentreMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErpExports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    OilVolume = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    GasVolume = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    WaterVolume = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CondensateVolume = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnmappedWellCount = table.Column<int>(type: "int", nullable: false),
                    ExportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExportedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpExports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    ClosedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPeriods", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "DataQualityRules",
                columns: new[] { "Code", "CreatedAt", "CreatedBy", "Description", "IsEnabled", "ModifiedAt", "ModifiedBy", "Severity" },
                values: new object[] { "ERP-01", null, null, "Producing well has no ERP cost centre", true, null, null, "Warning" });

            migrationBuilder.CreateIndex(
                name: "UX_ErpCostCentreMappings_WellId",
                table: "ErpCostCentreMappings",
                column: "WellId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ErpExports_ProductionMonth",
                table: "ErpExports",
                column: "ProductionMonth");

            migrationBuilder.CreateIndex(
                name: "UX_ProductionPeriods_ProductionMonth",
                table: "ProductionPeriods",
                column: "ProductionMonth",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErpCostCentreMappings");

            migrationBuilder.DropTable(
                name: "ErpExports");

            migrationBuilder.DropTable(
                name: "ProductionPeriods");

            migrationBuilder.DeleteData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "ERP-01");
        }
    }
}
