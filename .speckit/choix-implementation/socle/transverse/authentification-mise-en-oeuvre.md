---
type: Choix d'implémentation
title: 'Authentification : mise en œuvre'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Authentification : mise en œuvre

- Le compte administrateur unique ([Authentification](/contraintes-techniques/architecture-deploiement/authentification.md)) est implémenté via **ASP.NET Core Cookie Authentication** (sans ASP.NET Core Identity — pas de gestion multi-utilisateurs). Le cookie d'authentification est porté exclusivement par le conteneur `frontend` (Blazor), qui agit comme **BFF (Backend For Frontend)** : c'est lui qui affiche le formulaire de connexion, émet le cookie et protège ses propres pages/composants d'administration (`[Authorize]`).
- Le conteneur `api` n'est **jamais exposé publiquement** : il n'est joignable que par ses clients internes (`frontend` et `mcp`) via le réseau Docker interne. En défense en profondeur, `api` exige néanmoins un **secret interne partagé** (clé statique transmise via variable d'environnement, vérifiée par un middleware sur l'en-tête `X-Internal-Api-Key`) sur toutes ses requêtes. Ce secret n'est connu que de `api` et de ses clients internes, `frontend` et, pour l'outil pour agents IA, `mcp` ([Outil pour agents IA](/choix-implementation/agents-ia.md)) ; il ne remplace pas l'authentification de l'utilisateur, qui reste du ressort du client qui appelle `api` : `frontend` pour l'interface web, `mcp` pour l'outil pour agents IA, qui doit authentifier et autoriser l'administrateur avant tout appel à `/admin` (règles d'accès globales, [Règles d'accès](/fonctionnel/regles-d-acces.md)), il empêche seulement qu'un appel direct à `api` contourne la protection applicative si l'isolation réseau venait à être mal configurée.
