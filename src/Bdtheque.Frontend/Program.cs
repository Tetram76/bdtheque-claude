using Bdtheque.Frontend.Components;
using Bdtheque.Frontend.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Le cookie d'authentification est porté exclusivement par ce conteneur (BFF) :
// `api` n'a pas connaissance de l'utilisateur (cf. contraintes-techniques.md).
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Bdtheque.Auth";
        options.LoginPath = "/administration/connexion";
        options.AccessDeniedPath = "/administration/connexion";
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddOptions<InternalApiKeyOptions>()
    .Bind(builder.Configuration.GetSection(InternalApiKeyOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Client HTTP vers `api`, exclusivement joignable via le réseau Docker interne.
builder.Services.AddHttpClient("Api", (services, client) =>
{
    var apiOptions = services.GetRequiredService<IConfiguration>().GetSection("Api");
    client.BaseAddress = new Uri(apiOptions["BaseUrl"] ?? throw new InvalidOperationException("Api:BaseUrl n'est pas configuré."));

    var internalApiKey = services.GetRequiredService<IOptions<InternalApiKeyOptions>>().Value.Key;
    client.DefaultRequestHeaders.Add(InternalApiKeyOptions.HeaderName, internalApiKey);
});

builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapHealthChecks("/health");

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
