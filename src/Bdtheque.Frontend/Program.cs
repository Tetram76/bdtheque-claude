using Bdtheque.Frontend.Components;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// No text shown to the user is written in the code: each component reads its own (choix-implementation/socle.md
// § Internationalisation).
builder.Services.AddLocalization();

// The authentication cookie is owned exclusively by this container (BFF):
// `api` has no knowledge of the user (see choix-implementation/socle.md § Authentification : mise en œuvre).
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Bdtheque.Auth";
        options.LoginPath = AppPaths.AdminLogin;
        options.AccessDeniedPath = AppPaths.AdminLogin;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddOptions<InternalApiKeyOptions>()
    .Bind(builder.Configuration.GetSection(InternalApiKeyOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// HTTP client for `api`, only reachable over the internal Docker network.
builder.Services.AddHttpClient("Api", (services, client) =>
{
    var apiOptions = services.GetRequiredService<IConfiguration>().GetSection("Api");
    client.BaseAddress = new Uri(apiOptions["BaseUrl"] ?? throw new InvalidOperationException("Api:BaseUrl is not configured."));

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
