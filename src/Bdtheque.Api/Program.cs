using Bdtheque.Api.Errors;
using Bdtheque.Api.Security;
using Bdtheque.Contracts.Errors;
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

// Every error response is a ProblemDetails whose type is one of the three error categories, and
// no other (choix-implementation.md § Erreurs métier, fonctionnelles et techniques).
// ApiExceptionHandler categorizes the exceptions; whatever the framework or a middleware emits
// without one (unbindable request, unknown route, missing internal key…) gets the technical
// category here, no user input being able to produce it.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    if (context.ProblemDetails.Type is not (ProblemTypes.Business or ProblemTypes.Functional or ProblemTypes.Technical))
        context.ProblemDetails.Type = ProblemTypes.Technical;
});
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

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

app.UseExceptionHandler();
// Gives a ProblemDetails body to the error responses emitted without one; a response that already
// has a body (ApiExceptionHandler's, or /health's own report) is left untouched.
app.UseStatusCodePages();

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
