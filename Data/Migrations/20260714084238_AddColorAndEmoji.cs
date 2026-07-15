using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RssReader.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddColorAndEmoji : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Emoji",
                table: "Playlists",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Feeds",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Emoji",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Feeds");
        }
    }
}
