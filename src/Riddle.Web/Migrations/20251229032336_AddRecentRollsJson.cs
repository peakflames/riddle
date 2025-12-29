using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Riddle.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRecentRollsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RecentRollsJson",
                table: "CampaignInstances",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecentRollsJson",
                table: "CampaignInstances");
        }
    }
}
