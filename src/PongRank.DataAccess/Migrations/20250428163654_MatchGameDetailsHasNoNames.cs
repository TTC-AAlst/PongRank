using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PongRank.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MatchGameDetailsHasNoNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Away_FirstName",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Away_LastName",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Home_FirstName",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Home_LastName",
                table: "Matches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Away_FirstName",
                table: "Matches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Away_LastName",
                table: "Matches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Home_FirstName",
                table: "Matches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Home_LastName",
                table: "Matches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
