---
type: Choix d'implémentation
title: 'Dates renseignées automatiquement : mise en œuvre'
description: 'Dates de création, de modification et d''entrée dans la collection posées par le contexte, horloge de l''application, horloge des tests.'
applies_to:
  - 'src/Bdtheque.Infrastructure/BdthequeDbContext.cs'
  - 'src/Bdtheque.Domain/Entities/Common/EntityBase.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Dates renseignées automatiquement : mise en œuvre

- **Dates posées par le contexte à l'enregistrement**, point de passage unique de toute écriture, sans opération du domaine : dates de création et de modification ([Attributs communs à toutes les entités](../../modele-metier/attributs-communs-a-toutes-les-entites.md)), et date d'entrée dans la collection d'une édition ([Édition](../../modele-metier/edition.md)). La racine d'un agrégat étant marquée modifiée à toute écriture ([Concurrence d'accès](concurrence-d-acces.md)), sa date de modification suit celle de ses enfants.
  - **Alternative écartée (valeurs par défaut et trigger en base)** : une seconde mécanique, en SQL, pour ce que le point de passage d'EF Core couvre déjà, et par lequel passent aussi les écritures hors domaine ([Cohérence entre tables](coherence-entre-tables.md)).
  - **Alternative écartée (intercepteur d'enregistrement)** : à enregistrer dans chaque construction du contexte (API, outillage de conception, tests), au risque d'en oublier une ; la surcharge de l'enregistrement s'applique à toute instance.
- La **date d'entrée dans la collection** est déduite de l'**état** de l'édition (possédée sans date encore), et non de l'opération qui l'a rendue possédée : création, confirmation d'achat ou reprise des données la reçoivent sans y penser. Une édition possédée le restant jusqu'à sa suppression, la date n'est jamais retirée ni réécrite ; une contrainte CHECK la lie à la possession.
  - **Alternative écartée (date posée par le domaine, à l'acquisition)** : le domaine n'a pas d'horloge ; lui en fournir une ajouterait un paramètre à chaque acquisition, et chaque écriture hors domaine devrait reproduire la règle.
- **Horloge de l'application** (`TimeProvider`), partagée par les tâches de fond : l'heure UTC du serveur, tronquée à la microseconde de PostgreSQL pour que la valeur en mémoire soit celle stockée (Npgsql n'écrit un `timestamp with time zone` qu'avec un décalage nul, documentation *Date and Time Handling*). Le contexte la reçoit en paramètre **obligatoire** : aucune de ses constructions ne peut l'omettre.
  - **Alternative écartée (heure du système lue directement)** : les tests ne pourraient pas fixer l'heure des écritures ; ils antidateraient leurs données par SQL, ou dépendraient de leur date d'exécution et échoueraient à une frontière de mois ou d'année.
- **Horloge des tests** : une horloge réglable, qui donne l'heure du système tant qu'un test ne la fixe pas, et garde les minuteries du système : régler l'heure ne déclenche jamais les tâches de fond.
  - **Alternative écartée (`FakeTimeProvider`)** : son heure ne s'écoule pas d'elle-même ; tous les tests tourneraient sur une heure simulée, et des écritures successives pourraient recevoir la même date, leur ordre retombant alors sur l'identifiant, que le `Guid` v7 n'ordonne qu'à la milliseconde.
