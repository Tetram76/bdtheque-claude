namespace Bdtheque.Api.Tests;

/// <summary>The API host of <see cref="ApiHostFactory"/>, whose lifetime xunit drives as a class fixture.</summary>
public sealed class ApiWebApplicationFactory : ApiHostFactory, IAsyncLifetime
{
    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();
}
