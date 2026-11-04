---
type: Choix d'implémentation
title: 'Images de base des conteneurs'
description: 'Images de base épinglées, aspnet chiseled-extra, sonde de santé, contenu publié, connexion à db.'
applies_to:
  - 'docker/api/**'
  - 'docker/frontend/**'
  - 'src/Bdtheque.HealthProbe/**'
  - 'docker-compose.yml'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Images de base des conteneurs

- L'usage de conteneurs est imposé ([Architecture des tiers](/contraintes-techniques/architecture-deploiement/architecture-des-tiers.md)) ; le choix des images est libre, sous réserve des contraintes générales qui s'appliquent à tout composant : licences ([Licences](/contraintes-techniques/licences.md)) et compatibilité avec l'environnement Docker de Synology ([Hébergement](/contraintes-techniques/architecture-deploiement/hebergement.md)).
- **Versions épinglées** : chaque image de base (Dockerfiles, `docker-compose.yml`) est désignée par la version exacte de son composant (ex. `10.0.13`, `18.6-alpine3.24`), si bien que la version de .NET, de PostgreSQL ou d'Alpine ne change jamais sans une montée de version visible, revue avant sa fusion ([Mises à jour des dépendances](/gestion-projet/depot/mises-a-jour-des-dependances.md)). Sous ce tag, l'éditeur reconstruit l'image pour les correctifs de son système tant qu'il la maintient, en général jusqu'à la version corrective suivante (documentation de `dotnet/dotnet-docker`, *supported tags*), que le relevé des versions signale alors.
  - **Alternative écartée (tags flottants, ex. `10.0`, `18-alpine`)** : la version du composant change sans que rien dans le dépôt ne le montre, ni n'en déclenche la revue.
  - **Alternative écartée (empreinte, *digest*, en plus de la version)** : chaque correctif du système de l'image attendrait une montée de version dans le dépôt, au mieux mensuelle, et serait ainsi retardé sans gain. L'empreinte ne sert qu'à exiger un niveau précis de correctifs du système, ce que l'application n'exige pas ; Docker Scout refuse de toute façon une image qui ajoute une vulnérabilité corrigeable ([Publication des images Docker](/choix-implementation/deploiement/publication-des-images-docker.md)).
- **`frontend` et `api`** : `mcr.microsoft.com/dotnet/aspnet`, variante `noble-chiseled-extra` de la version de .NET retenue, image runtime officielle de Microsoft, dans sa variante *chiseled* : ni shell ni gestionnaire de paquets, utilisateur non root (UID `1654`), le strict nécessaire à l'exécution ([Déploiement](/contraintes-techniques/architecture-deploiement/deploiement.md), images optimisées pour la production). La variante *extra* embarque ICU, qu'exigent le tri linguistique et le formatage culturels ([Internationalisation](/choix-implementation/socle/transverse/internationalisation.md)).
  - **Alternative écartée (`aspnet:10.0`, Ubuntu complet)** : plus lourde, avec un shell et un gestionnaire de paquets sans usage à l'exécution, et `curl` à installer pour le bilan de santé.
  - **Bilan de santé** : sans shell ni `curl`, le `HEALTHCHECK` des images exécute une sonde (`Bdtheque.HealthProbe`, [Organisation du code](/choix-implementation/organisation-du-code.md)) sur le runtime .NET de l'image : elle interroge `/health` et sort en `0` sur un succès, en `1` sinon. L'équipe .NET n'a pas de recommandation officielle pour ces images, et propose cette approche (dépôt `dotnet/dotnet-docker`, demande n° 4300). `frontend` attend qu'`api` soit sain pour démarrer (`docker-compose.yml`).
  - **Contenu publié** : seules les bibliothèques natives de la plateforme visée (publication propre à l'architecture de l'image, `TARGETARCH`), sans symboles de débogage (compilation sans eux dans les Dockerfiles ; les traces des exceptions gardent les noms des méthodes, sans numéros de ligne), sans fichiers de documentation, `web.config`, lanceur natif ni `appsettings.Development.json` (réglages d'un lancement local depuis les sources).
  - **Connexion à `db`** : le chiffrement GSS (Kerberos), que Npgsql tente par défaut et dont la bibliothèque est absente de l'image *chiseled*, est désactivé dans la chaîne de connexion (`GSS Encryption Mode=Disable`, `docker-compose.yml`) : `db` n'est joignable que par le réseau Docker interne, avec une authentification par mot de passe.
- **`db`** : image officielle `postgres`, version et variante : cf. [Version de PostgreSQL](/choix-implementation/deploiement/version-de-postgresql.md).
