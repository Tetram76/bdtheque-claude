# Contraintes Techniques

Ce dossier recense les **contraintes techniques imposées** à l'application et à son déploiement — par l'utilisateur, des normes/lois, ou une réalité externe non négociable (système existant à migrer, environnement d'hébergement donné, etc.). Il peut aussi mentionner, explicitement, des **non-contraintes** (ouvertures de réflexion voulues par l'utilisateur). L'agent n'y ajoute rien de sa propre initiative : ses propres choix techniques, délibérés entre plusieurs options ou non, sont documentés dans `choix-implementation/`.
Il ne concerne pas non plus les aspects gestion de projet (repo, branches, processus de développement, etc.) — ceux-ci relèvent de `gestion-projet/`.

| Fichier | Contenu |
| --- | --- |
| `index.md` | Stack cible, exploitation des technologies retenues, suivi des versions et des vulnérabilités, licences. |
| `architecture-deploiement.md` | Architecture des tiers, indépendance de l'outil pour agents IA, déploiement, hébergement, stockage des visuels, compatibilité multi-supports, authentification, sources de données externes, statistiques du dashboard. |
| `reprise-donnees.md` | Application existante (client lourd) et reprise de ses données. Lu seulement pour la reprise des données (`AGENTS.md`). |

---

## Stack cible

| Élément | Valeur |
| --- | --- |
| Langage | C# / .NET |
| Frontend | Interface web (technologie : cf. `choix-implementation/socle.md` § Frontend) |
| Backend API | ASP.NET Core (style d'API : cf. `choix-implementation/api.md` § Organisation de l'API) |
| ORM | EF Core |
| Base de données | PostgreSQL |

## Exploitation des technologies retenues

Toute technologie retenue (langage, framework, bibliothèque, outil, base de données, Docker, CI…) est **utilisée au mieux de la version retenue** : ses fonctionnalités, syntaxes et API sont employées dès qu'elles améliorent l'élément concerné (performances, lisibilité, sécurité, concision, maintenabilité…). La règle vaut pour **tous les éléments de l'application**, et non pour le seul code : schéma et requêtes de la base, Dockerfiles et fichier Compose, workflows CI, configuration, scripts… Se limiter à ce que permettait une version antérieure, alors que la version retenue offre mieux, n'a pas de sens (ex. retenir la version N d'un langage, mais n'en écrire que les syntaxes de la version N-1).

Cette règle implique de **prendre connaissance des possibilités** de chaque technologie à la version retenue (documentation officielle, notes de version) : reproduire des modèles trouvés ailleurs (exemples, extraits, réponses en ligne, habitudes) ne suffit pas, surtout lorsqu'ils sont **datés**, écrits pour une version antérieure.

Lors d'une **montée de version** d'une technologie retenue, une **revue de l'existant** est menée pour identifier les chantiers d'amélioration que permet la nouvelle version.

## Suivi des versions et des vulnérabilités

Ces règles valent pour **toutes les technologies utilisées** : bibliothèques, SDK, images Docker, base de données, outils, actions du CI…

- **Suivi des versions** : les nouvelles versions de chaque technologie sont suivies.
- **Surveillance active des vulnérabilités** : les alertes de sécurité de chaque technologie sont surveillées activement, que le dépôt fasse ou non l'objet de travaux.
- **Correction au plus tôt** : une vulnérabilité signalée est corrigée dès que possible, c'est-à-dire dès qu'une correction est disponible.
- **Pas de blocage** : une vulnérabilité qu'aucune correction disponible ne permet encore de corriger ne bloque pas le projet ni la poursuite du développement.
- **Pas de mise à jour pour elle-même** : appliquer une mise à jour impose la revue de l'existant (§ Exploitation des technologies retenues), pour que le dépôt profite de la nouvelle version.
- **Souhait** : aucune Pull Request de mise à jour n'est ouverte automatiquement hors brouillon.

## Licences

- **Aucune licence payante** autorisée, pour quelque composant que ce soit (frameworks, bibliothèques, outils, bases de données, images Docker, etc.).
- Licences acceptées : **open source** (MIT, Apache 2.0, GPL, etc.) et **Community Edition** gratuites.
- Pour les projets open source : n'utiliser que des projets **reconnus, activement maintenus et largement adoptés** par la communauté. Exclure les projets confidentiels, abandonnés ou à faible adoption.
