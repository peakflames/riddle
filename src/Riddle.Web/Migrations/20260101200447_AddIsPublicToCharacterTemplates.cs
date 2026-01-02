using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Riddle.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPublicToCharacterTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "CharacterTemplates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "CharacterTemplates");
        }
    }
}
