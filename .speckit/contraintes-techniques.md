# Contraintes Techniques

Ce fichier recense les **contraintes techniques imposées** à l'application et à son déploiement — par l'utilisateur, des normes/lois, ou une réalité externe non négociable (système existant à migrer, environnement d'hébergement donné, etc.). Il peut aussi mentionner, explicitement, des **non-contraintes** (ouvertures de réflexion voulues par l'utilisateur). L'agent n'y ajoute rien de sa propre initiative : ses propres choix techniques, délibérés entre plusieurs options ou non, sont documentés dans `choix-implementation.md`.
Il ne concerne pas non plus les aspects gestion de projet (repo, branches, outillage dev, etc.) — ceux-ci relèvent de `gestion-projet.md`.

---

## Application existante (référence)

| Élément | Valeur |
| --- | --- |
| Langage | Delphi (version inconnue) |
| Base de données | Firebird 1.5 |
| Type | Client lourd (desktop) |

Cette application est la **référence fonctionnelle** : le périmètre de la réécriture web doit couvrir ses fonctionnalités.

## Stack cible

| Élément | Valeur |
| --- | --- |
| Langage | C# / .NET |
| Frontend | Interface web (technologie : cf. `choix-implementation.md` § Frontend) |
| Backend API | ASP.NET Core (style d'API : cf. `choix-implementation.md` § Organisation de l'API) |
| ORM | EF Core |
| Base de données | PostgreSQL |

## Architecture des tiers

L'application est découpée en **3 conteneurs Docker** :

| Conteneur | Rôle |
| --- | --- |
| `frontend` | UI et rendu des pages |
| `api` | API ASP.NET Core — logique métier, accès données, ML |
| `db` | PostgreSQL — persistance |

Les images de base des conteneurs sont des choix d'implémentation : cf. `choix-implementation.md` § Images de base des conteneurs.

Seul le conteneur `frontend` est exposé : `api` n'est jamais exposé, le `frontend` l'appelle via HTTP interne (réseau Docker). Le conteneur `api` est le seul à accéder à `db`.

## Indépendance de l'outil pour agents IA

L'implémentation de l'outil pour agents IA (`fonctionnel.md` § Interface pour agents IA) doit être **indépendante de l'API** : déployer l'API ne doit pas impliquer de déployer l'outil pour agents IA.

**Non-contraintes** (ouvertures de réflexion explicites, laissées au choix de l'agent) :

- Le déploiement de l'outil sous forme de **conteneur**, comme le reste de l'application, est souhaitable mais **non imposé**.
- L'**exposition** de l'outil au-delà de `frontend` (seul conteneur exposé, cf. § Architecture des tiers) est **à envisager** si la contrainte d'exposition impose une surcharge disproportionnée, mais **non souhaitée** dans l'idéal.

## Déploiement

- Architecture **n-tiers avec isolation stricte** : chaque tier est déployé dans un **conteneur Docker dédié**.
- Un tier = un conteneur (pas de cohabitation de responsabilités dans un même conteneur).
- Orchestration via **Docker Compose**, compatible avec Synology Container Manager.

## Licences

- **Aucune licence payante** autorisée, pour quelque composant que ce soit (frameworks, bibliothèques, outils, bases de données, images Docker, etc.).
- Licences acceptées : **open source** (MIT, Apache 2.0, GPL, etc.) et **Community Edition** gratuites.
- Pour les projets open source : n'utiliser que des projets **reconnus, activement maintenus et largement adoptés** par la communauté. Exclure les projets confidentiels, abandonnés ou à faible adoption.

## Migration des données

- Des données existantes sont stockées dans la base Firebird 1.5 de l'application client lourd.
- Une **migration de données** depuis Firebird vers la base cible devra être possible lorsque la nouvelle application sera suffisamment mature.
- La conception du modèle de données cible doit tenir compte de cette migration future : préserver la sémantique des données existantes et ne pas rendre la migration inutilement complexe.
- La migration pourra être **incrémentale** (imports successifs et partiels) : l'outil de migration devra **fusionner** les données importées avec les données déjà présentes dans la base cible lorsqu'il y a correspondance (pas de doublons, mise à jour des éléments existants).
- La migration est **unidirectionnelle** : aucun retour en arrière, aucune synchronisation vers l'application client lourd.
- Un outil ou script de migration dédié devra être prévu le moment venu.

## Hébergement

- L'application est hébergée sur un **NAS Synology**.
- Le déploiement Docker doit être compatible avec l'environnement Docker fourni par Synology (Container Manager).

## Compatibilité multi-supports

- L'application doit être **responsive** : utilisable sur PC, tablette et smartphone.
- Le rendu et la navigation doivent s'adapter à toutes les tailles d'écran (approche *mobile-first*).

## Authentification

- L'accès à la partie Administration est protégé par un **compte administrateur unique** (login + mot de passe).
- Pas d'authentification sur la partie Consultation (accès public).

## Stockage des visuels

- Les fichiers visuels (couvertures, planches, etc.) sont stockés sur un **volume Docker** monté sur le NAS.
- Le chemin de montage est configurable via variable d'environnement Docker Compose.
