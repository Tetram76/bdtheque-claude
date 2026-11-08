---
type: Choix d'implémentation
title: 'Versions et bibliothèques'
description: '.NET et EF Core 10 LTS, montée suivante vers .NET 12, Npgsql, ML.NET pour l''estimation.'
applies_to:
  - 'Directory.Packages.props'
  - 'global.json'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Versions et bibliothèques

`contraintes-techniques/` n'impose aucune version : les versions retenues sont des choix.

- **.NET 10 et EF Core 10**, alignés, et **uniquement des versions LTS** pour la plateforme .NET (runtime, SDK, ASP.NET Core, EF Core, fournisseur Npgsql) : une version majeure paraît chaque novembre, les paires LTS (trois ans de support), les impaires STS (deux ans). La montée suivante est **.NET 12**, à sa parution ([Mises à jour des dépendances](../gestion-projet/depot/mises-a-jour-des-dependances.md)) ; les versions mineures et correctives sont suivies au fil de leur parution.
  - **Alternative écartée (versions STS)** : une STS ne prolonge pas le support (la 11 le perd en même temps que la 10), et impose une montée de plus.
- **Fournisseur EF Core : Npgsql**, seule la base étant imposée.
- **Estimation de la valeur des éditions** : le Random Forest est imposé ([Estimation de la valeur des éditions](../fonctionnel/valorisation/estimation-de-la-valeur-des-editions.md)) ; sa bibliothèque, **ML.NET**, embarquée dans `api`, est un choix : même langage et même runtime que le reste de l'application, sans service externe.
