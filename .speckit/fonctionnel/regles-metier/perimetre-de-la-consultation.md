---
type: Règle métier
title: 'Périmètre de la consultation'
description: 'Base documentaire et inventaire : tout est consultable, l''appartenance est signalée, les statistiques ne portent que sur la collection, filtre de possession propre à chaque liste, conservé le temps de la liste.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-23T12:30:00Z }
---

# Périmètre de la consultation

La base est à la fois l'**inventaire de la collection** et une **base documentaire**. L'aspect documentaire porte **uniquement** sur les **albums**, les **séries**, les **éditeurs** et leurs **collections éditeur**, et les **auteurs** : ces fiches peuvent exister sans rien de possédé. Les **éditions** et leurs **visuels**, eux, ne sont jamais documentaires : une édition est soit **possédée** (elle fait partie de la collection), soit l'objet d'une **intention d'acquisition** (cf. [Appartenance à la collection](appartenance-a-la-collection.md)). En conséquence, dans la partie **Consultation** :

- Tout élément est **consultable et trouvable dès qu'il existe en base**, qu'il relève ou non de la collection : albums sans édition possédée, séries, auteurs, éditeurs, éditions visées par une intention d'acquisition, etc. Cette règle s'applique aux listes, à la recherche, aux fiches détaillées et à la navigation inter-entités.
- Dans une fiche, tout élément connu est visible (ex. tous les albums d'une série, toute la bibliographie d'un auteur, toutes les éditions d'un album).
- L'**appartenance à la collection** est **signalée visuellement** sur les albums et les éditions, pour que l'inventaire reste lisible au sein de la base documentaire.
- Les **statistiques** portent exclusivement sur la **collection** (albums et éditions possédés), jamais sur l'ensemble de la base.
- Un **filtre** permet de limiter l'affichage de la consultation à la collection. C'est un filtre **propre à chaque liste** de la consultation : il ne vaut que pour la liste où il est choisi, et les autres listes restent sur toute la base (décision de l'utilisateur, #603). Dans la liste des albums, il porte sur la possession, avec ses états ([Appartenance à la collection](appartenance-a-la-collection.md)), « Possédé » limitant la liste à la collection ; ce qu'il retient dans chacune des autres listes est précisé avec elle. Le choix n'est conservé que le temps de la liste : il suit sa navigation par initiale, sa pagination et le retour arrière du navigateur, mais l'entrée de la liste dans le menu de la consultation la rouvre sur toute la base (décision de l'utilisateur, #603). Les fiches, qui ne sont pas des listes, n'en dépendent pas : elles restent dans le contexte de la base documentaire.
