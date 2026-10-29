using System.Reflection;

namespace Bdtheque.Frontend.Navigation;

/// <summary>
/// Assemblies whose pages the application routes besides its own: none in the application. The end-to-end tests add theirs,
/// whose pages present the components shared by the screens in a browser before any screen uses them
/// (choix-implementation/outillage.md § Outillage de développement .NET, tests du frontend).
/// </summary>
public sealed class PageAssemblies(IReadOnlyList<Assembly> additional)
{
    public IReadOnlyList<Assembly> Additional { get; } = additional;
}
