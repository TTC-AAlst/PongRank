using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PongRank.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextRanking",
                table: "Players");

            migrationBuilder.CreateTable(
                name: "PlayerResults",
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
                    RankingValue = table.Column<int>(type: "integer", nullable: false),
                    NextRanking = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    NextRankingValue = table.Column<int>(type: "integer", nullable: true),
                    AWins = table.Column<int>(type: "integer", nullable: false),
                    ALosses = table.Column<int>(type: "integer", nullable: false),
                    B0Wins = table.Column<int>(type: "integer", nullable: false),
                    B0Losses = table.Column<int>(type: "integer", nullable: false),
                    B2Wins = table.Column<int>(type: "integer", nullable: false),
                    B2Losses = table.Column<int>(type: "integer", nullable: false),
                    B4Wins = table.Column<int>(type: "integer", nullable: false),
                    B4Losses = table.Column<int>(type: "integer", nullable: false),
                    B6Wins = table.Column<int>(type: "integer", nullable: false),
                    B6Losses = table.Column<int>(type: "integer", nullable: false),
                    C0Wins = table.Column<int>(type: "integer", nullable: false),
                    C0Losses = table.Column<int>(type: "integer", nullable: false),
                    C2Wins = table.Column<int>(type: "integer", nullable: false),
                    C2Losses = table.Column<int>(type: "integer", nullable: false),
                    C4Wins = table.Column<int>(type: "integer", nullable: false),
                    C4Losses = table.Column<int>(type: "integer", nullable: false),
                    C6Wins = table.Column<int>(type: "integer", nullable: false),
                    C6Losses = table.Column<int>(type: "integer", nullable: false),
                    D0Wins = table.Column<int>(type: "integer", nullable: false),
                    D0Losses = table.Column<int>(type: "integer", nullable: false),
                    D2Wins = table.Column<int>(type: "integer", nullable: false),
                    D2Losses = table.Column<int>(type: "integer", nullable: false),
                    D4Wins = table.Column<int>(type: "integer", nullable: false),
                    D4Losses = table.Column<int>(type: "integer", nullable: false),
                    D6Wins = table.Column<int>(type: "integer", nullable: false),
                    D6Losses = table.Column<int>(type: "integer", nullable: false),
                    E0Wins = table.Column<int>(type: "integer", nullable: false),
                    E0Losses = table.Column<int>(type: "integer", nullable: false),
                    E2Wins = table.Column<int>(type: "integer", nullable: false),
                    E2Losses = table.Column<int>(type: "integer", nullable: false),
                    E4Wins = table.Column<int>(type: "integer", nullable: false),
                    E4Losses = table.Column<int>(type: "integer", nullable: false),
                    E6Wins = table.Column<int>(type: "integer", nullable: false),
                    E6Losses = table.Column<int>(type: "integer", nullable: false),
                    FWins = table.Column<int>(type: "integer", nullable: false),
                    FLosses = table.Column<int>(type: "integer", nullable: false),
                    NGWins = table.Column<int>(type: "integer", nullable: false),
                    NGLosses = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerResults", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerResults");

            migrationBuilder.AddColumn<string>(
                name: "NextRanking",
                table: "Players",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);
        }
    }
}
