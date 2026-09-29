using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bdtheque.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseIntent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseIntents_Editions_EditionId",
                        column: x => x.EditionId,
                        principalTable: "Editions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseIntents");
        }
    }
}
