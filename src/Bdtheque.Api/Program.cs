using Bdtheque.Api.Security;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// L'environnement "Testing" (WebApplicationFactory) enregistre son propre fournisseur
// EF Core (SQLite en mémoire) : un seul fournisseur ne peut être enregistré à la fois.
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<BdthequeDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Bdtheque")));
}

builder.Services.AddHealthChecks()
    .AddDbContextCheck<BdthequeDbContext>();

builder.Services.AddOpenApi();

builder.Services.AddOptions<InternalApiKeyOptions>()
    .Bind(builder.Configuration.GetSection(InternalApiKeyOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Défense en profondeur : `api` n'est joignable que par `frontend` sur le réseau Docker
// interne, mais exige tout de même ce secret partagé (cf. contraintes-techniques.md).
app.UseMiddleware<InternalApiKeyMiddleware>();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    // Le détail (message d'exception) reste interne aux logs : la réponse HTTP ne doit
    // pas exposer de détails d'infrastructure (chaîne de connexion, etc.).
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var payload = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString());
        await context.Response.WriteAsJsonAsync(payload);
    },
});

app.Run();
