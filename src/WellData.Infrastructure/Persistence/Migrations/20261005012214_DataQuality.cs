using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WellData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DataQuality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasWarning",
                table: "VolumetricRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DataQualityRules",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Severity = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataQualityRules", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "DataQualityIssues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    ProductionMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceFileId = table.Column<int>(type: "int", nullable: true),
                    RowNumber = table.Column<int>(type: "int", nullable: true),
                    FacilityId = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    WellId = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Field = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    RawValue = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolutionNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataQualityIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataQualityIssues_DataQualityRules_RuleCode",
                        column: x => x.RuleCode,
                        principalTable: "DataQualityRules",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataQualityIssues_SourceFiles_SourceFileId",
                        column: x => x.SourceFileId,
                        principalTable: "SourceFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "DataQualityRules",
                columns: new[] { "Code", "Description", "IsEnabled", "Severity" },
                values: new object[,]
                {
                    { "CON-01", "Volume with 0 hours, or hours with no volume", true, "Warning" },
                    { "FMT-01", "Unparseable number or month", true, "Error" },
                    { "MIS-01", "Blank volume on an activity that reports volume", true, "Error" },
                    { "MSK-01", "Value masked (***) for confidentiality", true, "Info" },
                    { "REF-02", "Unknown product or activity code", true, "Error" },
                    { "RNG-01", "Negative volume on an activity that cannot be negative", true, "Error" },
                    { "RNG-02", "Well hours greater than the hours in the month", true, "Error" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataQualityIssues_Month_Rule_Status",
                table: "DataQualityIssues",
                columns: new[] { "ProductionMonth", "RuleCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DataQualityIssues_RuleCode",
                table: "DataQualityIssues",
                column: "RuleCode");

            migrationBuilder.CreateIndex(
                name: "IX_DataQualityIssues_SourceFileId",
                table: "DataQualityIssues",
                column: "SourceFileId");

            migrationBuilder.CreateIndex(
                name: "IX_DataQualityIssues_WellId",
                table: "DataQualityIssues",
                column: "WellId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataQualityIssues");

            migrationBuilder.DropTable(
                name: "DataQualityRules");

            migrationBuilder.DropColumn(
                name: "HasWarning",
                table: "VolumetricRecords");
        }
    }
}
