---
type: Choix d'implémentation
title: 'Images de base des conteneurs'
description: 'Images de base épinglées, aspnet chiseled-extra, sonde de santé, contenu publié, connexion à db.'
applies_to:
  - 'docker/api/**'
  - 'docker/frontend/**'
  - 'docker/validation-data/Dockerfile'
  - 'docker/validation-docs/Dockerfile'
  - 'src/Bdtheque.HealthProbe/**'
  - 'docker-compose.yml'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-19T20:25:33Z }
---

# Images de base des conteneurs

Les conteneurs sont imposés ([Architecture des tiers](../../contraintes-techniques/architecture-deploiement/architecture-des-tiers.md)), le choix des images est libre dans le respect des [Licences](../../contraintes-techniques/licences.md) et de l'environnement Docker de Synology ([Hébergement](../../contraintes-techniques/architecture-deploiement/hebergement.md)).

- **Versions épinglées** : chaque image de base est désignée par la version exacte de son composant (.NET, PostgreSQL, Alpine), qui ne change donc jamais sans une montée visible, revue avant sa fusion ([Mises à jour des dépendances](../../gestion-projet/depot/mises-a-jour-des-dependances.md)). Sous ce tag, l'éditeur reconstruit l'image pour les correctifs de son système tant qu'il la maintient (documentation de `dotnet/dotnet-docker`, *supported tags*).
  - **Alternative écartée (tags flottants, ex. `10.0`, `18-alpine`)** : la version change sans que rien dans le dépôt ne le montre ni n'en déclenche la revue.
  - **Alternative écartée (empreinte, *digest*, en plus de la version)** : chaque correctif du système attendrait une montée dans le dépôt, et serait retardé sans gain ; l'application n'exige aucun niveau précis de correctifs, et Docker Scout refuse de toute façon une image qui contient une vulnérabilité corrigeable par le dépôt ([Publication des images Docker](publication-des-images-docker.md)).
- **`frontend` et `api` : `mcr.microsoft.com/dotnet/aspnet`, variante `resolute-chiseled-extra`** : image runtime officielle, *chiseled* (ni shell ni gestionnaire de paquets, utilisateur non root, le strict nécessaire, [Déploiement](../../contraintes-techniques/architecture-deploiement/deploiement.md)), *extra* pour ICU, qu'exigent le tri et le formatage culturels ([Internationalisation](../socle/transverse/internationalisation.md)). Les images que le dépôt construit sur ce runtime (chargement des données et documentation de l'environnement de validation, [Environnement de validation fonctionnelle](environnement-de-validation-fonctionnelle.md)) reprennent la même image, ce que vérifie un test du dépôt.
  - **Ubuntu 26.04 LTS (*resolute*)**, la plus récente version LTS d'Ubuntu que .NET 10 prend en charge (dépôt `dotnet/core`, *release-notes/10.0/supported-os.md*) : ses paquets reçoivent des corrections qu'Ubuntu ne publie pas pour la version précédente (ex. ICU, CVE-2025-5222).
  - **Alternative écartée (variante `noble`, Ubuntu 24.04 LTS)** : son ICU 74 reste vulnérable sans correction publiée par Ubuntu pour cette version, alors que la variante *resolute* est publiée pour la même version de .NET.
  - **Étapes de construction sur l'image du SDK par défaut** (`sdk:<version>`, Ubuntu 24.04 pour .NET 10, documentation .NET, *Default .NET images use Ubuntu*) : la sortie publiée ne dépend pas de la distribution qui la construit (code intermédiaire, et bibliothèques natives tirées des packages pour l'architecture de l'image), et l'étape de construction n'entre pas dans l'image produite.
    - **Alternative écartée (`sdk:<version>-resolute`)** : une variante de plus à tenir alignée, sans rien changer à l'image produite.
  - **Alternative écartée (`aspnet` sur Ubuntu complet)** : plus lourde, avec un shell et un gestionnaire de paquets sans usage, et `curl` à installer pour le bilan de santé.
- **Bilan de santé par une sonde .NET** ([Organisation du code](../organisation-du-code.md)), exécutée sur le runtime de l'image, faute de shell et de `curl` : l'équipe .NET n'a pas de recommandation officielle pour ces images, et propose cette approche (dépôt `dotnet/dotnet-docker`, demande n° 4300).
- **Contenu publié réduit à l'exécution** : bibliothèques natives de la seule architecture de l'image, ni symboles de débogage (les traces gardent les noms des méthodes, sans numéros de ligne), ni fichiers de développement ou de documentation.
- **Restauration avant les sources**, pour que la couche de la restauration ne change qu'avec les projets : `frontend` déclare donc qu'il exige les ressources web d'ASP.NET Core (le script de Blazor), que le SDK n'en déduit que des fichiers `.razor` présents à la restauration ; sans eux, l'image n'aurait aucun composant interactif (constaté sur l'environnement de validation, #325).
  - **Alternative écartée (sources copiées avant la restauration)** : la restauration, la plus longue étape, serait refaite à chaque modification d'une source.
- **Chiffrement GSS désactivé** dans la connexion à `db` : Npgsql le tente par défaut, sa bibliothèque est absente de l'image *chiseled*, et `db` n'est joignable que par le réseau Docker interne, par mot de passe.
- **`db`** : image officielle `postgres` ([Version de PostgreSQL](version-de-postgresql.md)).
