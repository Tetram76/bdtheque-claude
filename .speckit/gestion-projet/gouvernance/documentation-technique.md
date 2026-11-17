---
type: Gouvernance
title: 'Documentation technique'
description: 'Trois périmètres documentaires (.speckit normatif, documentation technique descriptive de docs/tech, aide utilisateur réservée), rien de ce qui n'existe pas, consultation guidée subordonnée au .speckit, profondeur et diagrammes, mise à jour dans la même Pull Request que le code, contrôles déterministes et obligations sémantiques ; s''applique à tout chantier qui touche le code, la configuration ou l''outillage.'
tags: [socle]
applies_to:
  - 'docs/tech/**'
  - 'scripts/Build-TechnicalDocumentation.ps1'
  - 'scripts/Get-TechnicalDocumentationImpact.ps1'
  - 'scripts/Update-TechnicalReferences.ps1'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-17T10:50:00Z }
---

# Documentation technique

Le projet tient, à côté de sa spécification, une **documentation technique** : la description de l'application telle qu'elle est réellement implémentée, pour que tout agent (ou développeur) comprenne un sous-système, réponde à une question générale ou situe un changement **avant** de lire le code, sans charger plus que ce qu'exige son sujet. Ses outils et sa forme sont des choix de l'agent ([Documentation technique : mise en œuvre](../../choix-implementation/documentation-technique.md)) ; la façon de la lire, de la construire et de la vérifier est décrite par la documentation elle-même (`docs/tech/documentation/consulter-et-maintenir.md`).

## Trois périmètres, distincts

- **`.speckit/`, normatif** : exigences, contraintes, gouvernance, modèle métier, choix d'implémentation et leurs raisons ([Gouvernance documentaire](gouvernance-documentaire.md)). Il prime sur la documentation technique comme sur tout le reste.
- **`docs/tech/`, descriptif** : comment l'application est structurée et fonctionne (responsabilités, échanges, frontières, flux, mécanismes, invariants non évidents, exploitation, tests), avec ses points d'entrée dans le code. Elle n'autorise jamais un écart au `.speckit/` et ne recopie pas ses exigences ni ses raisons : elle y renvoie.
- **Documentation utilisateur et aide intégrée à l'application**, réservées à un chantier à venir : rien de la documentation technique n'en tient lieu, ne leur impose une forme, un outil ou un emplacement, ni n'en crée de page anticipée. Le contenu de l'aide contextuelle reste un livrable de l'agent ([Périmètre de l'agent](perimetre-de-l-agent.md)).

## Ce qu'elle affirme

- **Le réel, vérifié** : chaque affirmation correspond au code, aux tests et à la configuration présents, vérifiée à son écriture et à chaque mise à jour ; un fait présenté comme avéré n'est jamais « non vérifié ».
- **Rien de ce qui n'existe pas** : la documentation ne décrit, ni ne mentionne, rien que le code ne réalise, même annoncé comme prévu : ce que le `.speckit/` prévoit sans qu'il soit réalisé n'y entre qu'avec sa réalisation, par la Pull Request qui le réalise (exigence de l'utilisateur).
- **Un état, pas un historique** : comme un document de référence du `.speckit/`, une page décrit l'état actuel ; un passage devenu faux est corrigé ou retiré.
- **Divergence avec le code analysée, jamais recopiée** : un écart entre la documentation et le code est examiné ; si le code contrevient au `.speckit/`, c'est le code qui est corrigé, ou la spécification qui évolue selon [Prise de décision](prise-de-decision.md), jamais la documentation qui s'aligne sur un code incorrect.
- **Une orientation, pas une preuve** : ce qu'une page dit d'une méthode ou d'un workflow se vérifie dans le code, ou par une exécution, dès qu'une réponse ou une modification exige cette précision.

## Profondeur

- **Vue d'ensemble, toujours** : contexte, conteneurs et composants, responsabilités, échanges, frontières de sécurité, déploiement.
- **Mécanismes, selon leur valeur** : ce qui traverse plusieurs composants, et ce qui est critique (sécurité, persistance, erreurs, migrations…), quand il est réellement présent.
- **Détail interne, par exception** : un algorithme délicat, une concurrence, un invariant critique, un comportement que le code ne laisse pas deviner.
- **Critère d'arrêt** : une page cesse de s'approfondir là où le code, les tests ou une référence générée donnent l'information facilement et sûrement. Ni classe par classe, ni méthode par méthode, ni opération courante ; aucun quota de pages ni de diagrammes. Une page devenue inutile après un refactoring est simplifiée ou retirée.

## Diagrammes

La documentation est **visuelle par construction** : un schéma fait saisir une vue ou un mécanisme plus vite que le texte équivalent, que le texte complète (rôles, invariants, exceptions, renvois, points d'entrée).

- **Écrits en texte, versionnés**, rendus de façon reproductible, sans éditeur graphique ni service distant.
- **Une question par diagramme**, énoncée avec lui, et sa nature : *généré* depuis une source qui fait autorité (préféré dès que la génération est fiable), ou *interprétatif*, tenu à la main, dont le rendu valide la forme, jamais la fidélité au code.
- **Lisible** : intitulés explicites, relations compréhensibles ; découpé quand il ne l'est plus. Le rendu de chaque diagramme est vérifié, pas seulement sa syntaxe, et l'agent inspecte lui-même le rendu de ceux qu'il crée ou modifie ([Qualité du code](qualite-du-code.md), rendu vérifié par l'agent).

## Consultation

Subordonnée à la lecture du `.speckit/` qu'impose `AGENTS.md`, qu'elle ne raccourcit ni ne remplace :

- **Seulement si le sujet le justifie** (implémentation, analyse technique, chantier qui touche le code) : aucun « socle » technique, aucune lecture de tout `docs/tech/`, de toutes ses cartes, ni de son contenu chargé par un fichier d'instructions, une règle, un skill ou une session.
- **Guidée** : la carte racine (`docs/tech/index.md`), puis la carte du domaine, puis les seules pages utiles ; une recherche textuelle ciblée si les cartes ne suffisent pas.
- **Selon l'intention** : question générale, la documentation suffit si elle répond, la réponse disant qu'elle vient d'elle ; question précise ou certitude exigée sur l'état effectif, la documentation oriente vers le code, les tests, la configuration ou une vérification, proportionnés ; modification du code, elle situe les responsabilités et les points d'entrée, puis le code et les tests nécessaires sont lus en entier avant d'écrire.

## Maintenance, à chaque chantier

La documentation change dans la **même Pull Request** que ce qu'elle décrit :

1. **Avant d'implémenter** : les pages du sujet sont trouvées par les cartes, et lues ; elles ne dispensent pas de lire le code.
2. **Pendant** : un changement de conception met à jour `choix-implementation/` selon sa gouvernance, sans confondre la raison d'un choix et la description de son exécution.
3. **Avant le push** : l'impact documentaire de **chaque** changement (sources, tests, configuration, déploiement, schémas, API, outillage) est évalué ; les pages et diagrammes concernés sont mis à jour, les références générées régénérées, ou bien un commit de la Pull Request dit, par un *trailer* Git `Documentation-technique:`, pourquoi les pages concernées n'ont pas à changer. Une Pull Request n'est pas tenue de modifier `docs/tech/` : elle l'est de montrer que l'impact a été évalué.
4. **Avant la livraison** : les contrôles documentaires passent, et texte, diagrammes et liens décrivent l'état livré.
5. **Refactoring** : renvois, motifs de fichiers et diagrammes suivent le code ; l'obsolète est retiré, les cartes mises à jour.

Une couverture encore partielle est dite telle, avec l'inventaire de ce qui reste à documenter et les tickets qui le portent ; une page vide ou de pure forme n'est jamais présentée comme une documentation.

## Contrôles et leurs limites

- **Déterministes, bloquants** dans le CI : structure des cartes et des pages, liens, fichiers et symboles cités, renvois au `.speckit/` ; impact d'un changement (page concernée modifiée, ou *trailer* présent) ; références générées identiques à leurs sources ; construction du site et rendu des diagrammes sans erreur ni illisibilité détectable. Ils ne demandent à une Pull Request documentaire ni les tests de l'application, ni ses images.
- **Sémantiques, obligations de travail et de revue** : que le texte et les diagrammes décrivent fidèlement le code, que l'impact a été réellement évalué, qu'une raison donnée est juste. Aucun contrôle automatique, ni revue par un agent, ne le garantit ; aucun n'est présenté comme le faisant.
