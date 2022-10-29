using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Db.Migrations
{
    public partial class ReworkOwner : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TwitchId",
                table: "TwitchViewers");

            migrationBuilder.DropColumn(
                name: "TwitchId",
                table: "TwitchStreamers");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "TwitchMessages");

            migrationBuilder.AddColumn<string>(
                name: "TwitchOwner",
                table: "TwitchViewers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TwitchOwner",
                table: "TwitchStreamers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TwitchOwner",
                table: "TwitchMessages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TwitchOwner",
                table: "TwitchViewers");

            migrationBuilder.DropColumn(
                name: "TwitchOwner",
                table: "TwitchStreamers");

            migrationBuilder.DropColumn(
                name: "TwitchOwner",
                table: "TwitchMessages");

            migrationBuilder.AddColumn<string>(
                name: "TwitchId",
                table: "TwitchViewers",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TwitchId",
                table: "TwitchStreamers",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "Owner",
                table: "TwitchMessages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
