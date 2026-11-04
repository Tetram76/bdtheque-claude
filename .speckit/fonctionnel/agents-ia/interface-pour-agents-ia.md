---
type: Règle métier
title: 'Interface pour agents IA'
description: 'Outil pour agents IA : aide à la saisie, recherche et consultation, analyses et statistiques (Phase 6).'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Interface pour agents IA


Interface pour agents IA : fonctionnalités, accès par jeton et suivi de l'utilisation.

---

L'application met à disposition d'un **agent IA** un outil lui permettant d'interagir avec elle, en complément de l'interface web. Cet outil n'est pas lié à une fonction du frontend : il est utilisable par **n'importe quel agent conversationnel**. L'intégration d'un **agent IA dans le frontend** est par ailleurs **envisagée** ; rien n'est statué à ce stade (YAGNI). L'outil offre trois familles de fonctionnalités :

- **Aide à la saisie** : assister l'utilisateur dans la saisie des données de l'application. L'agent peut aussi **écrire en base**, mais uniquement **à la demande de l'utilisateur**.
  - **Fonction principale attendue** : répondre à la demande « à partir de cet ISBN, compléter ou saisir la fiche de l'album et de l'édition (visuels inclus), de la série si l'album en fait partie, et des auteurs ». L'**éditeur** et la **collection éditeur** de l'édition font aussi partie des données à saisir ou compléter.
  - Les données proviennent de **plusieurs sources externes** (jamais d'une seule). La **source principale** reste le **site de l'éditeur** ; le **catalogue général de la BnF** (API SRU, cf. [Structure de l'application](/fonctionnel/structure-de-l-application.md), Administration) est une **source d'information complémentaire**. Les autres sources seront arrêtées à l'ouverture de la phase d'implémentation.
- **Recherche et consultation** : rechercher et consulter les fiches, avec la même étendue que la partie Consultation (cf. [Périmètre de la consultation](/fonctionnel/regles-metier/perimetre-de-la-consultation.md)), **recherche avancée comprise** (cf. [Recherche avancée](/fonctionnel/recherche-avancee/recherche-avancee.md)) : toute entité recherchée, sur toute combinaison de critères. Les recherches enregistrées n'y sont pas proposées, ni pour leur rejeu ni pour leur gestion (aucun enregistrement de recherche, même avec un jeton qui donne accès à l'administration) : l'agent construit lui-même les recherches dont il a besoin.
- **Analyses et statistiques** : produire des analyses et statistiques sur la collection, avec les mêmes règles que les statistiques de l'application (portée sur la collection uniquement, montants en euro, cf. [Gestion des devises](/fonctionnel/valorisation/gestion-des-devises.md)).
