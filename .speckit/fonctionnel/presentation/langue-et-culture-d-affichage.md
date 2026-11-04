---
type: Règle métier
title: 'Langue et culture d''affichage'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Langue et culture d'affichage

La **culture d'affichage** (ex. français de France), et non une simple langue, est un paramètre de l'application : ce choix pilote la langue des textes de l'interface, mais aussi le **formatage des dates, nombres et montants** et l'**ordre de tri** des listes et résultats de recherche (voir « Tri et navigation par initiale » ci-dessous).

La culture s'applique à tout texte **au moment où il est présenté** : un texte que l'application produit à partir de données enregistrées (ex. le journal des modifications, cf. [Entrée du journal des modifications](/modele-metier/entree-du-journal-des-modifications.md)) n'est jamais figé dans la culture en vigueur lors de leur enregistrement.

**Fuseau horaire** : toute présentation à l'utilisateur et toute interaction avec lui suivent le **fuseau horaire de son navigateur**, celui en cours au moment de la présentation ou de l'interaction (un navigateur qui change de fuseau pendant une visite, ex. lors d'un voyage, suit son nouveau fuseau) : dates et heures présentées (ex. dates de création et de modification d'une fiche), comme dates et heures saisies ou recherchées (une date et heure est comparée par son jour dans ce fuseau, cf. [Comparaisons](/fonctionnel/recherche-avancee/comparaisons.md)). Côté serveur, les dates et heures sont enregistrées et traitées en **temps universel**.

Seule la culture **française** est proposée actuellement ; l'application est conçue pour pouvoir en proposer d'autres ultérieurement sans refonte.
