using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Riddle.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddReadAloudTonePacing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentReadAloudPacing",
                table: "CampaignInstances",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentReadAloudTone",
                table: "CampaignInstances",
                type: "TEXT",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentReadAloudPacing",
                table: "CampaignInstances");

            migrationBuilder.DropColumn(
                name: "CurrentReadAloudTone",
                table: "CampaignInstances");
        }
    }
}
