---
type: Modèle métier
title: 'Entrée du journal des modifications'
description: 'Attributs d''une entrée du journal des modifications et ses règles.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/ChangeLogEntry.cs'
  - 'src/Bdtheque.Domain/Entities/ChangeLogValue.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Entrée du journal des modifications

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Type de fiche | énuméré | oui | `Album` / `Édition` / `Série` / `Intention d'achat` / `Auteur` / `Éditeur` / `Collection éditeur` / `Genre` / `Univers` / `Recherche enregistrée` / `Paramétrage` / `Jeton d'accès`. |
| Fiche | identifiant | oui | Identifiant de la fiche écrite. Conservé après la suppression de la fiche : aucune contrainte d'intégrité référentielle. |
| Libellé | composantes du libellé | oui | Données qui composent le libellé de la fiche (ex. titre, série et tome d'un album, cf. [Libellé d'un album](/fonctionnel/presentation/libelle-d-un-album.md)), relevées au moment de l'écriture, pour qu'une fiche supprimée reste identifiable. Le libellé en est construit à la consultation. |
| Opération | énuméré | oui | `Création` / `Modification` / `Suppression` / `Reprise` (reprise d'une ancienne donnée depuis l'application existante). |
| Changements | liste | non | Pour une modification uniquement : chaque changement, avec l'attribut concerné, sa valeur **avant** et sa valeur **après**, telles qu'enregistrées dans la fiche ; une valeur qui désigne une autre fiche (ex. la série d'un album) l'est par les composantes de son libellé, comme ci-dessus. Un secret (valeur d'un jeton d'accès, renouvelée par son réarmement) est consigné comme changé, sans valeur avant ni après. |

> - Une entrée est ajoutée **automatiquement** à chaque écriture sur une fiche, dans la même opération ; elle n'est jamais saisie, modifiée ni supprimée, et le journal n'est jamais purgé. Sa date de création (attribut commun) est la date de l'écriture.
> - L'ajout ou le retrait d'un élément associé (genre, univers, contribution, visuel…) est un changement de la fiche qui le porte : un ajout n'a pas de valeur avant, un retrait pas de valeur après. Les éléments sans fiche propre (visuel, contribution) n'ont pas d'entrée à leur nom.
> - Aucun texte d'une entrée n'est figé à l'écriture : type de fiche, opération, attribut, valeurs et libellé sont présentés dans la culture d'affichage en vigueur lors de la consultation ([Langue et culture d'affichage](/fonctionnel/presentation/langue-et-culture-d-affichage.md)), comme sur la fiche elle-même. Cette présentation est **idempotente** : pour une même culture d'affichage et un même paramétrage, une entrée produit toujours le même message, à chaque lecture.
> - La reprise des données ajoute au moins une entrée `Reprise` pour chaque fiche qu'elle crée ou met à jour.
> - Le journal n'est consultable que dans l'administration.
