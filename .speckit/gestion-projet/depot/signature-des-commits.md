---
type: Gouvernance
title: 'Signature des commits'
description: 'Commits signés et vérifiés, vérification sur GitHub seulement ; s''applique à tout commit.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-17T18:00:00Z }
---

# Signature des commits

- **Tous les commits doivent être signés et vérifiés** (GPG/SSH).
- La signature est activée globalement sur le poste (`commit.gpgsign=true`). La clé de ce dépôt est la clé GPG ed25519 `FA9B32F52135D36C4593E4976FF254ADD108ACB0`, dont l'identité est l'adresse de commit privée de GitHub (`7486827+Tetram76@users.noreply.github.com`) : elle est déclarée dans la configuration locale du dépôt (`user.signingkey`, `user.email`), que partagent tous ses worktrees, et prime sur la clé globale du poste.
- La règle `required_signatures` est intégrée au Ruleset GitHub pour l'imposer côté serveur.
- **Vérification sur GitHub uniquement** : seul compte l'état que GitHub constate, celui que contrôle `required_signatures` : la mention « Verified » d'un commit, ou le champ `verification` de l'API des commits (`verified`, `reason`). La signature d'un commit poussé se vérifie donc toujours là, jamais en local : `git log --show-signature` ou `%G?` ne connaissent que les clés déclarées sur le poste (ex. `gpg.ssh.allowedSignersFile` pour une signature SSH), et peuvent ne pas reconnaître une signature que GitHub vérifie.
