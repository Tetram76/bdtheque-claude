using Bdtheque.ValidationData;

// Loads the validation data into the `api` of the validation environment, which runs this program once, on
// an empty database (choix-implementation/deploiement.md § Environnement de validation fonctionnelle). Same
// configuration as `frontend`, the client of the API it stands for.
var baseUrl = Required("Api__BaseUrl");
var internalApiKey = Required("InternalApiKey__Key");

using var client = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMinutes(5) };
client.DefaultRequestHeaders.Add(ValidationDataLoader.InternalApiKeyHeader, internalApiKey);

await new ValidationDataLoader(client).LoadAsync(CancellationToken.None);
Console.WriteLine("Validation data loaded.");

static string Required(string variable) =>
    Environment.GetEnvironmentVariable(variable) is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"The environment variable {variable} is required.");
