---
type: Choix d'implémentation
title: 'Cohérence entre tables'
description: 'Règles multi-lignes garanties par le domaine seul, verrou consultatif de la hiérarchie des univers.'
applies_to:
  - 'src/Bdtheque.Infrastructure/UniverseHierarchy.cs'
  - 'src/Bdtheque.Domain/Entities/**'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Cohérence entre tables

- Certaines règles portent sur **plusieurs lignes ou plusieurs tables** et ne sont garanties **que par le domaine**, la base ne vérifiant que leur partie mono-ligne : appartenance de la collection éditeur à l'éditeur (d'une édition, ou du template d'une série), exclusion mutuelle des intentions d'achat « album » et « édition », absence d'édition d'un album visé en entier, acyclicité de la hiérarchie des univers, date de référence d'un montant d'édition lorsqu'elle est celle de l'album. Motif : les garantir aussi en base (clés étrangères composites, contrainte d'exclusion PostgreSQL, table de verrou, trigger récursif) ajoute une complexité de schéma réelle, contre deux contournements non plausibles avec un **administrateur unique** ([Authentification](../../contraintes-techniques/architecture-deploiement/authentification.md)) : une écriture par SQL brut, ou deux requêtes concurrentes violant ensemble la règle dans une même fenêtre de quelques millisecondes. Pour une règle portée par un agrégat (intentions d'achat d'un album), ce second contournement est de toute façon fermé par la version de la racine ([Concurrence d'accès](concurrence-d-acces.md)). Pour l'acyclicité des univers, que la version d'un seul univers ne couvre pas (deux reparentages croisés d'univers différents, chacun vérifié sans voir l'autre, formeraient ensemble un cycle), il est fermé par un **verrou consultatif** PostgreSQL de transaction (`pg_advisory_xact_lock`, `UniverseHierarchy` dans `Bdtheque.Infrastructure`) que toute affectation d'un parent prend avant de lire la hiérarchie qu'elle vérifie : les changements de hiérarchie sont sérialisés sans rien ajouter au schéma. C'est le **premier** verrou de l'écriture, avant celui de la ligne de l'univers : dans l'ordre inverse, deux déplacements croisés (A sous C, C sous A) détiendraient chacun la ligne dont le contrôle de clé étrangère de l'autre a besoin, en attendant le verrou de hiérarchie (interblocage).
- Les écritures hors domaine (future reprise des données) doivent donc passer par le domaine, ou reproduire ces contrôles.
