---
type: Gouvernance
title: 'Règle de merge : non-régression, porte de qualité et revue Codex obligatoires'
description: 'Conditions de fusion d''une PR : non-régression, porte de qualité, approbation de Codex, validation fonctionnelle s''il y a lieu ; contrôles locaux avant push, ciblés sur la modification, suites complètes laissées au CI.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-23T20:30:00Z }
---

# Règle de merge : non-régression, porte de qualité et revue Codex obligatoires

> **Une Pull Request ne peut être fusionnée que si la non-régression est confirmée, que la porte de qualité de SonarQube Cloud est réussie ET que Codex l'a approuvée**, et, pour une Pull Request soumise à validation fonctionnelle, **que l'utilisateur l'a validée**.

- Tout merge sur `main` est conditionné à la **réussite des checks de non-régression** (pipeline CI), à la **réussite de la porte de qualité** de SonarQube Cloud sur le commit de tête ([Analyse SonarQube Cloud](analyse-sonarqube-cloud.md)) **et** à l'**approbation de la revue Codex** (voir « Revue de code » ci-dessous), ainsi qu'à la **validation fonctionnelle de l'utilisateur** lorsque la Pull Request y est soumise ([Validation fonctionnelle](validation-fonctionnelle.md)).
- **Contrôles locaux avant push, ciblés sur la modification** (décision de l'utilisateur) : avant tout push sur la branche de PR, il suffit que l'agent exécute localement les contrôles de non-régression qui portent sur ce que le push modifie, pour détecter au plus tôt ce que la modification casse sans attendre le CI distant :
  - la compilation, pour toute modification du code, des projets et de la configuration de la solution, commentaires compris : elle valide les commentaires de documentation ;
  - les tests qui portent sur ce que la modification change (code, projets et configuration de la solution, workflow CI, fichiers Docker), y compris les tests de bout en bout et les captures de référence ([Thèmes : mise en œuvre](../../choix-implementation/socle/themes.md), captures de référence) de l'écran modifié, dans les thèmes utiles ; une modification qui ne porte que sur des commentaires ne change le comportement de rien ;
  - les exécutions répétées qu'exige un test qui a un facteur temps ([Qualité du code](../gouvernance/qualite-du-code.md), tests qui ont un facteur temps), que le CI ne fait pas ;
  - aucun pour une modification de la seule documentation (`.speckit/`, fichiers Markdown, skills), qu'aucun de ces contrôles ne vérifie.

  **Les suites complètes sont laissées au CI**, qui les exécute de toute façon : sur le poste, les tests de bout en bout de toute l'application et les captures de tous les écrans dans chaque thème occupent Docker et font attendre les autres sessions ([Chantiers en parallèle](../gouvernance/chantiers-en-parallele.md#ressources), exécutions lourdes), pour un résultat que le CI donne à chaque push.

  Le cadre de ces pushs ne change pas : un push sur une PR en brouillon ne déclenche aucune revue Codex ; une PR ne passe en « prête pour la revue » qu'avec un CI vert ([Ouverture de la PR](revue-codex-bloquante.md#ouverture-de-la-pr)) ; après ce passage, chaque push déclenche une revue Codex et porte toujours un état complet et vérifié ([Budget IA](../gouvernance/budget-ia.md)).
- Le CI (GitHub Actions) constitue le filet de sécurité final et le verrou technique sur le merge ; sur une Pull Request, il n'exécute lui aussi que les tests que ses modifications peuvent faire échouer ([Intégration continue (CI)](../depot/integration-continue-ci.md)).
- La réussite de la CI et celle de la porte de qualité sont **imposées techniquement** par le Ruleset GitHub (required status checks, [Configuration du repository GitHub](../depot/configuration-du-repository-github.md)) : une PR dont un check requis n'a pas réussi ne peut pas être fusionnée.
- L'approbation de Codex n'est imposée par aucun mécanisme GitHub : elle reste une contrainte de processus, que l'agent vérifie avant tout merge.
- L'**analyse SonarQube Cloud** du commit de tête est **traitée** avant le merge ([Analyse SonarQube Cloud](analyse-sonarqube-cloud.md)) : chacun de ses nouveaux problèmes est corrigé, différé ou rejeté, et sa porte de qualité est réussie.
