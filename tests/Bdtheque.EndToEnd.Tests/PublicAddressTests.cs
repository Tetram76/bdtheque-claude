extern alias frontend;

using System.Globalization;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using frontend::Bdtheque.Frontend.Navigation;
using FrontendProgram = frontend::Program;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// <c>frontend</c> behind the reverse proxy of the NAS, which terminates TLS and relays in HTTP, and deployed
/// internally, in HTTP (choix-implementation/deploiement.md § Frontend derrière le reverse proxy du NAS): the same
/// image, the public address of the deployment behind the reverse proxy alone telling them apart. The requests of
/// these tests reach <c>frontend</c> in HTTP, as the reverse proxy relays them.
/// </summary>
public sealed class PublicAddressTests
{
    private const string CookiesPath = "/test/cookies";

    [Theory]
    [InlineData("https://bd.example.com")]
    [InlineData("https://bd.example.com:8443")]
    public async Task WithAPublicAddress_RedirectsToTheLoginPageAtThePublicAddress(string publicAddress)
    {
        await using var frontend = new FrontendFactory(publicAddress);

        var response = await frontend.CreateClient(NoRedirect).GetAsync(AppPaths.AdminHome);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith($"{publicAddress}{AppPaths.AdminLogin}?", response.Headers.Location!.AbsoluteUri);
    }

    [Fact]
    public async Task WithAnHttpsPublicAddress_CookiesAreSecure()
    {
        await using var frontend = new FrontendFactory("https://bd.example.com");

        var cookies = await IssuedCookiesAsync(frontend);

        Assert.All(cookies, cookie => Assert.Contains("; secure", cookie, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task WithAnHttpsPublicAddress_SendsNoStrictTransportSecurity()
    {
        await using var frontend = new FrontendFactory("https://bd.example.com");

        var response = await frontend.CreateClient(NoRedirect).GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task WithoutPublicAddress_RedirectsToTheLoginPageInHttp()
    {
        await using var frontend = new FrontendFactory(publicAddress: null);

        var response = await frontend.CreateClient(NoRedirect).GetAsync(AppPaths.AdminHome);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith($"http://localhost{AppPaths.AdminLogin}?", response.Headers.Location!.AbsoluteUri);
    }

    [Fact]
    public async Task WithoutPublicAddress_CookiesAreNotSecure()
    {
        await using var frontend = new FrontendFactory(publicAddress: null);

        var cookies = await IssuedCookiesAsync(frontend);

        Assert.All(cookies, cookie => Assert.DoesNotContain("; secure", cookie, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task WithoutPublicAddress_NeverRedirectsToHttps()
    {
        // A port for HTTPS known to the application, as a stray setting of the deployment would make it.
        await using var frontend = new FrontendFactory(publicAddress: null, httpsPort: 443);

        var response = await frontend.CreateClient(NoRedirect).GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Theory]
    [InlineData("bd.example.com")]
    [InlineData("/admin")]
    [InlineData("ftp://bd.example.com")]
    [InlineData("https://bd.example.com/bdtheque")]
    [InlineData("https://bd.example.com/?page=1")]
    [InlineData("https://bd.example.com/#top")]
    [InlineData("https://user@bd.example.com")]
    public async Task InvalidPublicAddress_PreventsTheStart(string publicAddress)
    {
        await using var frontend = new FrontendFactory(publicAddress);

        var exception = Assert.ThrowsAny<Exception>(() => frontend.CreateClient());

        Assert.Contains(UnwrapAll(exception), inner => inner is OptionsValidationException);
    }

    private static readonly WebApplicationFactoryClientOptions NoRedirect = new() { AllowAutoRedirect = false };

    /// <summary>
    /// The cookies the application issues, authentication and antiforgery, by an endpoint of the tests: the login
    /// page, which issues them in the application, is not implemented yet (#199).
    /// </summary>
    private static async Task<IReadOnlyList<string>> IssuedCookiesAsync(FrontendFactory frontend)
    {
        var response = await frontend.CreateClient(NoRedirect).GetAsync(CookiesPath);

        response.EnsureSuccessStatusCode();
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(cookies, cookie => cookie.StartsWith("Bdtheque.Auth=", StringComparison.Ordinal));
        Assert.Contains(cookies, cookie => cookie.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal));
        return cookies;
    }

    private static IEnumerable<Exception> UnwrapAll(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            yield return current;
            if (current is AggregateException aggregate)
                foreach (var inner in aggregate.InnerExceptions.SelectMany(UnwrapAll))
                    yield return inner;
        }
    }

    private sealed class FrontendFactory(string? publicAddress, int? httpsPort = null) : WebApplicationFactory<FrontendProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configBuilder) =>
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Api:BaseUrl"] = "http://api",
                    ["InternalApiKey:Key"] = ApiHostFactory.InternalApiKey,
                    ["PublicAddress:Url"] = publicAddress,
                    ["https_port"] = httpsPort?.ToString(CultureInfo.InvariantCulture),
                }));
            builder.ConfigureTestServices(services => services.AddTransient<IStartupFilter, CookiesEndpoint>());
        }
    }

    /// <summary>Appended after the whole pipeline of the application, public address included.</summary>
    private sealed class CookiesEndpoint : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.Map(CookiesPath, branch => branch.Run(async context =>
            {
                context.RequestServices.GetRequiredService<IAntiforgery>().GetAndStoreTokens(context);
                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], CookieAuthenticationDefaults.AuthenticationScheme)));
            }));
        };
    }
}
