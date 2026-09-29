using Bdtheque.Frontend.Components;
using Bdtheque.Frontend.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// The authentication cookie is owned exclusively by this container (BFF):
// `api` has no knowledge of the user (see choix-implementation.md).
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Bdtheque.Auth";
        options.LoginPath = "/admin/login";
        options.AccessDeniedPath = "/admin/login";
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
