using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCriptoNote.API.Migrations
{
    /// <inheritdoc />
    public partial class FolderPasswordHashAndNoteSaltRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Salt",
                table: "Folders",
                newName: "PasswordHash");

            migrationBuilder.AlterColumn<string>(
                name: "Salt",
                table: "Notes",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "Folders",
                newName: "Salt");

            migrationBuilder.AlterColumn<string>(
                name: "Salt",
                table: "Notes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
