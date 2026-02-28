using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCriptoNote.API.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteAuthTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthTag",
                table: "Notes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthTag",
                table: "Notes");
        }
    }
}
