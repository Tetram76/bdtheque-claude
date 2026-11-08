---
type: Choix d'implémentation
title: 'Authentification : mise en œuvre'
description: 'Cookie d''authentification porté par le frontend (BFF), compte administrateur, tentatives limitées, connexion et déconnexion, secret interne exigé par api.'
applies_to:
  - 'src/Bdtheque.Api/Security/**'
  - 'src/Bdtheque.Frontend/Security/**'
  - 'src/Bdtheque.Frontend/Components/Pages/AdminLog*.razor'
  - 'src/Bdtheque.Frontend/Api/InternalApiKeyHandler.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Authentification : mise en œuvre

- Le compte administrateur unique ([Authentification](../../../contraintes-techniques/architecture-deploiement/authentification.md)) repose sur l'**authentification par cookie d'ASP.NET Core**, portée par `frontend` seul, qui agit en **BFF** (*Backend For Frontend*) : formulaire de connexion, cookie et protection des pages de l'administration.
  - **Alternative écartée (ASP.NET Core Identity)** : une gestion de comptes multiples, sans usage pour un compte unique.
- **Identifiant et mot de passe en variables d'environnement** de `frontend`, dont l'absence empêche le démarrage ; le mot de passe est comparé en temps constant, pour que la durée d'un refus ne dise rien de ce qui était juste.
  - **Alternative écartée (empreinte du mot de passe dans la configuration)** : un outil de plus au déploiement, pour rien : le `.env` porte déjà en clair le mot de passe de la base, qui donne accès à toutes les données.
- **Tentatives limitées** (5 échecs en 5 minutes, puis tout refus sans vérification jusqu'à ce que le plus ancien sorte de la fenêtre), l'application étant exposée sur Internet. Le compteur est **unique pour l'application** : derrière le reverse proxy, aucune requête ne dit de façon sûre qui l'envoie ([Frontend derrière le reverse proxy du NAS](../../deploiement/frontend-derriere-le-reverse-proxy-du-nas.md)). Un tiers qui multiplie les échecs fait aussi attendre l'administrateur, quelques minutes au plus, ce qui est accepté pour une application à compte unique.
- **Connexion** hors des pages de l'administration, dans le thème de la visite, avec retour à la page demandée, mais seulement vers un chemin de l'application, jamais vers ce que le navigateur lirait comme un autre site. Un refus est une erreur métier, une tentative de trop une erreur fonctionnelle ([Présentation des erreurs](../../../fonctionnel/presentation-des-erreurs.md)).
- **Déconnexion par un formulaire** protégé contre la falsification, jamais par un lien, qu'une autre page pourrait faire suivre.
- **Secret interne exigé par `api`** sur toutes ses requêtes, connu de ses seuls clients internes (`frontend`, `mcp`, [Outil pour agents IA](../../agents-ia.md)) : `api` n'est joignable que par le réseau Docker interne, et ce secret, en défense en profondeur, empêche qu'un appel direct la contourne si l'isolation réseau venait à être mal configurée. Il ne remplace pas l'authentification de l'utilisateur, qui reste du ressort du client ([Règles d'accès](../../../fonctionnel/regles-d-acces.md)).
