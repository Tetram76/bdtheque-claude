using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>Operation recorded by an entry of the change log (modele-metier.md § Entrée du journal des modifications).</summary>
/// <remarks>The import of the existing data (Reprise) adds its own operation, with the import itself.</remarks>
[JsonConverter(typeof(JsonStringEnumConverter<ChangeLogOperation>))]
public enum ChangeLogOperation
{
    Creation = 1,
    Modification = 2,
    Deletion = 3,
}
