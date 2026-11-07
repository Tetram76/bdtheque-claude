---
type: Référence visuelle
title: 'Maquettes'
description: 'Maquettes HTML autonomes, visionneuse, gestion et publication systématique sur claude.ai.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Maquettes

Les maquettes sont des **pages HTML autonomes**, conservées dans [`maquettes/`](../maquettes) : elles s'ouvrent dans un navigateur, depuis le dépôt, sans autre ressource externe que leurs polices (Google Fonts). Leurs données sont fictives.

- [`maquettes/index.html`](../maquettes/index.html) les liste, en deux sections : **Consultation** et **Administration**.
- Chaque écran a **son dossier**, dont la page `index.html` est une **visionneuse** : elle présente ses versions **côte à côte**, une par thème en lice, à la largeur d'un écran de bureau (1440 px) ou d'un smartphone (414 px, largeur en pixels CSS d'un Pixel 10 XL), avec leurs réglages (exemple présenté, menu ouvert…). Chaque version peut aussi être agrandie, seule en pleine largeur : elle défile alors dans la hauteur de la fenêtre, ce qui montre ce qui reste figé au défilement.
- Chaque maquette est ainsi **déclinée dans le visuel de chaque thème** en lice. Le fichier de chaque écran indique ses sources.
- Le moteur d'affichage des maquettes et la visionneuse sont communs à tous les écrans ([`maquettes/shared/`](../maquettes/shared)).

## Gestion des maquettes

- **Source de vérité : le dépôt.** Les maquettes sont conçues et modifiées dans [`maquettes/`](../maquettes), et nulle part ailleurs.
- **Publication systématique sur claude.ai**, pour en faciliter la visualisation et permettre à l'utilisateur de **contrôler chaque modification** : le dossier `maquettes/` est publié tel quel comme artifact claude.ai, <https://claude.ai/artifact/ADvU1T6KZEjKQdPFCRfSAD>, toujours à cette même adresse. Chaque modification des maquettes y est republiée **dès qu'elle est faite**, avant tout commit, Pull Request ou fusion, et jamais seulement une fois fusionnée. Cette publication n'est qu'une copie de consultation : elle n'est jamais modifiée directement, et ne fait jamais foi face au dépôt.
