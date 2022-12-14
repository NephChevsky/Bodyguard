using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Db.Migrations
{
    public partial class RenameTableTwitchStreamers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Twitchers");

            migrationBuilder.CreateTable(
                name: "TwitchStreamers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", maxLength: 512, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwitchStreamers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TwitchStreamers_Id",
                table: "TwitchStreamers",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TwitchStreamers_Name",
                table: "TwitchStreamers",
                column: "Name",
                unique: true,
                filter: "Deleted = 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TwitchStreamers");

            migrationBuilder.CreateTable(
                name: "Twitchers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", maxLength: 512, nullable: false),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Twitchers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Twitchers_Id",
                table: "Twitchers",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Twitchers_Name",
                table: "Twitchers",
                column: "Name",
                unique: true,
                filter: "Deleted = 0");
        }
    }
}
