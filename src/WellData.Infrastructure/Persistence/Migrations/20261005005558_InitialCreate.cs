using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WellData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityCodes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CanBeNegative = table.Column<bool>(type: "bit", nullable: false),
                    ReportsVolume = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityCodes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "BusinessAssociates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BaId = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AbbreviatedName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CorporateStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessAssociates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductCodes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCodes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Wells",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WellId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Uwi = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LicenceNumber = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    LicenseeBaId = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true),
                    Field = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StatusFluid = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    StatusMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LicenceStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsConfidential = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wells", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Facilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FacilityId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    FacilityType = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubType = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: true),
                    SubTypeDescription = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    OperationalStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    OperatorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Facilities_BusinessAssociates_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "BusinessAssociates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WellFacilityLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WellId = table.Column<int>(type: "int", nullable: false),
                    FacilityId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WellFacilityLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WellFacilityLinks_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WellFacilityLinks_Wells_WellId",
                        column: x => x.WellId,
                        principalTable: "Wells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ActivityCodes",
                columns: new[] { "Code", "CanBeNegative", "Description", "ReportsVolume" },
                values: new object[,]
                {
                    { "DIFF", true, "Metering difference", true },
                    { "DISP", false, "Disposition", true },
                    { "EMIS", false, "Emissions", true },
                    { "FLARE", false, "Flared", true },
                    { "FRAC", true, "Fractionation yield", true },
                    { "FUEL", false, "Fuel", true },
                    { "IMBAL", false, "Imbalance", true },
                    { "INJ", false, "Injection", true },
                    { "INVADJ", true, "Inventory adjustment", true },
                    { "INVCL", false, "Closing inventory", true },
                    { "INVOP", false, "Opening inventory", true },
                    { "LDINJ", false, "Load fluid injection", true },
                    { "LDINVADJ", true, "Load fluid inventory adjustment", true },
                    { "LDINVCL", false, "Load fluid closing inventory", true },
                    { "LDINVOP", false, "Load fluid opening inventory", true },
                    { "LDREC", false, "Load fluid receipt", true },
                    { "PLTUSE", false, "Plant use", true },
                    { "PROC", true, "Processing gain or loss", true },
                    { "PROD", false, "Production", true },
                    { "PURDISP", false, "Purchase disposition", true },
                    { "PURREC", false, "Purchase receipt", true },
                    { "REC", false, "Receipt", true },
                    { "ROYALTY", false, "Royalty", true },
                    { "SHR", true, "Shrinkage", true },
                    { "SHUTIN", false, "Shut in", false },
                    { "UTIL", false, "Utilities", true },
                    { "VENT", false, "Vented", true }
                });

            migrationBuilder.InsertData(
                table: "ProductCodes",
                columns: new[] { "Code", "Description" },
                values: new object[,]
                {
                    { "ACGAS", "Acid gas" },
                    { "AIR", "Air" },
                    { "BRKWTR", "Brackish water" },
                    { "C1-MX", "Methane mix" },
                    { "C2-MX", "Ethane mix" },
                    { "C2-SP", "Ethane spec" },
                    { "C3-MX", "Propane mix" },
                    { "C3-SP", "Propane spec" },
                    { "C4-MX", "Butanes mix" },
                    { "C4-SP", "Butanes spec" },
                    { "C5-MX", "Pentanes plus mix" },
                    { "C5-SP", "Pentanes plus spec" },
                    { "C6-MX", "Hexanes plus mix" },
                    { "C6-SP", "Hexanes plus spec" },
                    { "CO2", "Carbon dioxide" },
                    { "CO2-MX", "Carbon dioxide mix" },
                    { "COND", "Condensate" },
                    { "DIESEL", "Diesel oil" },
                    { "ENTGAS", "Entrained gas" },
                    { "FSHWTR", "Fresh water" },
                    { "GAS", "Gas" },
                    { "HELIUM", "Helium" },
                    { "IC4-MX", "Iso-butane mix" },
                    { "IC4-SP", "Iso-butane spec" },
                    { "IC5-MX", "Iso-pentane mix" },
                    { "IC5-SP", "Iso-pentane spec" },
                    { "LITEMX", "Lite mix" },
                    { "N2", "Nitrogen" },
                    { "NC4-MX", "Normal butane mix" },
                    { "NC4-SP", "Normal butane spec" },
                    { "NC5-MX", "Normal pentane mix" },
                    { "NC5-SP", "Normal pentane spec" },
                    { "O2", "Oxygen" },
                    { "OIL", "Crude oil" },
                    { "SAND", "Sand" },
                    { "SBASE", "Sulphur basepad" },
                    { "SBLOC", "Sulphur block" },
                    { "SOLV", "Solvent" },
                    { "SPRILL", "Sulphur prilled" },
                    { "STEAM", "Steam" },
                    { "SUL", "Sulphur" },
                    { "SYNCRD", "Synthetic crude oil" },
                    { "WASTE", "Waste" },
                    { "WATER", "Water" }
                });

            migrationBuilder.CreateIndex(
                name: "UX_BusinessAssociates_BaId",
                table: "BusinessAssociates",
                column: "BaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_OperatorId",
                table: "Facilities",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "UX_Facilities_FacilityId",
                table: "Facilities",
                column: "FacilityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WellFacilityLinks_FacilityId",
                table: "WellFacilityLinks",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "UX_WellFacilityLinks_Well_Facility_StartDate",
                table: "WellFacilityLinks",
                columns: new[] { "WellId", "FacilityId", "StartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wells_LicenseeBaId",
                table: "Wells",
                column: "LicenseeBaId");

            migrationBuilder.CreateIndex(
                name: "UX_Wells_WellId",
                table: "Wells",
                column: "WellId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityCodes");

            migrationBuilder.DropTable(
                name: "ProductCodes");

            migrationBuilder.DropTable(
                name: "WellFacilityLinks");

            migrationBuilder.DropTable(
                name: "Facilities");

            migrationBuilder.DropTable(
                name: "Wells");

            migrationBuilder.DropTable(
                name: "BusinessAssociates");
        }
    }
}
