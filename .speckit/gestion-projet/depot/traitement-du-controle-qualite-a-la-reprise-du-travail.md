---
type: Gouvernance
title: 'Traitement du contrôle qualité à la reprise du travail'
description: 'À chaque reprise du travail, avec le relevé des versions : quand le traitement des entrées du contrôle qualité est dû et où sa trace se lit ; s''il est dû, lire Contrôle qualité hebdomadaire.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-12T15:10:00Z }
---

# Traitement du contrôle qualité à la reprise du travail

Le ticket de suivi du harnais qualité (étiquettes `quality-audit` et `harness`, [Issues](../revue-tickets/issues.md#tickets-de-suivi-des-harnais)) est lu à chaque reprise du travail, juste après le relevé des versions ([Relevé des versions à la reprise du travail](releve-des-versions-a-la-reprise-du-travail.md)), sans relevé supplémentaire : le contrôle qualité hebdomadaire ([Contrôle qualité hebdomadaire](controle-qualite-hebdomadaire.md)) l'a déjà fait.

- **Dû** quand la section « Critique » ou « Prioritaire » de la description du ticket n'est pas vide et que la description ne porte pas de section « Traitement du `<date>` » : le contrôle réécrit la description en entier à chaque exécution, si bien que la section disparaît avec lui, et un traitement au plus a lieu par exécution. La dette au fil de l'eau ne rend jamais le traitement dû. Le traitement est alors mené selon [Contrôle qualité hebdomadaire](controle-qualite-hebdomadaire.md), lu en entier, et sa trace ajoutée ; les entrées critiques y passent en tête, comme une vulnérabilité connue au relevé des versions. Sinon, la reprise passe au chantier : la conclusion du traitement reste valable jusqu'au contrôle suivant, et une Pull Request qu'il a ouverte se poursuit selon ses propres règles.
  - **Alternative écartée (commentaire du ticket à chaque traitement, ou trace dans un fichier du dépôt)** : écartée pour les mêmes raisons que pour le relevé des versions ([Relevé des versions à la reprise du travail](releve-des-versions-a-la-reprise-du-travail.md)).
