using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bdtheque.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Authors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LastName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, collation: "fr-FR-x-icu"),
                    FirstName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, collation: "fr-FR-x-icu"),
                    Pseudonym = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, collation: "fr-FR-x-icu"),
                    Biography = table.Column<string>(type: "text", nullable: true, collation: "fr-FR-x-icu"),
                    Nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, collation: "fr-FR-x-icu")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authors", x => x.Id);
                    table.CheckConstraint("CK_Authors_LastNameOrPseudonym", "COALESCE(LENGTH(TRIM(\"LastName\")), 0) > 0 OR COALESCE(LENGTH(TRIM(\"Pseudonym\")), 0) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, collation: "fr-FR-x-icu")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.Id);
                    table.CheckConstraint("CK_Genres_LabelNotBlank", "COALESCE(LENGTH(TRIM(\"Label\")), 0) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Publishers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, collation: "fr-FR-x-icu"),
                    Website = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true, collation: "fr-FR-x-icu")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Publishers", x => x.Id);
                    table.CheckConstraint("CK_Publishers_NameNotBlank", "COALESCE(LENGTH(TRIM(\"Name\")), 0) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Universes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, collation: "fr-FR-x-icu"),
                    Description = table.Column<string>(type: "text", nullable: true, collation: "fr-FR-x-icu"),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Universes", x => x.Id);
                    table.CheckConstraint("CK_Universes_NameNotBlank", "COALESCE(LENGTH(TRIM(\"Name\")), 0) > 0");
                    table.CheckConstraint("CK_Universes_NoSelfParent", "\"ParentId\" IS NULL OR \"ParentId\" <> \"Id\"");
                    table.ForeignKey(
                        name: "FK_Universes_Universes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Universes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PublisherCollections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, collation: "fr-FR-x-icu"),
                    PublisherId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublisherCollections", x => x.Id);
                    table.CheckConstraint("CK_PublisherCollections_NameNotBlank", "COALESCE(LENGTH(TRIM(\"Name\")), 0) > 0");
                    table.ForeignKey(
                        name: "FK_PublisherCollections_Publishers_PublisherId",
                        column: x => x.PublisherId,
                        principalTable: "Publishers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Series",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, collation: "fr-FR-x-icu"),
                    SortKey = table.Column<string>(type: "character varying(510)", maxLength: 510, nullable: false, collation: "fr-FR-x-icu"),
                    IsManualSortKey = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    TheoreticalVolumeCount = table.Column<int>(type: "integer", nullable: true),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    ExcludeFromMissingVolumes = table.Column<bool>(type: "boolean", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: true, collation: "fr-FR-x-icu"),
                    PersonalNotes = table.Column<string>(type: "text", nullable: true, collation: "fr-FR-x-icu"),
                    TemplateBinding = table.Column<int>(type: "integer", nullable: true),
                    TemplateOrientation = table.Column<int>(type: "integer", nullable: true),
                    TemplateReadingDirection = table.Column<int>(type: "integer", nullable: true),
                    TemplateFormat = table.Column<int>(type: "integer", nullable: true),
                    TemplateEditionCategory = table.Column<int>(type: "integer", nullable: true),
                    TemplateCondition = table.Column<int>(type: "integer", nullable: true),
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
                name: "Albums",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, collation: "fr-FR-x-icu"),
                    SortKey = table.Column<string>(type: "character varying(510)", maxLength: 510, nullable: true, collation: "fr-FR-x-icu"),
                    IsManualSortKey = table.Column<bool>(type: "boolean", nullable: false),
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsSpecialIssue = table.Column<bool>(type: "boolean", nullable: false),
                    VolumeNumber = table.Column<int>(type: "integer", nullable: true),
                    StartVolumeNumber = table.Column<int>(type: "integer", nullable: true),
                    EndVolumeNumber = table.Column<int>(type: "integer", nullable: true),
                    FirstPublicationYear = table.Column<int>(type: "integer", nullable: true),
                    FirstPublicationMonth = table.Column<int>(type: "integer", nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true, collation: "fr-FR-x-icu"),
                    PersonalNotes = table.Column<string>(type: "text", nullable: true, collation: "fr-FR-x-icu"),
                    Rating = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Albums", x => x.Id);
                    table.CheckConstraint("CK_Albums_ManualSortKeyRequiresTitle", "\"IsManualSortKey\" = false OR \"Title\" IS NOT NULL");
                    table.CheckConstraint("CK_Albums_PublicationMonthRange", "\"FirstPublicationMonth\" IS NULL OR \"FirstPublicationMonth\" BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_Albums_PublicationMonthRequiresYear", "\"FirstPublicationMonth\" IS NULL OR \"FirstPublicationYear\" IS NOT NULL");
                    table.CheckConstraint("CK_Albums_PublicationYearPositive", "\"FirstPublicationYear\" IS NULL OR \"FirstPublicationYear\" > 0");
                    table.CheckConstraint("CK_Albums_SortKeyNotBlank", "\"SortKey\" IS NULL OR LENGTH(TRIM(\"SortKey\")) > 0");
                    table.CheckConstraint("CK_Albums_SortKeyPresenceMatchesTitle", "(\"Title\" IS NULL) = (\"SortKey\" IS NULL)");
                    table.CheckConstraint("CK_Albums_TitleNotBlank", "\"Title\" IS NULL OR LENGTH(TRIM(\"Title\")) > 0");
                    table.CheckConstraint("CK_Albums_TitleRequiredWithoutSeries", "\"SeriesId\" IS NOT NULL OR LENGTH(TRIM(COALESCE(\"Title\", ''))) > 0");
                    table.CheckConstraint("CK_Albums_VolumeNumberPositive", "\"VolumeNumber\" IS NULL OR \"VolumeNumber\" > 0");
                    table.CheckConstraint("CK_Albums_VolumeRangeBothOrNeither", "(\"StartVolumeNumber\" IS NULL) = (\"EndVolumeNumber\" IS NULL)");
                    table.CheckConstraint("CK_Albums_VolumeRangeOmnibusOnly", "\"StartVolumeNumber\" IS NULL OR \"Type\" = 2");
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

            migrationBuilder.CreateTable(
                name: "Contributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlbumId = table.Column<Guid>(type: "uuid", nullable: true),
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Editions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlbumId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublisherId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublisherCollectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublicationYear = table.Column<int>(type: "integer", nullable: true),
                    Isbn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, collation: "fr-FR-x-icu"),
                    Binding = table.Column<int>(type: "integer", nullable: true),
                    Orientation = table.Column<int>(type: "integer", nullable: true),
                    ReadingDirection = table.Column<int>(type: "integer", nullable: true),
                    Format = table.Column<int>(type: "integer", nullable: true),
                    PageCount = table.Column<int>(type: "integer", nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: true),
                    IsDedicated = table.Column<bool>(type: "boolean", nullable: false),
                    IsColor = table.Column<bool>(type: "boolean", nullable: false),
                    Condition = table.Column<int>(type: "integer", nullable: true),
                    AcquisitionMode = table.Column<int>(type: "integer", nullable: true),
                    IsSecondHand = table.Column<bool>(type: "boolean", nullable: false),
                    AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AcquisitionAmount = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: true),
                    AcquisitionCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true, collation: "fr-FR-x-icu"),
                    IsFree = table.Column<bool>(type: "boolean", nullable: false),
                    PersonalReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, collation: "fr-FR-x-icu"),
                    PersonalNotes = table.Column<string>(type: "text", nullable: true, collation: "fr-FR-x-icu")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Editions", x => x.Id);
                    table.CheckConstraint("CK_Editions_AcquisitionAmountCurrencyTogether", "(\"AcquisitionAmount\" IS NULL) = (\"AcquisitionCurrency\" IS NULL)");
                    table.CheckConstraint("CK_Editions_AcquisitionAmountPositive", "\"AcquisitionAmount\" IS NULL OR \"AcquisitionAmount\" > 0");
                    table.CheckConstraint("CK_Editions_AcquisitionModeRequiredForDateOrPrice", "\"AcquisitionMode\" IS NOT NULL OR (\"AcquisitionDate\" IS NULL AND \"AcquisitionAmount\" IS NULL)");
                    table.CheckConstraint("CK_Editions_FreeRequiresNoAmount", "\"IsFree\" = false OR \"AcquisitionAmount\" IS NULL");
                    table.CheckConstraint("CK_Editions_PageCountPositive", "\"PageCount\" IS NULL OR \"PageCount\" > 0");
                    table.CheckConstraint("CK_Editions_PublicationYearPositive", "\"PublicationYear\" IS NULL OR \"PublicationYear\" > 0");
                    table.ForeignKey(
                        name: "FK_Editions_Albums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Editions_PublisherCollections_PublisherCollectionId",
                        column: x => x.PublisherCollectionId,
                        principalTable: "PublisherCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Editions_Publishers_PublisherId",
                        column: x => x.PublisherId,
                        principalTable: "Publishers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EditionVisuals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EditionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    MediaReference = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false, collation: "fr-FR-x-icu"),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditionVisuals", x => x.Id);
                    table.CheckConstraint("CK_EditionVisuals_DisplayOrderNotNegative", "\"DisplayOrder\" >= 0");
                    table.CheckConstraint("CK_EditionVisuals_MediaReferenceNotBlank", "COALESCE(LENGTH(TRIM(\"MediaReference\")), 0) > 0");
                    table.ForeignKey(
                        name: "FK_EditionVisuals_Editions_EditionId",
                        column: x => x.EditionId,
                        principalTable: "Editions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseIntents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlbumId = table.Column<Guid>(type: "uuid", nullable: false),
                    EditionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseIntents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseIntents_Albums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseIntents_Editions_EditionId",
                        column: x => x.EditionId,
                        principalTable: "Editions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateIndex(
                name: "IX_Editions_AlbumId",
                table: "Editions",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_Editions_PublisherCollectionId",
                table: "Editions",
                column: "PublisherCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Editions_PublisherId",
                table: "Editions",
                column: "PublisherId");

            migrationBuilder.CreateIndex(
                name: "IX_EditionVisuals_EditionId_Type_DisplayOrder",
                table: "EditionVisuals",
                columns: new[] { "EditionId", "Type", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Genres_Label",
                table: "Genres",
                column: "Label",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublisherCollections_PublisherId_Name",
                table: "PublisherCollections",
                columns: new[] { "PublisherId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Publishers_Name",
                table: "Publishers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseIntents_AlbumId",
                table: "PurchaseIntents",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseIntents_AlbumId_WholeAlbum",
                table: "PurchaseIntents",
                column: "AlbumId",
                unique: true,
                filter: "\"EditionId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseIntents_EditionId",
                table: "PurchaseIntents",
                column: "EditionId",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Universes_ParentId",
                table: "Universes",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlbumGenres");

            migrationBuilder.DropTable(
                name: "AlbumUniverses");

            migrationBuilder.DropTable(
                name: "Contributions");

            migrationBuilder.DropTable(
                name: "EditionVisuals");

            migrationBuilder.DropTable(
                name: "PurchaseIntents");

            migrationBuilder.DropTable(
                name: "SeriesGenres");

            migrationBuilder.DropTable(
                name: "SeriesUniverses");

            migrationBuilder.DropTable(
                name: "Authors");

            migrationBuilder.DropTable(
                name: "Editions");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "Universes");

            migrationBuilder.DropTable(
                name: "Albums");

            migrationBuilder.DropTable(
                name: "Series");

            migrationBuilder.DropTable(
                name: "PublisherCollections");

            migrationBuilder.DropTable(
                name: "Publishers");
        }
    }
}
