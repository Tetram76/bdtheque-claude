---
type: Choix d'implémentation
title: 'Cohérence entre tables'
description: 'Règles multi-lignes garanties par le domaine seul, verrou consultatif de la hiérarchie des univers.'
applies_to:
  - 'src/Bdtheque.Infrastructure/UniverseHierarchy.cs'
  - 'src/Bdtheque.Domain/Entities/**'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Cohérence entre tables

- Les règles qui portent sur **plusieurs lignes ou plusieurs tables** ne sont garanties **que par le domaine**, la base ne vérifiant que leur partie mono-ligne : appartenance de la collection éditeur à l'éditeur (d'une édition, ou du template d'une série), exclusion mutuelle des intentions d'achat « album » et « édition », absence d'édition d'un album visé en entier, acyclicité de la hiérarchie des univers, date de référence d'un montant d'édition lorsqu'elle est celle de l'album.
  - **Alternative écartée (garantie aussi en base : clés étrangères composites, contrainte d'exclusion, table de verrou, trigger récursif)** : une complexité de schéma réelle, contre deux contournements non plausibles avec un **administrateur unique** ([Authentification](../../contraintes-techniques/architecture-deploiement/authentification.md)) : une écriture par SQL brut, ou deux requêtes concurrentes qui violent ensemble la règle dans la même fenêtre de quelques millisecondes.
  - Pour une règle portée par un agrégat (intentions d'achat d'un album), le second contournement est de toute façon fermé par la version de la racine ([Concurrence d'accès](concurrence-d-acces.md)).
- **Hiérarchie des univers** : la version d'un univers ne couvre pas l'acyclicité (deux reparentages croisés d'univers différents, vérifiés chacun sans voir l'autre, formeraient ensemble un cycle). Toute affectation d'un parent prend donc un **verrou consultatif de transaction** PostgreSQL avant de lire la hiérarchie qu'elle vérifie : les changements de hiérarchie sont sérialisés sans rien ajouter au schéma.
  - C'est le **premier** verrou de l'écriture, avant celui de la ligne de l'univers : dans l'ordre inverse, deux déplacements croisés (A sous C, C sous A) détiendraient chacun la ligne dont le contrôle de clé étrangère de l'autre a besoin, en attendant le verrou de hiérarchie (interblocage).
- Les écritures hors domaine (reprise des données) passent donc par le domaine, ou reproduisent ces contrôles.
