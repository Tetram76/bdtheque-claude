using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bdtheque.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEdition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Editions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlbumId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublisherId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublisherCollectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublicationYear = table.Column<int>(type: "integer", nullable: true),
                    Isbn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Binding = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Orientation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReadingDirection = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PageCount = table.Column<int>(type: "integer", nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsDedicated = table.Column<bool>(type: "boolean", nullable: false),
                    IsColor = table.Column<bool>(type: "boolean", nullable: false),
                    Condition = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AcquisitionMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsSecondHand = table.Column<bool>(type: "boolean", nullable: false),
                    AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AcquisitionAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    AcquisitionCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    IsFree = table.Column<bool>(type: "boolean", nullable: false),
                    PersonalReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PersonalNotes = table.Column<string>(type: "text", nullable: true)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Editions");
        }
    }
}
