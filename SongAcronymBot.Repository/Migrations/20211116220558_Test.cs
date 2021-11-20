using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SongAcronymBot.Repository.Migrations
{
    public partial class Test : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Acronyms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcronymName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcronymType = table.Column<int>(type: "int", nullable: false),
                    ArtistName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlbumName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrackName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YearReleased = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Acronyms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Redditors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Redditors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subreddits",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    AcronymId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subreddits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subreddits_Acronyms_AcronymId",
                        column: x => x.AcronymId,
                        principalTable: "Acronyms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subreddits_AcronymId",
                table: "Subreddits",
                column: "AcronymId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Redditors");

            migrationBuilder.DropTable(
                name: "Subreddits");

            migrationBuilder.DropTable(
                name: "Acronyms");
        }
    }
}
