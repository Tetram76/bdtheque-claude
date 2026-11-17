using System.Text.RegularExpressions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The OpenAPI contract versioned in the technical documentation (docs/tech/references/openapi-v1.json) is the one the
/// endpoints produce (choix-implementation/documentation-technique.md, références générées): a change to the contract
/// shows in the diff of its pull request, and a contract left behind fails here.
/// </summary>
public sealed partial class OpenApiContractTests(ApiWebApplicationFactory api) : IClassFixture<ApiWebApplicationFactory>
{
    /// <summary>Set by scripts/Update-TechnicalReferences.ps1 to the file the generated contract is written to.</summary>
    private const string OutputVariable = "BDTHEQUE_OPENAPI_CONTRACT_OUTPUT";

    [Fact]
    public async Task VersionedContract_IsTheOneTheEndpointsProduce()
    {
        var generated = await GenerateAsync();
        var versioned = Path.Combine(RepositoryRoot(), "docs", "tech", "references", "openapi-v1.json");

        if (Environment.GetEnvironmentVariable(OutputVariable) is { Length: > 0 } output)
            await File.WriteAllTextAsync(output, generated);

        Assert.True(File.Exists(versioned), $"{versioned} is missing: run scripts/Update-TechnicalReferences.ps1.");
        var expected = (await File.ReadAllTextAsync(versioned)).ReplaceLineEndings("\n");
        Assert.True(expected == generated,
            "The versioned OpenAPI contract differs from the one the endpoints produce: run scripts/Update-TechnicalReferences.ps1 and commit the result.");
    }

    /// <summary>
    /// The document the API serves on /openapi/v1.json, taken from the host rather than over HTTP: its servers would
    /// otherwise carry the address of the request, which differs from one host to the other.
    /// </summary>
    private async Task<string> GenerateAsync()
    {
        var provider = api.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        var document = await provider.GetOpenApiDocumentAsync(CancellationToken.None);
        var json = await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1, CancellationToken.None);
        return SourceLineBreaks().Replace(json.ReplaceLineEndings("\n"), @"\n") + "\n";
    }

    // The descriptions are the XML comments of the sources, line breaks included, which a checkout on Windows ends with CRLF and
    // one on Linux with LF: an escaped CRLF of the JSON is made an escaped LF, so that the contract is the same on either. Only an
    // escape, not an escaped backslash followed by « r » (an even run of backslashes before it).
    [GeneratedRegex(@"(?<=(?:^|[^\\])(?:\\\\)*)\\r\\n")]
    private static partial Regex SourceLineBreaks();

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Bdtheque.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root (Bdtheque.slnx) not found.");
    }
}
