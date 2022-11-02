using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentimentAnalysis.Migrations
{
    public partial class TwitchSample : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TwitchSamples",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", maxLength: 512, nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Sentiment = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwitchSamples", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TwitchSamples_Id",
                table: "TwitchSamples",
                column: "Id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TwitchSamples");
        }
    }
}
