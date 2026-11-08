---
type: Choix d'implémentation
title: 'Frontend derrière le reverse proxy du NAS'
description: 'Une image pour le reverse proxy et l''accès interne en HTTP, adresse publique déclarée, cookies selon la requête.'
applies_to:
  - 'src/Bdtheque.Frontend/Hosting/**'
  - 'src/Bdtheque.Frontend/Security/**'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Frontend derrière le reverse proxy du NAS

- **Une même image pour les deux déploiements** ([Hébergement](../../contraintes-techniques/architecture-deploiement/hebergement.md)) : derrière le reverse proxy du NAS, qui termine TLS et relaie en HTTP, et en interne, directement en HTTP. `frontend` n'écoute qu'en HTTP et ne force jamais le HTTPS : une redirection vers HTTPS rendrait le déploiement interne inutilisable, HSTS est la politique du seul site HTTPS, que sert le reverse proxy, et, derrière un proxy qui termine TLS, ces deux mécanismes provoquent des redirections en boucle (documentation d'ASP.NET Core, *Configure ASP.NET Core to work with proxy servers and load balancers*).
  - **Alternative écartée (HTTPS servi par `frontend`, certificat monté dans le conteneur)** : contraire à la contrainte, et deux certificats à renouveler pour un même site.
- **Adresse publique déclarée** par le seul déploiement derrière le reverse proxy (schéma et hôte, sans chemin), et appliquée à chaque requête avant tout traitement : ce qui en dépend la suit, et non la connexion HTTP du proxy (redirections absolues, qui partiraient sinon en `http://` ; attribut `Secure` des cookies). Sans adresse déclarée, chaque requête est servie telle qu'elle est reçue, sans rien à configurer. Une adresse mal formée empêche le démarrage.
  - **Alternative écartée (schéma et hôte lus dans les en-têtes `X-Forwarded-*` du reverse proxy)** : il faudrait déclarer les adresses dont `frontend` accepte ces en-têtes, alors que le proxy le joint par le réseau Docker, dont l'adresse varie ; et l'application dépendrait d'en-têtes que la documentation de Synology ne garantit pas. Leur seul avantage, plusieurs adresses pour un même déploiement, est sans usage ici. L'adresse déclarée ne fait confiance à rien de ce que porte la requête.
- **Cookies selon la requête** (`SameAsRequest`, `Secure` sur une requête HTTPS seulement), pour tous les cookies, d'ASP.NET Core comme de `frontend`, sous une politique déclarée une seule fois.
  - **Alternative écartée (cookies toujours `Secure`)** : le navigateur ne les renverrait pas sur le déploiement interne en HTTP, où la connexion à l'administration deviendrait impossible.
