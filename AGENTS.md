# .speckit — Source de Vérité Absolue

Le dossier `.speckit/` est la **source de vérité absolue et contraignante** du projet : il prime sur toute autre information, historique de conversation, supposition ou connaissance générale de l'agent. Chaque règle qu'il contient — gouvernance, fonctionnel, modèle métier ou contraintes techniques — a la **même force obligatoire**, sans distinction de « majeur » ou « mineur ». Un manquement, même ponctuel ou sur un point jugé secondaire, est une violation, pas un détail.

## Fichiers et leur périmètre

Le périmètre détaillé de chaque fichier est exposé en tête du fichier lui-même.

| Fichier | Contenu |
| --- | --- |
| `gestion-projet.md` | Gouvernance du projet (branches, commits, outillage, CI/CD), hors application elle-même. |
| `fonctionnel.md` | Fonctionnalités et règles métier de l'application. |
| `modele-metier.md` | Modèle du domaine : entités, attributs, relations, contraintes d'intégrité, glossaire. |
| `contraintes-techniques.md` | Contraintes techniques **imposées** (et non-contraintes explicites). |
| `choix-implementation.md` | Choix techniques de l'agent, de sa propre initiative. |
| `journal-evenements.md` | Journal des évènements externes au repo (déroge aux règles de mise à jour ci-dessous). |

## Qui décide du contenu

- **`contraintes-techniques.md` et `fonctionnel.md`** : l'utilisateur est le **seul décisionnaire** de leur contenu. L'agent n'y modifie, n'y ajoute et n'en retire rien de sa propre initiative : tout changement exige l'accord explicite préalable de l'utilisateur.
- **`modele-metier.md`** : l'utilisateur en est le **décisionnaire principal**, puisqu'il choisit les données à utiliser et les règles qui les régissent (entités, attributs, relations, contraintes d'intégrité) : l'agent n'y modifie rien de cet ordre sans son accord explicite. L'agent peut en revanche le **compléter avec des informations plus techniques** (ex. type de stockage d'un attribut, précisions de représentation) sans accord préalable, à condition de ne modifier ni les données ni les règles choisies par l'utilisateur.
- **`choix-implementation.md`** : l'agent peut **à tout moment remettre en cause** ce qui y est noté (changer, remplacer ou retirer un choix) sans accord préalable, dès lors que cela respecte `contraintes-techniques.md` et `fonctionnel.md`, qui prévalent. Il documente alors le nouveau choix (cf. `gestion-projet.md` § « Prise de décision » pour l'information de l'utilisateur lorsque l'impact est visible sur le livrable).

## Règles de consultation

- **Lire l'intégralité des fichiers `.speckit/` est la toute première action de chaque conversation** — avant toute autre lecture de fichier, recherche dans le code, réponse à l'utilisateur (y compris une simple question ou une clarification) ou action de quelque nature que ce soit. Cette lecture n'est ni différable ni conditionnée à la nature apparente de la demande : elle a lieu même si la demande semble triviale, hors-sujet par rapport au `.speckit`, ou déjà couverte par le contexte de conversation.
- Cette lecture est **également requise avant toute décision architecturale, technique ou fonctionnelle** prise plus tard dans la conversation, même si le `.speckit` a déjà été lu en début de conversation.
- **Aucune décision, ligne de code ou réponse ne peut contredire** ce qui est documenté dans le `.speckit`, sans accord explicite de l'utilisateur — sans exception, sans oubli, sans arbitrage silencieux de l'agent au profit de sa propre appréciation.
- Ambiguïté ou silence : technique → l'agent décide et documente ; fonctionnel ou métier → décision de l'utilisateur (cf. `gestion-projet.md` § « Prise de décision »).

## Mise à jour du .speckit

Mettre à jour le fichier concerné **immédiatement et sans attendre** dès que l'utilisateur fournit une exigence, une contrainte, une règle ou toute information relevant du périmètre d'un des fichiers. Ne jamais laisser une information pertinente uniquement dans le fil de conversation.

**Format des mises à jour :**

- **Le speckit n'est pas un historique de décisions.** Les fichiers `.speckit/` sont des **documents de référence**, pas des journaux. Ils ne doivent contenir que l'état actuel et cible du projet — jamais d'historique, de dates, ni de traces de décisions successives ou de leur évolution.
- Toute nouvelle information est **intégrée dans le contenu existant** : mise à jour d'une section, enrichissement d'une définition, ajout dans la liste appropriée — jamais ajoutée en bas de fichier comme une entrée de log.
- En cas de changement de décision, l'**ancienne information est remplacée**, pas conservée à côté de la nouvelle avec une mention de type « anciennement », « auparavant » ou « suite à ».
- Le fichier doit rester cohérent, lisible et structuré comme une documentation vivante.

**Ces règles de mise à jour (immédiate, sans historique) s'appliquent aux cinq fichiers de référence ci-dessus. Le fichier `journal-evenements.md` en est exclu — voir la section suivante.**

## Journal des évènements externes

Le fichier `journal-evenements.md` est un **journal**, pas un document de référence : il déroge intentionnellement aux règles de la section précédente (mise à jour immédiate, sans historique). Ses règles complètes (mise à jour, contenu, format) sont documentées dans le fichier lui-même plutôt que dupliquées ici.

## Rôle de l'agent

Ce projet vise à produire une **application n-tiers web** et tous les éléments nécessaires à sa mise en production.

- L'agent décide **seul** des points techniques et des points fonctionnels ou métier déjà tranchés par le `.speckit` ; les autres relèvent de l'utilisateur (règles complètes : `gestion-projet.md` § « Prise de décision »).
- L'agent doit **toujours être en mesure de justifier** ses choix lorsqu'il est challengé : une contrainte imposée est traçable dans `contraintes-techniques.md`, un choix propre à l'agent dans `choix-implementation.md`, une règle de gouvernance dans `gestion-projet.md`.
