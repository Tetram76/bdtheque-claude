---
type: Gouvernance
title: 'Contrôle de cohérence à la reprise du travail'
description: 'À chaque reprise du travail : quand le contrôle de cohérence fonctionnelle est dû, qui le vérifie et où sa trace se lit ; s''il est dû, lire Contrôle de cohérence fonctionnelle.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-11T13:22:47Z }
---

# Contrôle de cohérence à la reprise du travail

Le [Contrôle de cohérence fonctionnelle](controle-de-coherence-fonctionnelle.md) est vérifié à chaque reprise du travail, après le relevé des versions ([Relevé des versions à la reprise du travail](../depot/releve-des-versions-a-la-reprise-du-travail.md)) et avant le chantier :

- **Dû** quand la description du ticket « Harnais fonctionnel : Cohérence ergonomique et fonctionnelle » (étiquettes `ergonomy-audit` et `harness`) ne porte aucun contrôle de la semaine calendaire en cours (du lundi au dimanche), ni achevé ni en cours ; il est alors mené selon [Contrôle de cohérence fonctionnelle](controle-de-coherence-fonctionnelle.md), lu en entier. Sinon, la reprise passe au chantier.
- **Qui le vérifie** : hors lot, la session qui reprend le travail, qui le mène avant son chantier ; dans un lot de chantiers en parallèle, l'**orchestrateur seul**, qui le confie à une session à part ([Chantiers en parallèle](../gouvernance/chantiers-en-parallele.md)) : chaque session de chantier le trouverait dû le même jour.
