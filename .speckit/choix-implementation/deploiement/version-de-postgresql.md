---
type: Choix d'implémentation
title: 'Version de PostgreSQL'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Version de PostgreSQL

- Le moteur PostgreSQL est imposé (`contraintes-techniques/`) ; sa version et sa variante d'image sont un choix : **`postgres`, version 18, variante `alpine`**, épinglée à sa version corrective et à sa version d'Alpine ([Images de base des conteneurs](/choix-implementation/deploiement/images-de-base-des-conteneurs.md)), utilisée à l'identique en production (`docker-compose.yml`) et par les tests (Testcontainers, alignement vérifié par un test).
  - **Version 18** : version majeure courante (support jusqu'en novembre 2030), prise en charge explicitement par `Npgsql.EntityFrameworkCore.PostgreSQL` 10. Changer de version majeure après la mise en production imposerait un dump/restore des données sur le NAS : le faire avant le premier déploiement est le moins coûteux. À partir de la 18, l'image officielle attend le volume de données sur `/var/lib/postgresql` (et non plus `/var/lib/postgresql/data`).
  - **Variante alpine** : image plus légère ; son absence de locales glibc est sans conséquence, le tri linguistique reposant sur les collations ICU qu'elle embarque ([Collation des colonnes texte](/choix-implementation/persistance/collation-des-colonnes-texte.md)). La version d'Alpine fixe celle d'ICU : sa montée, visible et revue comme toute autre ([Mises à jour des dépendances](/gestion-projet/depot/mises-a-jour-des-dependances.md)), peut changer la définition des collations, ce que l'application rattrape à chaque démarrage ([Collation des colonnes texte](/choix-implementation/persistance/collation-des-colonnes-texte.md), versions des collations). L'image officielle ne publiant que les deux dernières versions d'Alpine, le tag épinglé ne reçoit plus de correctifs une fois la sienne retirée : le relevé des versions signale alors la suivante.
