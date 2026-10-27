namespace Bdtheque.Api.Hosting;

/// <summary>The hosting environments of `api` besides those ASP.NET Core names itself.</summary>
public static class ApiEnvironments
{
    /// <summary>
    /// The validation environment, where the user validates a pull request on their own machine
    /// (choix-implementation/deploiement.md § Environnement de validation fonctionnelle): it behaves as
    /// Production, and serves in addition the documentation of the API.
    /// </summary>
    public const string Validation = "Validation";
}
