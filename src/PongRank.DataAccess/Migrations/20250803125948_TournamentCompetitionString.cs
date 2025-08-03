using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PongRank.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class TournamentCompetitionString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Competition",
                table: "Tournaments",
                type: "character varying(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.Sql("UPDATE public.\"Tournaments\" SET \"Competition\"='Vttl' WHERE \"Competition\"='0'");
            migrationBuilder.Sql("UPDATE public.\"Tournaments\" SET \"Competition\"='Sporta' WHERE \"Competition\"='1'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Competition",
                table: "Tournaments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)");
        }
    }
}
