using System.Net;
using Bdtheque.Api.Hosting;
using Microsoft.AspNetCore.Hosting;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The interactive documentation of the API, served in development and in the validation environment only
/// (choix-implementation/socle.md § Documentation de l'API), from a plain browser, without the internal key.
/// </summary>
public sealed class ApiDocumentationTests(ApiDocumentationTests.ValidationApi validationApi, ApiWebApplicationFactory production)
    : IClassFixture<ApiDocumentationTests.ValidationApi>, IClassFixture<ApiWebApplicationFactory>
{
    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar/v1")]
    public async Task ValidationEnvironment_ServesTheDocumentation_WithoutTheInternalKey(string path)
    {
        var response = await validationApi.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar/v1")]
    public async Task Production_DoesNotServeTheDocumentation(string path)
    {
        var response = await production.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>The API as the validation environment runs it.</summary>
    public sealed class ValidationApi : ApiHostFactory, IAsyncLifetime
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment(ApiEnvironments.Validation);
        }

        Task IAsyncLifetime.InitializeAsync() => InitializeAsync();

        Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();
    }
}
