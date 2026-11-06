namespace Bdtheque.Frontend.Hosting;

/// <summary>The hosting environments of `frontend` besides those ASP.NET Core names itself.</summary>
public static class FrontendEnvironments
{
    /// <summary>
    /// The validation environment, where the user validates a pull request on their own machine
    /// (choix-implementation/deploiement/environnement-de-validation-fonctionnelle.md): it behaves as Production, and lets
    /// the address of a page choose its theme (<see cref="Theming.ChosenThemeSelector"/>).
    /// </summary>
    public const string Validation = "Validation";
}
