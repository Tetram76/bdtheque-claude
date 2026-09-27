using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bdtheque.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Series",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SortKey = table.Column<string>(type: "character varying(510)", maxLength: 510, nullable: false),
                    IsManualSortKey = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TheoreticalVolumeCount = table.Column<int>(type: "integer", nullable: true),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    ExcludeFromMissingVolumes = table.Column<bool>(type: "boolean", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    PersonalNotes = table.Column<string>(type: "text", nullable: true),
                    TemplateBinding = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TemplateOrientation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TemplateReadingDirection = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TemplateFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TemplateEditionCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TemplateCondition = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TemplateIsColor = table.Column<bool>(type: "boolean", nullable: true),
                    TemplatePublisherId = table.Column<Guid>(type: "uuid", nullable: true),
                    TemplatePublisherCollectionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Series", x => x.Id);
                    table.CheckConstraint("CK_Series_SortKeyNotBlank", "COALESCE(LENGTH(TRIM(\"SortKey\")), 0) > 0");
                    table.CheckConstraint("CK_Series_TemplateCollectionRequiresPublisher", "\"TemplatePublisherCollectionId\" IS NULL OR \"TemplatePublisherId\" IS NOT NULL");
                    table.CheckConstraint("CK_Series_TheoreticalVolumeCountPositive", "\"TheoreticalVolumeCount\" IS NULL OR \"TheoreticalVolumeCount\" > 0");
                    table.CheckConstraint("CK_Series_TitleNotBlank", "COALESCE(LENGTH(TRIM(\"Title\")), 0) > 0");
                    table.ForeignKey(
                        name: "FK_Series_PublisherCollections_TemplatePublisherCollectionId",
                        column: x => x.TemplatePublisherCollectionId,
                        principalTable: "PublisherCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Series_Publishers_TemplatePublisherId",
                        column: x => x.TemplatePublisherId,
                        principalTable: "Publishers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeriesGenres",
                columns: table => new
                {
                    GenresId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesGenres", x => new { x.GenresId, x.SeriesId });
                    table.ForeignKey(
                        name: "FK_SeriesGenres_Genres_GenresId",
                        column: x => x.GenresId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeriesGenres_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeriesUniverses",
                columns: table => new
                {
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                    UniversesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesUniverses", x => new { x.SeriesId, x.UniversesId });
                    table.ForeignKey(
                        name: "FK_SeriesUniverses_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeriesUniverses_Universes_UniversesId",
                        column: x => x.UniversesId,
                        principalTable: "Universes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Series_SortKey",
                table: "Series",
                column: "SortKey");

            migrationBuilder.CreateIndex(
                name: "IX_Series_TemplatePublisherCollectionId",
                table: "Series",
                column: "TemplatePublisherCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Series_TemplatePublisherId",
                table: "Series",
                column: "TemplatePublisherId");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesGenres_SeriesId",
                table: "SeriesGenres",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesUniverses_UniversesId",
                table: "SeriesUniverses",
                column: "UniversesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeriesGenres");

            migrationBuilder.DropTable(
                name: "SeriesUniverses");

            migrationBuilder.DropTable(
                name: "Series");
        }
    }
}
