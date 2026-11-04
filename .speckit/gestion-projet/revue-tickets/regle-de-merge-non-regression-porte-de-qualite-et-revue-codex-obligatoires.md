---
type: Gouvernance
title: 'Règle de merge : non-régression, porte de qualité et revue Codex obligatoires'
description: 'Conditions de fusion d''une PR : non-régression, porte de qualité, approbation de Codex, validation fonctionnelle s''il y a lieu ; contrôles locaux avant push.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T19:44:14Z }
---

# Règle de merge : non-régression, porte de qualité et revue Codex obligatoires

> **Une Pull Request ne peut être fusionnée que si la non-régression est confirmée, que la porte de qualité de SonarQube Cloud est réussie ET que Codex l'a approuvée**, et, pour une Pull Request soumise à validation fonctionnelle, **que l'utilisateur l'a validée**.

- Tout merge sur `main` est conditionné à la **réussite des checks de non-régression** (pipeline CI), à la **réussite de la porte de qualité** de SonarQube Cloud sur le commit de tête ([Analyse SonarQube Cloud](/gestion-projet/revue-tickets/analyse-sonarqube-cloud.md)) **et** à l'**approbation de la revue Codex** (voir « Revue de code » ci-dessous), ainsi qu'à la **validation fonctionnelle de l'utilisateur** lorsque la Pull Request y est soumise ([Validation fonctionnelle](/gestion-projet/revue-tickets/validation-fonctionnelle.md)).
- Les contrôles de non-régression **doivent être exécutés localement avant tout push** sur la branche de PR, pour détecter les régressions au plus tôt et ne pas attendre le CI distant ; **chacun l'est si le push modifie ce qu'il vérifie**, et seulement dans ce cas, le CI restant le filet de sécurité :
  - la compilation, pour toute modification du code, des projets et de la configuration de la solution, commentaires compris : elle valide les commentaires de documentation ;
  - les tests, pour toute modification du comportement d'un élément qu'ils vérifient (code, projets et configuration de la solution, workflow CI, fichiers Docker) ; une modification qui ne porte que sur des commentaires ne change le comportement de rien ;
  - les captures de référence des écrans ([Thèmes : mise en œuvre](/choix-implementation/socle/themes.md), captures de référence), pour une modification qui peut changer le rendu d'un écran : composants, styles, scripts et ressources du frontend, thèmes, polices, références des captures ;
  - aucun pour une modification de la seule documentation (`.speckit/`, fichiers Markdown, skills), qu'aucun de ces contrôles ne vérifie.
- Le CI (GitHub Actions) constitue le filet de sécurité final et le verrou technique sur le merge.
- La réussite de la CI et celle de la porte de qualité sont **imposées techniquement** par le Ruleset GitHub (required status checks, [Configuration du repository GitHub](/gestion-projet/depot/configuration-du-repository-github.md)) : une PR dont un check requis n'a pas réussi ne peut pas être fusionnée.
- L'approbation de Codex n'est imposée par aucun mécanisme GitHub : elle reste une contrainte de processus, que l'agent vérifie avant tout merge.
- L'**analyse SonarQube Cloud** du commit de tête est **traitée** avant le merge ([Analyse SonarQube Cloud](/gestion-projet/revue-tickets/analyse-sonarqube-cloud.md)) : chacun de ses nouveaux problèmes est corrigé, différé ou rejeté, et sa porte de qualité est réussie.
