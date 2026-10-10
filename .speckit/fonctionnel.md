# Fonctionnel

Ce fichier décrit les fonctionnalités de l'application, ainsi que les éléments de design et la charte graphique.
Il ne doit contenir **aucun détail d'implémentation laissé à la discrétion de l'agent** (stack, architecture, mécanique de calcul, structure de données, bibliothèque, etc.) : ceux-ci relèvent de `contraintes-techniques.md` ou `choix-implementation.md`. En revanche, un élément d'apparence technique mais **explicitement imposé par l'utilisateur comme règle métier** (ex. l'algorithme d'estimation retenu) reste documenté ici. Les règles sont formulées au niveau métier — le comportement attendu et les cas visibles de l'utilisateur.

---

## Contexte

L'application est la réécriture d'une application client lourd existante sous forme d'application web n-tiers. L'application existante est une **BDthèque** : un gestionnaire de collection de bandes dessinées.

## Nom de l'application

Le nom définitif de l'application n'est pas encore défini. Le **nom de travail** est **`bdtheque-claude`** (nom du dépôt git).

## Règles métier

### Appartenance à la collection

Un album est considéré comme **faisant partie de la collection** uniquement s'il possède au moins une édition **possédée** (dont le `Mode d'acquisition` est renseigné). Un album sans édition possédée est un album catalogué mais non possédé — même s'il dispose d'une édition issue d'une intention d'achat non encore concrétisée.

Une édition peut exister dans la base sans être possédée (sans données d'acquisition). C'est le cas uniquement des éditions créées via une intention d'achat. Le `Mode d'acquisition` est optionnel en base, mais la saisie normale d'une édition continue à l'imposer : il n'est absent que lorsque l'édition est issue d'une intention d'achat non encore concrétisée.

### Périmètre de la consultation

La base est à la fois l'**inventaire de la collection** et une **base documentaire**. L'aspect documentaire porte **uniquement** sur les **albums**, les **séries**, les **éditeurs** et leurs **collections éditeur**, et les **auteurs** : ces fiches peuvent exister sans rien de possédé. Les **éditions** et leurs **visuels**, eux, ne sont jamais documentaires : une édition est soit **possédée** (elle fait partie de la collection), soit l'objet d'une **intention d'achat** (cf. § Appartenance à la collection). En conséquence, dans la partie **Consultation** :

- Toute fiche est **consultable et trouvable dès qu'elle existe en base**, qu'elle relève ou non de la collection : albums sans édition possédée, séries, auteurs, éditeurs, éditions visées par une intention d'achat, etc. Cette règle s'applique aux listes, à la recherche, aux fiches détaillées et à la navigation inter-entités.
- Dans une fiche, tout élément connu est visible (ex. tous les albums d'une série, toute la bibliographie d'un auteur, toutes les éditions d'un album).
- L'**appartenance à la collection** est **signalée visuellement** sur les albums et les éditions, pour que l'inventaire reste lisible au sein de la base documentaire.
- Les **statistiques** portent exclusivement sur la **collection** (albums et éditions possédés), jamais sur l'ensemble de la base.
- Un **filtre** permettra de limiter l'affichage de la consultation à la collection (fonctionnement à préciser).

### Initialisation d'une nouvelle édition depuis la série

Lors de la création d'une nouvelle édition pour un album, si la série de l'album définit des valeurs template, les champs suivants sont pré-remplis :

- Éditeur et collection éditeur
- Catégorie d'édition, état, reliure, orientation, sens de lecture, format
- En couleur

Le template de la série fait foi : un champ sans valeur dans le template n'est pas pré-rempli. Pour un album **sans série**, les champs sont pré-remplis avec leur **valeur par défaut** (cf. `modele-metier.md` § Édition). Ce pré-remplissage ne concerne que le formulaire de saisie, jamais la base. Ces valeurs restent modifiables : l'édition est source de vérité, la série n'est qu'un point de départ.

### Initialisation des contributions depuis la série

Lors du rattachement d'un album à une série, si l'album n'a encore aucune contribution saisie, les contributions de la série sont recopiées sur l'album comme point de départ. L'album reste la source de vérité : ces contributions peuvent ensuite être modifiées ou supprimées indépendamment de la série.

### Genres et univers d'un album

Les genres et univers affichés pour un album dépendent de son rattachement à une série. La même règle s'applique aux deux :

- **Album dans une série** : les valeurs affichées sont l'**union sans doublons** de celles de l'album et de celles de la série.
- **Album sans série** : seules les valeurs propres à l'album sont affichées.

Les genres et univers sont optionnels sur l'album comme sur la série.

### Hiérarchie des univers

Un élément rattaché à un univers est **de facto rattaché aux univers parents** de cet univers, à tous les niveaux de la hiérarchie : les éléments rattachés à un univers comprennent ceux qui sont rattachés à ses sous-univers (ex. une recherche des albums d'un univers remonte aussi ceux de ses sous-univers).

### Ordre des albums dans une série

Les albums d'une série sont toujours présentés dans l'ordre suivant :

1. **Albums non hors-série** en premier (réguliers et intégrales non hors-série), triés par :
   - Numéro de tome pour les réguliers, tome de début pour les intégrales (si renseigné).
   - À défaut : date de première publication.
2. **Albums hors-série** en dernier (réguliers et intégrales hors-série), selon le même critère de tri.

### Ordre des visuels d'une édition

Les visuels d'une édition sont présentés dans l'ordre suivant :

1. Par type, dans cet ordre fixe : Couverture → Dédicace → Page de garde → Planche → 4e de couverture.
2. Pour les visuels du même type : par ordre d'affichage, ajustable manuellement par l'utilisateur.

### Visuel par défaut

Règle globale : **toute représentation visuelle** d'une édition **sans couverture** utilise un **visuel générique par défaut** à la place de sa couverture, quel que soit l'écran ou le bloc qui la présente, à une seule exception : la pioche au hasard du dashboard (cf. § Page d'accueil).

### Langue et culture d'affichage

L'utilisateur choisit une **culture d'affichage** (ex. français de France), pas seulement une langue : ce choix pilote la langue des textes de l'interface, mais aussi le **formatage des dates, nombres et montants** et l'**ordre de tri** des listes et résultats de recherche (voir « Tri et navigation par initiale » ci-dessous).

Seule la culture **française** est proposée actuellement ; l'application est conçue pour pouvoir en proposer d'autres ultérieurement sans refonte.

### Tri et navigation par initiale

#### Règle générale

Le tri alphabétique des **séries**, **albums** et **artistes** repose sur une **clé de tri** distincte de la valeur affichée. L'affichage respecte toujours la forme naturelle ; seule la clé de tri est transformée.

#### Entrées de la navigation par initiale

La navigation par initiale propose les entrées **A** à **Z**, une entrée **#** et une entrée **@** :

- Une initiale qui est une lettre ayant une **lettre de base** de `A` à `Z` dans l'ordre alphabétique (lettre accentuée, en minuscule, ligature…) est rangée sous cette lettre de base en majuscule : `É`, `È`, `Ê` → **E** ; `à` → **A** ; `Ç` → **C** ; `Œ` → **O**. À l'intérieur d'une entrée, l'ordre de tri linguistique habituel s'applique.
- Une initiale **numérique** (chiffre) est rangée sous **#** (ex. `13`, `2001 Nights`).
- Toute **autre** initiale (ponctuation, symbole, lettre sans lettre de base de `A` à `Z`, comme celles d'un autre alphabet que le latin) est rangée sous **@** (ex. `...Et après`, `Ωmega`).

#### Titres (séries et albums)

- La clé de tri d'un titre place le mot significatif en tête et reporte l'**article initial en suffixe**, entre crochets (ex. `Lotus bleu [Le]`), plutôt que de le supprimer : ceci garantit un ordre **déterministe** entre deux titres qui ne diffèrent que par leur article (ex. `Un Lotus bleu` et `Le Lotus bleu` ne doivent jamais se trouver à une position arbitraire l'un par rapport à l'autre).
- L'**initiale de navigation** (navigation par lettre) est la première lettre de la clé de tri, soit le premier caractère du mot significatif (**hors article**).
- L'affichage du titre reste toujours en forme naturelle, article inclus en tête.
- **Articles reconnus** : `L'`, `Le`, `La`, `Les`, `Un`, `Une`, `Des`.

Exemples :

| Titre affiché | Clé de tri | Initiale | Entrée de navigation |
| --- | --- | --- | --- |
| `Le Lotus bleu` | `Lotus bleu [Le]` | **L** | **L** |
| `Un Lotus bleu` | `Lotus bleu [Un]` | **L** | **L** |
| `Les Schtroumpfs` | `Schtroumpfs [Les]` | **S** | **S** |
| `L'Épervier` | `Épervier [L']` | **É** | **E** |
| `Tintin` | `Tintin` | **T** | **T** |
| `13` | `13` | **1** | **#** |
| `...Et après` | `...Et après` | **.** | **@** |

Ce traitement est **non configurable** : il est obligatoire pour que la navigation par initiale soit viable.

#### Calcul et stockage de la clé de tri

La clé de tri des titres est **calculée automatiquement** à partir du titre et **stockée explicitement** en base. Elle est toujours visible dans le formulaire de saisie et modifiable par l'utilisateur.

La clé possède deux états :

- **Auto** : calculée depuis le titre, jamais modifiée manuellement. Recalculée automatiquement à chaque modification du titre (mise à jour en temps réel dans le formulaire). C'est l'état par défaut.
- **Manuelle** : l'utilisateur a explicitement saisi une valeur. Elle est conservée telle quelle même si le titre change. Un indicateur visuel signale cet état dans le formulaire ; un bouton "réinitialiser" permet de repasser en mode Auto.

La clé stockée fait foi pour le tri et la navigation par initiale.

#### Artistes

L'**identifiant principal** d'un artiste est son pseudonyme s'il est renseigné, sinon `Prénom Nom` (ou `Nom` seul si le prénom est absent).

- **Affichage** : identifiant principal.
- **Clé de tri** : identifiant principal. Exception : si l'identifiant est `Prénom Nom`, la clé de tri est `Nom Prénom` pour un classement alphabétique par nom de famille.
- **Initiale de navigation** : première lettre de la clé de tri.

Exemples :

| Données | Affiché | Clé de tri | Initiale |
| --- | --- | --- | --- |
| Jean Van Hamme (sans pseudo) | `Jean Van Hamme` | `Van Hamme Jean` | **V** |
| Pseudonyme `Moebius` | `Moebius` | `Moebius` | **M** |
| Georges Remi + pseudo `Hergé` | `Hergé` | `Hergé` | **H** |

### Libellé d'un album

Le libellé d'un album utilise son titre en forme naturelle (cf. § Tri et navigation par initiale).

Il peut optionnellement inclure le **titre de la série**, utilisé quand le contexte ne rend pas la série évidente (ex. résultat de recherche multi-séries). Quand la série est incluse et que l'album n'a pas de titre propre, le titre de la série tient lieu de titre.

#### Représentation du tome selon le type d'album

| Type | Hors-série | Format du tome (avec titre propre) | Format du tome (sans titre propre) |
| --- | --- | --- | --- |
| Régulier | non | `T. {N}` | `Tome {N}` |
| Régulier | oui | `HS[ {N}]` | `Hors-série[ {N}]` |
| Intégrale | non | `INT.[ - {N}][ [{Début} à {Fin}]]` | `Intégrale[ - {N}][ [{Début} à {Fin}]]` |
| Intégrale | oui | `INT.HS[ - {N}][ [{Début} à {Fin}]]` | `Intégrale hors-série[ - {N}][ [{Début} à {Fin}]]` |

Le numéro de tome n'apparaît que s'il est renseigné. Pour les intégrales, la séquence `[Début à Fin]` n'apparaît que si les deux bornes sont renseignées.

#### Deux formats configurables (préférence utilisateur)

**Format 0 — `Album (Série - Tome)`** *(défaut)*

```text
{Titre} ({Série} - {Tome})
```

- Si pas de titre : `{Série} - {Tome}`
- Si pas de série : `{Titre} ({Tome})`
- Si pas de tome : `{Titre} ({Série})`
- Si pas de titre ni de tome : `{Série}`
- Si pas de série ni de tome : `{Titre}`

**Format 1 — `Tome - Album (Série)`**

```text
{Tome} - {Titre} ({Série})
```

- Si pas de titre : `{Tome} - {Série}`
- Si pas de série : `{Tome} - {Titre}`
- Si pas de tome : `{Titre} ({Série})`
- Si pas de titre ni de tome : `{Série}`
- Si pas de série ni de tome : `{Titre}`

#### Fallback

Si toutes les composantes sont vides : `<Sans titre>`.

#### Exemples (format 0, avec série)

| Album | Résultat |
| --- | --- |
| Titre + série + tome 5 | `Le Lotus bleu (Tintin - T. 5)` |
| Sans titre, série + tome 5 | `Tintin - T. 5` |
| Titre + hors-série 2 | `Titre (Série - HS 2)` |
| Titre + intégrale tomes 1 à 6 | `Titre (Série - INT. [1 à 6])` |

### Libellé d'une édition

Le libellé d'une édition est construit dynamiquement selon le modèle suivant :

```text
{Éditeur}[ ({Collection})][ [{Année d'édition}]][ - ISBN {ISBN formaté}]
```

- La collection n'apparaît que si elle est renseignée.
- L'année n'apparaît que si elle est connue.
- L'ISBN n'apparaît que s'il est connu, préfixé de `ISBN ` et séparé par ` - `.

**Exemples :**

- `Dargaud` *(aucune collection, année ni ISBN)*
- `Dargaud (Lucky Luke) [1978]`
- `Dargaud (Lucky Luke) [1978] - ISBN 978-2-205-01234-5`
- `Dargaud [1978] - ISBN 978-2-205-01234-5`

### Libellés contextuels sur l'édition

Le libellé de la date et du montant d'acquisition s'adapte au mode d'acquisition :

| Mode | Libellé de la date | Libellé du montant |
| --- | --- | --- |
| `Achat` | Date d'achat | Prix d'achat |
| `Offerte` | Date d'acquisition | Valeur d'acquisition |
| `Échange` | Date d'acquisition | Valeur d'acquisition |
| `Gagnée` | Date d'acquisition | Valeur d'acquisition |
| `Héritée` | Date d'acquisition | Valeur d'acquisition |

Dans tous les cas, le champ montant est affiché et reste optionnel : même sans transaction financière, une édition peut avoir une valeur marchande connue. Si l'édition est marquée **Gratuite**, le champ montant est désactivé et vidé, de même que la valeur initiale. Une édition achetée (mode `Achat`) ne peut pas être marquée gratuite.

### Validation de l'ISBN

Les formats **ISBN-10** et **ISBN-13/EAN-13** sont tous deux supportés. La saisie d'un ISBN vérifie le chiffre de contrôle correspondant afin de détecter les erreurs de frappe. Cette vérification est **non bloquante** : l'utilisateur est averti en cas d'incohérence mais peut enregistrer la valeur telle quelle (certains éditeurs ont publié des albums avec un ISBN erroné).

### Séquence théorique de tomes d'une série

La **séquence de tomes** d'une série représente l'ensemble des numéros de tomes attendus. Elle sert de référence pour identifier les manquants et calculer les rythmes de parution.

Règles de construction :

- N'inclut que les **tomes réguliers** et les **intégrales** — les **hors-série sont exclus** de la séquence.
- Commence à **1**.
- Le **tome final** est défini par le numéro de tome (ou tome de fin pour les intégrales) catalogué le plus élevé.
- Si le **nombre de tomes théorique** n'est pas indiqué sur la série, le tome final détermine la fin de la séquence.
- Sinon, la fin de la séquence est le **maximum** entre le tome final catalogué et le nombre de tomes théorique.

### Suppression des entités

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
| Visuel d'édition, Contribution, Intention d'achat portant sur l'album | — | — | Suppression sans autre effet (le média d'un visuel est supprimé avec lui). |

### Calcul des estimations

Toute **estimation** produite par l'application (valeur estimée d'une édition, estimation de sortie d'un nouvel album) est **toujours calculée dynamiquement**, à partir de l'**état courant** de la base : elle n'est jamais stockée, ni figée à partir d'un état antérieur des données.

Objectif : tirer parti de l'enrichissement progressif de la base pour **affiner les estimations au fil du temps**, dans la mesure où les données ajoutées le permettent.

### Estimation de la valeur des éditions

- La **valeur estimée** d'une édition est l'estimation de son **prix d'acquisition** lorsqu'il n'est pas renseigné. Seules les éditions **possédées**, **non gratuites** et **sans prix d'acquisition** en ont une : une édition dont le prix d'acquisition est connu n'est jamais estimée.
- Elle sert à valoriser la collection : la **valeur totale estimée** est la somme des prix d'acquisition connus et des valeurs estimées.
- Elle suit la règle de [Calcul des estimations](#calcul-des-estimations) : elle est calculée à partir des données de la collection, et n'est pas stockée en base.
- Le modèle d'estimation retenu est un **Random Forest**.

### Coût de remplacement de la collection

Statistique **envisagée** : rien n'est statué, elle n'est pas à implémenter tant qu'elle n'est pas décidée (YAGNI).

- Le **coût de remplacement** de la collection est ce que coûterait aujourd'hui l'acquisition de toutes ses éditions : la valeur de chaque édition y est remplacée par son **prix actuel**.
- **Problème non résolu** : obtenir ces prix actuels. Le volume de la collection rend impraticable une recherche du prix de chaque édition, et certaines éditions ne sont plus en vente.
- **Piste envisagée** : établir des **groupes d'éditions** selon des critères **hors dates** (par exemple à la manière d'un Random Forest), récupérer les prix actuels de quelques éditions de chaque groupe, puis **extrapoler** ces prix aux autres éditions du même groupe.

### Gestion des devises

- L'application gère des **prix, montants et valeurs** (ex. valeur d'achat, valeur estimée d'un album, etc.).
- **Saisie et affichage des données de base** : peuvent être faits dans n'importe quelle devise. Une fiche affiche chaque montant **deux fois** : la valeur saisie, dans sa devise, et sa valeur en **euros du jour**.
- **Euros courants** : un montant exprimé en euros courants est **converti en euro** au taux de change de sa date de référence (cf. *Taux de change* ci-dessous), **sans correction de l'inflation**.
- **Euros du jour** : pour qu'une valorisation ait un sens quelle que soit l'époque du montant, un montant exprimé en euros du jour est d'abord exprimé en **euros courants**, puis **corrigé de l'inflation** entre sa date de référence et aujourd'hui. La correction utilise les coefficients annuels de **pouvoir d'achat de l'euro et du franc** publiés par l'**[INSEE](https://www.insee.fr/fr/information/2417794)** (source officielle et gratuite, disponible depuis 1901) : elle est **annuelle**, une date de référence plus précise étant corrigée selon son année.
- **Analyses et statistiques** : chaque montant est affiché **en parallèle** dans les deux versions, pour mettre en relation l'**investissement réalisé** (euros courants) et son **équivalent en pouvoir d'achat** à la date du jour (euros du jour). Un montant non convertible (cf. *Taux de change* ci-dessous) n'entre dans aucune des deux : elles portent toujours sur les mêmes données.
- **Comparaison de montants** : toute comparaison entre montants (classement, minimum, maximum, record…) se fait **en euros du jour**, seule version qui compare les époques entre elles, sauf cas particulier où l'on souhaite explicitement comparer les montants **d'origine**. Le résultat reste affiché dans les deux versions.
- **Agrégation multi-devises** : toute agrégation de données exprimées dans des devises différentes est convertie et consolidée en euro, dans ces deux versions.
- **Date de référence d'un montant** : première date connue, dans cet ordre :
  - **prix d'acquisition** : date d'acquisition, puis année d'édition, puis date de première publication de l'album ;
  - **valeur initiale** : année d'édition, puis date de première publication de l'album.

  Un montant ne peut pas être enregistré sans date de référence, **quelle que soit sa devise**.
- **Taux de change** : tout montant est **stocké dans sa devise** de saisie, jamais converti à l'enregistrement. Lorsqu'il doit être exprimé en euro, quel que soit l'usage (statistiques, valeur de la collection, estimation de valeur), il est converti au taux de change en vigueur à sa **date de référence** :
  - Certaines devises ont un taux **fixe et définitif** vis-à-vis de l'euro (ex. Franc français : 6,55957 FF = 1 €) → le taux est une constante, identique quelle que soit la date.
  - **Ancien franc** : proposé comme devise à la saisie, pour les prix français antérieurs au nouveau franc de 1960 ; il est converti à raison de 100 anciens francs pour 1 franc, puis comme le franc.
  - D'autres devises ont un taux **variable** (ex. Dollar américain) → le taux est celui de la date de référence, récupéré depuis l'API **[Frankfurter](https://www.frankfurter.app/)** (open source, gratuite, sans clé API). Lorsque la date de référence est partielle, le taux appliqué est le **taux moyen** de la période connue : celui du **mois** pour un mois et une année, celui de l'**année** pour une année seule.
  - **Montant non convertible** : un montant pour lequel aucun taux de change ou coefficient d'inflation n'est disponible à sa date de référence (date antérieure à l'historique de la devise ou à 1901) peut être saisi et reste affiché dans sa devise, mais il n'est pas pris en compte dans les calculs exprimés en euro.

## Design et charte graphique

L'interface doit offrir un rendu **soigné et personnel**, clairement au-dessus du rendu par défaut des frameworks UI. Un template de base (Bootstrap, Material out-of-the-box, etc.) n'est pas acceptable.

La charte graphique, la présentation de chaque écran et leurs maquettes de référence sont décrites dans le dossier [`visuel/`](visuel/) : [`charte-graphique.md`](visuel/charte-graphique.md), puis un fichier par écran (ex. [`dashboard.md`](visuel/dashboard.md)).

Le dossier `visuel/` est une **référence visuelle et ergonomique** (apparence, mise en page, maquettes) : il ne définit **pas le contenu** des écrans, qui relève de ce fichier. Un écart entre le contenu d'une maquette et les règles fonctionnelles n'est pas une contradiction : les règles fonctionnelles s'appliquent, la maquette n'illustrant qu'un rendu possible.

## Page d'accueil

La page d'accueil est un **dashboard public** présentant les statistiques principales de la collection. Elle n'est pas personnalisée (pas d'authentification sur la partie consultation). Certaines statistiques pourront être présentées sous forme de **graphiques** lorsque c'est pertinent.

Sa présentation est décrite dans [`visuel/dashboard.md`](visuel/dashboard.md). Étant public, il n'affiche que des statistiques de niveau **public** (cf. § Règles d'accès) : il est considéré comme **purement public** dans un premier temps, sans variante pour une session authentifiée. Afficher aussi des données privées dans une session authentifiée est une ouverture **envisagée**, si une raison particulière le justifiait, mais **rien n'est statué** : elle n'est pas à implémenter tant qu'elle n'est pas décidée (YAGNI).

**Composition du dashboard** :

- Il présente **toujours** les trois blocs suivants, qui ne font pas partie du tirage :
  - **Compteurs globaux** : nombre total d'albums, de séries et d'éditions.
  - **Dernières entrées** : les dernières éditions entrées dans la collection, de la plus récente à la plus ancienne selon leur date d'acquisition, ou à défaut leur date d'entrée dans la collection (`modele-metier.md` § Édition).
  - **Indicateurs de valeur** (exprimés en €, en euros courants et en euros du jour, cf. § Gestion des devises) : prix moyen, médian, min/max, valeur totale connue et valeur totale estimée. Les prix minimum et maximum sont ceux des éditions dont le prix d'acquisition est **connu** (jamais une valeur estimée).
- Chacun de ses **autres blocs** présente une statistique **tirée au hasard** parmi les statistiques ci-dessous. Le tirage a lieu **à chaque affichage**. Le nombre de ces blocs dépend de la représentation retenue, et sera fixé avec elle. La représentation (le thème) ne fait que **fournir ce paramétrage** : le tirage est fait **en amont**, selon les règles ci-dessous, et le thème présente les statistiques tirées sans choisir lui-même lesquelles afficher. Il choisit en revanche **l'ordre** dans lequel il les présente.
- **Catégories** : ce sont les rubriques de la liste ci-dessous. Les statistiques **utiles** (toutes celles qui ne sont pas ludiques) forment quatre catégories : répartitions clés (dont chaque répartition est une statistique distincte), suivi des séries, dépenses et valeur, composition de la collection. Les statistiques **ludiques** forment une seule catégorie pour le tirage, elle-même divisée en **catégories ludiques** (ses rubriques).
- **Règles du tirage** :
  - un bloc présente une statistique **ludique**, les autres une statistique **utile**, toutes de **catégories différentes** ;
  - si la représentation demande **plus de blocs qu'il n'y a de catégories**, chaque catégorie, ludique comprise, est présente **au moins une fois**, et les blocs en surplus sont tirés parmi toutes les statistiques qui ne sont pas encore présentées, quelles que soient leur catégorie et leur nature (utile ou ludique) ;
  - lorsque plusieurs statistiques ludiques sont présentées, elles appartiennent chacune à une **catégorie ludique différente**, selon la même règle : au moins une de chaque catégorie ludique si elles sont plus nombreuses que ces catégories.
- Exemples, pour cinq catégories :

  | Blocs tirés | Résultat |
  | --- | --- |
  | 3 | 1 ludique, 2 utiles de deux catégories différentes, toutes deux marquées *(graphique)* |
  | 5 | 1 ludique, 1 utile de chacune des quatre catégories |
  | 6 | les 5 ci-dessus, plus 1 statistique quelconque non encore présentée : utile de n'importe quelle catégorie, ou ludique d'une autre catégorie ludique que la première |
- Le dashboard est **graphique** : au moins **75 %** des statistiques **tirées au hasard** **peuvent** être représentées graphiquement, c'est-à-dire sont marquées *(graphique)* dans la liste ci-dessous ; les trois blocs toujours présents et la statistique ludique **obligatoire** (aucune statistique ludique n'étant marquée *(graphique)*) ont leur propre présentation et n'entrent pas dans ce compte ; les statistiques ludiques tirées en surplus y entrent. L'objectif est de **privilégier le visuel** : le dashboard ne doit pas devenir un pavé de texte, sauf choix du thème. Le tirage écarte toute combinaison qui n'atteint pas ce seuil. Le seuil porte sur le tirage, non sur le rendu : la représentation retenue (le thème) reste libre de formuler en texte une statistique marquée *(graphique)*.

Les statistiques que le dashboard tire au hasard sont listées ci-dessous, sous réserve de ce niveau d'accès (liste non exhaustive). Celles qui sont marquées *(graphique)* **peuvent** être présentées sous forme de graphique (la forme indiquée n'est qu'une suggestion) : le choix final revient à la représentation retenue (le thème), qui peut aussi bien les formuler en texte, comme le ferait une présentation de type magazine :

- **Répartitions clés** : par type d'album, par genre, par éditeur *(graphique, chacune)*. La répartition par type d'album distingue trois catégories exclusives, réguliers, intégrales et hors-séries, où le hors-série l'emporte sur le type : une intégrale hors-série compte parmi les hors-séries, comme dans l'ordre des albums d'une série (cf. § Ordre des albums dans une série).
- **Suivi des séries** :
  - les séries **complètes** et **à compléter** : nombre et part des séries de la collection marquées complètes *(graphique)* ;
  - les **tomes manquants** : leur nombre total, et les séries qui en comptent le plus (cf. § Identification des albums manquants dans une série) *(graphique : le classement des séries)* ;
  - la répartition des séries de la collection par **statut** (en cours, terminée, abandonnée, non renseigné) *(graphique)* ;
  - les **prochaines sorties estimées** : quelques séries, celles dont la sortie estimée du prochain tome est la plus proche (cf. § Estimation de sortie d'un nouvel album) ; leur nombre dépend de la représentation retenue, et sera fixé avec elle. La liste complète est réservée à un écran dédié *(graphique : une frise chronologique est envisagée)*.
- **Dépenses et valeur** :
  - les **dépenses par année** : total des prix d'acquisition des éditions achetées (mode `Achat`), par année d'acquisition ; les éditions achetées sans date d'acquisition forment une tranche à part. Sur le dashboard, les deux versions des montants (cf. § Gestion des devises) forment **deux statistiques distinctes**, les dépenses par année en euros courants et les dépenses par année en euros du jour, chacune avec son propre graphique *(graphique)* ;
  - la part de la valeur totale estimée qui est **connue** (prix d'acquisition) et celle qui est **estimée** (valeurs estimées) *(graphique)* ;
  - la répartition des éditions par **mode d'acquisition** *(graphique)* ;
  - la répartition des éditions de la collection entre **neuves** et **d'occasion**, quel que soit leur mode d'acquisition *(graphique, par exemple en camembert)*.
- **Composition de la collection** :
  - les albums par **décennie** de première publication *(graphique)* ;
  - l'**âge moyen d'un album au moment de son achat** : l'écart moyen entre la date de première publication de l'album et la date d'acquisition de l'édition, sur les éditions achetées qui ont les deux. L'écart se calcule **à l'année** (année d'acquisition moins année de première publication), quelle que soit la précision des dates, et la moyenne s'exprime en années ;
  - les **auteurs les plus présents**, par rôle (scénariste, dessinateur, coloriste) *(graphique)* ;
  - la répartition des **auteurs** de la collection **par rôle** : le nombre d'auteurs distincts crédités comme scénariste, comme dessinateur et comme coloriste ; un auteur qui tient plusieurs rôles compte dans chacun *(graphique)* ;
  - la répartition des albums par **univers**, chaque univers comptant les albums de ses sous-univers (cf. § Hiérarchie des univers) *(graphique, par exemple hiérarchique, qui montre l'imbrication des univers)* ;
  - la répartition des éditions par **catégorie** (originale, spéciale, tirage de tête) *(graphique)* ;
  - la répartition des éditions par **état** *(graphique)* ;
  - le nombre d'albums possédés en **plusieurs éditions**.
- **Statistiques ludiques**, propres au dashboard (aucune autre page ne les présente) :
  - **Records et curiosités** :
    - le **doyen** et le **benjamin** : les albums dont la date de première publication est la plus ancienne et la plus récente ;
    - le **plus cher** et le **moins cher** : les éditions qui portent les prix maximum et minimum des indicateurs de valeur, désignées nommément ;
    - le **plus gros pavé** : l'édition qui compte le plus de pages ;
    - l'**univers le plus vaste** : l'univers qui compte le plus d'albums, sous-univers compris (cf. § Hiérarchie des univers) ;
    - la **plus longue série** : la série dont la séquence théorique de tomes (cf. § Séquence théorique de tomes d'une série) est la plus longue, et la **série la plus fournie** : celle dont la collection compte le plus d'albums ;
    - les **auteurs les plus prolifiques** : l'auteur crédité sur le plus d'albums, tous rôles confondus et pour chaque rôle (scénariste, dessinateur, coloriste) ; le thème choisit la ou les versions qu'il présente, à partir des mêmes données ;
    - le **duo** scénariste-dessinateur (deux auteurs distincts) crédité ensemble sur le plus d'albums ;
    - les **hommes-orchestres** : le nombre d'albums dont un même auteur est à la fois scénariste et dessinateur ;
    - les **éditions dédicacées** : leur nombre.
  - **Machine à remonter le temps** :
    - **payé en francs** : le nombre d'éditions dont le prix d'acquisition est en francs ou en anciens francs, et la plus ancienne d'entre elles selon la date de référence de ce prix (cf. § Gestion des devises), avec ce prix en euros du jour ;
    - **il y a N ans ce mois-ci** : les albums parus le mois en cours d'une année passée ; seuls les albums dont la date de première publication comporte le mois y figurent ;
    - le **mois le plus dépensier** : le mois dont les prix d'acquisition des éditions achetées (mode `Achat`) totalisent le montant le plus élevé ; le **record d'achats en une journée** : la date d'acquisition qui compte le plus d'éditions achetées.
  - **La collection en volume** : le **nombre total de pages**, et le **temps de lecture** qu'il représente, à raison d'**une minute par page**. Seules les éditions dont le nombre de pages est renseigné y comptent.
  - **Jeux sur les données** :
    - le **titre le plus long** et le **titre le plus court**, parmi les albums qui ont un titre propre ;
    - la **pioche au hasard** : une **édition possédée** tirée au hasard à chaque affichage, présentée avec son album et sa couverture. Si elle n'a pas de couverture, par exception à la règle du visuel par défaut, la couverture de n'importe laquelle des autres éditions possédées de l'album qui en ont une est présentée à la place ; à défaut, le visuel générique (cf. § Visuel par défaut).

  Ces statistiques portent, comme toutes les autres, sur la collection seule (cf. § Périmètre de la consultation) : albums de la collection, éditions possédées, séries et auteurs de ces albums.

## Structure de l'application

L'application se compose de trois parties distinctes :

1. **Consultation** — accessible **publiquement** (sans authentification) :
   - Affichage de la **fiche détaillée** de chaque entité : toutes les informations publiques disponibles sont présentées. Les notes personnelles, la numérotation personnelle, les données d'acquisition (mode, date, prix, occasion, gratuité) et l'appréciation sont des informations **publiques**. Exception : les **visuels d'édition** et les **intentions d'achat** n'ont pas de fiche propre, toutes leurs informations étant présentées par la fiche qui les porte — un visuel dans la fiche de son édition (avec accès à l'image originale), une intention dans la fiche de l'album ou de l'édition visée, ainsi que dans la liste publique des intentions, qui mène à cette fiche.
   - La fiche d'une **série** présente ses contributions template comme les **auteurs de la série**, et son éditeur et sa collection éditeur template comme son **éditeur** et sa **collection**. Ses autres valeurs template d'édition (catégorie, état, reliure, orientation, sens de lecture, format, couleur) et ses indicateurs **Exclure des manquants** et **Exclure des estimations de sortie**, paramètres de la saisie et des calculs, n'y figurent pas : ils ne décrivent pas la série.
   - La fiche d'un **auteur** présente sa **bibliographie complète**, en une seule liste qui réconcilie les **séries** et les **albums** auxquels il a participé, chacun avec le ou les **rôles** qu'il y a tenus. Elle ne reprend pas le détail de ces fiches, auxquelles elle mène.
     - Une **série** y figure dès que l'auteur est crédité sur la série ou sur l'un de ses albums ; elle regroupe les albums de la série auxquels il a participé, dans l'ordre de la série (cf. § Ordre des albums dans une série).
     - **Albums sans série** : leur place dans la bibliographie n'est pas statuée. **Piste envisagée** : les présenter au même niveau que les séries, une série étant alors visuellement distinguée d'un album (icône, mise en forme…).
   - **Navigation inter-entités** : depuis la fiche d'une entité, il est possible de naviguer vers les fiches des entités associées (ex. album → série, album → édition, album → auteur, auteur → bibliographie, etc.).
   - **Recherche facilement accessible** à tout moment depuis n'importe quelle page de la partie consultation.
   - **Recherche simple** : par type d'entité (ex. rechercher des albums, des auteurs, des séries, etc.).
   - **Recherche avancée** : exploite les **liens entre entités** pour des requêtes cross-domaines (ex. albums d'un auteur donné, séries d'un éditeur, etc.).
   - **États et statistiques** : rapports et indicateurs sur la collection (à préciser), selon le niveau d'accès de chacun (cf. § Règles d'accès).
2. **Administration** — protégée par **authentification** :
   - CRUD sur toutes les entités, paramétrage de l'application, gestion des référentiels, etc.
   - La **saisie des données est manuelle**, mais assistée par des **imports depuis des sources externes** : APIs, extraction de données de sites web, etc. Cette aide à la saisie permet de **compléter une fiche** à partir de ces sources. Le **[catalogue général de la BnF](https://api.bnf.fr/fr/api-sru-catalogue-general)** (API SRU) en fait partie, comme **source d'information complémentaire** ; les autres sources restent à définir.
   - L'accès est protégé par un **compte administrateur unique** (login + mot de passe). Pas de gestion multi-utilisateurs.
3. **Aide contextuelle** — accessible à tout moment, depuis n'importe quelle page de l'application :
   - Affiche des informations d'aide **relatives à la page en cours** (aide sensible au contexte).
   - Le contenu peut être **riche** : texte, captures d'écran, tableaux, exemples, etc. — pas seulement de courts textes explicatifs.

## Règles d'accès

Ces règles sont **globales** : elles s'appliquent à l'interface web comme à l'interface pour agents IA, une fonctionnalité ayant les mêmes restrictions d'accès quelle que soit l'interface qui la propose.

- **Recherche et consultation** : publiques. L'authentification n'est pas requise, mais n'est pas interdite pour autant.
- **Analyses et statistiques** : deux niveaux d'accès, **public** et **privé** (réservé au compte administrateur). Le niveau est choisi **au cas par cas**, en fonction des données accédées et exposées par chaque analyse ou statistique.
- **Écriture de données** (saisie, modification, suppression, paramétrage) : réservée au **compte administrateur**, seul compte autorisé à modifier les données.

## Interface pour agents IA

L'application met à disposition d'un **agent IA** un outil lui permettant d'interagir avec elle, en complément de l'interface web. Cet outil n'est pas lié à une fonction du frontend : il est utilisable par **n'importe quel agent conversationnel**. L'intégration d'un **agent IA dans le frontend** est par ailleurs **envisagée** ; rien n'est statué à ce stade (YAGNI). L'outil offre trois familles de fonctionnalités :

- **Aide à la saisie** : assister l'utilisateur dans la saisie des données de l'application. L'agent peut aussi **écrire en base**, mais uniquement **à la demande de l'utilisateur**.
  - **Fonction principale attendue** : répondre à la demande « à partir de cet ISBN, compléter ou saisir la fiche de l'album et de l'édition (visuels inclus), de la série si l'album en fait partie, et des auteurs ». L'**éditeur** et la **collection éditeur** de l'édition font aussi partie des données à saisir ou compléter.
  - Les données proviennent de **plusieurs sources externes** (jamais d'une seule). La **source principale** reste le **site de l'éditeur** ; le **catalogue général de la BnF** (API SRU, cf. § Structure de l'application, Administration) est une **source d'information complémentaire**. Les autres sources seront arrêtées à l'ouverture de la phase d'implémentation.
- **Recherche et consultation** : rechercher et consulter les fiches, avec la même étendue que la partie Consultation (cf. § Périmètre de la consultation).
- **Analyses et statistiques** : produire des analyses et statistiques sur la collection, avec les mêmes règles que les statistiques de l'application (portée sur la collection uniquement, montants en euro, cf. § Gestion des devises).

Les règles d'accès sont celles de l'application (cf. § Règles d'accès) : l'outil n'a pas de règles propres, et les fonctionnalités équivalentes de l'interface web ont les mêmes restrictions.

<!-- À préciser : périmètre exact de l'aide à la saisie, cas d'usage détaillés, analyses et statistiques attendues. -->

## Présentation des erreurs

Toute erreur présentée à l'utilisateur doit lui permettre de distinguer **très facilement**, au premier coup d'œil, s'il s'agit :

- d'une **erreur métier** : l'action demandée enfreint une règle de gestion de l'application (ex. une donnée obligatoire manquante, une incohérence entre deux champs, une intention d'achat déjà existante pour cet album). L'utilisateur peut la résoudre lui-même en corrigeant sa saisie ou sa demande ;
- d'une **erreur fonctionnelle** : la demande n'enfreint aucune règle de gestion et l'application fonctionne normalement, mais la demande ne peut pas aboutir en l'état des données au moment où elle est traitée. Cas type : l'enregistrement d'une fiche **modifiée entre-temps** (depuis un autre onglet ou un autre appareil), refusé pour ne pas écraser silencieusement cette autre modification. L'utilisateur peut la résoudre lui-même en tenant compte du nouvel état des données. Cette catégorie regroupe les erreurs de même nature ;
- d'une **erreur technique** : l'application n'a pas pu traiter la demande pour une raison indépendante de la saisie de l'utilisateur (dysfonctionnement, service indisponible, etc.).

Cette distinction s'applique à toutes les parties de l'application (consultation, administration, aide contextuelle).

## Fonctionnalités de second plan

Ces fonctionnalités sont prévues dans une phase ultérieure.

### Identification des albums manquants dans une série

Accessible en **mode consultation** (public). Permet de savoir quels albums d'une série ne sont pas encore présents dans la collection.

Un tome est **manquant** s'il est absent de la séquence théorique de tomes de la série (voir [Séquence théorique de tomes d'une série](#séquence-théorique-de-tomes-dune-série)).

Les intégrales **couvrent** la plage `tome de début → tome de fin` de la séquence (chaque tome de la plage est considéré présent).

**Exclusions systématiques :**

- les séries avec l'attribut **Exclure des manquants**

**Affichage des manquants :**

Les tomes manquants consécutifs sont regroupés et affichés sous forme d'intervalle (ex. `T. 3 à 5` plutôt que `T. 3`, `T. 4`, `T. 5`).

**Options utilisateur :**

- **Exclure les intégrales** : les intégrales ne couvrent plus leur plage (pour posséder chaque tome en album individuel).
- **Exclure les intentions d'achat** : les intentions ne comblent plus les trous — affiche tous les manquants réels, intentions incluses.

### Estimation de sortie d'un nouvel album

Accessible en **mode consultation** (public). Permet d'estimer la date de sortie du prochain tome d'une série, sur la base du rythme de parution observé.
On parle d'**estimation** et non de prévision : le facteur humain rend toute prédiction précise impossible, et l'objectif est uniquement de donner un ordre de grandeur.

Comme toute estimation, elle suit la règle de [Calcul des estimations](#calcul-des-estimations).

Règles de calcul :

1. L'estimation se fait **série par série**. Elle n'est calculée que pour les séries dont le statut est `En cours` — les séries `Terminée` (tous les albums prévus ont été publiés) et `Abandonnée` ne font l'objet d'aucune estimation — et qui n'ont pas l'attribut **Exclure des estimations de sortie**.
2. Elle se base sur les **dates de première publication** des albums réguliers de la série (les intégrales et hors-série sont exclus du calcul).
3. Il faut **au moins 2 albums réguliers** avec une date de première publication **passée** pour pouvoir produire une estimation. Les albums dont la date de première publication est strictement dans le futur sont exclus du calcul du rythme. La règle s'adapte à la granularité de la date saisie :

- **Mois + année** : le mois en cours est considéré comme passé. Seuls les mois strictement postérieurs au mois courant sont exclus.
- **Année seule** : l'année en cours est considérée comme passée. Seules les années strictement postérieures à l'année courante sont exclues.

4. Le délai attendu avant le prochain tome est déduit à partir des **délais entre les parutions précédentes**, en tenant compte des éventuels **trous dans la séquence de tomes** (un tome manquant dans la collection ne doit pas fausser le calcul du rythme).
5. Les **évolutions de rythme** (accélération ou décélération des parutions) doivent être prises en compte dans l'estimation.

### Intention d'achat

Gestion en **mode administration** (authentifié). Consultation de la liste en **mode consultation** (public, lecture seule).

Deux cas d'usage distincts :

1. **Album** : l'utilisateur souhaite acquérir un album en particulier, sans contrainte sur l'édition. L'intention porte sur l'album ; n'importe quelle édition satisfera l'intention.
2. **Édition spécifique** : l'utilisateur souhaite acquérir une édition précise d'un album (éditeur, collection, année, etc.), qu'il possède déjà cet album dans une autre édition ou non.

Ces deux cas sont représentés par une entité **Intention d'achat** distincte, liée soit à un Album soit à une Édition.

Pour un même album, les deux cas sont **mutuellement exclusifs** : l'album est visé soit par une intention portant sur l'album lui-même, soit par des intentions portant sur une ou plusieurs de ses éditions (une intention par édition), jamais les deux à la fois.

Un cas d'usage permettra de **convertir** une intention portant sur l'album en intention portant sur une édition, et inversement, dans le respect de cette exclusivité.

Une édition **non possédée** et l'intention qui la vise sont **indissociables** : l'une ne va pas sans l'autre. Supprimer l'une supprime l'autre, après confirmation (cf. § Suppression des entités) ; de même, convertir une intention portant sur une édition en intention portant sur l'album supprime l'édition visée, après confirmation. Une édition non possédée ne peut donc jamais exister sans intention d'achat.

#### Réalisation d'une intention

La **confirmation d'un achat** est un **acte explicite** de l'utilisateur, effectué depuis un écran ou un menu dédié.

Une intention d'achat réalisée est **supprimée** :

- une intention portant sur l'**album** est réalisée, donc supprimée, par l'achat de n'importe laquelle de ses éditions (cf. cas d'usage 1) ;
- une intention portant sur une **édition** est réalisée, donc supprimée, par l'achat de cette édition. Lorsqu'un album fait l'objet de plusieurs intentions (sur plusieurs de ses éditions), **seules les intentions des éditions achetées** sont supprimées ; les autres sont conservées.

Une édition **déjà possédée** ne peut pas faire l'objet d'une intention d'achat : une édition achetée ne redevient jamais une intention. Pour acquérir un second exemplaire d'une même édition, l'utilisateur ajoute à l'album une **nouvelle édition** aux mêmes caractéristiques (une fonction de duplication d'édition pourra faciliter cette saisie) et place son intention sur cette nouvelle édition.

La saisie d'une intention d'achat présente le **minimum de champs** à renseigner : les caractéristiques propres à l'exemplaire possédé (gratuité, occasion, etc.) n'y figurent pas et ne font l'objet d'aucune règle particulière pour une édition non possédée.

Les autres règles métier sont à définir.

<!-- À compléter : cas d'usage, user stories, autres règles métier, flux applicatifs, etc. -->
