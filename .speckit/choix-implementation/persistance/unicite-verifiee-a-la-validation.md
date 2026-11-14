---
type: Choix d'implémentation
title: 'Unicité vérifiée à la validation'
description: 'Contrainte unique différable, vérifiée à la validation de la transaction, pour une unicité que l''enregistrement d''un agrégat peut permuter (noms des collections d''un éditeur) ; revue des autres unicités des enfants d''agrégat.'
applies_to:
  - 'src/Bdtheque.Infrastructure/DeferredUniqueness.cs'
  - 'src/Bdtheque.Infrastructure/BdthequeMigrationsSqlGenerator.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/PublisherCollectionConfiguration.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-14T12:00:00Z }
---

# Unicité vérifiée à la validation

- **Une unicité que l'enregistrement d'un agrégat peut permuter est une contrainte unique différable** (`DEFERRABLE INITIALLY DEFERRED`), vérifiée à la validation de la transaction, sur l'état enregistré : le nom d'une collection chez son éditeur ([Collection éditeur](../../modele-metier/collection-editeur.md)), que la saisie de l'éditeur peut échanger entre deux de ses collections, ou faire tourner entre plusieurs, en un seul enregistrement ([Organisation de l'API](../api/organisation-de-l-api.md), collections d'un éditeur enregistrées avec lui).
  - Un **index unique** ne le permet pas, pour deux raisons : EF Core ordonne les commandes d'un enregistrement d'après les index uniques de son modèle, et ne sait pas ordonner deux mises à jour qui prennent chacune la valeur de l'autre (cycle, refusé avant toute écriture, [dotnet/efcore#15319](https://github.com/dotnet/efcore/issues/15319)) ; PostgreSQL vérifie un index unique non différable ligne par ligne, même au sein d'une seule instruction (documentation de PostgreSQL, *CREATE TABLE*, `DEFERRABLE`).
  - **Déclarée dans le modèle**, par une annotation sur un index que le modèle tient pour non unique (EF Core ne l'ordonne donc plus en cycle), et traduite en contrainte de même nom par le générateur SQL des migrations : Npgsql ne sait pas déclarer une contrainte unique différable. Elle survit ainsi à la régénération de la migration unique ([Migrations EF Core](migrations-ef-core.md)) et au contrôle du modèle par les migrations.
    - **Alternative écartée (contrainte écrite à la main dans la migration)** : absente du modèle, elle disparaîtrait à la régénération de la migration unique, sans que rien ne le signale.
    - **Alternative écartée (clé alternative)** : EF Core la traduit en contrainte unique non différable, et interdit de modifier ses valeurs, alors qu'un renommage en est une.
    - Le générateur dérive de celui de Npgsql, construit avec ses options, d'un type interne au fournisseur (avertissement `EF1001`) : aucun type public ne les porte, et c'est le seul moyen de reprendre tout ce qu'il génère d'autre.
  - **Violation signalée en erreur métier** comme celle d'un index unique, par la même correspondance avec son code de règle ([Erreurs métier, fonctionnelles et techniques](../ecritures/erreurs-metier-fonctionnelles-et-techniques.md)) : levée à la validation, elle n'est pas enveloppée par l'enregistrement, mais son code d'erreur PostgreSQL est le même.
  - **Alternative écartée (nom provisoire écrit en base, puis le nom voulu)** : deux enregistrements pour une seule modification de l'utilisateur, que le journal des modifications présenterait comme deux ([Journal des modifications : mise en œuvre](../domaine/journal-des-modifications-mise-en-oeuvre.md)).
- **Les autres unicités restent des index uniques**, vérifiés ligne par ligne, aucune saisie ne pouvant les permuter en un enregistrement :
  - celles des racines (libellé d'un genre, nom affiché d'un auteur, nom d'un éditeur, nom d'un univers sous son parent, nom d'une recherche enregistrée) : un enregistrement n'écrit qu'une racine ;
  - les contributions d'un album ou d'une série : remplacées en bloc, elles ne sont jamais modifiées, seulement ajoutées ou retirées, ce qu'EF Core ordonne (retraits d'abord) ;
  - les intentions d'achat : leur cible ne change jamais, une conversion retirant l'intention pour en créer une autre.
