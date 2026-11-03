---
name: validation-fonctionnelle
description: >-
  Drives the functional validation of a chantier the user decides on (screens,
  behaviours, content of `fonctionnel/` or `visuel/`, ticket marked
  « Validation fonctionnelle : requise ») on this repository. Use as soon as the
  code of such a chantier is written and its local checks pass — before any
  other step (reference captures, PR description polish, marking the PR ready)
  — and whenever the user gives feedback on a version under validation.
---

# Validation fonctionnelle

The `.speckit/` is the source of truth: `.speckit/gestion-projet/revue-tickets.md`
§ Validation fonctionnelle, and `.speckit/choix-implementation/deploiement.md`
§ Environnement de validation fonctionnelle. **Read both sections again, in
full, before step 1**: this skill only orders their steps, it adds no rule.

The one way the user validates what they see: the version of the draft pull
request, launched on their machine by `scripts/Start-Validation.ps1`. Never
anything else in its place — no screenshots sent in the conversation, no
browser of the agent, no reference captures shown as a validation.

## Steps, in this order

1. **Content complete, local checks green** → commit and push the branch, and
   open the pull request **as a draft** (template of `Tetram76/.github`), right
   away. Do not wait for the reference captures: they come with the corrections
   (step 4), and a capture failure of the CI does not prevent validation.
2. **Load the validation data the screen needs** (`Bdtheque.ValidationData`),
   if the chantier adds cases, in the same draft.
3. **Ask for the validation**, in one message, as § Validation fonctionnelle
   prescribes:
   - screen by screen, what is new or changed, the state or case to observe,
     the address and the width where to observe it; what does not change;
   - **every command**, complete and ready to copy, each in its own block, in
     the order the user runs them: `git pull` (scripts of `main`),
     `pwsh ./scripts/Start-Validation.ps1 -PullRequest <number>` (with
     `-Empty` when the screen needs it), `pwsh ./scripts/Stop-Validation.ps1`.
     Never refer to an earlier message nor to the documentation.
   Then wait: nothing else on the chantier until the user answers.
4. **Each feedback** → fixed in the draft PR (code, tests, reference captures
   of what changed, `scripts/Test-Captures.ps1 -Update`), pushed, then step 3
   again with the new head commit.
5. **Explicit validation of the head commit** → only then the reference
   captures are complete, the SonarQube Cloud analysis is handled, and the PR
   is marked « ready for review » (`codex-review-loop`).

A question of the user about this procedure is answered by quoting the
`.speckit/` sections above, never by inventing another way.
