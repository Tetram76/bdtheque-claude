using Microsoft.AspNetCore.Components;

namespace Bdtheque.Frontend.Audience;

/// <summary>
/// The visit of the audience measurement an interactive circuit records its actions in: the one the prerendering of its
/// page recorded the screen in, persisted into the circuit, which has no request nor cookie of its own
/// (choix-implementation/mesure-audience/releve-par-le-frontend.md). None while the visitor opposes the measurement, or
/// when the screen could not be recorded: the actions of the circuit are then not recorded either.
/// </summary>
public sealed class CircuitAudienceVisit
{
    [PersistentState]
    public Guid? VisitId { get; set; }
}
