---
type: Contrainte technique
title: 'Statistiques du dashboard'
description: 'Seules les statistiques présentées par un affichage du dashboard sont calculées : le tirage précède le chargement.'
applies_to:
  - 'src/Bdtheque.Api/Endpoints/DashboardEndpoints.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Statistiques du dashboard

Seules les données des statistiques **présentées** lors d'un affichage du dashboard (les quatre blocs toujours présents et les statistiques tirées, [Page d'accueil](/fonctionnel/page-accueil.md)) sont calculées et chargées : le tirage précède le chargement, et aucune donnée n'est chargée pour une statistique qui ne sera pas présentée.
