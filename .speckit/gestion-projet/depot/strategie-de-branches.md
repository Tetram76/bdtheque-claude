---
type: Gouvernance
title: 'Stratégie de branches'
description: 'Branches persistantes (main et branches de release seules), nommage des branches (feat/, fix/, chore/), branche de travail tenue à jour de sa branche de destination.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-10T13:53:56Z }
---

# Stratégie de branches

- `main` : branche stable, reflète l'état livrable du projet
- `feat/<sujet>` : développement de fonctionnalités
- `fix/<sujet>` : corrections de bugs
- `chore/<sujet>` : maintenance (config, refacto, outillage, CI)
- **Branches persistantes** : seules `main` et les branches de release le sont. Toute autre branche disparaît une fois sa Pull Request fusionnée ou abandonnée : un travail à conserver sans être fusionné l'est dans le dépôt lui-même (ex. un prototype dans `poc/`), jamais dans une branche.
- **Branche de travail tenue à jour de sa branche de destination**, quelle qu'elle soit, conflit ou non (exigence de l'utilisateur) : le code est écrit, validé par l'utilisateur, construit, testé, analysé et revu sur l'état actuel de la branche où il sera fusionné. Un retard découvert tard oblige à reprendre ce qui a été écrit et validé sur une base périmée, et un conflit se résout tôt, tant qu'il est petit. Le Ruleset de `main` ne l'exige pas ([Configuration du repository GitHub](configuration-du-repository-github.md)) : l'agent s'en charge.
  - **Moment** : la branche part de sa branche de destination à jour ; l'agent y intègre ensuite la branche de destination dès qu'elle a avancé, à chaque reprise du travail sur la branche, avant d'y écrire du code, avant de soumettre un état à la validation fonctionnelle ([Validation fonctionnelle](../revue-tickets/validation-fonctionnelle.md)), avant chaque push et avant de passer la PR en « prête pour la revue ».
  - **PR prête pour la revue** : chaque push y déclenche une revue Codex ([Revue Codex (bloquante)](../revue-tickets/revue-codex-bloquante.md)). L'intégration y reste locale jusqu'au push suivant d'une correction, sauf en cas de conflit, qui prive la PR de ses checks ([Intégration continue (CI)](integration-continue-ci.md)) ; jamais de push pendant une revue en cours, ni tant que le budget de Codex est épuisé.
  - **Méthode, la plus appropriée au cas** : rebase de la branche de travail sur sa branche de destination, ou fusion de la branche de destination dans la branche de travail. Le squash merge ([Configuration du repository GitHub](configuration-du-repository-github.md)) rend l'historique de la branche de destination identique dans les deux cas ; le choix porte sur la branche de travail elle-même :
    - **rebase** tant qu'aucune revue ne désigne les commits de la PR (brouillon, ou aucun fil de revue) : la branche reste une suite de commits lisibles, chacun sur l'état actuel de sa destination. Le push qui le publie est forcé, avec `--force-with-lease`, qui refuse d'écraser un commit poussé entre-temps : le push forcé n'est interdit que sur `main` ([Configuration du repository GitHub](configuration-du-repository-github.md)) ;
    - **fusion** quand des revues ou des commentaires désignent déjà des commits de la PR, qu'un rebase rendrait introuvables, ou quand un conflit, résolu une fois par la fusion, serait à résoudre commit par commit par un rebase.
