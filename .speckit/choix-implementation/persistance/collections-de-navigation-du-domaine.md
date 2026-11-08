---
type: Choix d'implémentation
title: 'Collections de navigation du domaine'
description: 'Collections en lecture seule, enfants créés par leur parent, AutoInclude des collections de l''album.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/**'
  - 'src/Bdtheque.Infrastructure/Configurations/AlbumConfiguration.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Collections de navigation du domaine

- **Collections en lecture seule** : aucune collection de navigation n'est modifiable hors de son entité. EF Core répercute un ajout dans une collection sur la clé étrangère de l'élément ajouté : une collection publique modifiable permettrait de rattacher un univers à un nouveau parent sans contrôle d'acyclicité, ou de déplacer une collection éditeur vers un autre éditeur au mépris des éditions et des templates qui la référencent.
- **Enfants créés par leur parent** : un enfant possédé n'est créé, déplacé ou retiré que par une opération de son parent, seule à pouvoir vérifier les règles de l'agrégat ; le titre et la série d'un album ne s'écrivent qu'avec ses contributions, et une édition appartient à son album dès sa construction, sans pouvoir en changer.
- Une **contribution retirée** est supprimée à l'enregistrement, par le contexte, et non par chaque appelant : sa relation à chacun de ses deux propriétaires exclusifs (album, série) étant optionnelle, EF Core mettrait sinon sa clé étrangère à `null`, ce que la base refuse.
- Les **contributions et les éditions d'un album sont toujours chargées avec lui** (`AutoInclude`), comme ses intentions d'achat ([Intention d'achat : agrégat porté par l'album](../domaine/intention-d-achat-agregat-porte-par-l-album.md)) : une règle évaluée sur un agrégat partiellement chargé serait fausse (la recopie des contributions de la série dépend de leur absence sur l'album ; le contrôle de la date de référence des montants porte sur toutes ses éditions, [Valeur d'une édition : mise en œuvre](../domaine/valeur-d-une-edition-mise-en-oeuvre.md)).
- Les **associations plusieurs-à-plusieurs** (genres, univers) s'ajoutent et se retirent de façon idempotente, en comparant par identifiant : un doublon violerait la clé primaire de la table de jointure.
