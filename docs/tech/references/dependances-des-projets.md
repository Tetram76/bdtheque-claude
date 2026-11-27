---
title: 'Dépendances des projets'
description: 'Cette page, générée depuis les fichiers de projet, montre quel projet de la solution référence quel autre ; à lire pour un fait exact sur les dépendances entre projets.'
code:
  - 'Bdtheque.slnx'
  - 'src/*/*.csproj'
  - 'tests/*/*.csproj'
generated: 'scripts/Update-TechnicalReferences.ps1'
---

# Dépendances des projets

Cette page est générée par [`Update-TechnicalReferences.ps1`](../../../scripts/Update-TechnicalReferences.ps1), qui lit la liste des projets dans [`Bdtheque.slnx`](../../../Bdtheque.slnx) puis les références de projet (`ProjectReference`) déclarées dans chaque fichier de projet ; elle ne se modifie donc jamais à la main. Le rôle de chaque projet, et la façon dont ils communiquent à l'exécution, sont expliqués dans [Projets de la solution](../architecture/projets-de-la-solution.md).

Les deux schémas suivants sont générés : ils reproduisent exactement les références directes de chaque projet, sans les références transitives, et séparent les projets de l'application de ceux des tests pour rester lisibles.

Le premier répond à une question : quel projet de l'application référence directement quel autre ?

```mermaid
flowchart LR
    Bdtheque_Api["Bdtheque.Api"]
    Bdtheque_Contracts["Bdtheque.Contracts"]
    Bdtheque_Currencies["Bdtheque.Currencies"]
    Bdtheque_Domain["Bdtheque.Domain"]
    Bdtheque_Frontend["Bdtheque.Frontend"]
    Bdtheque_HealthProbe["Bdtheque.HealthProbe"]
    Bdtheque_Infrastructure["Bdtheque.Infrastructure"]
    Bdtheque_ValidationData["Bdtheque.ValidationData"]
    Bdtheque_Api --> Bdtheque_Contracts
    Bdtheque_Api --> Bdtheque_Domain
    Bdtheque_Api --> Bdtheque_Infrastructure
    Bdtheque_Domain --> Bdtheque_Currencies
    Bdtheque_Frontend --> Bdtheque_Contracts
    Bdtheque_Frontend --> Bdtheque_Currencies
    Bdtheque_Infrastructure --> Bdtheque_Domain
    Bdtheque_ValidationData --> Bdtheque_Contracts
```

Le second répond à une autre : quels projets chaque projet de tests référence-t-il directement ?

```mermaid
flowchart LR
    Bdtheque_Api["Bdtheque.Api"]
    Bdtheque_Api_Tests["Bdtheque.Api.Tests"]
    Bdtheque_Documentation_Tests["Bdtheque.Documentation.Tests"]
    Bdtheque_Domain["Bdtheque.Domain"]
    Bdtheque_Domain_Tests["Bdtheque.Domain.Tests"]
    Bdtheque_EndToEnd_Tests["Bdtheque.EndToEnd.Tests"]
    Bdtheque_Frontend["Bdtheque.Frontend"]
    Bdtheque_Frontend_Tests["Bdtheque.Frontend.Tests"]
    Bdtheque_Infrastructure["Bdtheque.Infrastructure"]
    Bdtheque_Infrastructure_Tests["Bdtheque.Infrastructure.Tests"]
    Bdtheque_Testing["Bdtheque.Testing"]
    Bdtheque_ValidationData["Bdtheque.ValidationData"]
    Bdtheque_ValidationData_Tests["Bdtheque.ValidationData.Tests"]
    Bdtheque_Api_Tests --> Bdtheque_Api
    Bdtheque_Api_Tests --> Bdtheque_Testing
    Bdtheque_Domain_Tests --> Bdtheque_Domain
    Bdtheque_EndToEnd_Tests --> Bdtheque_Frontend
    Bdtheque_EndToEnd_Tests --> Bdtheque_Testing
    Bdtheque_Frontend_Tests --> Bdtheque_Domain
    Bdtheque_Frontend_Tests --> Bdtheque_Frontend
    Bdtheque_Infrastructure_Tests --> Bdtheque_Testing
    Bdtheque_Testing --> Bdtheque_Api
    Bdtheque_Testing --> Bdtheque_Infrastructure
    Bdtheque_ValidationData_Tests --> Bdtheque_Testing
    Bdtheque_ValidationData_Tests --> Bdtheque_ValidationData
```

| Projet | Dossier | Référence |
| --- | --- | --- |
| `Bdtheque.Api` | `src` | `Bdtheque.Contracts`, `Bdtheque.Domain`, `Bdtheque.Infrastructure` |
| `Bdtheque.Contracts` | `src` | — |
| `Bdtheque.Currencies` | `src` | — |
| `Bdtheque.Domain` | `src` | `Bdtheque.Currencies` |
| `Bdtheque.Frontend` | `src` | `Bdtheque.Contracts`, `Bdtheque.Currencies` |
| `Bdtheque.HealthProbe` | `src` | — |
| `Bdtheque.Infrastructure` | `src` | `Bdtheque.Domain` |
| `Bdtheque.ValidationData` | `src` | `Bdtheque.Contracts` |
| `Bdtheque.Api.Tests` | `tests` | `Bdtheque.Api`, `Bdtheque.Testing` |
| `Bdtheque.Documentation.Tests` | `tests` | — |
| `Bdtheque.Domain.Tests` | `tests` | `Bdtheque.Domain` |
| `Bdtheque.EndToEnd.Tests` | `tests` | `Bdtheque.Frontend`, `Bdtheque.Testing` |
| `Bdtheque.Frontend.Tests` | `tests` | `Bdtheque.Domain`, `Bdtheque.Frontend` |
| `Bdtheque.Infrastructure.Tests` | `tests` | `Bdtheque.Testing` |
| `Bdtheque.Testing` | `tests` | `Bdtheque.Api`, `Bdtheque.Infrastructure` |
| `Bdtheque.ValidationData.Tests` | `tests` | `Bdtheque.Testing`, `Bdtheque.ValidationData` |
