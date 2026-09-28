# .speckit — Source de Vérité Absolue

Le dossier `.speckit/` est la **source de vérité absolue et contraignante** du projet : il prime sur toute autre information, historique de conversation, supposition ou connaissance générale de l'agent. Chaque règle qu'il contient — gouvernance, fonctionnel, modèle métier ou contraintes techniques — a la **même force obligatoire**, sans distinction de « majeur » ou « mineur ». Un manquement, même ponctuel ou sur un point jugé secondaire, est une violation, pas un détail.

## Fichiers et leur périmètre

| Fichier | Contenu |
| --- | --- |
| `gestion-projet.md` | Gouvernance du projet : stockage, branches, conventions de commit, outillage, CI/CD. Tout ce qui ne concerne PAS l'application elle-même. |
| `fonctionnel.md` | Fonctionnalités de l'application : cas d'usage, user stories, règles métier, flux. Les règles sont formulées au niveau métier — le comportement attendu et les cas visibles de l'utilisateur — **sans détail d'implémentation laissé à la discrétion de l'agent** (stack, architecture, mécanique de calcul, structures internes, bibliothèque, etc.) : ces derniers vivent dans le code, ses tests, `contraintes-techniques.md` ou `choix-implementation.md`. Un élément d'apparence technique mais explicitement imposé par l'utilisateur comme règle métier (ex. un algorithme retenu) reste documenté ici aussi. |
| `modele-metier.md` | Modèle du domaine : entités, attributs, relations, contraintes d'intégrité, glossaire. |
| `contraintes-techniques.md` | Contraintes techniques **imposées** à l'application et à son déploiement — par l'utilisateur, des normes/lois, ou une réalité externe non négociable (stack, hébergement, sécurité, licences, etc.). L'agent n'y ajoute rien de sa propre initiative : ce n'est pas la destination de ses propres choix techniques. |
| `choix-implementation.md` | Choix d'implémentation retenus par l'agent **de sa propre initiative** (architecture, bibliothèque, algorithme non imposé, etc.), qu'il y ait eu ou non délibération explicite entre options. Non imposés : remis en cause possible si une meilleure option apparaît. |
| `journal-evenements.md` | Journal des évènements externes au repo pouvant influencer son développement (ex. : déploiement d'une version en test/production, incident, changement d'infrastructure externe). Fichier journal à part — voir « Journal des évènements externes » ci-dessous, il déroge aux règles de mise à jour et de format des autres fichiers. |

## Règles de consultation

- **Lire l'intégralité des fichiers `.speckit/` est la toute première action de chaque conversation** — avant toute autre lecture de fichier, recherche dans le code, réponse à l'utilisateur (y compris une simple question ou une clarification) ou action de quelque nature que ce soit. Cette lecture n'est ni différable ni conditionnée à la nature apparente de la demande : elle a lieu même si la demande semble triviale, hors-sujet par rapport au `.speckit`, ou déjà couverte par le contexte de conversation.
- Cette lecture est **également requise avant toute décision architecturale, technique ou fonctionnelle** prise plus tard dans la conversation, même si le `.speckit` a déjà été lu en début de conversation.
- **Aucune décision, ligne de code ou réponse ne peut contredire** ce qui est documenté dans le `.speckit`, sans accord explicite de l'utilisateur — sans exception, sans oubli, sans arbitrage silencieux de l'agent au profit de sa propre appréciation.
- En cas d'ambiguïté ou de silence sur un sujet **technique**, l'agent décide seul et documente le choix dans le fichier concerné. Sur un sujet **fonctionnel ou métier**, l'ambiguïté ou le silence est soumis à une décision explicite de l'utilisateur avant toute mise à jour du fichier concerné (cf. `gestion-projet.md` § « Prise de décision »).

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

- L'agent prend **seul et en totale autonomie toutes les décisions techniques** (architecturales, d'implémentation, d'outillage) nécessaires à la livraison d'une application fonctionnelle, ainsi que tout point fonctionnel ou métier déjà tranché par le `.speckit` existant. Cette autonomie **exclut de poser à l'utilisateur une question technique** au seul motif d'un doute, de l'existence de plusieurs options, ou d'un `.speckit` encore incomplet sur le point : dans ces cas, l'agent décide et documente son choix dans `choix-implementation.md`, sans s'arrêter pour demander confirmation. **Seule exception** (cf. `gestion-projet.md` § « Prise de décision ») : lorsque les pour et les contre d'un point technique s'équilibrent réellement et qu'il n'existe **objectivement** pas de meilleur choix, l'agent peut solliciter l'avis de l'utilisateur avant de trancher — une exception étroite, qui ne couvre ni le simple doute ni la présence de plusieurs options dès que l'une d'elles est objectivement préférable.
- Seules les décisions **fonctionnelles ou métier** qui constituent un véritable changement d'exigence, ou restent ambiguës au regard du `.speckit` existant, relèvent de l'utilisateur : soit par sa mise à jour directe des fichiers `.speckit/`, soit par sa réponse explicite à un point soumis par l'agent (cf. `gestion-projet.md` § « Prise de décision »).
- L'agent doit **toujours être en mesure de justifier** ses choix lorsqu'il est challengé. Une contrainte imposée est traçable dans `.speckit/contraintes-techniques.md` ; un choix d'implémentation propre à l'agent, dans `.speckit/choix-implementation.md` ; une règle de gouvernance, dans `gestion-projet.md`.
