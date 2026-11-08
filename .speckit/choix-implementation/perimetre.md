---
type: Périmètre
title: 'Choix d''Implémentation'
description: 'Ce que recense choix-implementation/ : choix de l''agent, leur raison et les alternatives écartées, rien de ce que le code montre ; remis en cause s''il le faut.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T16:00:00Z }
---

# Choix d'Implémentation


Ce dossier recense les choix d'implémentation retenus par l'agent **de sa propre initiative** (architecture, bibliothèque, algorithme non imposé, etc.), qu'il y ait eu ou non délibération explicite entre plusieurs options. `contraintes-techniques/` reste réservé aux contraintes **imposées** — par l'utilisateur, des normes/lois, ou une réalité externe non négociable : l'agent n'y ajoute rien de sa propre initiative.

Ces choix ne sont pas imposés : ils peuvent être remis en cause si une meilleure option apparaît ou si une nouvelle contrainte le justifie (cf. [Prise de décision](../gestion-projet/gouvernance/prise-de-decision.md)).

## Contenu d'un choix

Ce dossier **complète ce que le code, ou les éléments concernés (configuration, Dockerfiles, workflows, schéma…), n'expliquent pas** ; il n'est pas le récit de ce qu'on peut y découvrir. Pour chaque choix, un concept donne seulement :

- le **choix retenu**, en peu de mots ;
- **pourquoi** il a été retenu ;
- les **alternatives écartées**, et pourquoi chacune l'a été (`**Alternative écartée (…)** : …`).

Le détail de la mise en œuvre (classes, méthodes, paramètres, étapes, fichiers) n'y figure pas : il se lit dans le code lui-même, et sa copie ne ferait que vieillir à côté de lui.


---
