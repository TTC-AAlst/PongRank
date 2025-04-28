using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PongRank.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MatchEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    MatchUniqueId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Home_PlayerUniqueIndex = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Home_FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Home_LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Home_SetCount = table.Column<int>(type: "integer", nullable: false),
                    Away_PlayerUniqueIndex = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Away_FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Away_LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Away_SetCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Matches");
        }
    }
}
