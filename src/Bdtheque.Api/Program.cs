using Bdtheque.Api.Security;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BdthequeDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Bdtheque")));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<BdthequeDbContext>();

builder.Services.AddOpenApi();

builder.Services.AddOptions<InternalApiKeyOptions>()
    .Bind(builder.Configuration.GetSection(InternalApiKeyOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

// Nothing else applies the schema before the container starts serving traffic (no init
// container, no migration bundle), so a fresh deployment would otherwise leave PostgreSQL
// empty while the health check only verifies connectivity.
using (var migrationScope = app.Services.CreateScope())
{
    migrationScope.ServiceProvider.GetRequiredService<BdthequeDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Defense in depth: `api` is only reachable by `frontend` over the internal Docker
// network, but still requires this shared secret (see choix-implementation.md).
app.UseMiddleware<InternalApiKeyMiddleware>();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    // Details (exception messages) stay in the logs only: the HTTP response must not
    // leak infrastructure details (connection string, etc.).
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var payload = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString());
        await context.Response.WriteAsJsonAsync(payload);
    },
});

app.Run();
