using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bdtheque.Testing;

/// <summary>
/// Keeps a host of the tests from running the background tasks of the application. Started with the host, the first run of a
/// periodic task reads the time while the first test of the class runs, and acts on its data on another time than the one the
/// test sets: the reconciliation of the visuals deleted a pending visual just uploaded on a past time (#597). A test of a task
/// runs it itself, on its own time. Every hosted service the application defines is concerned, present or to come, whatever the
/// way it is registered; those of the framework, the web server first, keep running.
/// </summary>
public static class ApplicationBackgroundTasks
{
    /// <summary>
    /// Replaces each hosted service of <paramref name="services"/> that one of the assemblies of the application defines with one
    /// that does not run it; the application being the one <paramref name="entryAssembly"/> starts.
    /// </summary>
    public static void DoNotRun(IServiceCollection services, Assembly entryAssembly)
    {
        var application = AssembliesOf(entryAssembly);
        for (var index = 0; index < services.Count; index++)
        {
            var descriptor = services[index];
            if (descriptor.ServiceType != typeof(IHostedService) || descriptor.IsKeyedService)
                continue;

            if (descriptor.ImplementationFactory is { } factory)
            {
                // A factory tells its type only once called, as the host calls it at startup.
                services[index] = ServiceDescriptor.Singleton<IHostedService>(provider =>
                {
                    var service = (IHostedService)factory(provider);
                    return application.Contains(service.GetType().Assembly) ? new NotRun(service) : service;
                });
            }
            else if (application.Contains((descriptor.ImplementationType ?? descriptor.ImplementationInstance!.GetType()).Assembly))
            {
                services.RemoveAt(index--);
            }
        }
    }

    /// <summary>The entry assembly and those of the same solution it references, directly or not.</summary>
    private static HashSet<Assembly> AssembliesOf(Assembly entryAssembly)
    {
        var name = entryAssembly.GetName().Name!;
        var solutionPrefix = name[..(name.IndexOf('.') + 1)];
        var found = new HashSet<Assembly>();
        var pending = new Stack<Assembly>([entryAssembly]);
        while (pending.TryPop(out var assembly))
        {
            if (!found.Add(assembly))
                continue;
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name!.StartsWith(solutionPrefix, StringComparison.Ordinal))
                    pending.Push(Assembly.Load(reference));
            }
        }

        return found;
    }

    /// <summary>
    /// Stands for a hosted service its factory created, never started; disposed in its place, the container owning what a
    /// factory returns.
    /// </summary>
    private sealed class NotRun(IHostedService service) : IHostedService, IDisposable, IAsyncDisposable
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void Dispose() => (service as IDisposable)?.Dispose();

        public ValueTask DisposeAsync()
        {
            if (service is IAsyncDisposable asyncDisposable)
                return asyncDisposable.DisposeAsync();
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
