using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Db.Migrations
{
    public partial class Sentiment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Analyzed",
                table: "TwitchMessages");

            migrationBuilder.AddColumn<bool>(
                name: "Sentiment",
                table: "TwitchMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sentiment",
                table: "TwitchMessages");

            migrationBuilder.AddColumn<bool>(
                name: "Analyzed",
                table: "TwitchMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
