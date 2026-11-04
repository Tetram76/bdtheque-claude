---
type: Plan d'implémentation
title: 'Phase 2 — Contracts et API'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Phase 2 — Contracts et API

**Version** : `v0.0.3`, avec l'ajustement issu de la reprise des données (#102), réalisé pendant la phase.

Exposition du modèle de domaine via l'API : DTOs dans `Bdtheque.Contracts`, endpoints Minimal API dans `Bdtheque.Api` — administration (`/admin` : saisie, suppression selon [Suppression des entités](/fonctionnel/regles-metier/suppression-des-entites.md)) et consultation (`/catalog` : listes, navigation par initiale, recherche, fiches). Choix techniques : [Organisation de l'API](/choix-implementation/api/organisation-de-l-api.md) et sections suivantes.

Hors périmètre (phases dédiées ci-dessous) : statistiques, conversion de devises, estimations.

| Ticket | Titre |
| --- | --- |
| ~~#73~~ | Socle des endpoints et des réponses d'erreur |
| ~~#74~~ | Entrée de navigation par initiale et clé de tri des auteurs |
| ~~#75~~ | Alignement des suppressions en base sur les règles de suppression |
| ~~#76~~ | Administration des genres et des univers |
| ~~#77~~ | Administration des éditeurs et de leurs collections |
| ~~#78~~ | Administration des auteurs |
| ~~#79~~ | Administration des séries |
| ~~#103~~ | Ajustement de l'API des séries issu de la reprise des données |
| ~~#80~~ | Administration des albums |
| ~~#81~~ | Administration des éditions |
| ~~#82~~ | Téléversement et gestion des visuels d'édition |
| ~~#83~~ | Intentions d'achat |
| ~~#84~~ | Listes et recherche de la consultation |
| ~~#85~~ | Fiches détaillées de la consultation |

## Consolidation avant la Phase 3

Corrections issues de la revue complète de la Phase 2 (ticket parent #121), à réaliser avant d'ouvrir la Phase 3.

| Ticket | Titre |
| --- | --- |
| ~~#122~~ | Filtres des séries par auteur, éditeur et collection sur les données de la série |
| ~~#123~~ | Remplacement des contributions porté par le domaine |
| ~~#124~~ | Retrait des opérations du domaine supplantées par les opérations atomiques |
| ~~#125~~ | Borne de la mémoire des traitements simultanés de visuels |
| ~~#126~~ | Maîtrise des versions de collation ICU de la base |
| ~~#127~~ | Correction et validation de la documentation du code de la Phase 2 |
| ~~#134~~ | Optimisation des images Docker pour la production |
| ~~#136~~ | Journalisation de production et chargement des agrégats par EF Core |

## Ajustement issu de la maquette du dashboard

Répercussion dans le modèle de domaine et dans l'API de la date d'entrée dans la collection, ajoutée au `.speckit/` avec la maquette du dashboard. Les phases 1 et 2 étant validées, un seul ticket couvre les deux.

| Ticket | Titre |
| --- | --- |
| ~~#141~~ | Date d'entrée dans la collection dans le modèle de domaine et l'API |

## Ajustement issu de la maquette de l'administration

Répercussion dans le modèle de domaine et dans l'API du journal des modifications, ajouté au `.speckit/` avec la maquette de l'administration pour son bloc « Dernières modifications ». Les phases 1 et 2 étant validées, un seul ticket couvre les deux.

| Ticket | Titre |
| --- | --- |
| ~~#148~~ | Journal des modifications dans le modèle de domaine et l'API |

## Ajustement issu des maquettes des fiches de série et d'auteur

Répercussion dans l'API des données que les cartes d'album des fiches de série et d'auteur affichent (couverture, appréciation, intention d'achat, nombre d'albums d'une série), après que l'utilisateur a statué quelle édition fournit la couverture d'un album. La phase 2 étant validée, un seul ticket la couvre.

| Ticket | Titre |
| --- | --- |
| ~~#156~~ | Données des cartes d'album dans les fiches de série et d'auteur |

## Ajustement issu des fiches de consultation

Répercussion dans l'API de la règle qui réserve les fiches de consultation aux albums, aux séries et aux auteurs : éditions présentées par la fiche de leur album ; éditeurs et collections éditeur sans fiche ; genres et univers menant à la liste de leurs séries et albums présentée comme la bibliographie d'un auteur. La phase 2 étant validée, un seul ticket la couvre.

| Ticket | Titre |
| --- | --- |
| ~~#157~~ | Fiches de consultation réservées aux albums, séries et auteurs |

## Ajustement issu de la définition de la recherche avancée

Répercussion dans le modèle de domaine et dans l'API de la recherche avancée ([Recherche avancée](/fonctionnel/recherche-avancee/recherche-avancee.md)) : recherche de toute entité consultable sur toute combinaison de critères portant sur les données du modèle métier, et recherches enregistrées, avec leurs variables. Les phases 1 et 2 étant validées, un seul ticket couvre les deux (ticket parent #162), découpé en sous-tickets, le chantier étant trop vaste pour une seule Pull Request. Les critères sur des données que calculent des phases ultérieures (montants en euros, valeur estimée : phase 4 ; tomes manquants, sortie estimée : phase 5) en sont exclus, et rejoignent la recherche avancée avec ces phases.

| Ticket | Titre |
| --- | --- |
| ~~#168~~ | Moteur de la recherche avancée en consultation |
| ~~#169~~ | Recherches enregistrées : administration et rejeu en consultation |
| ~~#170~~ | Blocage de la suppression des fiches désignées par une recherche enregistrée |
| ~~#171~~ | Comparaison entre deux données dans la recherche avancée |

## Ajustement issu des maquettes des listes et saisies de l'administration

Répercussion dans le modèle de domaine et dans l'API de la règle qui réserve une intention d'achat sur l'album à un album sans édition, ajoutée au `.speckit/` avec la page des éditions et intentions d'achat d'un album ([Intention d'achat](/fonctionnel/regles-metier/intention-d-achat.md)). Les phases 1 et 2 étant validées, un seul ticket couvre les deux.

| Ticket | Titre |
| --- | --- |
| ~~#185~~ | Intention d'achat sur un album réservée à un album sans édition |

## Ajustement issu de la mesure d'audience de l'interface web

Répercussion dans le modèle de domaine et dans l'API de la mesure d'audience de l'interface web ([Mesure d'audience de l'interface web](/fonctionnel/mesure-audience.md)) : visites, utilisations des fonctions et compteurs d'audience mensuels, leur conservation et la lecture de leurs indicateurs. Les phases 1 et 2 étant validées, un seul ticket couvre les deux ; la part de la Phase 3, pas encore découpée, y est notée.

| Ticket | Titre |
| --- | --- |
| ~~#186~~ | Mesure d'audience de l'interface web dans le modèle de domaine et l'API |

## Ajustement issu de l'exploitation des technologies retenues

Revue de l'existant au regard de la contrainte d'exploitation au mieux des technologies retenues ([Exploitation des technologies retenues](/contraintes-techniques/exploitation-des-technologies-retenues.md)), pour en identifier les chantiers d'amélioration, chacun avec son ticket. Les phases 0 à 2 étant validées, un seul ticket les couvre.

| Ticket | Titre |
| --- | --- |
| ~~#189~~ | Revue de l'existant au regard de l'exploitation des technologies retenues |

Chantiers d'amélioration identifiés par cette revue, chacun réalisé par sa propre Pull Request.

| Ticket | Titre |
| --- | --- |
| ~~#219~~ | Horloge de l'application pour les dates renseignées automatiquement |
| ~~#220~~ | Pipeline CI et outillage .NET : SDK, restauration verrouillée, cache NuGet, couverture et projets de test |
| ~~#221~~ | Paramétrage des valeurs de la recherche avancée par EF Core |

## Ajustement issu de la sécurisation SSL/TLS par le reverse proxy du NAS

Répercussion dans le frontend de la sécurisation SSL/TLS assurée par le reverse proxy du NAS Synology ([Hébergement](/contraintes-techniques/architecture-deploiement/hebergement.md)) : une même image derrière le reverse proxy et en déploiement interne en HTTP, adresse publique déclarée par le déploiement derrière le reverse proxy. La configuration du frontend relevant de la Phase 0, validée, un seul ticket la couvre ; il précède la connexion de l'administration (#199), qui en dépend.

| Ticket | Titre |
| --- | --- |
| ~~#249~~ | Frontend derrière le reverse proxy du NAS Synology, qui assure la sécurisation SSL/TLS |

## Ajustement issu de l'hébergement de plusieurs versions sur le NAS

Répercussion dans le déploiement de la récupération des données de la production par une pré-version ([Hébergement](/contraintes-techniques/architecture-deploiement/hebergement.md)). Le déploiement relevant de la Phase 0, validée, un seul ticket la couvre ; sans objet avant la première mise en production (`1.0.0`), il est à réaliser avant la première pré-version qui la suivra.

| Ticket | Titre |
| --- | --- |
| #267 | Récupération des données de production par une pré-version sur le NAS |
