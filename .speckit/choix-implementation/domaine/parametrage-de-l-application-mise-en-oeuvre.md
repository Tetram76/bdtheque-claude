---
type: Choix d'implémentation
title: 'Paramétrage de l''application : mise en œuvre'
description: 'Paramétrage en fiche unique, provisionnée au démarrage, adressée sans identifiant.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/ApplicationSettings.cs'
  - 'src/Bdtheque.Infrastructure/ApplicationSettingsProvisioning.cs'
  - 'src/Bdtheque.Api/Endpoints/ApplicationSettingsEndpoints.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Paramétrage de l'application : mise en œuvre

- **Fiche unique, toujours présente** ([Paramétrage de l'application](../../modele-metier/parametrage-de-l-application.md)) : une racine d'agrégat dont l'identifiant est **connu d'avance**, le seul que la table accepte. L'API l'adresse donc sans identifiant, sans création ni suppression ; sa modification passe par sa version, comme toute fiche ([Concurrence d'accès](../persistance/concurrence-d-acces.md)).
- **Provisionnée au démarrage** d'`api`, après les migrations ([Application du schéma au démarrage](../deploiement/application-du-schema-au-demarrage.md)), avec ses valeurs par défaut, si elle n'existe pas : un déploiement neuf l'a dès son premier démarrage, et les suivants la laissent telle que l'administration l'a modifiée. Elle fait partie de la base provisionnée, comme son schéma : aucune écriture de l'utilisateur ne la crée, et le journal des modifications n'en consigne que les modifications.
  - **Alternative écartée (ligne insérée par la migration)** : ses dates de création et de modification seraient des constantes écrites dans la migration, et non celles du déploiement.
  - **Alternative écartée (création par l'application à la première lecture)** : chaque lecture devrait la prévoir, et une création serait consignée au journal pour une fiche qui ne se crée pas.
- Son **libellé** dans le journal des modifications n'a aucune composante : la fiche est unique.
