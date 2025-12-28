using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Riddle.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddInviteCodeToCampaign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "CampaignInstances",
                type: "TEXT",
                maxLength: 6,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignInstances_InviteCode",
                table: "CampaignInstances",
                column: "InviteCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CampaignInstances_InviteCode",
                table: "CampaignInstances");

            migrationBuilder.DropColumn(
                name: "InviteCode",
                table: "CampaignInstances");
        }
    }
}
