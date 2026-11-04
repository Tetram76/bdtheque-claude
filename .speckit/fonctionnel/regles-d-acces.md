---
type: Règle métier
title: 'Règles d''accès'
description: 'Recherche et consultation publiques, statistiques publiques ou privées, écriture réservée à l''administrateur, mêmes règles pour l''outil pour agents IA.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Règles d'accès

Ces règles sont **globales** : elles s'appliquent à l'interface web comme à l'interface pour agents IA, une fonctionnalité ayant les mêmes restrictions d'accès quelle que soit l'interface qui la propose. Seul le moyen de s'authentifier diffère : l'interface pour agents IA ne connaît que ses jetons d'accès, dont chacun donne le niveau public ou celui du compte administrateur (cf. [Interface pour agents IA](/fonctionnel/agents-ia/interface-pour-agents-ia.md), Accès par jeton).

- **Recherche et consultation** : publiques. L'authentification n'est pas requise, mais n'est pas interdite pour autant.
- **Analyses et statistiques** : deux niveaux d'accès, **public** et **privé** (réservé au compte administrateur). Le niveau est choisi **au cas par cas**, en fonction des données accédées et exposées par chaque analyse ou statistique.
- **Écriture de données** (saisie, modification, suppression, paramétrage) : réservée au **compte administrateur**, seul compte autorisé à modifier les données.
