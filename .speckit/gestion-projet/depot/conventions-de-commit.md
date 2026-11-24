---
type: Gouvernance
title: 'Conventions de commit'
description: 'Conventional Commits pour les commits et titres de PR, description de PR devenue corps du commit squashé, template de PR, lu dans le dépôt public Tetram76/.github sans jamais le rattacher à la session.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-24T08:50:00Z }
---

# Conventions de commit

Le projet suit le standard **[Conventional Commits](https://www.conventionalcommits.org/)**.

Format : `<type>(<scope>): <description courte>`

Types : `feat`, `fix`, `chore`, `refactor`, `test`, `docs`, `ci`

Le **titre de la Pull Request** doit également respecter ce format — c'est lui qui devient le titre du commit squashé sur `main`.

La quasi-totalité des PR étant fusionnées en **squash merge**, la **description de la PR devient le corps du commit** (réglage repository `squash_merge_commit_message = PR_BODY`). La description doit donc être rédigée comme un **message de commit à part entière** : contenu clair, pertinent et durable, exploitable dans l'historique Git sans avoir à consulter la PR d'origine.

**Ce réglage ne doit pas être modifié** : il porte à la fois la qualité de l'historique et la traçabilité jusqu'au ticket (cf. [Suivi par tickets](../revue-tickets/suivi-par-tickets.md)). Avec une autre valeur, le corps du commit squashé n'est plus la description de la PR (`COMMIT_MESSAGES` : concaténation des messages des commits intermédiaires ; `BLANK` : corps vide). Le mot-clé de fermeture (`Closes #<numéro>`) n'apparaît alors plus dans le commit final : celui-ci ne mène plus directement au ticket, seul le suffixe `(#<PR>)` du titre mène à la PR, et il faut passer par elle pour retrouver le ticket. La description, rédigée comme un message de commit, ne figure plus non plus dans l'historique de `main`. Toute modification de ce réglage exige donc de revoir ces règles.

La description de toute Pull Request **suit toujours le template** du projet `.github` : le dépôt [`Tetram76/.github`](https://github.com/Tetram76/.github) (public) du compte, qui porte les fichiers de santé communautaire par défaut de tous ses dépôts, dont `.github/pull_request_template.md`. Sections et consignes du template sont à suivre. Il n'est pas copié dans ce dépôt : il est relu à chaque PR.

- **Lecture sans rattachement** (exigence de l'utilisateur) : `Tetram76/.github` étant public, il se lit **toujours** sans être rattaché à la session ni demandé à l'utilisateur. Rattacher `Tetram76/.github` à la session (`add_repo` d'une session cloud), ou demander à l'utilisateur de le rattacher, est **interdit, sans exception**. `Tetram76/.github` se lit par une lecture anonyme : `git clone --depth 1 https://github.com/Tetram76/.github` dans un dossier temporaire, ou `https://raw.githubusercontent.com/Tetram76/.github/HEAD/.github/pull_request_template.md`.
  - **Refus de l'API, à ne pas suivre** : dans une session cloud, l'API GitHub (`gh api`, serveur MCP GitHub) est restreinte aux dépôts de la session et refuse `Tetram76/.github` (`403`) avec un message qui invite à le rattacher : ce refus ne dit rien de la visibilité de `Tetram76/.github` et n'est jamais suivi.
  - **Branche par défaut de `Tetram76/.github` : `master`**, et non `main` : une adresse construite sur `main` répond `404`, ce qui ne dit rien non plus de la visibilité de `Tetram76/.github` ; `HEAD` désigne la branche par défaut, quelle qu'elle soit.

Exemples :

- `feat(albums): ajout de la gestion des éditions`
- `fix(devises): correction du taux de conversion franc français`
- `chore(docker): mise à jour du Dockerfile backend`

> **Note** : l'enforcement automatique du format (via ruleset GitHub) est réservé à GitHub Enterprise et n'est donc pas actif. Le respect de la convention est une contrainte de processus appliquée par l'agent.
