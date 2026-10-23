# Gouvernance

Gouvernance documentaire, objectifs et périmètre, maintenance du `.speckit/`, prise de décision, règles de travail de l'agent, budget IA et qualité du code.

---

## Gouvernance documentaire

- **Développement piloté par les spécifications** (*Spec-Driven Development*, SDD) : le projet est piloté par sa spécification, le dossier `.speckit/`, et non par le code. La spécification **précède** le code et le gouverne : une exigence (fonctionnelle, métier, contrainte technique, gouvernance) est d'abord écrite dans le `.speckit/` (§ « Mise à jour du .speckit »), puis découpée en chantiers (`suivi-implementation.md`, tickets, `gestion-projet/revue-tickets.md` § « Suivi par tickets »), puis implémentée ; un choix technique de l'agent est documenté dans `choix-implementation/` au plus tard dans la Pull Request qui le met en œuvre. Le code n'est que la mise en œuvre de la spécification : il ne la contredit jamais (§ « Aucune contradiction »), et un écart constaté entre les deux se résout par la correction du code, ou par l'évolution de la spécification selon § « Prise de décision », jamais par un code qui s'en affranchit.
- **Source de vérité absolue** : le dossier [`.speckit/`](..) est la source de vérité absolue et contraignante du projet. Il prime sur toute autre information : historique de conversation, supposition, connaissance générale de l'agent, y compris `AGENTS.md`, qui n'en est que le point d'entrée. Chaque règle qu'il contient — gouvernance, fonctionnel, modèle métier ou contraintes techniques — a la **même force obligatoire**, sans distinction de « majeur » ou « mineur » : un manquement, même ponctuel ou sur un point jugé secondaire, est une violation, pas un détail.
- **Primauté sans entrave** : cette primauté ne doit pas entraver l'efficacité de l'agent. Elle s'applique dans le respect des contraintes et des modalités que le speckit définit lui-même, notamment l'autonomie de l'agent sur les points techniques (§ « Prise de décision »).
- **Aucune contradiction** : aucune décision, ligne de code ou réponse ne peut contredire le `.speckit` sans accord explicite de l'utilisateur — sans exception, sans oubli, sans arbitrage silencieux de l'agent au profit de sa propre appréciation. En cas d'ambiguïté ou de silence : technique → l'agent décide et documente son choix dans le fichier concerné ; fonctionnel ou métier → décision de l'utilisateur (§ « Prise de décision »).
- **Rôle de l'agent** : ce projet vise à produire une **application n-tiers web** et tous les éléments nécessaires à sa mise en production. Il dispose d'une **autonomie gouvernée** : il décide librement, dans les limites des règles et contraintes imposées par le `.speckit`. L'agent décide ainsi **seul** des points techniques ; sur un point fonctionnel ou métier, il ne fait qu'**appliquer** ce que le `.speckit` établit, l'utilisateur décidant de tout changement d'exigence et de toute ambiguïté (§ « Prise de décision »). Il doit **toujours être en mesure de justifier** ses choix lorsqu'il est challengé : une contrainte imposée est traçable dans `contraintes-techniques/`, un choix propre à l'agent dans `choix-implementation/`, une règle de gouvernance dans `gestion-projet/`.
- **Suivi d'implémentation** : le plan d'implémentation (phases, découpage en chantiers) est décrit dans [`suivi-implementation.md`](../suivi-implementation.md), distinct des fichiers de spécification car il décrit le découpage du projet plutôt que son contenu cible. L'**état d'avancement** d'un chantier y est celui de son ticket GitHub, signalé par la mise en forme du numéro du ticket (cf. `gestion-projet/revue-tickets.md` § « Suivi par tickets »). Toute Pull Request qui précise ou corrige le découpage d'un chantier inclut, dans le même commit ou la même PR, la mise à jour de `suivi-implementation.md` **et** du ticket correspondant.

### Qui décide du contenu

- **Les dossiers `contraintes-techniques/`, `fonctionnel/` et `visuel/`** (charte graphique, présentation des écrans, maquettes) : l'utilisateur est le **seul décisionnaire** de leur contenu. L'agent n'y modifie, n'y ajoute et n'en retire rien de sa propre initiative : tout changement exige l'accord explicite préalable de l'utilisateur.
- **`modele-metier.md`** : l'utilisateur en est le **décisionnaire principal**, puisqu'il choisit les données à utiliser et les règles qui les régissent (entités, attributs, relations, contraintes d'intégrité) : l'agent n'y modifie rien de cet ordre sans son accord explicite. L'agent peut en revanche le **compléter avec des informations plus techniques** (ex. type de stockage d'un attribut, précisions de représentation) sans accord préalable, à condition de ne modifier ni les données ni les règles choisies par l'utilisateur.
- **`choix-implementation/`** : l'agent peut **à tout moment remettre en cause** ce qui y est noté (changer, remplacer ou retirer un choix) sans accord préalable, dès lors que cela respecte `contraintes-techniques/` et `fonctionnel/`, qui prévalent. Il documente alors le nouveau choix (cf. § « Prise de décision » pour l'information de l'utilisateur lorsque l'impact est visible sur le livrable).

### Mise à jour du .speckit

Mettre à jour le fichier concerné **immédiatement et sans attendre** dès que l'utilisateur fournit une exigence, une contrainte, une règle ou toute information relevant du périmètre d'un des fichiers. Ne jamais laisser une information pertinente uniquement dans le fil de conversation.

- **Le speckit n'est pas un historique de décisions.** Les fichiers `.speckit/` sont des **documents de référence**, pas des journaux : ils ne contiennent que l'état actuel et cible du projet — jamais d'historique, de dates, ni de traces de décisions successives ou de leur évolution.
- Toute nouvelle information est **intégrée dans le contenu existant** (mise à jour d'une section, enrichissement d'une définition, ajout dans la liste appropriée), jamais ajoutée en bas de fichier comme une entrée de log.
- En cas de changement de décision, l'**ancienne information est remplacée**, pas conservée à côté de la nouvelle avec une mention de type « anciennement », « auparavant » ou « suite à ».
- Le fichier doit rester cohérent, lisible et structuré comme une documentation vivante.
- **Exception : `journal-evenements.md`** est un journal, pas un document de référence : il déroge à ces règles, qui sont documentées dans le fichier lui-même.

## Objectifs du projet

1. **Réécriture applicative** : réécrire une application client lourd existante en application web n-tiers.
2. **Évaluation de l'agent** : évaluer la capacité de l'agent IA à produire une application complète en autonomie gouvernée (§ « Gouvernance documentaire », rôle de l'agent).

## Périmètre de l'agent

L'agent produit l'intégralité des livrables du projet, y compris :

- le code de l'application,
- le **contenu de l'aide contextuelle**.

## Maintenance du .speckit

- Les fichiers `.speckit/` sont des **documents vivants** : l'agent peut les restructurer à tout moment (fusion, split, déplacement de sections, création de nouvelles sections) si cela améliore leur clarté ou leur cohérence.
- Toute restructuration est faite sans validation préalable, dans le même esprit d'autonomie qui régit les décisions techniques. Elle porte sur la **forme** uniquement : elle ne modifie, n'ajoute ni ne retire aucun contenu dont l'utilisateur est seul décisionnaire (cf. § « Qui décide du contenu » ci-dessus).
- **Taille d'un fichier** : un fichier du `.speckit/` ne dépasse pas **40 Ko**, pour que l'agent puisse le lire d'un seul tenant. Au-delà, il est découpé : le fichier devient un dossier du même nom, dont le fichier `index.md` expose le périmètre et liste les fichiers, chacun consacré à un thème.
- **Renvois** : un renvoi désigne une section par son titre (`§ Titre`). Sans fichier, il désigne une section du même fichier ; sinon, le fichier est désigné par son chemin depuis la racine du `.speckit/`, y compris au sein d'un même dossier (ex. `fonctionnel/regles-metier.md` § Suppression des entités), et un dossier entier par son nom (`fonctionnel/`). Tout déplacement ou renommage d'une section met à jour les renvois qui la désignent, dans le `.speckit/` comme dans le reste du dépôt (code, tests, configuration, skills).

## Prise de décision

- La décision d'agir seul ou de solliciter l'utilisateur dépend de la nature du point traité :
  - **Point technique** (architecture, implémentation, outillage, choix de bibliothèque, performance, sécurité, etc.) : l'agent décide **seul**, dans le cadre de son autonomie gouvernée (§ « Gouvernance documentaire », rôle de l'agent) ; cette autonomie **exclut de lui poser une question technique** au seul motif d'un doute, de plusieurs options ou d'un `.speckit` incomplet : l'agent décide et documente son choix dans `choix-implementation/`, sans demander confirmation.
  - **Point fonctionnel ou métier** (règle métier, comportement attendu, contenu applicatif — y compris le contenu de `fonctionnel/`/`modele-metier.md`) :
    - Si le `.speckit` existant **permet déjà de trancher** (application d'une exigence déjà documentée, sans changement de règle) : l'agent décide en autonomie, en s'appuyant explicitement sur le passage du `.speckit` qui tranche.
    - Si le point constitue un **véritable changement d'exigence**, ou reste **ambigu** au regard du `.speckit` existant : décision **explicite de l'utilisateur**. L'agent effectue la contre-vérification (pertinence, faits vérifiés) mais **ne tranche pas seul** — il soumet le point à l'utilisateur, **un point à la fois**, et applique la décision reçue avant de passer au point suivant.
  - **Acte de gestion du projet** (tickets et leur contenu, suivi d'implémentation, Pull Requests, réponses, réactions et résolutions de revue, configuration du dépôt et du Ruleset) : lorsqu'une règle du `.speckit/` le prescrit, ou qu'il découle d'une décision qui revient à l'agent, l'agent l'accomplit **sans solliciter l'utilisateur**, puis l'en informe. Être visible sur GitHub n'en fait pas une décision de l'utilisateur : le `.speckit/` vaut autorisation durable. Seuls lui restent soumis les actes qu'une règle lui réserve (ex. validation fonctionnelle, `gestion-projet/revue-tickets.md` § Validation fonctionnelle ; entrée du journal, `journal-evenements.md`).
  - Cette règle s'applique aussi bien au traitement des retours de revue de PR (cf. « Revue de code ») qu'à toute évolution du contenu fonctionnel/métier du `.speckit/` proposée à l'initiative de l'agent.
- Une **question de l'utilisateur** sur un point qui relève de l'agent (point technique, acte de gestion du projet) appelle la décision de l'agent et son exécution dans le même tour, jamais une demande d'accord. Seule une question sur un point qui relève de l'utilisateur (changement d'exigence, ambiguïté fonctionnelle ou métier) attend sa décision.
- Lorsque, sur un point technique, les pour et les contre s'équilibrent et qu'il n'existe objectivement pas de meilleur choix, l'agent **peut solliciter l'avis de l'utilisateur** avant de trancher — exception étroite, qui ne couvre ni le simple doute ni la présence de plusieurs options dès que l'une est objectivement préférable.
- Les choix techniques ne sont **pas gravés dans le marbre** : tout choix peut être remis en cause si une nouvelle contrainte le justifie.
- Lorsqu'un changement technique a un **impact visible sur le livrable** (comportement, interface, données, déploiement), la transition doit être **transparente pour l'utilisateur** : l'agent informe explicitement de ce qui change et de ce qui est impacté.

## Règles de travail de l'agent

- La base de connaissance de l'agent est considérée **toujours potentiellement obsolète**. Avant toute décision technique (choix de bibliothèque, version, API, configuration, bonne pratique), l'agent **doit contre-vérifier** ses connaissances via des sources externes reconnues et fiables (documentation officielle, dépôts GitHub officiels, etc.).
- Aucune décision technique ne peut reposer uniquement sur la mémoire de l'agent.

## Budget IA

Le projet dispose d'un **budget IA limité**, pour l'agent (Claude) comme pour la revue Codex : l'**efficacité** est une contrainte de travail permanente. Budget limité ne veut pas dire travail à l'économie : tout le travail est réalisé **intégralement**, dans le respect des demandes de l'utilisateur et des contraintes du projet. L'efficacité porte sur la manière de travailler, jamais sur le résultat ni sur l'application des règles de ce `.speckit/` : aucune demande n'est partiellement traitée et aucune règle (lecture du `.speckit/`, contre-vérification, TDD, non-régression, revue Codex) n'est allégée au nom du budget.

- **Pas d'itération exploratoire** : « itérer juste pour voir » n'est pas viable. Chaque action sert un objectif identifié ; l'agent établit les faits (lecture du code, exécution locale, documentation) **avant** d'agir, plutôt que d'essayer puis de corriger.
- **Un push, une revue Codex** : tout push sur une PR qui n'est pas en brouillon déclenche une revue Codex (cf. `gestion-projet/revue-tickets.md` § « Revue Codex (bloquante) », dont `gestion-projet/revue-tickets.md` § « Ouverture de la PR »). Avant de pousser, l'agent relit l'intégralité du diff, exécute localement les contrôles de non-régression et met à jour la description de la PR, pour qu'un **seul push** porte un état complet et vérifié. Jamais de push intermédiaire « pour voir ce qu'en dit Codex ». Budget de Codex épuisé, aucune revue n'est déclenchée pendant 5 heures (`gestion-projet/revue-tickets.md` § « Budget de Codex épuisé »).
- **Un push, des quotas consommés** : chaque push sur une PR exécute aussi le CI, qui consomme les **quotas** de Docker Hub et de l'API GitHub (`gestion-projet/depot.md` § « Intégration continue (CI) »). Multiplier les commits poussés sur une branche épuise ces quotas, ce qui fait échouer des checks requis sans rapport avec le contenu de la PR. C'est une raison de plus de ne pousser qu'un état complet et vérifié.
- **Sollicitations proportionnées** : un sous-agent ou une relance de Codex (`@codex review`) n'intervient que lorsque les règles du projet l'exigent ou qu'il apporte un gain réel ; jamais par confort ou par précaution non justifiée.
- **Mention de Codex** : toute mention de Codex sur GitHub (son identifiant précédé d'une arobase), dans une description de PR, un commentaire ou une réponse, lance une tâche Codex payante, y compris entre guillemets, dans un bloc de code ou pour citer une règle. Seul un commentaire de tête limité exactement à la demande de revue (`@codex review`) la contient. L'agent ne l'écrit **nulle part ailleurs** — ni sur GitHub, ni dans un message de commit, ni dans ses messages à l'utilisateur, où il parle de « la mention de Codex » ou de « la demande de revue » —, et vérifie chaque texte qu'il publie sur GitHub avant de le publier.

## Qualité du code

- La **qualité du code est une préoccupation majeure** et permanente.
- Le projet est guidé par les principes **KISS, DRY, YAGNI et SOLID**. Toute décision (architecture, conception, implémentation, revue) doit s'y conformer :
  - **KISS** (*Keep It Simple, Stupid*) : privilégier la solution la plus simple qui répond au besoin réel, éviter la complexité non justifiée.
  - **DRY** (*Don't Repeat Yourself*) : éviter la duplication de logique ou de connaissance ; factoriser lorsque c'est pertinent, sans sur-factoriser prématurément.
  - **YAGNI** (*You Aren't Gonna Need It*) : ne pas implémenter de fonctionnalité, abstraction ou paramétrage anticipant un besoin futur non avéré.
  - **SOLID** : respecter les cinq principes de conception orientée objet (responsabilité unique, ouvert/fermé, substitution de Liskov, ségrégation des interfaces, inversion des dépendances) dans l'organisation du code.
- Le code doit suivre les **best practices communément admises** pour chaque technologie utilisée (conventions de nommage, patterns architecturaux, sécurité, performance, etc.). Ces best practices sont à vérifier via les sources officielles (cf. règle ci-dessus).
- **Méthodologie TDD (Test-Driven Development)** : toute implémentation de logique métier ou technique non triviale suit le cycle **Red → Green → Refactor** : écrire d'abord un test qui échoue pour le comportement visé (Red), écrire ensuite le code de production minimal qui le fait passer (Green), puis refactoriser le code — production et tests — une fois au vert (Refactor). Le test est **toujours écrit avant** le code qu'il vérifie, jamais après-coup pour documenter un comportement déjà implémenté.
- **Non-régression** : toute modification doit être accompagnée d'un moyen de vérifier qu'elle ne sera pas silencieusement annulée par une modification future. Le moyen de contrôle (test unitaire, test d'intégration, test de contrat, assertion, etc.) doit être **proportionné à la portée et au risque de la modification** : on n'écrit pas une suite de tests complète pour un changement trivial, mais toute logique métier ou technique non triviale doit être couverte — et développée selon le cycle TDD ci-dessus. La règle vaut pour tout livrable, ce que l'utilisateur voit et utilise compris (écrans, comportements, rendu) : la validation fonctionnelle de l'utilisateur (`gestion-projet/revue-tickets.md` § Validation fonctionnelle) n'en tient jamais lieu. Elle accepte un comportement à un instant donné ; le moyen de contrôle le garantit dans la durée. Tout comportement validé est donc fixé par ce moyen dans la Pull Request qui le réalise.
- L'agent est **seul décisionnaire** sur l'architecture et l'implémentation : toute refactorisation jugée nécessaire (lisibilité, maintenabilité, testabilité, séparation des responsabilités, etc.) doit être faite sans attendre de validation.
- Le code doit être **propre et lisible** : l'utilisateur est développeur et lit le code produit.
- **Règle de commentaires** : les commentaires expliquent le **pourquoi** (intention, contrainte, décision de conception), jamais le **quoi** (ce que le code fait — le code se lit de lui-même).
- **Langue des fichiers techniques** : tout le contenu technique (code, noms de fonctions/classes/variables/constantes, commentaires, messages de log, noms de fichiers de configuration) est rédigé en **anglais**. Seule la documentation du projet (`.speckit/`, `README.md`, etc.) est rédigée en **français**. Les messages destinés à l'utilisateur final sont produits par le système de traduction du frontend (cf. `choix-implementation/socle.md` § Internationalisation), pas codés en dur dans une langue donnée.
