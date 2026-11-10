using Bdtheque.ValidationData;

// Loads the validation data into the `api` of the validation environment, which runs this program once, on
// an empty database (choix-implementation/deploiement/environnement-de-validation-fonctionnelle.md). Same
// configuration as `frontend`, the client of the API it stands for. Its only argument, passed by
// scripts/Start-Validation.ps1 on demand, puts the pale cover in the spotlight.
const string PaleCoverSpotlightArgument = "--pale-cover-spotlight";

var paleCoverInTheSpotlight = args switch
{
    [] => false,
    [PaleCoverSpotlightArgument] => true,
    _ => throw new ArgumentException($"Unexpected arguments '{string.Join(' ', args)}': the only one accepted is {PaleCoverSpotlightArgument}."),
};
var baseUrl = Required("Api__BaseUrl");
var internalApiKey = Required("InternalApiKey__Key");

using var client = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMinutes(5) };
client.DefaultRequestHeaders.Add(ValidationDataLoader.InternalApiKeyHeader, internalApiKey);

await new ValidationDataLoader(client, TimeProvider.System).LoadAsync(paleCoverInTheSpotlight, CancellationToken.None);
Console.WriteLine(paleCoverInTheSpotlight ? "Validation data loaded, the pale cover in the spotlight." : "Validation data loaded.");

static string Required(string variable) =>
    Environment.GetEnvironmentVariable(variable) is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"The environment variable {variable} is required.");
