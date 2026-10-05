using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WellData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VolumetricIngestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceFileId = table.Column<int>(type: "int", nullable: false),
                    ProductionMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowsStaged = table.Column<int>(type: "int", nullable: false),
                    RowsLoaded = table.Column<int>(type: "int", nullable: false),
                    RowsRejected = table.Column<int>(type: "int", nullable: false),
                    RowsSkipped = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportRuns_SourceFiles_SourceFileId",
                        column: x => x.SourceFileId,
                        principalTable: "SourceFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StagingVolumetricRows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceFileId = table.Column<int>(type: "int", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    ProductionMonth = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OperatorBaId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OperatorName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ReportingFacilityId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReportingFacilityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReportingFacilityName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SubmissionDate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ActivityId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProductId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FromToId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FromToIdType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Volume = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Energy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Hours = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProrationProduct = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProrationFactor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StagingVolumetricRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StagingVolumetricRows_SourceFiles_SourceFileId",
                        column: x => x.SourceFileId,
                        principalTable: "SourceFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VolumetricRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    OperatorBaId = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: false),
                    ReportingFacilityId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ActivityId = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    ProductId = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    FromToId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    FromToIdType = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: true),
                    Volume = table.Column<decimal>(type: "decimal(13,3)", precision: 13, scale: 3, nullable: true),
                    Energy = table.Column<decimal>(type: "decimal(13,3)", precision: 13, scale: 3, nullable: true),
                    Hours = table.Column<int>(type: "int", nullable: true),
                    IsMasked = table.Column<bool>(type: "bit", nullable: false),
                    SourceFileId = table.Column<int>(type: "int", nullable: false),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolumetricRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolumetricRecords_ActivityCodes_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "ActivityCodes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolumetricRecords_ProductCodes_ProductId",
                        column: x => x.ProductId,
                        principalTable: "ProductCodes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolumetricRecords_SourceFiles_SourceFileId",
                        column: x => x.SourceFileId,
                        principalTable: "SourceFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportRuns_ProductionMonth",
                table: "ImportRuns",
                column: "ProductionMonth");

            migrationBuilder.CreateIndex(
                name: "IX_ImportRuns_SourceFileId",
                table: "ImportRuns",
                column: "SourceFileId");

            migrationBuilder.CreateIndex(
                name: "IX_StagingVolumetricRows_SourceFile_RowNumber",
                table: "StagingVolumetricRows",
                columns: new[] { "SourceFileId", "RowNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_VolumetricRecords_ActivityId",
                table: "VolumetricRecords",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_VolumetricRecords_Month_Facility",
                table: "VolumetricRecords",
                columns: new[] { "ProductionMonth", "ReportingFacilityId" });

            migrationBuilder.CreateIndex(
                name: "IX_VolumetricRecords_ProductId",
                table: "VolumetricRecords",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_VolumetricRecords_SourceFileId",
                table: "VolumetricRecords",
                column: "SourceFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportRuns");

            migrationBuilder.DropTable(
                name: "StagingVolumetricRows");

            migrationBuilder.DropTable(
                name: "VolumetricRecords");
        }
    }
}
