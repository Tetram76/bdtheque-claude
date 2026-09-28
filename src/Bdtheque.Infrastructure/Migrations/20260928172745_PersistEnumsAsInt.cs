using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bdtheque.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PersistEnumsAsInt : Migration
    {
        // PostgreSQL has no implicit/assignment cast from character varying to integer (verified
        // against a real PostgreSQL 17 instance: EF's scaffolded ALTER COLUMN ... TYPE integer,
        // with no USING clause, fails with "column ... cannot be cast automatically to type
        // integer" as soon as a row exists). Each column below is converted via an explicit
        // USING clause mapping the old member-name string to the new explicit int value.

        private const string SeriesStatusCase =
            "CASE \"Status\" " +
            "WHEN 'InProgress' THEN 1 WHEN 'Completed' THEN 2 WHEN 'Abandoned' THEN 3 END";

        private const string BindingTypeCase =
            "WHEN 'Paperback' THEN 1 WHEN 'Hardcover' THEN 2 END";

        private const string BookOrientationCase =
            "WHEN 'Portrait' THEN 1 WHEN 'Landscape' THEN 2 END";

        private const string ReadingDirectionCase =
            "WHEN 'LeftToRight' THEN 1 WHEN 'RightToLeft' THEN 2 END";

        private const string EditionFormatCase =
            "WHEN 'Pocket' THEN 1 WHEN 'Medium' THEN 2 WHEN 'Standard' THEN 3 " +
            "WHEN 'Large' THEN 4 WHEN 'Special' THEN 5 END";

        private const string EditionCategoryCase =
            "WHEN 'FirstEdition' THEN 1 WHEN 'SpecialEdition' THEN 2 WHEN 'LimitedEdition' THEN 3 END";

        private const string EditionConditionCase =
            "WHEN 'Excellent' THEN 1 WHEN 'VeryGood' THEN 2 WHEN 'Good' THEN 3 " +
            "WHEN 'Poor' THEN 4 WHEN 'VeryPoor' THEN 5 END";

        private const string ContributionRoleCase =
            "CASE \"Role\" " +
            "WHEN 'Scenarist' THEN 1 WHEN 'Illustrator' THEN 2 WHEN 'Colorist' THEN 3 END";

        private const string AlbumTypeCase =
            "CASE \"Type\" " +
            "WHEN 'Regular' THEN 1 WHEN 'Omnibus' THEN 2 END";

        private const string AlbumRatingCase =
            "CASE \"Rating\" " +
            "WHEN 'VeryPoor' THEN 1 WHEN 'Poor' THEN 2 WHEN 'Average' THEN 3 " +
            "WHEN 'Good' THEN 4 WHEN 'VeryGood' THEN 5 END";

        private const string AcquisitionModeCase =
            "CASE \"AcquisitionMode\" " +
            "WHEN 'Purchase' THEN 1 WHEN 'Gift' THEN 2 WHEN 'Trade' THEN 3 " +
            "WHEN 'Won' THEN 4 WHEN 'Inherited' THEN 5 END";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Albums_VolumeRangeOmnibusOnly",
                table: "Albums");

            // Series (template fields + own status).
            migrationBuilder.Sql(
                $"ALTER TABLE \"Series\" ALTER COLUMN \"TemplateReadingDirection\" TYPE integer " +
                $"USING (CASE \"TemplateReadingDirection\" {ReadingDirectionCase});");
            migrationBuilder.Sql(
                $"ALTER TABLE \"Series\" ALTER COLUMN \"TemplateOrientation\" TYPE integer " +
                $"USING (CASE \"TemplateOrientation\" {BookOrientationCase});");
            migrationBuilder.Sql(
                $"ALTER TABLE \"Series\" ALTER COLUMN \"TemplateFormat\" TYPE integer " +
                $"USING (CASE \"TemplateFormat\" {EditionFormatCase});");
            migrationBuilder.Sql(
                $"ALTER TABLE \"Series\" ALTER COLUMN \"TemplateEditionCategory\" TYPE integer " +
                $"USING (CASE \"TemplateEditionCategory\" {EditionCategoryCase});");
            migrationBuilder.Sql(
                $"ALTER TABLE \"Series\" ALTER COLUMN \"TemplateCondition\" TYPE integer " +
                $"USING (CASE \"TemplateCondition\" {EditionConditionCase});");
            migrationBuilder.Sql(
                $"ALTER TABLE \"Series\" ALTER COLUMN \"TemplateBinding\" TYPE integer " +
                $"USING (CASE \"TemplateBinding\" {BindingTypeCase});");
            migrationBuilder.Sql(
                $"ALTER TABLE \"Series\" ALTER COLUMN \"Status\" TYPE integer USING ({SeriesStatusCase});");

            // Editions (physical characteristics + acquisition mode).
            migrationBuilder.Sql(
                $"ALTER TABLE \"Editions\" ALTER COLUMN \"ReadingDirection\" TYPE integer " +
                $"USING (CASE \"ReadingDirection\" {ReadingDirectionCase});");
            migrationBuilder.Sql(
                $"ALTER TABLE \"Editions\" ALTER COLUMN \"Orientation\" TYPE integer " +
                $"USING (CASE \"Orientation\" {BookOrientationCase});");
            migrationBuilder.Sql(
                $"ALTER TABLE \"Editions\" ALTER COLUMN \"Format\" TYPE integer " +
                $"USING (CASE \"Format\" {EditionFormatCase});");
            migrationBuilder.Sql(
                $"ALTER TABLE \"Editions\" ALTER COLUMN \"Condition\" TYPE integer " +
                $"USING (CASE \"Condition\" {EditionConditionCase});");
            migrationBuilder.Sql(
                $"ALTER TABLE \"Editions\" ALTER COLUMN \"Category\" TYPE integer " +
                $"USING (CASE \"Category\" {EditionCategoryCase});");
            migrationBuilder.Sql(
                $"ALTER TABLE \"Editions\" ALTER COLUMN \"Binding\" TYPE integer " +
                $"USING (CASE \"Binding\" {BindingTypeCase});");
            migrationBuilder.Sql(
                $"ALTER TABLE \"Editions\" ALTER COLUMN \"AcquisitionMode\" TYPE integer USING ({AcquisitionModeCase});");

            // Contributions, Albums.
            migrationBuilder.Sql(
                $"ALTER TABLE \"Contributions\" ALTER COLUMN \"Role\" TYPE integer USING ({ContributionRoleCase});");
            migrationBuilder.Sql(
                $"ALTER TABLE \"Albums\" ALTER COLUMN \"Type\" TYPE integer USING ({AlbumTypeCase});");
            migrationBuilder.Sql(
                $"ALTER TABLE \"Albums\" ALTER COLUMN \"Rating\" TYPE integer USING ({AlbumRatingCase});");

            migrationBuilder.AlterColumn<int>(
                name: "TemplateReadingDirection",
                table: "Series",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TemplateOrientation",
                table: "Series",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TemplateFormat",
                table: "Series",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TemplateEditionCategory",
                table: "Series",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TemplateCondition",
                table: "Series",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TemplateBinding",
                table: "Series",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Series",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ReadingDirection",
                table: "Editions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Orientation",
                table: "Editions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Format",
                table: "Editions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Condition",
                table: "Editions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Category",
                table: "Editions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Binding",
                table: "Editions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AcquisitionMode",
                table: "Editions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "Contributions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Albums",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "Rating",
                table: "Albums",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Albums_VolumeRangeOmnibusOnly",
                table: "Albums",
                sql: "\"StartVolumeNumber\" IS NULL OR \"Type\" = 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Albums_VolumeRangeOmnibusOnly",
                table: "Albums");

            migrationBuilder.Sql(
                "ALTER TABLE \"Series\" ALTER COLUMN \"TemplateReadingDirection\" TYPE character varying(50) " +
                "USING (CASE \"TemplateReadingDirection\" WHEN 1 THEN 'LeftToRight' WHEN 2 THEN 'RightToLeft' END);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Series\" ALTER COLUMN \"TemplateOrientation\" TYPE character varying(50) " +
                "USING (CASE \"TemplateOrientation\" WHEN 1 THEN 'Portrait' WHEN 2 THEN 'Landscape' END);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Series\" ALTER COLUMN \"TemplateFormat\" TYPE character varying(50) " +
                "USING (CASE \"TemplateFormat\" WHEN 1 THEN 'Pocket' WHEN 2 THEN 'Medium' WHEN 3 THEN 'Standard' " +
                "WHEN 4 THEN 'Large' WHEN 5 THEN 'Special' END);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Series\" ALTER COLUMN \"TemplateEditionCategory\" TYPE character varying(50) " +
                "USING (CASE \"TemplateEditionCategory\" WHEN 1 THEN 'FirstEdition' WHEN 2 THEN 'SpecialEdition' " +
                "WHEN 3 THEN 'LimitedEdition' END);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Series\" ALTER COLUMN \"TemplateCondition\" TYPE character varying(50) " +
                "USING (CASE \"TemplateCondition\" WHEN 1 THEN 'Excellent' WHEN 2 THEN 'VeryGood' WHEN 3 THEN 'Good' " +
                "WHEN 4 THEN 'Poor' WHEN 5 THEN 'VeryPoor' END);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Series\" ALTER COLUMN \"TemplateBinding\" TYPE character varying(50) " +
                "USING (CASE \"TemplateBinding\" WHEN 1 THEN 'Paperback' WHEN 2 THEN 'Hardcover' END);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Series\" ALTER COLUMN \"Status\" TYPE character varying(50) " +
                "USING (CASE \"Status\" WHEN 1 THEN 'InProgress' WHEN 2 THEN 'Completed' WHEN 3 THEN 'Abandoned' END);");

            migrationBuilder.Sql(
                "ALTER TABLE \"Editions\" ALTER COLUMN \"ReadingDirection\" TYPE character varying(50) " +
                "USING (CASE \"ReadingDirection\" WHEN 1 THEN 'LeftToRight' WHEN 2 THEN 'RightToLeft' END);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Editions\" ALTER COLUMN \"Orientation\" TYPE character varying(50) " +
                "USING (CASE \"Orientation\" WHEN 1 THEN 'Portrait' WHEN 2 THEN 'Landscape' END);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Editions\" ALTER COLUMN \"Format\" TYPE character varying(50) " +
                "USING (CASE \"Format\" WHEN 1 THEN 'Pocket' WHEN 2 THEN 'Medium' WHEN 3 THEN 'Standard' " +
                "WHEN 4 THEN 'Large' WHEN 5 THEN 'Special' END);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Editions\" ALTER COLUMN \"Condition\" TYPE character varying(50) " +
                "USING (CASE \"Condition\" WHEN 1 THEN 'Excellent' WHEN 2 THEN 'VeryGood' WHEN 3 THEN 'Good' " +
                "WHEN 4 THEN 'Poor' WHEN 5 THEN 'VeryPoor' END);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Editions\" ALTER COLUMN \"Category\" TYPE character varying(50) " +
                "USING (CASE \"Category\" WHEN 1 THEN 'FirstEdition' WHEN 2 THEN 'SpecialEdition' " +
                "WHEN 3 THEN 'LimitedEdition' END);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Editions\" ALTER COLUMN \"Binding\" TYPE character varying(50) " +
                "USING (CASE \"Binding\" WHEN 1 THEN 'Paperback' WHEN 2 THEN 'Hardcover' END);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Editions\" ALTER COLUMN \"AcquisitionMode\" TYPE character varying(50) " +
                "USING (CASE \"AcquisitionMode\" WHEN 1 THEN 'Purchase' WHEN 2 THEN 'Gift' WHEN 3 THEN 'Trade' " +
                "WHEN 4 THEN 'Won' WHEN 5 THEN 'Inherited' END);");

            migrationBuilder.Sql(
                "ALTER TABLE \"Contributions\" ALTER COLUMN \"Role\" TYPE character varying(50) " +
                "USING (CASE \"Role\" WHEN 1 THEN 'Scenarist' WHEN 2 THEN 'Illustrator' WHEN 3 THEN 'Colorist' END);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Albums\" ALTER COLUMN \"Type\" TYPE character varying(50) " +
                "USING (CASE \"Type\" WHEN 1 THEN 'Regular' WHEN 2 THEN 'Omnibus' END);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Albums\" ALTER COLUMN \"Rating\" TYPE character varying(50) " +
                "USING (CASE \"Rating\" WHEN 1 THEN 'VeryPoor' WHEN 2 THEN 'Poor' WHEN 3 THEN 'Average' " +
                "WHEN 4 THEN 'Good' WHEN 5 THEN 'VeryGood' END);");

            migrationBuilder.AlterColumn<string>(
                name: "TemplateReadingDirection",
                table: "Series",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TemplateOrientation",
                table: "Series",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TemplateFormat",
                table: "Series",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TemplateEditionCategory",
                table: "Series",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TemplateCondition",
                table: "Series",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TemplateBinding",
                table: "Series",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Series",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReadingDirection",
                table: "Editions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Orientation",
                table: "Editions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Format",
                table: "Editions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Condition",
                table: "Editions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Editions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Binding",
                table: "Editions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AcquisitionMode",
                table: "Editions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Contributions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Albums",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Rating",
                table: "Albums",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Albums_VolumeRangeOmnibusOnly",
                table: "Albums",
                sql: "\"StartVolumeNumber\" IS NULL OR \"Type\" = 'Omnibus'");
        }
    }
}
