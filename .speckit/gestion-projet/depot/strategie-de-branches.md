---
type: Gouvernance
title: 'Stratégie de branches'
description: 'Modèle trunk-based, branche de release créée au besoin seulement, pour un correctif d''une version dépassée, branches persistantes (main et branches de release seules), nommage des branches (feat/, fix/, chore/), branche de travail tenue à jour de sa branche de destination.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-21T18:30:00Z }
---

# Stratégie de branches

- `main` : branche stable, reflète l'état livrable du projet
- `feat/<sujet>` : développement de fonctionnalités
- `fix/<sujet>` : corrections de bugs
- `chore/<sujet>` : maintenance (config, refacto, outillage, CI)
- **Modèle *trunk-based*** (décision de l'utilisateur) : tout le développement se fait sur `main`, par des branches courtes et leurs Pull Requests ; une version se publie par un tag posé sur `main` ([Versions et releases](versions-et-releases.md)).
  - **Branche de release, créée au besoin seulement** : `release/<majeur>.<mineur>` (ex. `release/0.1`) n'est créée que pour publier une version corrective d'une version que `main` a déjà dépassée, depuis le tag de cette version. On n'y développe rien : un correctif est fait et testé sur `main`, par sa Pull Request, puis reporté sur la branche de release (*cherry-pick*), qui reçoit le tag de la version corrective. Elle n'est jamais fusionnée dans `main`.
  - **Alternative écartée (git-flow : branches `develop` et `release/*` permanentes)** : son auteur la réserve aux logiciels qui maintiennent plusieurs versions en service ; pour une livraison continue, il recommande lui-même un flux plus simple (nvie.com, *A successful Git branching model*, note de 2020).
  - **Alternative écartée (branche de release créée à chaque publication)** : une branche sans objet tant qu'aucun correctif n'est à publier pour une version dépassée ; *Trunk-Based Development* recommande de publier depuis le tronc, et de ne créer la branche, depuis le tag, qu'au besoin (trunkbaseddevelopment.com, *Branch for release*).
- **Branches persistantes** : seules `main` et les branches de release le sont. Toute autre branche disparaît une fois sa Pull Request fusionnée ou abandonnée : un travail à conserver sans être fusionné l'est dans le dépôt lui-même (ex. un prototype dans `poc/`), jamais dans une branche.
- **Branche de travail tenue à jour de sa branche de destination**, quelle qu'elle soit, conflit ou non (exigence de l'utilisateur) : le code est écrit, validé par l'utilisateur, construit, testé, analysé et revu sur l'état actuel de la branche où il sera fusionné. Un retard découvert tard oblige à reprendre ce qui a été écrit et validé sur une base périmée, et un conflit se résout tôt, tant qu'il est petit. Le Ruleset de `main` ne l'exige pas ([Configuration du repository GitHub](configuration-du-repository-github.md)) : l'agent s'en charge.
  - **Moment** : la branche part de sa branche de destination à jour ; l'agent y intègre ensuite la branche de destination dès qu'elle a avancé, à chaque reprise du travail sur la branche, avant d'y écrire du code, avant de soumettre un état à la validation fonctionnelle ([Validation fonctionnelle](../revue-tickets/validation-fonctionnelle.md)), avant chaque push et avant de passer la PR en « prête pour la revue ».
  - **PR prête pour la revue** : chaque push y déclenche une revue Codex ([Revue Codex (bloquante)](../revue-tickets/revue-codex-bloquante.md)). L'intégration y reste locale jusqu'au push suivant d'une correction, sauf en cas de conflit, qui prive la PR de ses checks ([Intégration continue (CI)](integration-continue-ci.md)) ; jamais de push pendant une revue en cours, ni tant que le budget de Codex est épuisé.
  - **Méthode, la plus appropriée au cas** : rebase de la branche de travail sur sa branche de destination, ou fusion de la branche de destination dans la branche de travail. Le squash merge ([Configuration du repository GitHub](configuration-du-repository-github.md)) rend l'historique de la branche de destination identique dans les deux cas ; le choix porte sur la branche de travail elle-même :
    - **rebase** tant qu'aucune revue ne désigne les commits de la PR (brouillon, ou aucun fil de revue) : la branche reste une suite de commits lisibles, chacun sur l'état actuel de sa destination. Le push qui le publie est forcé, avec `--force-with-lease`, qui refuse d'écraser un commit poussé entre-temps : le push forcé n'est interdit que sur `main` ([Configuration du repository GitHub](configuration-du-repository-github.md)) ;
    - **fusion** quand des revues ou des commentaires désignent déjà des commits de la PR, qu'un rebase rendrait introuvables, ou quand un conflit, résolu une fois par la fusion, serait à résoudre commit par commit par un rebase.
