---
type: Gouvernance
title: 'Chantiers en parallèle'
description: 'Lot de chantiers toujours réalisés en parallèle, chacun dans sa session, 3 au plus à la fois selon les ressources, nombre du lot fixé par l''utilisateur, choix parmi les tickets existants et ordre de priorité, indépendance, ressources évaluées à chaque lancement ; à lire pour lancer un lot ou l''un de ses chantiers.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-11T10:42:55Z }
---

# Chantiers en parallèle

À la demande de l'utilisateur, l'agent réalise un **lot de chantiers en parallèle** : chaque chantier est confié à une session à part, dans son propre worktree, qui le mène de bout en bout selon les règles du `.speckit/` (lecture du `.speckit/`, découpage, Pull Request, validation fonctionnelle, revue Codex, fusion, clôture). La session qui lance le lot ne réalise aucun chantier elle-même : elle choisit les chantiers, les lance, et rend compte de leur avancement à l'utilisateur.

## Le lot

- **Toujours en parallèle** : les chantiers du lot sont toujours menés en parallèle, jamais l'un après l'autre par choix ; seules les dépendances entre chantiers (§ Choix des chantiers) en font attendre un.
- **Nombre de chantiers en même temps** : **3 au plus** (exigence de l'utilisateur), dans la limite de ce que permettent les ressources du poste (§ Ressources).
- **Nombre de chantiers du lot** : l'utilisateur le fixe. Il n'est pas figé : l'utilisateur peut le changer à tout moment, et un nombre qu'il n'a pas donné n'est pas inventé par l'agent, qui le lui demande.
- **Place libre** : un chantier occupe sa place jusqu'à la fusion de sa Pull Request. Une session qui attend l'utilisateur (validation fonctionnelle, décision) occupe toujours la sienne. À chaque fusion, l'agent évalue de nouveau les ressources et lance, sans attendre de demande, autant de chantiers du lot qu'elles le permettent.
- **Sous-tickets** : un ticket découpé par une session ([Suivi par tickets](../revue-tickets/suivi-par-tickets.md), découpage au démarrage d'un chantier) donne autant de chantiers du lot que de sous-tickets, choisis et lancés comme les autres (dépendances comprises). Un sous-ticket est confié à la session qui a découpé le ticket si son contexte s'y prête : ce qu'elle sait sert le sous-ticket, et sa conversation n'est pas trop chargée pour le mener de bout en bout. Sinon, il l'est à une nouvelle session.

## Choix des chantiers

- **Tickets existants seulement** : un chantier ne s'invente pas. Les chantiers sont choisis parmi les tickets ouverts du dépôt ; l'agent ne fouille ni l'historique du CI ni le code pour en trouver de nouveaux.
- **Choix refait à chaque lancement** : avant de lancer un chantier, l'agent relit les tickets ouverts, dont ceux que les sessions du lot ont créés entre-temps (sous-tickets, tickets ouverts sur un problème relevé), et les Pull Requests ouvertes.
- **Ordre de priorité** :
  1. **Stabilisation du CI** : un ticket qui corrige une instabilité du CI (test instable, échec intermittent). Retirer au CI une cause d'échec extérieure à son contenu (ex. les quotas d'un registre) n'est pas une stabilisation, mais une optimisation, qui suit l'ordre commun.
  2. **Plan d'implémentation** : un chantier listé dans [`suivi-implementation/`](../../suivi-implementation/index.md), sous-tickets compris.
  3. **Jalon de la version en cours** : un ticket rattaché au jalon de la prochaine version ([Versions et releases](../depot/versions-et-releases.md)).
  4. Les autres tickets.
- **Indépendance** : un chantier qui modifierait les mêmes fichiers qu'un chantier en cours (même écran, même thème, même workflow) est écarté au profit du suivant, tant que l'autre n'est pas fusionné.
- **Dépendances** : deux chantiers dont l'un doit voir le résultat de l'autre (ex. deux tickets qui modifient le même workflow, l'un devant partir de ce que l'autre décide) sont menés l'un après l'autre, jamais en même temps.
- **Décisions de l'utilisateur** : un ticket qui attend une décision de l'utilisateur avant toute réalisation reste choisissable ; sa session soumet ces décisions à l'utilisateur, un point à la fois ([Prise de décision](prise-de-decision.md)).

## Ressources

- **Évaluées à chaque lancement** : avant de lancer un chantier, l'agent évalue les ressources du poste à cet instant ([Règles de travail de l'agent](regles-de-travail-de-l-agent.md), ressources d'une commande) : processeurs et mémoire libres, espace disque, service Docker et ressources de sa machine virtuelle, exécutions lourdes déjà en cours (tests de bout en bout, captures, environnement de validation fonctionnelle). Une évaluation faite pour un lancement ne vaut pas pour le suivant.
- **Parallélisme effectif** : le nombre de chantiers menés en même temps est le plus petit entre le maximum de 3 (§ Le lot) et celui que les ressources permettent. Une ressource qui ne suffit pas à un chantier de plus retarde son lancement, ce que l'agent signale à l'utilisateur avec la ressource en cause.
- **Exécutions lourdes** : les sessions du lot se partagent le poste. Chacune vérifie, avant une commande qui démarre des conteneurs ou des navigateurs, qu'aucune autre session n'a d'exécution lourde en cours, et dimensionne la sienne d'après les ressources réelles ; l'environnement de validation fonctionnelle n'est lancé que pour une session à la fois.
