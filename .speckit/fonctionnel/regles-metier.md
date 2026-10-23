# Règles métier

Règles métier de l'application : appartenance à la collection, périmètre de la consultation, initialisation des saisies, rattachements, ordres de présentation, visuels, ISBN, séquence de tomes, suppression des entités et intention d'achat.

---

## Appartenance à la collection

Un album est considéré comme **faisant partie de la collection** uniquement s'il possède au moins une édition **possédée** (dont le `Mode d'acquisition` est renseigné). Un album sans édition possédée est un album catalogué mais non possédé — même s'il dispose d'une édition issue d'une intention d'achat non encore concrétisée.

Une édition peut exister dans la base sans être possédée (sans données d'acquisition). C'est le cas uniquement des éditions créées via une intention d'achat. Le `Mode d'acquisition` est optionnel en base, mais la saisie normale d'une édition continue à l'imposer : il n'est absent que lorsque l'édition est issue d'une intention d'achat non encore concrétisée.

## Périmètre de la consultation

La base est à la fois l'**inventaire de la collection** et une **base documentaire**. L'aspect documentaire porte **uniquement** sur les **albums**, les **séries**, les **éditeurs** et leurs **collections éditeur**, et les **auteurs** : ces fiches peuvent exister sans rien de possédé. Les **éditions** et leurs **visuels**, eux, ne sont jamais documentaires : une édition est soit **possédée** (elle fait partie de la collection), soit l'objet d'une **intention d'achat** (cf. § Appartenance à la collection). En conséquence, dans la partie **Consultation** :

- Tout élément est **consultable et trouvable dès qu'il existe en base**, qu'il relève ou non de la collection : albums sans édition possédée, séries, auteurs, éditeurs, éditions visées par une intention d'achat, etc. Cette règle s'applique aux listes, à la recherche, aux fiches détaillées et à la navigation inter-entités.
- Dans une fiche, tout élément connu est visible (ex. tous les albums d'une série, toute la bibliographie d'un auteur, toutes les éditions d'un album).
- L'**appartenance à la collection** est **signalée visuellement** sur les albums et les éditions, pour que l'inventaire reste lisible au sein de la base documentaire.
- Les **statistiques** portent exclusivement sur la **collection** (albums et éditions possédés), jamais sur l'ensemble de la base.
- Un **filtre** permettra de limiter l'affichage de la consultation à la collection (fonctionnement à préciser).

## Initialisation d'une nouvelle édition depuis la série

Lors de la création d'une nouvelle édition pour un album, si la série de l'album définit des valeurs template, les champs suivants sont pré-remplis :

- Éditeur et collection éditeur
- Catégorie d'édition, état, reliure, orientation, sens de lecture, format
- En couleur

Le template de la série fait foi : un champ sans valeur dans le template n'est pas pré-rempli. Pour un album **sans série**, les champs sont pré-remplis avec leur **valeur par défaut** (cf. `modele-metier.md` § Édition). Ce pré-remplissage ne concerne que le formulaire de saisie, jamais la base. Ces valeurs restent modifiables : l'édition est source de vérité, la série n'est qu'un point de départ.

## Initialisation des contributions depuis la série

Lors du rattachement d'un album à une série, si l'album n'a encore aucune contribution saisie, les contributions de la série sont recopiées sur l'album comme point de départ. L'album reste la source de vérité : ces contributions peuvent ensuite être modifiées ou supprimées indépendamment de la série.

## Genres et univers d'un album

Les genres et univers affichés pour un album dépendent de son rattachement à une série. La même règle s'applique aux deux :

- **Album dans une série** : les valeurs affichées sont l'**union sans doublons** de celles de l'album et de celles de la série.
- **Album sans série** : seules les valeurs propres à l'album sont affichées.

Les genres et univers sont optionnels sur l'album comme sur la série.

## Hiérarchie des univers

Un élément rattaché à un univers est **de facto rattaché aux univers parents** de cet univers, à tous les niveaux de la hiérarchie : les éléments rattachés à un univers comprennent ceux qui sont rattachés à ses sous-univers (ex. une recherche des albums d'un univers remonte aussi ceux de ses sous-univers).

## Ordre des albums dans une série

Les albums d'une série sont toujours présentés dans l'ordre suivant :

1. **Albums non hors-série** en premier (réguliers et intégrales non hors-série), triés par :
   - Numéro de tome pour les réguliers, tome de début pour les intégrales (si renseigné).
   - À défaut : date de première publication.
2. **Albums hors-série** en dernier (réguliers et intégrales hors-série), selon le même critère de tri.

## Ordre des visuels d'une édition

Les visuels d'une édition sont présentés dans l'ordre suivant :

1. Par type, dans cet ordre fixe : Couverture → Dédicace → Page de garde → Planche → 4e de couverture.
2. Pour les visuels du même type : par ordre d'affichage, ajustable manuellement par l'utilisateur.

## Visuel par défaut

Règle globale : **toute représentation visuelle** d'une édition **sans couverture** utilise un **visuel générique par défaut** à la place de sa couverture.

## Couverture d'un album

Un album n'a pas de couverture propre : sa couverture est celle de l'une de ses éditions (un visuel appartient toujours à une édition, `modele-metier.md` § Visuel d'édition). Toute représentation d'un album par sa couverture (ex. cartes d'album, piles de couvertures d'une série) retient, parmi les éditions qui ont un visuel de type **Couverture**, la plus ancienne selon son **année d'édition**, les éditions sans année d'édition venant en dernier, et de cette édition sa **première couverture** dans l'ordre de ses visuels (cf. § Ordre des visuels d'une édition).

Les éditions prises en compte dépendent du contexte d'affichage :

- **donnée de la collection** (ex. statistiques, consultation limitée à la collection, cf. § Périmètre de la consultation) : seules les éditions **possédées** ;
- **ailleurs** (ex. fiches de série et d'auteur) : toutes les éditions de l'album, possédées ou visées par une intention d'achat.

Un album dont aucune des éditions prises en compte n'a de couverture (y compris un album sans édition) est représenté par le visuel générique (cf. § Visuel par défaut).

## Validation de l'ISBN

Les formats **ISBN-10** et **ISBN-13/EAN-13** sont tous deux supportés. La saisie d'un ISBN vérifie le chiffre de contrôle correspondant afin de détecter les erreurs de frappe. Cette vérification est **non bloquante** : l'utilisateur est averti en cas d'incohérence mais peut enregistrer la valeur telle quelle (certains éditeurs ont publié des albums avec un ISBN erroné).

## Séquence théorique de tomes d'une série

La **séquence de tomes** d'une série représente l'ensemble des numéros de tomes attendus. Elle sert de référence pour identifier les manquants et calculer les rythmes de parution.

Règles de construction :

- N'inclut que les **tomes réguliers** et les **intégrales** — les **hors-série sont exclus** de la séquence.
- Commence à **1**.
- Le **tome final** est défini par le numéro de tome (ou tome de fin pour les intégrales) catalogué le plus élevé.
- Si le **nombre de tomes théorique** n'est pas indiqué sur la série, le tome final détermine la fin de la séquence.
- Sinon, la fin de la séquence est le **maximum** entre le tome final catalogué et le nombre de tomes théorique.

## Suppression des entités

Toute suppression est soumise à la **confirmation** de l'utilisateur. Son effet dépend de la nature de chaque lien qui unit l'entité supprimée au reste des données :

- **Référence** — une autre fiche désigne l'entité par l'un de ses attributs : la suppression est **refusée** tant que la référence existe (erreur métier indiquant l'impact, cf. ci-dessous).
- **Association** — simple lien sans donnée propre : la suppression est **autorisée** ; les associations disparaissent avec l'entité.
- **Composition** — élément qui fait partie de l'entité : il est **supprimé avec elle**.

La suppression n'aboutit que si aucun lien de type **Référence** ne la bloque.

**Impact de la suppression** : le message de confirmation — comme l'erreur d'une suppression refusée — indique l'impact de la suppression, c'est-à-dire le **nombre de fiches impactées, distingué par type d'entité** (ex. « utilisé par 12 albums et 3 séries », « supprime 2 éditions et 5 visuels ») : fiches qui perdent leur association avec l'entité supprimée, fiches supprimées avec elle, ou fiches qui bloquent la suppression.

| Entité supprimée | Lien | Type | Effet |
| --- | --- | --- | --- |
| Genre | Genres d'albums et de séries | Association | Le genre est retiré de ces albums et séries. |
| Univers | Univers d'albums et de séries | Association | L'univers est retiré de ces albums et séries. |
| Univers | Univers parent de sous-univers | Référence | Refusée tant que l'univers a des sous-univers. |
| Auteur | Contributions sur des albums ou des séries | Référence | Refusée tant que l'auteur est crédité. |
| Éditeur | Éditeur d'éditions | Référence | Refusée. |
| Éditeur | Éditeur template d'une série | Référence | Refusée. |
| Éditeur | Ses collections éditeur | Composition | Supprimées avec lui. |
| Collection éditeur | Collection d'éditions | Référence | Refusée. |
| Collection éditeur | Collection template d'une série | Référence | Refusée. |
| Série | Albums rattachés | Référence | Refusée tant que la série contient des albums. |
| Série | Ses contributions template | Composition | Supprimées avec elle. |
| Album | Ses éditions (avec leurs visuels et intentions d'achat), ses contributions, son intention d'achat | Composition | Supprimés avec lui. |
| Édition | Ses visuels, son intention d'achat | Composition | Supprimés avec elle. Si c'était la dernière édition possédée de l'album, la confirmation signale que l'album sort de la collection. |
| Intention d'achat portant sur une édition | L'édition visée (non possédée), avec ses visuels | Composition | Supprimée avec elle : une édition non possédée n'existe que par son intention (cf. § Intention d'achat). |
| Toute fiche | Désignée par un critère ou par la valeur par défaut d'une variable d'une recherche enregistrée (cf. `fonctionnel/recherche-avancee.md` § Recherche avancée) | Référence | Refusée tant qu'une recherche enregistrée la désigne, elle ou une fiche supprimée avec elle. |
| Jeton d'accès | Son historique d'utilisation et ses retours écrits (cf. `fonctionnel/agents-ia.md` § Suivi de l'utilisation) | Composition | Supprimés avec lui. Pour conserver cet historique, le jeton est archivé plutôt que supprimé (cf. `fonctionnel/agents-ia.md` § Accès par jeton). |
| Visuel d'édition, Contribution, Intention d'achat portant sur l'album, Recherche enregistrée | — | — | Hors la ligne « Toute fiche », suppression sans autre effet (le média d'un visuel est supprimé avec lui). |

## Intention d'achat

Gestion en **mode administration** (authentifié). Consultation de la liste en **mode consultation** (public, lecture seule).

Deux cas d'usage distincts :

1. **Album** : l'utilisateur souhaite acquérir un album en particulier, sans contrainte sur l'édition. L'intention porte sur l'album ; n'importe quelle édition satisfera l'intention.
2. **Édition spécifique** : l'utilisateur souhaite acquérir une édition précise d'un album (éditeur, collection, année, etc.), qu'il possède déjà cet album dans une autre édition ou non.

Ces deux cas sont représentés par une entité **Intention d'achat** distincte, liée soit à un Album soit à une Édition.

Pour un même album, les deux cas sont **mutuellement exclusifs** : l'album est visé soit par une intention portant sur l'album lui-même, soit par des intentions portant sur une ou plusieurs de ses éditions (une intention par édition), jamais les deux à la fois.

Une intention ne peut porter sur l'**album** que tant qu'il n'a **aucune édition**, possédée ou visée par une intention : tant que l'album n'est pas dans la collection, l'utilisateur peut ne pas connaître les éditions disponibles ; mais dès qu'il en possède une ou en vise une en particulier, souhaiter acquérir une édition quelconque n'a plus de sens, et c'est une édition précise qu'il vise (cas d'usage 2).

Un cas d'usage permettra de **convertir** une intention portant sur l'album en intention portant sur une édition, et inversement, dans le respect de ces règles : une intention portant sur une édition ne se convertit en intention portant sur l'album que si cette édition est la **seule** de l'album, la conversion la supprimant (cf. ci-dessous).

Une intention d'achat est toujours **présentée comme une intention d'achat**, jamais comme un album ou une édition : une édition visée ne figure jamais parmi les éditions (d'un album, d'une liste, d'un décompte), mais parmi les intentions d'achat, avec ce qu'elle vise. Un album visé reste en revanche un album, dont l'appartenance à la collection signale qu'il fait l'objet d'une intention d'achat.

Une édition **non possédée** et l'intention qui la vise sont **indissociables** : l'une ne va pas sans l'autre. Supprimer l'une supprime l'autre, après confirmation (cf. § Suppression des entités) ; de même, convertir une intention portant sur une édition en intention portant sur l'album supprime l'édition visée, après confirmation. Une édition non possédée ne peut donc jamais exister sans intention d'achat.

### Réalisation d'une intention

La **confirmation d'un achat** est un **acte explicite** de l'utilisateur, effectué depuis un écran ou un menu dédié.

Une intention d'achat réalisée est **supprimée** :

- une intention portant sur l'**album** est réalisée, donc supprimée, par l'achat de n'importe laquelle de ses éditions (cf. cas d'usage 1) ;
- une intention portant sur une **édition** est réalisée, donc supprimée, par l'achat de cette édition. Lorsqu'un album fait l'objet de plusieurs intentions (sur plusieurs de ses éditions), **seules les intentions des éditions achetées** sont supprimées ; les autres sont conservées.

Une édition **déjà possédée** ne peut pas faire l'objet d'une intention d'achat : une édition achetée ne redevient jamais une intention. Pour acquérir un second exemplaire d'une même édition, l'utilisateur ajoute à l'album une **nouvelle édition** aux mêmes caractéristiques (une fonction de duplication d'édition pourra faciliter cette saisie) et place son intention sur cette nouvelle édition.

La saisie d'une intention d'achat présente le **minimum de champs** à renseigner : les caractéristiques propres à l'exemplaire possédé (gratuité, occasion, etc.) n'y figurent pas et ne font l'objet d'aucune règle particulière pour une édition non possédée.

Les autres règles métier sont à définir.
