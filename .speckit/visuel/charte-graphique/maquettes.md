---
type: Référence visuelle
title: 'Maquettes'
description: 'Maquettes HTML autonomes, visionneuse, gestion et publication systématique sur claude.ai, décisions finales seulement ; maquettes de décision en artefact temporaire, supprimé une fois la décision consignée, et leur report selon la nature de la décision ; à lire pour toute maquette.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-26T10:00:00Z }
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
- **Décisions finales seulement** (exigence de l'utilisateur) : les maquettes de référence, dans le dépôt comme dans leur publication, ne portent que la décision finale. Elles ne servent jamais à présenter différentes propositions, ni par un réglage qui les met côte à côte.

## Maquettes de décision

Une maquette nécessaire à une décision demandée à l'utilisateur est une **maquette de décision** (exigence de l'utilisateur) :

- **Artefact temporaire** : elle est présentée dans un artefact claude.ai temporaire, distinct de celui des maquettes de référence, qui porte les propositions soumises à la décision. Cet artefact est **supprimé une fois la décision consignée**.
- **Report dans les maquettes de référence, selon la nature de la décision** :
  - la maquette retenue entre dans les maquettes de référence ([`maquettes/`](../maquettes)) quand la décision fixe l'apparence ou la disposition d'un écran qui a, ou doit avoir, une maquette de référence : élément ajouté, déplacé ou redessiné (ex. le lien vers la saisie sur les fiches de la consultation, #658) ;
  - elle n'y entre pas quand elle n'a servi qu'à choisir un comportement, un libellé ou une option sans changer le visuel de référence de l'écran : la décision n'est alors consignée qu'en texte (ex. « Nouvelle collection », #620) ;
  - l'utilisateur n'est interrogé, au moment de consigner la décision, qu'en cas de doute **réel** sur sa nature : on ne s'invente pas de doute.
