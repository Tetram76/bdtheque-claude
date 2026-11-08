---
type: Choix d'implémentation
title: 'Version de PostgreSQL'
description: 'postgres 18 alpine, épinglé, identique en production et dans les tests.'
applies_to:
  - 'docker-compose.yml'
  - 'tests/Bdtheque.Testing/**'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Version de PostgreSQL

Le moteur PostgreSQL est imposé ([Stack cible](../../contraintes-techniques/stack-cible.md)) ; sa version et sa variante d'image sont un choix : **`postgres` 18, variante `alpine`**, épinglée à sa version corrective et à sa version d'Alpine ([Images de base des conteneurs](images-de-base-des-conteneurs.md)), **la même en production et dans les tests**, pour que les tests vérifient le moteur qui sera déployé (un test en vérifie l'alignement).

- **Version 18** : version majeure courante (support jusqu'en novembre 2030), prise en charge explicitement par le fournisseur Npgsql d'EF Core 10. Changer de version majeure après la mise en production imposerait un dump/restore des données sur le NAS : le faire avant le premier déploiement est le moins coûteux.
- **Variante alpine** : image plus légère. Son absence de locales glibc est sans conséquence, le tri linguistique reposant sur les collations ICU qu'elle embarque ([Collation des colonnes texte](../persistance/collation-des-colonnes-texte.md)).
  - La version d'Alpine fixe celle d'ICU : sa montée, revue comme toute autre ([Mises à jour des dépendances](../../gestion-projet/depot/mises-a-jour-des-dependances.md)), peut changer la définition des collations, ce que l'application rattrape à chaque démarrage ([Collation des colonnes texte](../persistance/collation-des-colonnes-texte.md), versions des collations).
  - L'image officielle ne publie que les deux dernières versions d'Alpine : une fois la sienne retirée, le tag épinglé ne reçoit plus de correctifs, et le relevé des versions signale la suivante.
