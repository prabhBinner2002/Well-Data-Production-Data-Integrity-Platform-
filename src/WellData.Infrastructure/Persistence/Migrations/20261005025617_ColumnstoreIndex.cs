using Microsoft.EntityFrameworkCore.Migrations;
using WellData.Infrastructure.Persistence;

#nullable disable

namespace WellData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ColumnstoreIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(SqlScripts.Load("indexes/NCCI_VolumetricRecords_Analytics.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS NCCI_VolumetricRecords_Analytics ON dbo.VolumetricRecords;");
        }
    }
}
