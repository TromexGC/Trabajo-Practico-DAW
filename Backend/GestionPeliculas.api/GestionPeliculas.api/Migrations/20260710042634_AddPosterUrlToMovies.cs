using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionPeliculas.api.Migrations
{
    /// <inheritdoc />
    public partial class AddPosterUrlToMovies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PosterUrl",
                table: "Movies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PosterUrl",
                table: "Movies");
        }
    }
}
