---
type: Règle métier
title: 'Périmètre de la consultation'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Périmètre de la consultation

La base est à la fois l'**inventaire de la collection** et une **base documentaire**. L'aspect documentaire porte **uniquement** sur les **albums**, les **séries**, les **éditeurs** et leurs **collections éditeur**, et les **auteurs** : ces fiches peuvent exister sans rien de possédé. Les **éditions** et leurs **visuels**, eux, ne sont jamais documentaires : une édition est soit **possédée** (elle fait partie de la collection), soit l'objet d'une **intention d'achat** (cf. [Appartenance à la collection](/fonctionnel/regles-metier/appartenance-a-la-collection.md)). En conséquence, dans la partie **Consultation** :

- Tout élément est **consultable et trouvable dès qu'il existe en base**, qu'il relève ou non de la collection : albums sans édition possédée, séries, auteurs, éditeurs, éditions visées par une intention d'achat, etc. Cette règle s'applique aux listes, à la recherche, aux fiches détaillées et à la navigation inter-entités.
- Dans une fiche, tout élément connu est visible (ex. tous les albums d'une série, toute la bibliographie d'un auteur, toutes les éditions d'un album).
- L'**appartenance à la collection** est **signalée visuellement** sur les albums et les éditions, pour que l'inventaire reste lisible au sein de la base documentaire.
- Les **statistiques** portent exclusivement sur la **collection** (albums et éditions possédés), jamais sur l'ensemble de la base.
- Un **filtre** permettra de limiter l'affichage de la consultation à la collection (fonctionnement à préciser).
