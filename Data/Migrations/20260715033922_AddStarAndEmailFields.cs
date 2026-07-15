using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RssReader.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStarAndEmailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailNotifications",
                table: "Feeds",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DigestFrequencyHours",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDigestSent",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailNotifications",
                table: "Feeds");

            migrationBuilder.DropColumn(
                name: "DigestFrequencyHours",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastDigestSent",
                table: "AspNetUsers");
        }
    }
}
