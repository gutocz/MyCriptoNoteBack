using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCriptoNote.API.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeUserEmailsToLowercase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $block$
                BEGIN
                  IF EXISTS (
                    SELECT 1 FROM (
                      SELECT LOWER(TRIM("Email")) AS ne FROM "Users" GROUP BY LOWER(TRIM("Email")) HAVING COUNT(*) > 1
                    ) dups
                  ) THEN
                    RAISE EXCEPTION 'NormalizeUserEmails: duplicate emails after LOWER(TRIM); resolve conflicts before migrating';
                  END IF;
                END $block$;

                UPDATE "Users" SET "Email" = LOWER(TRIM("Email"));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
