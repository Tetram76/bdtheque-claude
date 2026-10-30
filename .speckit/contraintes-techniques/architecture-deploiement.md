# Architecture, déploiement et exploitation

Architecture des tiers, indépendance de l'outil pour agents IA, déploiement, hébergement (plusieurs versions sur le même NAS) et sécurisation SSL/TLS, stockage des visuels, compatibilité multi-supports, authentification, sources de données externes et statistiques du dashboard.

---

## Architecture des tiers

L'application repose sur un **socle de 3 conteneurs Docker**, fondation à laquelle d'autres conteneurs peuvent s'ajouter (ex. l'outil pour agents IA, cf. § Indépendance de l'outil pour agents IA) :

| Conteneur | Rôle |
| --- | --- |
| `frontend` | UI et rendu des pages |
| `api` | API ASP.NET Core — logique métier, accès données, ML |
| `db` | PostgreSQL — persistance |

Les images de base des conteneurs sont des choix d'implémentation : cf. `choix-implementation/deploiement.md` § Images de base des conteneurs.

Seul le conteneur `frontend` est exposé : `api` n'est jamais exposé, le `frontend` l'appelle via HTTP interne (réseau Docker). Le conteneur `api` est le seul à accéder à `db`.

## Indépendance de l'outil pour agents IA

L'implémentation de l'outil pour agents IA (`fonctionnel/agents-ia.md` § Interface pour agents IA) doit être **indépendante de l'API** : déployer l'API ne doit pas impliquer de déployer l'outil pour agents IA.

**Non-contraintes** (ouvertures de réflexion explicites, laissées au choix de l'agent) :

- Le déploiement de l'outil sous forme de **conteneur**, comme le reste de l'application, est souhaitable mais **non imposé**.
- L'**exposition** de l'outil au-delà de `frontend` (seul conteneur exposé, cf. § Architecture des tiers) est **à envisager** si la contrainte d'exposition impose une surcharge disproportionnée, mais **non souhaitée** dans l'idéal.

## Déploiement

- Architecture **n-tiers avec isolation stricte** : chaque tier est déployé dans un **conteneur Docker dédié**.
- Un tier = un conteneur (pas de cohabitation de responsabilités dans un même conteneur).
- Orchestration via **Docker Compose**, compatible avec Synology Container Manager.
- Les **images Docker** de l'application sont publiées sur **Docker Hub**, sous le compte [`tetram76`](https://hub.docker.com/repositories/tetram76), avec pour nom de projet **`bdtheque`** (et non le nom du dépôt, `bdtheque-claude`). Les dépôts Docker Hub sont **privés**. Une image est publiée **quotidiennement** à partir de la branche `main`, sous le tag **`nightly`**, sauf si `main` n'a pas changé depuis la précédente publication quotidienne (aucune image n'est alors republiée) ; cette publication garantit qu'il n'existe qu'**une seule image `nightly`** sur Docker Hub. En dehors de cette publication quotidienne, les images ne sont publiées qu'**à la demande** (jamais à chaque fusion).
- Le contenu des images publiées est **optimisé pour la production**, en taille comme en contenu : elles ne contiennent que ce qui sert à l'exécution de l'application.
- **Vulnérabilités** : une modification n'introduit dans les images aucune vulnérabilité **déjà connue et corrigeable**, et surtout une image qui en introduit une n'est **jamais publiée**. La surveillance des vulnérabilités découvertes après publication est assurée par **Docker Hub**, sur les images publiées.

## Hébergement

- L'application est hébergée sur un **NAS Synology RS1221+** (processeur AMD Ryzen V1500B, architecture x86_64).
- Le déploiement Docker doit être compatible avec l'environnement Docker fourni par Synology (Container Manager).
- **Sécurisation SSL/TLS** : le chiffrement des échanges avec le navigateur (HTTPS, certificats) est assuré par le **reverse proxy du NAS Synology**, placé devant le conteneur `frontend` : il reçoit les requêtes en HTTPS et les relaie en HTTP au `frontend`, sans rediriger le HTTP vers le HTTPS. Le `frontend` ne gère pas lui-même la sécurisation SSL/TLS : il ne sert que du HTTP.
- **Accès direct en HTTP** : pendant le développement, les versions à valider ou à tester (validation fonctionnelle, versions candidates, RC, versions de développement) sont déployées **en interne**, sans reverse proxy et sans accès depuis l'extérieur : le `frontend` y est accessible directement en HTTP.
- **Plusieurs versions sur le même NAS** : le NAS qui héberge la version en **production** héberge aussi les **pré-versions** : la version candidate (RC) et une version de développement (alpha ou bêta, termes équivalents), appelée à devenir la version candidate suivante. À un instant donné, le NAS peut donc faire tourner jusqu'à **trois versions** de l'application en même temps.
- **Données d'une pré-version** : la mise en place d'une pré-version permet, et idéalement réalise elle-même, la **récupération des données de la production**.
  - **Non-contrainte** (ouverture de dernier recours) : si la récupération l'exige, la pré-version peut recevoir un accès **en lecture seule** aux données de la production (base et visuels), par exception à § Architecture des tiers (seul `api` accède à `db`). Une récupération qui s'en passe reste préférable.

## Stockage des visuels

- Les fichiers visuels (couvertures, planches, etc.) sont stockés sur un **volume Docker** monté sur le NAS.
- Le chemin de montage est configurable via variable d'environnement Docker Compose.

## Compatibilité multi-supports

- L'application doit être **responsive** : utilisable sur PC, tablette et smartphone.
- Le rendu et la navigation doivent s'adapter à toutes les tailles d'écran (approche *mobile-first*).

## Authentification

- L'accès à la partie Administration est protégé par un **compte administrateur unique** (login + mot de passe).
- Pas d'authentification sur la partie Consultation (accès public).

## Sources de données externes

Les données récupérées auprès de sources externes (taux de change Frankfurter, coefficients d'inflation INSEE, cf. `fonctionnel/valorisation.md` § Gestion des devises) sont **mises en cache** par l'application, pour ne pas être récupérées à chaque utilisation.

## Statistiques du dashboard

Seules les données des statistiques **présentées** lors d'un affichage du dashboard (les quatre blocs toujours présents et les statistiques tirées, `fonctionnel/page-accueil.md` § Page d'accueil) sont calculées et chargées : le tirage précède le chargement, et aucune donnée n'est chargée pour une statistique qui ne sera pas présentée.
