---
type: Gouvernance
title: 'Signature des commits'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Signature des commits

- **Tous les commits doivent être signés et vérifiés** (GPG/SSH).
- La signature est configurée globalement sur le poste (`commit.gpgsign=true`, clé GPG `CC10F185AA085B2DD98025986A5E6B8341983E31`).
- La règle `required_signatures` est intégrée au Ruleset GitHub pour l'imposer côté serveur.
- **Vérification sur GitHub uniquement** : seul compte l'état que GitHub constate, celui que contrôle `required_signatures` : la mention « Verified » d'un commit, ou le champ `verification` de l'API des commits (`verified`, `reason`). La signature d'un commit poussé se vérifie donc toujours là, jamais en local : `git log --show-signature` ou `%G?` ne connaissent que les clés déclarées sur le poste (ex. `gpg.ssh.allowedSignersFile` pour une signature SSH), et peuvent ne pas reconnaître une signature que GitHub vérifie.
