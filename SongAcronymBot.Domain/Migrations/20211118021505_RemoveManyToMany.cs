using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SongAcronymBot.Repository.Migrations
{
    public partial class RemoveManyToMany : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcronymSubreddit");

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

        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
