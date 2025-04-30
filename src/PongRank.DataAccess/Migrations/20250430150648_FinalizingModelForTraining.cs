using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PongRank.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FinalizingModelForTraining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextRankingValue",
                table: "PlayerResults");

            migrationBuilder.RenameColumn(
                name: "RankingValue",
                table: "PlayerResults",
                newName: "TotalGames");

            migrationBuilder.AlterColumn<string>(
                name: "NextRanking",
                table: "PlayerResults",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(5)",
                oldMaxLength: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalGames",
                table: "PlayerResults",
                newName: "RankingValue");

            migrationBuilder.AlterColumn<string>(
                name: "NextRanking",
                table: "PlayerResults",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(5)",
                oldMaxLength: 5,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NextRankingValue",
                table: "PlayerResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
