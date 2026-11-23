using Bdtheque.Api.Audience;
using Bdtheque.Api.Visuals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The host of the tests never runs the background tasks of the API on its own (#597): started with the host, their first run
/// would act on the data of a test on another time than the one the test sets, as the reconciliation deleting a pending visual
/// just uploaded on a past time. The tests of a task run it themselves, on their time.
/// </summary>
public sealed class TestHostBackgroundTasksTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    [Fact]
    public async Task TheHostOfTheTests_StartsNoneOfTheHostedServicesOfTheApi_ButServesItsRequests()
    {
        using var client = factory.CreateApiClient();

        using var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        var started = factory.Services.GetServices<IHostedService>().ToList();
        Assert.NotEmpty(started);
        Assert.DoesNotContain(started, service => service.GetType().Assembly == typeof(Program).Assembly);
    }

    [Fact]
    public void AHostedServiceOfTheApplicationRegisteredByItsType_IsRemoved_ThoseOfTheFrameworkKept()
    {
        var services = new ServiceCollection();
        services.AddHostedService<AudienceRetention>();
        services.AddHealthChecks();
        var framework = services.Where(d => d.ServiceType == typeof(IHostedService)).Except([services[0]]).ToList();

        ApplicationBackgroundTasks.DoNotRun(services, typeof(Program).Assembly);

        Assert.Equal(framework, services.Where(d => d.ServiceType == typeof(IHostedService)));
    }

    [Fact]
    public void TheBackgroundTasksOfTheApi_StayAvailableToTheirTests()
    {
        Assert.NotNull(factory.Services.GetRequiredService<VisualReconciliation>());
        Assert.NotNull(factory.Services.GetRequiredService<AudienceRetention>());
    }
}
