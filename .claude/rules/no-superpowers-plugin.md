# Interdiction du plugin superpowers

Le plugin **superpowers** est **interdit** dans ce projet : aucun de ses skills, aucune de ses règles, ni par invocation explicite ni par auto-déclenchement.

**Motif** : le plugin gère mal le nettoyage de ses worktrees après fusion/discard de branche — il laisse des worktrees orphelins dans `.claude/worktrees/` (constaté sur ce dépôt).

**Portée** : cette interdiction couvre tous les skills `superpowers:*` (brainstorming, using-git-worktrees, finishing-a-development-branch, test-driven-development, etc.) ainsi que toute règle qu'il fournit. Elle ne concerne pas les autres plugins/skills du projet (`codex-review-loop`, etc.), qui restent utilisables normalement.
