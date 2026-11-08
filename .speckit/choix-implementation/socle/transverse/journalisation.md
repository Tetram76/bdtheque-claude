---
type: Choix d'implémentation
title: 'Journalisation'
description: 'Journal de production sans requêtes SQL, échecs journalisés une fois par leur appelant.'
applies_to:
  - 'src/Bdtheque.Api/appsettings.json'
  - 'src/Bdtheque.Infrastructure/BdthequeDbContext.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Journalisation

- Le journal de production garde ce qui sert à l'exploitation (démarrage, avertissements, erreurs, migrations appliquées), **sans les requêtes SQL**, qui le noieraient ; les niveaux restent réglables au déploiement.
- Un échec d'EF Core (instruction, enregistrement, requête) n'est **pas journalisé par EF Core** : il parvient toujours à son appelant sous forme d'exception, qui le journalise **une fois**, au niveau que sa nature appelle ([Erreurs métier, fonctionnelles et techniques](../../ecritures/erreurs-metier-fonctionnelles-et-techniques.md) pour une requête HTTP).
  - **Alternative écartée (journalisation d'EF Core conservée)** : chaque échec serait journalisé deux fois, et toujours au niveau `Error` : un libellé déjà utilisé, erreur de saisie que l'utilisateur corrige, passerait pour une défaillance de l'application.
