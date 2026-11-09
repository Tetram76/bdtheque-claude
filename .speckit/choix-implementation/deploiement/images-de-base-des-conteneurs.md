---
type: Choix d'implémentation
title: 'Images de base des conteneurs'
description: 'Images de base épinglées, aspnet chiseled-extra, sonde de santé, contenu publié, connexion à db.'
applies_to:
  - 'docker/api/**'
  - 'docker/frontend/**'
  - 'src/Bdtheque.HealthProbe/**'
  - 'docker-compose.yml'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-09T11:30:00Z }
---

# Images de base des conteneurs

Les conteneurs sont imposés ([Architecture des tiers](../../contraintes-techniques/architecture-deploiement/architecture-des-tiers.md)), le choix des images est libre dans le respect des [Licences](../../contraintes-techniques/licences.md) et de l'environnement Docker de Synology ([Hébergement](../../contraintes-techniques/architecture-deploiement/hebergement.md)).

- **Versions épinglées** : chaque image de base est désignée par la version exacte de son composant (.NET, PostgreSQL, Alpine), qui ne change donc jamais sans une montée visible, revue avant sa fusion ([Mises à jour des dépendances](../../gestion-projet/depot/mises-a-jour-des-dependances.md)). Sous ce tag, l'éditeur reconstruit l'image pour les correctifs de son système tant qu'il la maintient (documentation de `dotnet/dotnet-docker`, *supported tags*).
  - **Alternative écartée (tags flottants, ex. `10.0`, `18-alpine`)** : la version change sans que rien dans le dépôt ne le montre ni n'en déclenche la revue.
  - **Alternative écartée (empreinte, *digest*, en plus de la version)** : chaque correctif du système attendrait une montée dans le dépôt, et serait retardé sans gain ; l'application n'exige aucun niveau précis de correctifs, et Docker Scout refuse de toute façon une image qui ajoute une vulnérabilité corrigeable ([Publication des images Docker](publication-des-images-docker.md)).
- **`frontend` et `api` : `mcr.microsoft.com/dotnet/aspnet`, variante `noble-chiseled-extra`** : image runtime officielle, *chiseled* (ni shell ni gestionnaire de paquets, utilisateur non root, le strict nécessaire, [Déploiement](../../contraintes-techniques/architecture-deploiement/deploiement.md)), *extra* pour ICU, qu'exigent le tri et le formatage culturels ([Internationalisation](../socle/transverse/internationalisation.md)).
  - **Alternative écartée (`aspnet` sur Ubuntu complet)** : plus lourde, avec un shell et un gestionnaire de paquets sans usage, et `curl` à installer pour le bilan de santé.
- **Bilan de santé par une sonde .NET** ([Organisation du code](../organisation-du-code.md)), exécutée sur le runtime de l'image, faute de shell et de `curl` : l'équipe .NET n'a pas de recommandation officielle pour ces images, et propose cette approche (dépôt `dotnet/dotnet-docker`, demande n° 4300).
- **Contenu publié réduit à l'exécution** : bibliothèques natives de la seule architecture de l'image, ni symboles de débogage (les traces gardent les noms des méthodes, sans numéros de ligne), ni fichiers de développement ou de documentation.
- **Restauration avant les sources**, pour que la couche de la restauration ne change qu'avec les projets : `frontend` déclare donc qu'il exige les ressources web d'ASP.NET Core (le script de Blazor), que le SDK n'en déduit que des fichiers `.razor` présents à la restauration ; sans eux, l'image n'aurait aucun composant interactif (constaté sur l'environnement de validation, #325).
  - **Alternative écartée (sources copiées avant la restauration)** : la restauration, la plus longue étape, serait refaite à chaque modification d'une source.
- **Chiffrement GSS désactivé** dans la connexion à `db` : Npgsql le tente par défaut, sa bibliothèque est absente de l'image *chiseled*, et `db` n'est joignable que par le réseau Docker interne, par mot de passe.
- **`db`** : image officielle `postgres` ([Version de PostgreSQL](version-de-postgresql.md)).
