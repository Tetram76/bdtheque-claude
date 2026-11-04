---
type: Choix d'implémentation
title: 'Versions et bibliothèques'
description: '.NET et EF Core 10 LTS, montée suivante vers .NET 12, Npgsql, ML.NET pour l''estimation.'
applies_to:
  - 'Directory.Packages.props'
  - 'global.json'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Versions et bibliothèques

- **Aucune version n'est imposée** : `contraintes-techniques/` n'impose jamais un numéro de version. Les versions retenues sont des choix : **.NET 10** (LTS) et **EF Core 10**, alignés.
- **Versions LTS uniquement** pour la plateforme .NET (runtime, SDK, ASP.NET Core, EF Core, et le fournisseur Npgsql, aligné sur EF Core) : d'après la politique de support de .NET, une version majeure paraît chaque novembre, les versions paires étant LTS (trois ans de support) et les impaires STS (deux ans). Une STS ne prolonge pas le support — la 11 le perd en même temps que la 10 — et imposerait une montée de plus : la montée suivante est **.NET 12**, LTS, à sa parution ([Mises à jour des dépendances](/gestion-projet/depot/mises-a-jour-des-dependances.md)). Les versions mineures et correctives sont suivies au fil de leur parution.
- **Le pilote PostgreSQL n'est pas imposé** (seule la base l'est) : le fournisseur EF Core retenu est **Npgsql** (`Npgsql.EntityFrameworkCore.PostgreSQL` 10).
- **Estimation de la valeur des éditions** : le modèle **Random Forest** est imposé comme règle métier ([Estimation de la valeur des éditions](/fonctionnel/valorisation/estimation-de-la-valeur-des-editions.md)). Sa bibliothèque d'implémentation, **ML.NET**, embarquée dans le conteneur `api`, est un choix : même langage et même runtime que le reste de l'application, sans service externe.
