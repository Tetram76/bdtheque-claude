namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// Keeps the generated reference of the database schema that of the migrations: it needs a migrated database, which only
/// these tests have, so it is checked here rather than by <c>Update-TechnicalReferences.ps1 -Check</c>
/// (choix-implementation/documentation-technique.md, références générées).
/// </summary>
public sealed class DatabaseSchemaReferenceTests
{
    /// <summary>Set by scripts/Update-TechnicalReferences.ps1 to the file the generated page is written to.</summary>
    private const string OutputVariable = "BDTHEQUE_DATABASE_SCHEMA_OUTPUT";

    [Fact]
    public async Task VersionedPage_IsTheSchemaOfTheMigrations()
    {
        var connectionString = await PostgreSqlTestServer.CreateMigratedDatabaseAsync();
        string generated;
        try
        {
            generated = await DatabaseSchemaReference.RenderAsync(connectionString);
        }
        finally
        {
            PostgreSqlTestServer.ReleaseConnections(connectionString);
        }

        Assert.DoesNotContain('\r', generated);
        if (Environment.GetEnvironmentVariable(OutputVariable) is { Length: > 0 } output)
            await File.WriteAllTextAsync(output, generated, new System.Text.UTF8Encoding(false));

        var path = Path.Combine(RepositoryFiles.Root, DatabaseSchemaReference.PagePath);
        Assert.True(File.Exists(path), $"{DatabaseSchemaReference.PagePath} is missing: run scripts/Update-TechnicalReferences.ps1.");
        var versioned = (await File.ReadAllTextAsync(path)).ReplaceLineEndings("\n");
        Assert.True(versioned == generated,
            $"{DatabaseSchemaReference.PagePath} is not the schema of the migrations: run scripts/Update-TechnicalReferences.ps1 and commit the result.");
    }
}
