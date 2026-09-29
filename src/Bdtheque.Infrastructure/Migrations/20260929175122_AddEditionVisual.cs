using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bdtheque.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEditionVisual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EditionVisuals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EditionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    MediaReference = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_EditionVisuals_EditionId_Type_DisplayOrder",
                table: "EditionVisuals",
                columns: new[] { "EditionId", "Type", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EditionVisuals");
        }
    }
}
