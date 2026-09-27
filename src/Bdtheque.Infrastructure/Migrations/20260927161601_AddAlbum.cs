using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bdtheque.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAlbum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Albums",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortKey = table.Column<string>(type: "character varying(510)", maxLength: 510, nullable: true),
                    IsManualSortKey = table.Column<bool>(type: "boolean", nullable: false),
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsSpecialIssue = table.Column<bool>(type: "boolean", nullable: false),
                    VolumeNumber = table.Column<int>(type: "integer", nullable: true),
                    StartVolumeNumber = table.Column<int>(type: "integer", nullable: true),
                    EndVolumeNumber = table.Column<int>(type: "integer", nullable: true),
                    FirstPublicationYear = table.Column<int>(type: "integer", nullable: true),
                    FirstPublicationMonth = table.Column<int>(type: "integer", nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    PersonalNotes = table.Column<string>(type: "text", nullable: true),
                    Rating = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Albums", x => x.Id);
                    table.CheckConstraint("CK_Albums_PublicationMonthRange", "\"FirstPublicationMonth\" IS NULL OR \"FirstPublicationMonth\" BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_Albums_PublicationMonthRequiresYear", "\"FirstPublicationMonth\" IS NULL OR \"FirstPublicationYear\" IS NOT NULL");
                    table.CheckConstraint("CK_Albums_PublicationYearPositive", "\"FirstPublicationYear\" IS NULL OR \"FirstPublicationYear\" > 0");
                    table.CheckConstraint("CK_Albums_SortKeyNotBlank", "\"SortKey\" IS NULL OR LENGTH(TRIM(\"SortKey\")) > 0");
                    table.CheckConstraint("CK_Albums_SortKeyPresenceMatchesTitle", "(\"Title\" IS NULL) = (\"SortKey\" IS NULL)");
                    table.CheckConstraint("CK_Albums_TitleNotBlank", "\"Title\" IS NULL OR LENGTH(TRIM(\"Title\")) > 0");
                    table.CheckConstraint("CK_Albums_TitleRequiredWithoutSeries", "\"SeriesId\" IS NOT NULL OR LENGTH(TRIM(COALESCE(\"Title\", ''))) > 0");
                    table.CheckConstraint("CK_Albums_VolumeNumberPositive", "\"VolumeNumber\" IS NULL OR \"VolumeNumber\" > 0");
                    table.CheckConstraint("CK_Albums_VolumeRangeBothOrNeither", "(\"StartVolumeNumber\" IS NULL) = (\"EndVolumeNumber\" IS NULL)");
                    table.CheckConstraint("CK_Albums_VolumeRangeOmnibusOnly", "\"StartVolumeNumber\" IS NULL OR \"Type\" = 'Omnibus'");
                    table.CheckConstraint("CK_Albums_VolumeRangeOrder", "\"StartVolumeNumber\" IS NULL OR \"StartVolumeNumber\" <= \"EndVolumeNumber\"");
                    table.CheckConstraint("CK_Albums_VolumeRangeStartPositive", "\"StartVolumeNumber\" IS NULL OR \"StartVolumeNumber\" > 0");
                    table.ForeignKey(
                        name: "FK_Albums_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AlbumGenres",
                columns: table => new
                {
                    AlbumId = table.Column<Guid>(type: "uuid", nullable: false),
                    GenresId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbumGenres", x => new { x.AlbumId, x.GenresId });
                    table.ForeignKey(
                        name: "FK_AlbumGenres_Albums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlbumGenres_Genres_GenresId",
                        column: x => x.GenresId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlbumUniverses",
                columns: table => new
                {
                    AlbumId = table.Column<Guid>(type: "uuid", nullable: false),
                    UniversesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbumUniverses", x => new { x.AlbumId, x.UniversesId });
                    table.ForeignKey(
                        name: "FK_AlbumUniverses_Albums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlbumUniverses_Universes_UniversesId",
                        column: x => x.UniversesId,
                        principalTable: "Universes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlbumGenres_GenresId",
                table: "AlbumGenres",
                column: "GenresId");

            migrationBuilder.CreateIndex(
                name: "IX_Albums_SeriesId",
                table: "Albums",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_Albums_SortKey",
                table: "Albums",
                column: "SortKey");

            migrationBuilder.CreateIndex(
                name: "IX_AlbumUniverses_UniversesId",
                table: "AlbumUniverses",
                column: "UniversesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlbumGenres");

            migrationBuilder.DropTable(
                name: "AlbumUniverses");

            migrationBuilder.DropTable(
                name: "Albums");
        }
    }
}
