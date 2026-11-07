---
type: Règle métier
title: 'Entités recherchées'
description: 'Entités recherchées et comportement de leurs résultats.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Entités recherchées

La recherche n'est pas limitée à certaines entités : elle porte sur toute entité dont la consultation présente les données (cf. [Périmètre de la consultation](../regles-metier/perimetre-de-la-consultation.md)), c'est-à-dire les albums, éditions, séries, auteurs, éditeurs, collections éditeur, genres, univers, visuels d'édition, intentions d'achat et contributions. Le journal des modifications, consultable dans la seule administration ([Entrée du journal des modifications](../../modele-metier/entree-du-journal-des-modifications.md)), le paramétrage de l'application et les recherches enregistrées n'en font pas partie.

Les résultats sont présentés comme les entités de leur type le sont dans la consultation, et chacun se comporte comme la consultation le prévoit pour son entité (cf. [Structure de l'application](../structure-de-l-application.md)) : il mène à la fiche d'un album, d'une série ou d'un auteur ; à la fiche de l'album d'une édition, d'un visuel ou d'une intention d'achat ; à la fiche de l'album ou de la série qui porte une contribution ; au site web d'un éditeur, s'il est renseigné ; à la liste d'un genre ou d'un univers. Un éditeur sans site web et une collection éditeur ne sont pas des liens.
