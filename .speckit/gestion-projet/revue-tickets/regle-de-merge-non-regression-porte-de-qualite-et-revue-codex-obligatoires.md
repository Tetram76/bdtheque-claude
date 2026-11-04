---
type: Gouvernance
title: 'Règle de merge : non-régression, porte de qualité et revue Codex obligatoires'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Règle de merge : non-régression, porte de qualité et revue Codex obligatoires

> **Une Pull Request ne peut être fusionnée que si la non-régression est confirmée, que la porte de qualité de SonarQube Cloud est réussie ET que Codex l'a approuvée**, et, pour une Pull Request soumise à validation fonctionnelle, **que l'utilisateur l'a validée**.

- Tout merge sur `main` est conditionné à la **réussite des checks de non-régression** (pipeline CI), à la **réussite de la porte de qualité** de SonarQube Cloud sur le commit de tête ([Analyse SonarQube Cloud](/gestion-projet/revue-tickets/analyse-sonarqube-cloud.md)) **et** à l'**approbation de la revue Codex** (voir « Revue de code » ci-dessous), ainsi qu'à la **validation fonctionnelle de l'utilisateur** lorsque la Pull Request y est soumise ([Validation fonctionnelle](/gestion-projet/revue-tickets/validation-fonctionnelle.md)).
- Les contrôles de non-régression **doivent être exécutés localement avant tout push** sur la branche de PR qui modifie un élément qu'ils vérifient (code, projets et configuration de la solution, workflow CI, fichiers Docker) — pour détecter les régressions au plus tôt et ne pas attendre le CI distant. Un push qui ne modifie que de la documentation (`.speckit/`, fichiers Markdown, skills) en est dispensé : aucun de ces contrôles ne la vérifie, et le CI reste le filet de sécurité.
- Le CI (GitHub Actions) constitue le filet de sécurité final et le verrou technique sur le merge.
- La réussite de la CI et celle de la porte de qualité sont **imposées techniquement** par le Ruleset GitHub (required status checks, [Configuration du repository GitHub](/gestion-projet/depot/configuration-du-repository-github.md)) : une PR dont un check requis n'a pas réussi ne peut pas être fusionnée.
- L'approbation de Codex n'est imposée par aucun mécanisme GitHub : elle reste une contrainte de processus, que l'agent vérifie avant tout merge.
- L'**analyse SonarQube Cloud** du commit de tête est **traitée** avant le merge ([Analyse SonarQube Cloud](/gestion-projet/revue-tickets/analyse-sonarqube-cloud.md)) : chacun de ses nouveaux problèmes est corrigé, différé ou rejeté, et sa porte de qualité est réussie.
