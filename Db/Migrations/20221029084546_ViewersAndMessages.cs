using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Db.Migrations
{
    public partial class ViewersAndMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TwitchStreamers_Name",
                table: "TwitchStreamers");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "TwitchStreamers");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreationDateTime",
                table: "TwitchStreamers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModificationDateTime",
                table: "TwitchStreamers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwitchId",
                table: "TwitchStreamers",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "TwitchMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", maxLength: 512, nullable: false),
                    Owner = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<Guid>(type: "uniqueidentifier", maxLength: 512, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CreationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationDateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwitchMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TwitchViewers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", maxLength: 512, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    TwitchId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CreationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationDateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwitchViewers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TwitchMessages_Id",
                table: "TwitchMessages",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TwitchViewers_Id",
                table: "TwitchViewers",
                column: "Id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TwitchMessages");

            migrationBuilder.DropTable(
                name: "TwitchViewers");

            migrationBuilder.DropColumn(
                name: "CreationDateTime",
                table: "TwitchStreamers");

            migrationBuilder.DropColumn(
                name: "LastModificationDateTime",
                table: "TwitchStreamers");

            migrationBuilder.DropColumn(
                name: "TwitchId",
                table: "TwitchStreamers");

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "TwitchStreamers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TwitchStreamers_Name",
                table: "TwitchStreamers",
                column: "Name",
                unique: true,
                filter: "Deleted = 0");
        }
    }
}
