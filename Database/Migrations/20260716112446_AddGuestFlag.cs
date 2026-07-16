using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RssReader.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GuestCreatedAt",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGuest",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestCreatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsGuest",
                table: "AspNetUsers");
        }
    }
}
