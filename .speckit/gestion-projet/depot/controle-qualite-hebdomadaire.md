---
type: Gouvernance
title: 'Contrôle qualité hebdomadaire'
description: 'Contrôle hebdomadaire du code global de main (SonarQube Cloud, règles de style, tests de mutation), rien de bloquant, ticket de suivi de la dette qualité, référence et entrées à traiter, traitement borné à une Pull Request par semaine, dette existante corrigée au fil de l''eau ; à lire quand le traitement est dû ou pour modifier le contrôle.'
applies_to:
  - '.github/workflows/quality-audit.yml'
  - 'scripts/style-audit.globalconfig'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-12T10:45:00Z }
---

# Contrôle qualité hebdomadaire

La porte de qualité de SonarQube Cloud ne juge que le nouveau code d'une Pull Request ([Analyse de la qualité du code](analyse-de-la-qualite-du-code.md)), et la couverture ne dit pas ce que vérifient les tests ([Outillage de développement](../../choix-implementation/outillage.md)) : un problème déjà présent sur `main`, ou un test qui manque, n'est relevé par aucun contrôle d'une Pull Request. Le contrôle qualité hebdomadaire (`.github/workflows/quality-audit.yml`) les surveille dans la durée, comme le contrôle de sécurité quotidien surveille les vulnérabilités des technologies employées ([Mises à jour des dépendances](mises-a-jour-des-dependances.md)). Son outillage est un choix de l'agent ([Outillage de développement](../../choix-implementation/outillage.md), contrôle qualité).

## Ce qui est relevé

Chaque semaine, et à la demande, sur `main` :

- **SonarQube Cloud, code global** : les problèmes ouverts de l'analyse de `main`, dont les méthodes à la complexité cognitive au-delà du seuil de la règle `S3776` du profil de qualité ; les points sensibles de sécurité non revus ; chaque note du projet (fiabilité, sécurité, maintenabilité, revue de sécurité) autre que A.
- **Règles de style .NET** (catégorie *Style*) qui n'expriment pas une préférence de forme (`scripts/style-audit.globalconfig`) : celles d'un **défaut**, code sans effet, redondant ou trompeur (ex. `IDE0005`, `IDE0004`, `IDE0060`, `IDE0059`), et celles d'une **syntaxe plus récente** du langage non employée là où elle dit la même chose plus directement (ex. `IDE0300` à `IDE0306`, `IDE0290`), ce que demande [Exploitation des technologies retenues](../../contraintes-techniques/exploitation-des-technologies-retenues.md). Les règles de préférence (`var`, accolades, corps d'expression, forme du namespace, `new` ciblé…) en sont exclues.
- **Tests de mutation** : les mutants survivants ou qu'aucun test ne couvre, chacun un test qui manque, d'abord sur `Bdtheque.Domain` ; d'autres projets s'y ajoutent selon ce qu'il apprend.

## Rien ne bloque

- Le contrôle n'est **pas un check requis**, et n'échoue que pour notifier (ci-dessous).
- **Aucune règle de style n'est imposée à la compilation** : leur configuration n'est incluse que par la construction du contrôle.
  - **Alternative écartée (`.editorconfig` et règles de style imposées à la compilation)** : mesuré sur le dépôt (catégorie *Style* entière, sens par défaut des préférences), l'essentiel des écarts sont des désaccords entre le sens par défaut des règles de préférence et le code du dépôt, ou des règles de jugement. Les imposer figerait un modèle de code, contraire à [Exploitation des technologies retenues](../../contraintes-techniques/exploitation-des-technologies-retenues.md), qui demande la syntaxe la plus récente « dès qu'elle améliore » l'élément, jugement que seul l'agent porte au cas par cas.
- **Aucun seuil**, de mutation comme de couverture ([Outillage de développement](../../choix-implementation/outillage.md)), ne fait échouer une Pull Request ni le contrôle : un mutant survivant est un test qui manque, traité comme toute entrée.
- **Aucun outil n'écrit dans le dépôt ni n'ouvre de Pull Request** ([Budget IA](../gouvernance/budget-ia.md)) : le contrôle ne tient qu'un ticket ; une correction est une Pull Request de l'agent, faite de ses commits signés.
- **Fréquence hebdomadaire**, et non à chaque Pull Request : les tests de mutation coûtent du temps d'exécuteur et des quotas ([Intégration continue (CI)](integration-continue-ci.md)), et la dette qu'il suit évolue lentement.

## Ticket de suivi

Le contrôle tient la liste de ses entrées dans le ticket de suivi du harnais qualité (étiquettes `quality-audit` et `harness`, par lesquelles il le retrouve, type `Task`, posé à sa création, [Issues](../revue-tickets/issues.md#tickets-de-suivi-des-harnais)), dont il réécrit la description à chaque exécution, ouvert tant que la liste n'est pas vide.

- **Entrée** : sa source (`sonar`, `hotspot`, `rating`, `style`, `mutant`), la règle ou le mutateur, et le fichier, ou pour un mutant le code muté et son remplacement ; jamais la ligne, qui bouge à chaque modification voisine : une entrée reste la même d'une semaine à l'autre.
- **Seule une exécution sur `main` tient le ticket** : une exécution lancée sur une autre branche, pour essayer une modification du contrôle, ne laisse ses relevés que dans son journal, sans mêler au ticket l'état d'une branche non fusionnée.
- **Notification** : le contrôle n'échoue que lorsqu'il trouve une entrée absente de la liste. GitHub notifie alors l'échec à l'utilisateur qui a créé le workflow, une fois par entrée, et non chaque semaine tant qu'elle reste.
- **Référence, puis entrées à traiter** : la première exécution pose la référence, la **dette existante**, sans rien de nouveau ni d'échec. Ensuite, la description distingue, dans une section « À traiter », les entrées **nouvelles** d'une exécution et celles des exécutions précédentes que l'agent n'a pas encore traitées, tant que le contrôle les relève ; le reste de la liste est la dette existante.

## Traitement des entrées

Au moment que fixe [Traitement du contrôle qualité à la reprise du travail](traitement-du-controle-qualite-a-la-reprise-du-travail.md), les entrées de la section « À traiter » sont traitées, chacune comme un retour de revue ([Revue de code](../revue-tickets/revue-de-code.md), [Traiter un retour](../revue-tickets/revue-codex-bloquante.md#traiter-un-retour)) : contre-vérification, cause de fond ([Qualité du code](../gouvernance/qualite-du-code.md)), puis correction, report tracé ou rejet justifié.

- **Volume borné** (exigence de l'utilisateur) : un traitement par exécution du contrôle au plus, en une seule Pull Request de l'agent, qui réunit ses corrections et suit le cycle de toute Pull Request. Ce que le traitement ne couvre pas reste dans « À traiter », pour le traitement de la semaine suivante. Un mutant survivant se corrige par le test qui manque.
- **Dette existante, au fil de l'eau** (exigence de l'utilisateur) : elle n'est jamais traitée en elle-même ; une de ses entrées se corrige quand un chantier modifie de toute façon le fichier concerné, dans la Pull Request de ce chantier.
- **Report** : un ticket ([Issues](../revue-tickets/issues.md)) porte la correction ; l'entrée reste dans la liste jusqu'à elle, mais quitte « À traiter ».
- **Rejet** : porté, avec sa raison, par le mécanisme de l'outil, pour que le contrôle suivant ne relève plus l'entrée : marquage dans SonarQube Cloud ([Analyse SonarQube Cloud](../revue-tickets/analyse-sonarqube-cloud.md), trace) ; suppression de la règle de style à l'endroit en cause, avec sa justification ; commentaire de désactivation de Stryker.NET sur le mutant (mutant équivalent), avec sa raison.
- **Trace** : l'agent retire de « À traiter » les entrées qu'il a traitées (corrigées, reportées ou rejetées), et ajoute à la description du ticket, après la liste, une section « Traitement du `<date>` », qui donne la conclusion du traitement : rien à faire, ou la Pull Request et les tickets qu'il a produits.
  - **Alternative écartée (ticket séparé pour la dette existante)** : la référence du contrôle la porte déjà, dans son ticket de suivi.
