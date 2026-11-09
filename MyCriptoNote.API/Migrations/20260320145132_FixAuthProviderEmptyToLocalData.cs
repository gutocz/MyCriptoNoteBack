using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCriptoNote.API.Migrations
{
    /// <inheritdoc />
    public partial class FixAuthProviderEmptyToLocalData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Users\" SET \"AuthProvider\" = 'local' WHERE \"AuthProvider\" IS NULL OR \"AuthProvider\" = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Users\" SET \"AuthProvider\" = '' WHERE \"AuthProvider\" = 'local' AND \"GoogleId\" IS NULL");
        }
    }
}
