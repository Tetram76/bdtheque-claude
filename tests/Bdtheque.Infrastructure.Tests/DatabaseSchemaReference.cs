using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Npgsql;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// Renders the generated reference of the database schema (docs/tech/references/schema-de-la-base.md) from the catalog of
/// a database that received the migrations, so that the page is the schema the migrations really produce
/// (choix-implementation/documentation-technique.md, références générées).
/// </summary>
/// <remarks>
/// The output depends on the migrations alone: every list is sorted here, ordinally, never by a collation of the server;
/// nothing names the server, its version, the database, its owner or the version that ICU gives a collation, all of which
/// change from one machine or image to the next without the schema changing.
/// </remarks>
internal static partial class DatabaseSchemaReference
{
    public const string PagePath = "docs/tech/references/schema-de-la-base.md";

    private const string Generator = "scripts/Update-TechnicalReferences.ps1";

    private const string Description =
        "Tables, colonnes, contraintes, index, collations et relations de la base, générés depuis une base qui a reçu les migrations ; pour un fait exact sur le schéma physique.";

    // The relations, in one diagram per part of the schema: in a single one, Mermaid shrinks them past legibility. Each
    // table is placed here by hand, which is the only interpretation of the page; a table left out fails the generation.
    // A part where many tables reference the same one is laid out from left to right: from top to bottom, they would all
    // stand side by side, wider than the page.
    private static readonly DiagramGroup[] Groups =
    [
        new("Albums, séries et éditions", "quelles tables du catalogue référencent quelles autres ?", "TB",
            ["Albums", "Authors", "Contributions", "EditionSpotlights", "EditionVisuals", "Editions", "PublisherCollections", "Publishers", "PurchaseIntents", "Series"]),
        new("Genres et univers", "comment albums et séries sont-ils rattachés aux genres et aux univers ?", "LR",
            ["AlbumGenres", "AlbumUniverses", "Genres", "SeriesGenres", "SeriesUniverses", "Universes"]),
        new("Recherches enregistrées", "quelles fiches une recherche enregistrée peut-elle désigner ?", "LR",
            ["SavedSearchDesignations", "SavedSearches"]),
        new("Mesure d'audience", "comment les tables de la mesure d'audience se référencent-elles ?", "TB",
            ["AudienceCounterObjects", "AudienceCounters", "AudienceUsages", "AudienceVisits"]),
    ];

    // Tables that take part in no relation, hence in no diagram.
    private static readonly string[] Unrelated = ["ApplicationSettings", "ChangeLogEntries", "PendingVisuals", "__EFMigrationsHistory"];

    public static async Task<string> RenderAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await EnsureNothingElseAsync(connection);
        var collations = await ReadAsync(connection, CollationsQuery, row => new Collation(row.GetString(0), row.GetString(1), row.GetString(2), row.GetBoolean(3)));
        var columns = await ReadAsync(connection, ColumnsQuery, row => new Column(
            row.GetString(0), row.GetInt16(1), row.GetString(2), row.GetString(3), row.GetBoolean(4),
            row.IsDBNull(5) ? null : row.GetString(5), row.IsDBNull(6) ? null : row.GetString(6), row.GetString(7)));
        var constraints = await ReadAsync(connection, ConstraintsQuery, row => new Constraint(row.GetString(0), row.GetString(1), row.GetChar(2), row.GetString(3)));
        var indexes = await ReadAsync(connection, IndexesQuery, row => new Index(row.GetString(0), row.GetString(1), row.GetBoolean(2), row.GetString(3)));
        var foreignKeys = await ReadAsync(connection, ForeignKeysQuery, row => new ForeignKey(
            row.GetString(0), row.GetString(1), row.GetString(2), row.GetBoolean(3), row.GetBoolean(4)));

        var tables = columns.Select(column => column.Table).Distinct().Order(StringComparer.Ordinal).ToList();
        var page = new StringBuilder();
        WriteHeader(page);
        WriteCollations(page, collations);
        WriteRelations(page, tables, foreignKeys);
        WriteTables(page, tables, columns, constraints, indexes);

        return page.ToString().TrimEnd('\n') + "\n";
    }

    private static void WriteHeader(StringBuilder page)
    {
        page.Append(CultureInfo.InvariantCulture, $"""
            ---
            title: 'Schéma de la base'
            description: '{Description.Replace("'", "''", StringComparison.Ordinal)}'
            code:
              - 'src/Bdtheque.Infrastructure/Migrations/**'
              - 'src/Bdtheque.Infrastructure/BdthequeMigrationsSqlGenerator.cs'
            generated: '{Generator}'
            ---

            # Schéma de la base

            Page générée par [`Update-TechnicalReferences.ps1`](../../../{Generator}), au moyen de [`DatabaseSchemaReference`](../../../tests/Bdtheque.Infrastructure.Tests/DatabaseSchemaReference.cs), depuis le catalogue d'une base PostgreSQL de l'image de production qui a reçu les [migrations](../../../src/Bdtheque.Infrastructure/Migrations/) du commit : elle ne se modifie pas à la main. Elle ne retient rien qui dépende du serveur (version, base, propriétaire, version des collations). Le rôle des conventions et des contraintes, et la façon dont le schéma est appliqué, sont décrits dans [Persistance : modèle et schéma](../mecanismes/persistance-modele-et-schema.md).


            """);
    }

    private static void WriteCollations(StringBuilder page, List<Collation> collations)
    {
        page.Append("""
            ## Collations

            Collations créées par les migrations ; les colonnes texte portent en outre `fr-FR-x-icu`, fournie par PostgreSQL.

            | Collation | Fournisseur | Locale | Déterministe |
            | --- | --- | --- | --- |

            """);
        foreach (var collation in collations.OrderBy(collation => collation.Name, StringComparer.Ordinal))
            page.Append(CultureInfo.InvariantCulture, $"| {Code(collation.Name)} | {collation.Provider} | {Code(collation.Locale)} | {YesNo(collation.Deterministic)} |\n");
        page.Append('\n');
    }

    private static void WriteRelations(StringBuilder page, List<string> tables, List<ForeignKey> foreignKeys)
    {
        var placed = Groups.SelectMany(group => group.Tables).Concat(Unrelated).ToList();
        if (tables.Except(placed, StringComparer.Ordinal).ToList() is { Count: > 0 } missing)
            throw new InvalidOperationException($"Place each table in a diagram of {nameof(DatabaseSchemaReference)}, or among the unrelated ones: {string.Join(", ", missing)}.");
        var related = foreignKeys.SelectMany(key => new[] { key.Table, key.Referenced }).Intersect(Unrelated, StringComparer.Ordinal).ToList();
        if (related.Count > 0)
            throw new InvalidOperationException($"These tables now take part in a relation, and belong in a diagram: {string.Join(", ", related)}.");

        page.Append("""
            ## Relations

            Schémas générés : chaque clé étrangère y figure, dans le schéma de la table qui la porte, avec la table qu'elle référence. Une clé qui admet `NULL` rend la référence facultative (`|o`) ; une clé sur laquelle porte un index unique, sans condition, ne désigne au plus qu'une ligne (`o|`). Les règles `ON DELETE` sont dans les contraintes de chaque table.


            """);
        page.Append(CultureInfo.InvariantCulture,
            $"Tables qui ne référencent aucune autre, et qu'aucune ne référence : {string.Join(", ", Unrelated.Order(StringComparer.Ordinal).Select(Code))}.\n\n");

        foreach (var group in Groups)
        {
            var keys = foreignKeys.Where(key => group.Tables.Contains(key.Table, StringComparer.Ordinal))
                .OrderBy(key => key.Table, StringComparer.Ordinal).ThenBy(key => key.Columns, StringComparer.Ordinal).ToList();
            page.Append(CultureInfo.InvariantCulture, $"### {group.Title}\n\nQuestion du schéma : *{group.Question}*\n\n```mermaid\nerDiagram\n    direction {group.Direction}\n");
            foreach (var key in keys)
            {
                var referenced = key.Mandatory ? "||" : "|o";
                var referencing = key.Unique ? "o|" : "o{";
                page.Append(CultureInfo.InvariantCulture, $"    {key.Referenced} {referenced}--{referencing} {key.Table} : \"{key.Columns}\"\n");
            }

            page.Append("```\n\n");
        }
    }

    private static void WriteTables(StringBuilder page, List<string> tables, List<Column> columns, List<Constraint> constraints, List<Index> indexes)
    {
        page.Append("## Tables\n\n");
        foreach (var table in tables)
        {
            page.Append(CultureInfo.InvariantCulture, $"### {Code(table)}\n\n| Colonne | Type | NULL | Défaut ou expression | Collation |\n| --- | --- | --- | --- | --- |\n");
            foreach (var column in columns.Where(column => column.Table == table).OrderBy(column => column.Position))
            {
                var type = column.Generated is { } generated ? $"{column.Type}, {generated}" : column.Type;
                page.Append(CultureInfo.InvariantCulture,
                    $"| {Code(column.Name)} | {Code(type)} | {(column.NotNull ? "non" : "oui")} | {(column.Default is null ? "" : Code(column.Default))} | {(column.Collation is null ? "" : Code(column.Collation))} |\n");
            }

            var own = constraints.Where(constraint => constraint.Table == table)
                .OrderBy(constraint => ConstraintKinds.IndexOf(constraint.Kind)).ThenBy(constraint => constraint.Name, StringComparer.Ordinal).ToList();
            if (own.Count > 0)
            {
                page.Append("\n| Contrainte | Définition |\n| --- | --- |\n");
                foreach (var constraint in own)
                    page.Append(CultureInfo.InvariantCulture, $"| {Code(constraint.Name)} | {Code(constraint.Definition)} |\n");
            }

            var ownIndexes = indexes.Where(index => index.Table == table).OrderBy(index => index.Name, StringComparer.Ordinal).ToList();
            if (ownIndexes.Count > 0)
            {
                page.Append("\n| Index | Unique | Définition |\n| --- | --- | --- |\n");
                foreach (var index in ownIndexes)
                    page.Append(CultureInfo.InvariantCulture, $"| {Code(index.Name)} | {YesNo(index.Unique)} | {Code(index.Definition)} |\n");
            }

            page.Append('\n');
        }
    }

    // A schema object that the page does not render would be left out of it in silence: the generation refuses it instead.
    private static async Task EnsureNothingElseAsync(NpgsqlConnection connection)
    {
        var others = await ReadAsync(connection, OtherObjectsQuery, row => row.GetString(0));
        if (others.Count > 0)
            throw new InvalidOperationException($"{nameof(DatabaseSchemaReference)} does not render these objects of the schema: {string.Join(", ", others.Order(StringComparer.Ordinal))}.");
    }

    private static async Task<List<T>> ReadAsync<T>(NpgsqlConnection connection, string query, Func<NpgsqlDataReader, T> read)
    {
        await using var command = new NpgsqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<T>();
        while (await reader.ReadAsync())
            rows.Add(read(reader));

        return rows;
    }

    // Inline code in a table cell, on one line: PostgreSQL lays out some expressions on several (CASE), which would end the
    // row. Its pipes are escaped, and a backtick of the text calls for a longer delimiter.
    private static string Code(string text)
    {
        var escaped = Whitespace().Replace(text.Trim(), " ").Replace("|", "\\|", StringComparison.Ordinal);
        return text.Contains('`', StringComparison.Ordinal) ? $"`` {escaped} ``" : $"`{escaped}`";
    }

    private static string YesNo(bool value) => value ? "oui" : "non";

    // Primary key, unique, exclusion, foreign keys, then checks: from what identifies a row to what constrains it.
    private const string ConstraintKinds = "puxfc";

    private const string CollationsQuery = """
        SELECT collname,
               CASE collprovider WHEN 'i' THEN 'ICU' WHEN 'c' THEN 'libc' WHEN 'b' THEN 'intégré' ELSE collprovider::text END,
               coalesce(colllocale, collcollate, ''),
               collisdeterministic
        FROM pg_collation
        WHERE collnamespace = 'public'::regnamespace
        """;

    private const string ColumnsQuery = """
        SELECT c.relname, a.attnum, a.attname, format_type(a.atttypid, a.atttypmod), a.attnotnull,
               pg_get_expr(d.adbin, d.adrelid),
               CASE WHEN a.attidentity = 'a' THEN 'identité toujours générée'
                    WHEN a.attidentity = 'd' THEN 'identité générée par défaut'
                    WHEN a.attgenerated = 's' THEN 'calculée et stockée'
                    WHEN a.attgenerated = 'v' THEN 'calculée' END,
               coalesce(co.collname, '')
        FROM pg_attribute a
        JOIN pg_class c ON c.oid = a.attrelid
        LEFT JOIN pg_attrdef d ON d.adrelid = a.attrelid AND d.adnum = a.attnum
        LEFT JOIN pg_collation co ON co.oid = a.attcollation
        WHERE c.relnamespace = 'public'::regnamespace AND c.relkind IN ('r', 'p') AND a.attnum > 0 AND NOT a.attisdropped
        """;

    // The NOT NULL constraints are those of the columns, already shown with them.
    private const string ConstraintsQuery = """
        SELECT c.relname, k.conname, k.contype, pg_get_constraintdef(k.oid)
        FROM pg_constraint k
        JOIN pg_class c ON c.oid = k.conrelid
        WHERE c.relnamespace = 'public'::regnamespace AND k.contype IN ('p', 'u', 'x', 'f', 'c')
        """;

    // The indexes that no constraint of their table creates, the constraints listing those; their definition from its
    // access method on, the table and the name being those of the row.
    private const string IndexesQuery = """
        SELECT c.relname, i.relname, x.indisunique, substring(pg_get_indexdef(x.indexrelid) FROM ' USING (.*)$')
        FROM pg_index x
        JOIN pg_class i ON i.oid = x.indexrelid
        JOIN pg_class c ON c.oid = x.indrelid
        WHERE c.relnamespace = 'public'::regnamespace
          AND NOT EXISTS (SELECT FROM pg_constraint k WHERE k.conindid = x.indexrelid AND k.conrelid = x.indrelid AND k.contype IN ('p', 'u', 'x'))
        """;

    // A key is mandatory when none of its columns admits NULL, and designates one row at most when a unique index without
    // condition bears exactly its columns, compared as text: an int2vector cast to an array starts at index 0, a constraint
    // key at 1, and arrays of different bounds are never equal.
    private const string ForeignKeysQuery = """
        SELECT c.relname, r.relname,
               (SELECT string_agg(a.attname, ', ' ORDER BY p.n) FROM unnest(k.conkey) WITH ORDINALITY p(attnum, n)
                JOIN pg_attribute a ON a.attrelid = k.conrelid AND a.attnum = p.attnum),
               (SELECT bool_and(a.attnotnull) FROM pg_attribute a WHERE a.attrelid = k.conrelid AND a.attnum = ANY (k.conkey)),
               EXISTS (SELECT FROM pg_index u WHERE u.indrelid = k.conrelid AND u.indisunique AND u.indpred IS NULL
                       AND u.indkey::text = array_to_string(k.conkey, ' '))
        FROM pg_constraint k
        JOIN pg_class c ON c.oid = k.conrelid
        JOIN pg_class r ON r.oid = k.confrelid
        WHERE c.relnamespace = 'public'::regnamespace AND k.contype = 'f'
        """;

    // Views, sequences and other relations, functions, types of their own, triggers and extensions besides PL/pgSQL.
    private const string OtherObjectsQuery = """
        SELECT 'relation ' || relname FROM pg_class
        WHERE relnamespace = 'public'::regnamespace AND relkind NOT IN ('r', 'p', 'i', 'I')
        UNION ALL
        SELECT 'function ' || proname FROM pg_proc WHERE pronamespace = 'public'::regnamespace
        UNION ALL
        SELECT 'type ' || typname FROM pg_type t
        WHERE typnamespace = 'public'::regnamespace AND typtype <> 'b'
          AND NOT EXISTS (SELECT FROM pg_class c WHERE c.reltype = t.oid)
        UNION ALL
        SELECT 'base type ' || typname FROM pg_type
        WHERE typnamespace = 'public'::regnamespace AND typtype = 'b' AND typelem = 0
        UNION ALL
        SELECT 'trigger ' || tgname FROM pg_trigger g JOIN pg_class c ON c.oid = g.tgrelid
        WHERE c.relnamespace = 'public'::regnamespace AND NOT g.tgisinternal
        UNION ALL
        SELECT 'extension ' || extname FROM pg_extension WHERE extname <> 'plpgsql'
        """;

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    private sealed record DiagramGroup(string Title, string Question, string Direction, string[] Tables);

    private sealed record Collation(string Name, string Provider, string Locale, bool Deterministic);

    private sealed record Column(string Table, short Position, string Name, string Type, bool NotNull, string? Default, string? Generated, string CollationName)
    {
        // A column of a type without collation has none; one that keeps that of its type shows it as such.
        public string? Collation => CollationName.Length == 0 ? null : CollationName;
    }

    private sealed record Constraint(string Table, string Name, char Kind, string Definition);

    private sealed record Index(string Table, string Name, bool Unique, string Definition);

    private sealed record ForeignKey(string Table, string Referenced, string Columns, bool Mandatory, bool Unique);
}
