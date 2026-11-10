---
type: Choix d'implémentation
title: 'Collation des colonnes texte'
description: 'Collation ICU française, unicité insensible à la casse et aux accents, versions des collations vérifiées au démarrage.'
applies_to:
  - 'src/Bdtheque.Infrastructure/BdthequeDbContext.cs'
  - 'src/Bdtheque.Infrastructure/CollationVersions.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-10T15:32:35Z }
---

# Collation des colonnes texte

- Toutes les colonnes texte portent la **collation ICU française** (`fr-FR-x-icu`), déclarée par une convention unique du modèle : tout tri de la base (listes, pagination, index des clés de tri) suit l'ordre linguistique français ([Langue et culture d'affichage](../../fonctionnel/presentation/langue-et-culture-d-affichage.md)), là où la collation par défaut de l'image compare les octets.
  - **Alternative écartée (locale par défaut de la base, à l'initialisation)** : absente du schéma, elle ne vaut que pour le volume initialisé ainsi ; une base restaurée ou initialisée autrement retomberait sur l'ordre binaire, et les tests devraient la reproduire.
  - **Alternative écartée (tri en mémoire)** : incompatible avec la pagination par la base.
  - **Alternative écartée (locale glibc d'une image Debian)** : l'ordre glibc peut changer à une mise à jour du système sans que PostgreSQL le détecte, et corrompre les index en silence ; les collations ICU sont versionnées, ce qui rend le changement détectable (ci-dessous).
  - Une autre culture d'affichage, le jour où elle sera proposée, triera par requête (`COLLATE`), sans évolution du schéma.
- **Unicité insensible à la casse et aux accents** (libellé de genre, [Genre](../../modele-metier/genre.md) ; nom affiché d'un auteur, stocké pour elle, [Auteur / Artiste](../../modele-metier/auteur-artiste.md)) : la colonne porte une collation ICU **non déterministe**, où seules les lettres de base comptent ; l'index unique compare avec elle, l'ordre de tri reste français, et PostgreSQL 18 y accepte `LIKE`, ce qui préserve la recherche.
  - **Alternative écartée (index unique sur une expression normalisée, ex. `lower(unaccent(…))`)** : SQL brut hors du modèle EF, dépendance à l'extension `unaccent`, et une normalisation à reproduire dans chaque requête de recherche.
- **Versions des collations vérifiées au démarrage** d'`api`, après les migrations : la définition d'une collation ICU suit la version d'ICU de l'image `db` ([Version de PostgreSQL](../deploiement/version-de-postgresql.md)), et les index construits sous l'ancienne peuvent être corrompus (ordre faux, unicité des genres faussée), à reconstruire avant de mettre à jour la version enregistrée (documentation PostgreSQL, *ALTER COLLATION*). À la moindre différence, `api` reconstruit tous les index de la base (la base est petite, et chaque colonne texte porte l'une de ces collations), met à jour la version et le journalise ; un index unique impossible à reconstruire (deux valeurs devenues égales) l'empêche de démarrer, la donnée en double étant à corriger à la main (procédure du `README.md`).
  - **Alternative écartée (avertissement seul, reconstruction manuelle)** : PostgreSQL n'avertit que dans les journaux de `db`, que rien n'oblige à lire après une mise à jour ; les index resteraient corrompus en silence.
