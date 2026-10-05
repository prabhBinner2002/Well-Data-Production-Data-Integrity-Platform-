using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WellData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditAndConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DataQualityRules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "DataQualityRules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "DataQualityRules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "DataQualityRules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DataQualityRules",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DataQualityIssues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "DataQualityIssues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "DataQualityIssues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "DataQualityIssues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DataQualityIssues",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.UpdateData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "CON-01",
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "DUP-01",
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "FMT-01",
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "GAP-01",
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "MIS-01",
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "MSK-01",
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "OUT-01",
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "RAT-01",
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "REF-01",
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "REF-02",
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "RNG-01",
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "RNG-02",
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "RST-01",
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "DataQualityRules",
                keyColumn: "Code",
                keyValue: "SPK-01",
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy" },
                values: new object[] { null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DataQualityRules");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DataQualityRules");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "DataQualityRules");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "DataQualityRules");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DataQualityRules");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DataQualityIssues");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DataQualityIssues");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "DataQualityIssues");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "DataQualityIssues");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DataQualityIssues");
        }
    }
}
