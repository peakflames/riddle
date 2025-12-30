using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Riddle.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterTemplates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: true),
                    SourceFile = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CharacterJson = table.Column<string>(type: "text", nullable: false),
                    Race = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Class = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Level = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterTemplates_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterTemplates_Class",
                table: "CharacterTemplates",
                column: "Class");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterTemplates_Name_OwnerId",
                table: "CharacterTemplates",
                columns: new[] { "Name", "OwnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterTemplates_OwnerId",
                table: "CharacterTemplates",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterTemplates_Race",
                table: "CharacterTemplates",
                column: "Race");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterTemplates");
        }
    }
}
