---
title: 'Dépendances des projets'
description: 'Références entre les projets de la solution, générées depuis ses fichiers de projet ; pour un fait exact sur qui référence qui.'
code:
  - 'Bdtheque.slnx'
  - 'src/*/*.csproj'
  - 'tests/*/*.csproj'
generated: 'scripts/Update-TechnicalReferences.ps1'
---

# Dépendances des projets

Page générée par [`Update-TechnicalReferences.ps1`](../../../scripts/Update-TechnicalReferences.ps1) depuis [`Bdtheque.slnx`](../../../Bdtheque.slnx) et les références (`ProjectReference`) de chaque fichier de projet : elle ne se modifie pas à la main. Le rôle de chaque projet est décrit dans [Projets de la solution](../architecture/projets-de-la-solution.md).

Schémas générés : ils reproduisent exactement les références directes, sans les références transitives, en deux vues pour rester lisibles.

Question du schéma : *quel projet de l'application référence directement quel autre ?*

```mermaid
flowchart LR
    Bdtheque_Api["Bdtheque.Api"]
    Bdtheque_Contracts["Bdtheque.Contracts"]
    Bdtheque_Domain["Bdtheque.Domain"]
    Bdtheque_Frontend["Bdtheque.Frontend"]
    Bdtheque_HealthProbe["Bdtheque.HealthProbe"]
    Bdtheque_Infrastructure["Bdtheque.Infrastructure"]
    Bdtheque_ValidationData["Bdtheque.ValidationData"]
    Bdtheque_Api --> Bdtheque_Contracts
    Bdtheque_Api --> Bdtheque_Domain
    Bdtheque_Api --> Bdtheque_Infrastructure
    Bdtheque_Frontend --> Bdtheque_Contracts
    Bdtheque_Infrastructure --> Bdtheque_Domain
    Bdtheque_ValidationData --> Bdtheque_Contracts
```

Question du schéma : *quels projets chaque projet de tests référence-t-il directement ?*

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
| `Bdtheque.Domain` | `src` | — |
| `Bdtheque.Frontend` | `src` | `Bdtheque.Contracts` |
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
