---
type: Choix d'implémentation
title: 'Paramétrage de l''application : mise en œuvre'
description: 'Paramétrage en fiche unique, provisionnée au démarrage, adressée sans identifiant.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/ApplicationSettings.cs'
  - 'src/Bdtheque.Infrastructure/ApplicationSettingsProvisioning.cs'
  - 'src/Bdtheque.Api/Endpoints/ApplicationSettingsEndpoints.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Paramétrage de l'application : mise en œuvre

- **Fiche unique, toujours présente** ([Paramétrage de l'application](../../modele-metier/parametrage-de-l-application.md)) : entité `ApplicationSettings`, racine d'agrégat, dont l'identifiant est **connu d'avance** (`ApplicationSettings.SingletonId`), le seul qu'accepte la table : une contrainte CHECK l'impose, et la clé primaire rend la ligne unique. L'API l'adresse donc sans identifier (`GET`/`PUT /admin/settings`, `GET /catalog/settings`), et ne propose ni création ni suppression. Sa modification passe par la version de la fiche ([Concurrence d'accès](../persistance/concurrence-d-acces.md)), comme toute fiche.
- **Provision au démarrage** : l'application ne crée jamais le paramétrage. Au démarrage d'`api`, après les migrations ([Application du schéma au démarrage](../deploiement/application-du-schema-au-demarrage.md)), la ligne est insérée avec ses valeurs par défaut (`Album (Série - Tome)`, `Français (France)`), datée par l'horloge de l'application, si elle n'existe pas encore (`INSERT … ON CONFLICT DO NOTHING`, `ApplicationSettingsProvisioning`) : un déploiement neuf l'a dès son premier démarrage, et les suivants la laissent telle que l'administration l'a modifiée. Elle est écrite hors de `SaveChangesAsync` : elle fait partie de la base provisionnée, comme son schéma, et aucune écriture de l'utilisateur ne la crée, si bien que le journal des modifications n'en consigne que les modifications (et, avec la reprise des données, sa reprise).
  - **Alternative écartée (ligne insérée par la migration, `HasData`)** : ses dates de création et de modification seraient des constantes écrites dans la migration, et non celles du déploiement.
  - **Alternative écartée (création par l'application à la première lecture)** : chaque lecture devrait la prévoir, et une création serait consignée au journal pour une fiche qui ne se crée pas.
- **Libellé** dans le journal des modifications : aucune composante (`ApplicationSettingsLabel`), la fiche étant unique.
