---
type: Choix d'implémentation
title: 'Valeur d''une édition : mise en œuvre'
description: 'Règles des montants d''une édition, écriture atomique de l''acquisition, date de référence.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/Edition.cs'
  - 'src/Bdtheque.Domain/Entities/EditionAcquisition.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/EditionConfiguration.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Valeur d'une édition : mise en œuvre

- Prix d'acquisition et valeur initiale ([Édition](../../modele-metier/edition.md)) suivent les mêmes règles de forme (montant et devise ensemble, montant positif, code de devise de trois lettres majuscules, ancien franc compris), avec chacun ses codes de règle (`DomainRules`) pour un message précis.
- Les contraintes d'intégrité mono-ligne (aucune donnée d'acquisition ni valeur pour une édition non possédée, aucun montant pour une édition gratuite, achat jamais gratuit) sont vérifiées par le domaine **et** par des contraintes CHECK (`EditionConfiguration`). La gratuité d'une édition **non possédée** n'est soumise à aucune règle ([Intention d'achat](../../fonctionnel/regles-metier/intention-d-achat.md)) : elle n'est confrontée au mode qu'à l'acquisition (`Album.RecordAcquisition` refuse l'achat d'une édition gratuite avant toute modification de l'agrégat).
- **Écriture atomique** : mode, date et prix d'acquisition, gratuité et valeur initiale (`EditionAcquisition`) s'écrivent ensemble avec l'année d'édition (`Edition.SetPublicationYearAndAcquisition`, ou `Album.RecordAcquisition` pour l'acquisition), jamais champ par champ : l'année d'édition et la date d'acquisition se relaient comme date de référence du prix, et chaque règle lie au moins deux de ces champs, si bien qu'une suite de setters réussirait ou échouerait selon son ordre. Les règles sont vérifiées sur l'état complet visé, avant toute modification. Une édition gratuite ne reçoit donc aucun montant (erreur métier), le formulaire les vidant lui-même ([Libellés contextuels sur l'édition](../../fonctionnel/presentation/libelles-contextuels-sur-l-edition.md)).
- **Date de référence** ([Gestion des devises](../../fonctionnel/valorisation/gestion-des-devises.md)) : tout montant doit en garder une, ce que le domaine vérifie à **chaque écriture** qui peut la faire disparaître — écriture de l'acquisition, de l'année d'édition, ou de la date de première publication de l'album (`Album.SetFirstPublicationDate`, sur `Album.Editions`). Le contrôle porte sur les valeurs qu'aurait l'édition après l'écriture, et ne lit l'album que lorsque les dates propres de l'édition ne suffisent pas : une édition lue sans son album n'échoue (erreur technique) que dans ce cas. La règle traversant deux tables, elle n'a pas de contrainte CHECK ([Cohérence entre tables](../persistance/coherence-entre-tables.md)).
