using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bdtheque.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlbumId = table.Column<Guid>(type: "uuid", nullable: true),
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contributions", x => x.Id);
                    table.CheckConstraint("CK_Contributions_ExactlyOneOfAlbumOrSeries", "(\"AlbumId\" IS NOT NULL) <> (\"SeriesId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Contributions_Albums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contributions_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contributions_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_AlbumId_Role_AuthorId",
                table: "Contributions",
                columns: new[] { "AlbumId", "Role", "AuthorId" },
                unique: true,
                filter: "\"AlbumId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_AuthorId",
                table: "Contributions",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_SeriesId_Role_AuthorId",
                table: "Contributions",
                columns: new[] { "SeriesId", "Role", "AuthorId" },
                unique: true,
                filter: "\"SeriesId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contributions");
        }
    }
}
