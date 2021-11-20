using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SongAcronymBot.Repository.Migrations
{
    public partial class RemapSubsToAcronyms : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subreddits_Acronyms_AcronymId",
                table: "Subreddits");

            migrationBuilder.DropIndex(
                name: "IX_Subreddits_AcronymId",
                table: "Subreddits");

            migrationBuilder.DropColumn(
                name: "AcronymId",
                table: "Subreddits");

            migrationBuilder.AddColumn<string>(
                name: "SubredditId",
                table: "Acronyms",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Acronyms_SubredditId",
                table: "Acronyms",
                column: "SubredditId");

            migrationBuilder.AddForeignKey(
                name: "FK_Acronyms_Subreddits_SubredditId",
                table: "Acronyms",
                column: "SubredditId",
                principalTable: "Subreddits",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Acronyms_Subreddits_SubredditId",
                table: "Acronyms");

            migrationBuilder.DropIndex(
                name: "IX_Acronyms_SubredditId",
                table: "Acronyms");

            migrationBuilder.DropColumn(
                name: "SubredditId",
                table: "Acronyms");

            migrationBuilder.AddColumn<int>(
                name: "AcronymId",
                table: "Subreddits",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subreddits_AcronymId",
                table: "Subreddits",
                column: "AcronymId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subreddits_Acronyms_AcronymId",
                table: "Subreddits",
                column: "AcronymId",
                principalTable: "Acronyms",
                principalColumn: "Id");
        }
    }
}
