using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Components;
using Bdtheque.Frontend.Formatting;
using Bdtheque.Frontend.Hosting;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Security;
using Bdtheque.Frontend.Theming;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    // An interactive component keeps the time zone of the request that opened its circuit, which brings no cookie.
    .RegisterPersistentService<BrowserTimeZone>(RenderMode.InteractiveServer);

// No text shown to the user is written in the code: each component reads its own (choix-implementation/socle.md
// § Internationalisation). The display culture is a setting of the application, French of France the only one
// offered (fonctionnel/presentation.md § Langue et culture d'affichage): never the one the browser asks for.
builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture(DisplayCulture.Name).AddSupportedCultures(DisplayCulture.Name).AddSupportedUICultures(DisplayCulture.Name);
    options.RequestCultureProviders.Clear();
});
// The time zone of the browser, which its cookie brings to every request (choix-implementation/socle.md § Internationalisation),
// re-executed error pages included.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(services => new BrowserTimeZone
{
    Name = services.GetRequiredService<IHttpContextAccessor>().HttpContext?.Request.Cookies[BrowserTimeZone.CookieName],
});
builder.Services.AddScoped<DisplayFormatter>();
builder.Services.AddSingleton<IThemeSelector, FirstOfferedThemeSelector>();
builder.Services.AddSingleton(TimeProvider.System);

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
// Secure on an HTTPS request only, like the authentication cookie: never Secure by default, and always Secure would
// not be sent back by the browser on the internal deployment in HTTP (choix-implementation/deploiement.md § Frontend
// derrière le reverse proxy du NAS).
builder.Services.AddAntiforgery(options => options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest);

builder.Services.AddOptions<PublicAddressOptions>()
    .Bind(builder.Configuration.GetSection(PublicAddressOptions.SectionName))
    .Validate(options => options.IsValid(), "PublicAddress:Url must be an absolute http or https address, without path, query, fragment nor user information.")
    .ValidateOnStart();

builder.Services.AddOptions<InternalApiKeyOptions>()
    .Bind(builder.Configuration.GetSection(InternalApiKeyOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// HTTP client for `api`, only reachable over the internal Docker network.
builder.Services.AddHttpClient<ApiClient>(ApiClient.HttpClientName, (services, client) =>
{
    var apiOptions = services.GetRequiredService<IConfiguration>().GetSection("Api");
    client.BaseAddress = new Uri(apiOptions["BaseUrl"] ?? throw new InvalidOperationException("Api:BaseUrl is not configured."));

    var internalApiKey = services.GetRequiredService<IOptions<InternalApiKeyOptions>>().Value.Key;
    client.DefaultRequestHeaders.Add(InternalApiKeyOptions.HeaderName, internalApiKey);
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// TLS is terminated by the reverse proxy of the NAS, and the internal deployment is reached in HTTP: neither
// UseHttpsRedirection nor UseHsts (choix-implementation/deploiement.md § Frontend derrière le reverse proxy du NAS).
// Before any other middleware, so that everything follows the public address rather than the connection of the proxy.
if (app.Services.GetRequiredService<IOptions<PublicAddressOptions>>().Value.ParsedUrl is { } publicAddress)
{
    var host = HostString.FromUriComponent(publicAddress);
    app.Use((context, next) =>
    {
        context.Request.Scheme = publicAddress.Scheme;
        context.Request.Host = host;
        return next(context);
    });
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

app.UseRequestLocalization();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapHealthChecks("/health");

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
