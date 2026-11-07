---
type: Gouvernance
title: 'Maintenance du .speckit'
description: 'Format OKF des concepts (en-tête, socle, applies_to), carte, log.md, contrôles du bundle, forme des renvois, y compris dans les tickets ouverts ; à suivre pour toute modification du .speckit.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Maintenance du .speckit

- Les concepts du `.speckit/` sont des **documents vivants** : l'agent peut les restructurer à tout moment (fusion, découpage, déplacement, création de concepts ou de dossiers) si cela améliore leur clarté, leur cohérence ou le coût de leur lecture.
- Toute restructuration est faite sans validation préalable, dans le même esprit d'autonomie qui régit les décisions techniques. Elle porte sur la **forme** uniquement : elle ne modifie, n'ajoute ni ne retire aucun contenu dont l'utilisateur est seul décisionnaire (cf. [Qui décide du contenu](gouvernance-documentaire.md#qui-décide-du-contenu)).
- **Concept** : un fichier Markdown par concept, consacré à un seul sujet, assez petit pour qu'un chantier n'ait à lire que ce qui le concerne ; un concept qui traite plusieurs sujets indépendants est découpé. Son en-tête YAML (OKF) porte :
  - `type`, obligatoire, parmi : `Gouvernance`, `Périmètre` (ce que contient un dossier), `Règle métier`, `Modèle métier`, `Contrainte technique`, `Choix d'implémentation`, `Plan d'implémentation`, `Journal`, `Référence visuelle` ;
  - `title`, le titre du concept, repris par son titre de niveau 1 ;
  - `description`, obligatoire : une phrase qui dit ce que porte le concept et **quand il s'applique**, telle que la carte la présente ;
  - `tags` : `socle` pour un concept qui s'applique quel que soit le chantier (toujours lu, `AGENTS.md`) ; un concept qui ne s'applique qu'à un moment précis du travail (création d'une Pull Request, push, merge…) n'est pas au socle : la table des moments d'`AGENTS.md` le désigne pour ce moment ;
  - `applies_to` : les motifs (`*`, `**`) des fichiers du dépôt que le concept régit, lorsqu'il régit du code existant ; chaque motif désigne au moins un fichier ;
  - `generated: { by, at }` : l'auteur (`claude-code/<modèle>`, ou `human:<identifiant>`) et l'instant (ISO 8601, UTC) de la dernière modification du contenu, mis à jour par toute modification ;
  - `verified` : seulement pour un contenu que l'utilisateur a explicitement validé, `{ by: human:<identifiant>, at: <instant> }`, jamais apposé d'office.
- **Carte** : chaque dossier a un `index.md`, sans en-tête (sauf celui de la racine, qui déclare `okf_version`), qui ne fait que lister ses concepts et ses sous-dossiers, chacun avec sa description, dans l'ordre de lecture. Tout concept ajouté, retiré, déplacé ou dont la description change met à jour son `index.md` dans le même commit.
- **[`log.md`](../../log.md)** : toute Pull Request qui modifie le `.speckit/` y ajoute, dans son propre contenu, une entrée courte : ce qui a changé et la PR qui le porte ; le pourquoi reste dans le concept modifié. Sa forme est celle d'OKF (§ 9, *Log files*) : une liste à plat d'entrées regroupées sous le titre de leur date (`## AAAA-MM-JJ`, ISO 8601), les dates les plus récentes en premier ; une entrée s'ajoute sous la date du jour, créée en tête du journal si elle n'y est pas encore.
- **Contrôles** : `SpeckitStructureTests` (`Bdtheque.Infrastructure.Tests`) vérifie la structure du bundle (en-têtes, `type`, `description`, cartes, liens internes, `applies_to`) ; `scripts/Get-SpeckitConcepts.ps1` liste les concepts dont `applies_to` couvre les fichiers modifiés d'une branche ; le hook du dépôt (`.claude/settings.json`) rappelle, avant chaque commande d'un moment, les concepts que la table des moments d'`AGENTS.md` lui associe.
- **Renvois** : un renvoi est un lien vers le concept qui porte ce qu'il désigne, par son **chemin relatif** depuis le fichier qui le contient (`[Titre](../dossier/concept.md)`, `[Titre](concept-voisin.md)`), suivi d'une ancre pour une section du concept (`#titre-de-la-section`) ; un dossier entier est désigné par son index (``[`fonctionnel/`](../../fonctionnel/index.md)``). Un lien qui commence par `/` est interdit, ce que vérifie `SpeckitStructureTests`.
  - **Alternative écartée (chemin depuis la racine du bundle, `/dossier/concept.md`, forme que recommande OKF)** : stable quand un concept est déplacé, mais GitHub et les éditeurs résolvent un chemin qui commence par `/` depuis la racine du dépôt, pas depuis celle du `.speckit/` : tous les renvois mèneraient dans le vide pour qui les suit. OKF admet aussi la forme relative (§ 6.1), que tous résolvent.

  Hors du `.speckit/` (code, tests, configuration, skills), le renvoi est le chemin du concept depuis la racine du `.speckit/` (ex. `gestion-projet/depot/integration-continue-ci.md`). Tout déplacement ou renommage d'un concept ou d'une section met à jour les renvois qui le désignent, dans le `.speckit/` comme dans le reste du dépôt.
- **Tickets ouverts** : leur description renvoie au `.speckit/` comme le reste du dépôt, et un ticket qui reste à réaliser ne mène jamais à un concept ou à une section qui n'existe plus. La Pull Request qui déplace ou renomme un concept ou une section met donc à jour, à sa fusion, les renvois des tickets ouverts qui le désignent ; de même pour un nom que le `.speckit/` change (ex. celui d'un thème), repris par les tickets ouverts qui le citent. Ces tickets sont hors du dépôt : aucun contrôle ne le vérifie, et la recherche de leurs renvois fait partie de cette fusion.
