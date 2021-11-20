using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SongAcronymBot.Repository.Migrations
{
    public partial class ManyToMany : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "AcronymSubreddit",
                columns: table => new
                {
                    AcronymsId = table.Column<int>(type: "int", nullable: false),
                    SubredditsId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcronymSubreddit", x => new { x.AcronymsId, x.SubredditsId });
                    table.ForeignKey(
                        name: "FK_AcronymSubreddit_Acronyms_AcronymsId",
                        column: x => x.AcronymsId,
                        principalTable: "Acronyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AcronymSubreddit_Subreddits_SubredditsId",
                        column: x => x.SubredditsId,
                        principalTable: "Subreddits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcronymSubreddit_SubredditsId",
                table: "AcronymSubreddit",
                column: "SubredditsId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcronymSubreddit");

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
    }
}
