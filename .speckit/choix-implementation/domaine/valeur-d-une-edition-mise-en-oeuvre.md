---
type: Choix d'implémentation
title: 'Valeur d''une édition : mise en œuvre'
description: 'Règles des montants d''une édition, écriture atomique de l''acquisition, date de référence.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/Edition.cs'
  - 'src/Bdtheque.Domain/Entities/EditionAcquisition.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/EditionConfiguration.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Valeur d'une édition : mise en œuvre

- Prix d'acquisition et valeur initiale ([Édition](../../modele-metier/edition.md)) suivent les **mêmes règles de forme**, avec chacun ses codes de règle, pour un message précis.
- Les contraintes d'intégrité **mono-ligne** (aucune donnée d'acquisition ni valeur pour une édition non possédée, aucun montant pour une édition gratuite, achat jamais gratuit) sont vérifiées par le domaine, qui en donne le message, **et** par des contraintes CHECK, qui les garantissent en base. La gratuité d'une édition non possédée n'est soumise à aucune règle ([Intention d'achat](../../fonctionnel/regles-metier/intention-d-achat.md)) : elle n'est confrontée au mode qu'à l'acquisition, qui refuse l'achat d'une édition gratuite avant toute modification de l'agrégat.
- **Écriture atomique** : mode, date et prix d'acquisition, gratuité et valeur initiale s'écrivent **ensemble avec l'année d'édition**, jamais champ par champ, et les règles sont vérifiées sur l'état complet visé, avant toute modification.
  - **Alternative écartée (un setter par champ)** : l'année d'édition et la date d'acquisition se relaient comme date de référence du prix, et chaque règle lie au moins deux de ces champs : une suite de setters réussirait ou échouerait selon son ordre.
- **Date de référence** ([Gestion des devises](../../fonctionnel/valorisation/gestion-des-devises.md)) : le domaine vérifie qu'un montant en garde une à **chaque écriture qui peut la faire disparaître** (acquisition, année d'édition, date de première publication de l'album), sur les valeurs qu'aurait l'édition après l'écriture. Il ne lit l'album que lorsque les dates propres de l'édition ne suffisent pas. La règle traversant deux tables, elle n'a pas de contrainte CHECK ([Cohérence entre tables](../persistance/coherence-entre-tables.md)).
