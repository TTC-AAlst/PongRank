using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PongRank.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clubs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Competition = table.Column<string>(type: "character varying(10)", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    UniqueIndex = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    CategoryName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clubs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Competition = table.Column<string>(type: "character varying(10)", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WeekName = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    MatchId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MatchUniqueId = table.Column<int>(type: "integer", nullable: false),
                    Home_PlayerUniqueIndex = table.Column<int>(type: "integer", nullable: false),
                    Home_FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Home_LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Home_SetCount = table.Column<int>(type: "integer", nullable: false),
                    Away_PlayerUniqueIndex = table.Column<int>(type: "integer", nullable: false),
                    Away_FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Away_LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Away_SetCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Competition = table.Column<string>(type: "character varying(10)", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    UniqueIndex = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Ranking = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    NextRanking = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    Club = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_Competition_Year",
                table: "Clubs",
                columns: new[] { "Competition", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_Players_Competition_Year",
                table: "Players",
                columns: new[] { "Competition", "Year" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clubs");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}
