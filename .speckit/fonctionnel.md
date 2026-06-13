# Fonctionnel

Ce fichier décrit les fonctionnalités de l'application, ainsi que les éléments de design et la charte graphique.

---

## Contexte

L'application est la réécriture d'une application client lourd existante sous forme d'application web n-tiers. L'application existante est une **BDthèque** : un gestionnaire de collection de bandes dessinées.

## Nom de l'application

Le nom définitif de l'application n'est pas encore défini. Le **nom de travail** est **`bdtheque-claude`** (nom du dépôt git).

## Règles métier

### Appartenance à la collection

Un album est considéré comme **faisant partie de la collection** uniquement s'il possède au moins une édition. Un album sans édition est un album catalogué mais non possédé.

Une édition peut exister dans la base sans être possédée (sans données d'acquisition). C'est le cas uniquement des éditions créées via une intention d'achat. Le `Mode d'acquisition` est optionnel en base, mais la saisie normale d'une édition continue à l'imposer : il n'est absent que lorsque l'édition est issue d'une intention d'achat non encore concrétisée.

### Initialisation d'une nouvelle édition depuis la série

Lors de la création d'une nouvelle édition pour un album, si la série de l'album définit des valeurs template, les champs suivants sont pré-remplis :

- Éditeur et collection éditeur
- Catégorie d'édition, état, reliure, orientation, sens de lecture, format
- En couleur

Ces valeurs restent modifiables : l'édition est source de vérité, la série n'est qu'un point de départ.

### Initialisation des contributions depuis la série

Lors du rattachement d'un album à une série, si l'album n'a encore aucune contribution saisie, les contributions de la série sont recopiées sur l'album comme point de départ. L'album reste la source de vérité : ces contributions peuvent ensuite être modifiées ou supprimées indépendamment de la série.

### Genres et univers d'un album

Les genres et univers affichés pour un album dépendent de son rattachement à une série. La même règle s'applique aux deux :

- **Album dans une série** : les valeurs affichées sont l'**union sans doublons** de celles de l'album et de celles de la série.
- **Album sans série** : seules les valeurs propres à l'album sont affichées.

Les genres et univers sont optionnels sur l'album comme sur la série.

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

### Tri et navigation par initiale

#### Règle générale

Le tri alphabétique des **séries**, **albums** et **artistes** repose sur une **clé de tri** distincte de la valeur affichée. L'affichage respecte toujours la forme naturelle ; seule la clé de tri est transformée.

#### Titres (séries et albums)

- La clé de tri d'un titre est le titre **sans son article initial**.
- L'**initiale de navigation** (navigation par lettre) est la première lettre de la clé de tri, soit le premier caractère **hors article**.
- L'affichage du titre reste toujours en forme naturelle, article inclus en tête.

Exemples :

| Titre affiché | Clé de tri | Initiale |
|---|---|---|
| `Le Lotus bleu` | `Lotus bleu` | **L** |
| `Les Schtroumpfs` | `Schtroumpfs` | **S** |
| `L'Épervier` | `Épervier` | **É** |
| `Tintin` | `Tintin` | **T** |

Ce traitement est **non configurable** : il est obligatoire pour que la navigation par initiale soit viable.

#### Calcul et stockage de la clé de tri

La clé de tri des titres est **calculée automatiquement** (suppression de l'article initial par liste prédéfinie) et **stockée explicitement** en base. Elle est toujours visible dans le formulaire de saisie et modifiable par l'utilisateur.

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
|---|---|---|---|
| Jean Van Hamme (sans pseudo) | `Jean Van Hamme` | `Van Hamme Jean` | **V** |
| Pseudonyme `Moebius` | `Moebius` | `Moebius` | **M** |
| Georges Remi + pseudo `Hergé` | `Hergé` | `Hergé` | **H** |

### Libellé d'un album

Un libellé d'album existe en deux **modes** :

- **Simple** : le titre de l'album est utilisé tel quel.
- **Complet** : le titre est mis en forme selon une convention de saisie (gestion des articles, ordre de tri). *Cette convention est à définir ultérieurement.*

Le libellé peut optionnellement inclure le **titre de la série** (`AvecSerie`), utilisé quand le contexte ne rend pas la série évidente (ex. résultat de recherche multi-séries). Quand la série est incluse et que l'album n'a pas de titre propre, le titre de la série tient lieu de titre.

#### Représentation du tome selon le type d'album

| Type | Format du tome |
|---|---|
| Régulier | `T. {N}` (simple) ou `Tome {N}` (si pas de titre propre) |
| Hors-série | `HS[ {N}]` (simple) ou `Hors-série[ {N}]` (si pas de titre) |
| Intégrale | `INT.[ - {N}][ [{Début} à {Fin}]]` ou `Intégrale[ - {N}][ [{Début} à {Fin}]]` |

Le numéro de tome n'apparaît que s'il est renseigné. Pour les intégrales, la séquence `[Début à Fin]` n'apparaît que si au moins l'un des deux est renseigné.

#### Deux formats configurables (préférence utilisateur)

**Format 0 — `Album (Série - Tome)`** *(défaut)*

```
{Titre} ({Série} - {Tome})
```
- Si pas de titre : `{Série} - {Tome}`
- Si pas de série : `{Titre} ({Tome})`

**Format 1 — `Tome - Album (Série)`**

```
{Tome} - {Titre} ({Série})
```
- Si pas de titre : `{Tome} - {Série}`
- Si pas de série : `{Tome} - {Titre}`

#### Fallback

Si toutes les composantes sont vides : `<Sans titre>`.

#### Exemples (format 0, avec série)

| Album | Résultat |
|---|---|
| Titre + série + tome 5 | `Le Lotus bleu (Tintin - T. 5)` |
| Sans titre, série + tome 5 | `Tintin - T. 5` |
| Titre + hors-série 2 | `Titre (Série - HS 2)` |
| Titre + intégrale tomes 1 à 6 | `Titre (Série - INT. [1 à 6])` |

### Libellé d'une édition

Le libellé d'une édition est construit dynamiquement selon le modèle suivant :

```
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
|---|---|---|
| `Achat` | Date d'achat | Prix d'achat |
| `Offerte` | Date d'acquisition | Valeur d'acquisition |
| `Échange` | Date d'acquisition | Valeur d'acquisition |
| `Gagnée` | Date d'acquisition | Valeur d'acquisition |
| `Héritée` | Date d'acquisition | Valeur d'acquisition |

Dans tous les cas, le champ montant est affiché et reste optionnel : même sans transaction financière, une édition peut avoir une valeur marchande connue. Si l'édition est marquée **Gratuite**, le champ montant est désactivé et vidé.

### Validation de l'ISBN

La saisie d'un ISBN vérifie le chiffre de contrôle (ISBN-10 ou ISBN-13) afin de détecter les erreurs de frappe. Cette vérification est **non bloquante** : l'utilisateur est averti en cas d'incohérence mais peut enregistrer la valeur telle quelle (certains éditeurs ont publié des albums avec un ISBN erroné).

### Estimation de la valeur des éditions

- La **valeur estimée** d'une édition est calculée dynamiquement à partir des données de la collection, elle n'est pas stockée en base.
- Le modèle d'estimation retenu est un **Random Forest**.

### Gestion des devises

- L'application gère des **prix, montants et valeurs** (ex. valeur d'achat, valeur estimée d'un album, etc.).
- **Saisie et affichage des données de base** : peuvent être faits dans n'importe quelle devise.
- **Analyses et statistiques** : toujours affichées en **euro (€)**.
- **Agrégation multi-devises** : toute agrégation de données exprimées dans des devises différentes est convertie et consolidée en euro.
- **Taux de change** :
  - Certaines devises ont un taux **fixe et définitif** vis-à-vis de l'euro (ex. Franc français : 6,55957 FF = 1 €) → le taux est une constante.
  - D'autres devises ont un taux **variable** (ex. Dollar américain) → le taux de change utilisé devra être configurable ou récupéré dynamiquement.

## Design et charte graphique

L'interface doit offrir un rendu **visuellement premium**, clairement au-dessus du rendu par défaut des frameworks UI. Un template de base (Bootstrap, Material out-of-the-box, etc.) n'est pas acceptable.

### Identité visuelle

- **Thème** : dark first — fond sombre, accents lumineux.
- **Couleur d'accent** : ambre chaud (évoque l'impression et la presse BD classique).
- **Typographie** : serif élégant pour les titres (ex. *Playfair Display*), sans-serif moderne pour le corps (ex. *Inter*).
- **Visuels** : les couvertures d'albums sont le principal élément visuel — elles doivent être mises en valeur (hero, cards avec effet au survol, profondeur).
- **Esthétique générale** : médiathèque culturelle premium (référence : Letterboxd).

## Page d'accueil

La page d'accueil est un **dashboard public** présentant les statistiques principales de la collection. Elle n'est pas personnalisée (pas d'authentification sur la partie consultation). Certaines statistiques pourront être présentées sous forme de **graphiques** lorsque c'est pertinent.

Les statistiques du dashboard incluent notamment (liste non exhaustive) :

- **Compteurs globaux** : nombre total d'albums, nombre total de séries.
- **Répartitions clés** : intégrales, hors-séries, par genre, par éditeur.
- **Indicateurs de valeur** (exprimés en €) : prix moyen, médian, min/max, valeur totale connue et valeur totale estimée.

## Structure de l'application

L'application se compose de trois parties distinctes :

1. **Consultation** — accessible **publiquement** (sans authentification) :
   - Affichage de la **fiche détaillée** de chaque entité : toutes les informations publiques disponibles sont présentées.
   - **Navigation inter-entités** : depuis la fiche d'une entité, il est possible de naviguer vers les fiches des entités associées (ex. album → série, album → édition, album → auteur, auteur → bibliographie, etc.).
   - **Recherche facilement accessible** à tout moment depuis n'importe quelle page de la partie consultation.
   - **Recherche simple** : par type d'entité (ex. rechercher des albums, des auteurs, des séries, etc.).
   - **Recherche avancée** : exploite les **liens entre entités** pour des requêtes cross-domaines (ex. albums d'un auteur donné, séries d'un éditeur, etc.).
   - **États et statistiques** : rapports et indicateurs sur la collection (à préciser).
2. **Administration** — protégée par **authentification** :
   - CRUD sur toutes les entités, paramétrage de l'application, gestion des référentiels, etc.
   - La **saisie des données est manuelle**, mais assistée par des **imports depuis des sources externes** : APIs, parsing de sites web, etc. (les sources concrètes restent à définir).
   - L'accès est protégé par un **compte administrateur unique** (login + mot de passe). Pas de gestion multi-utilisateurs.
3. **Aide contextuelle** — accessible à tout moment, depuis n'importe quelle page de l'application :
   - Affiche des informations d'aide **relatives à la page en cours** (aide sensible au contexte).
   - Le contenu peut être **riche** : texte, captures d'écran, tableaux, exemples, etc. — pas seulement de courts textes explicatifs.

## Fonctionnalités de second plan

Ces fonctionnalités sont prévues dans une phase ultérieure.

### Identification des albums manquants dans une série

Accessible en **mode consultation** (public). Permet de savoir quels albums d'une série ne sont pas encore présents dans la collection.

Un album est considéré **manquant** pour une série si toutes les conditions suivantes sont réunies :

1. La série n'est pas exclue de la recherche des manquants (attribut `Exclure des manquants` sur la série).
2. Il existe un **trou dans la séquence numérotée** de la série. La séquence est construite à partir de :
   - Le **numéro de tome** des albums réguliers.
   - La **plage [tome de début → tome de fin]** des intégrales (chaque tome couvert par la plage est considéré présent).
3. Si le **nombre d'albums théorique** de la série est renseigné, il tient lieu de **tome maximum** : tout tome absent entre le tome le plus élevé présent dans la collection et ce maximum est également considéré manquant.

Les albums sans numéro de tome et les hors-série ne participent pas à la détection des manquants.

Par défaut, seules les **éditions possédées** couvrent leur numéro de tome. Les intégrales possédées couvrent leur plage de tomes, et les albums marqués en intention d'achat sont considérés comme "pris en charge". Trois options permettent d'affiner ce calcul :

- **Exclure les intégrales** : les intégrales présentes dans la collection ne couvrent plus leur plage de tomes. Les tomes correspondants restent manquants. Utile pour un utilisateur qui souhaite posséder chaque tome sous forme d'album individuel.
- **Exclure les intentions d'achat** : les albums marqués en intention d'achat ne comblent plus les trous et apparaissent comme manquants. Sans cette option, la liste affiche uniquement les manquants non encore planifiés (utile pour identifier les prochains achats à programmer) ; avec cette option, elle affiche tous les manquants réels de la collection, intentions d'achat incluses.
- **Inclure les non possédées** : par défaut, seules les éditions possédées couvrent leur numéro de tome. Cette option étend la couverture aux éditions cataloguées mais non possédées.

### Estimation de sortie d'un nouvel album

Accessible en **mode consultation** (public). Permet d'estimer la date de sortie du prochain tome d'une série, sur la base du rythme de parution observé. On parle d'**estimation** et non de prévision : le facteur humain rend toute prédiction précise impossible, et l'objectif est uniquement de donner un ordre de grandeur.

Règles de calcul :

1. L'estimation se fait **série par série**.
2. Elle se base sur les **dates de première publication** des albums réguliers de la série (les intégrales et hors-série sont exclus du calcul).
3. Il faut **au moins 2 albums réguliers** avec une date de première publication **passée** pour pouvoir produire une estimation. Les albums dont la date de première publication est strictement dans le futur sont exclus du calcul du rythme. La règle s'adapte à la granularité de la date saisie :

- **Mois + année** : le mois en cours est considéré comme passé. Seuls les mois strictement postérieurs au mois courant sont exclus.
- **Année seule** : l'année en cours est considérée comme passée. Seules les années strictement postérieures à l'année courante sont exclues.

Dans les deux cas, la granularité ne permettant pas de savoir si l'album est déjà sorti, l'algorithme considère qu'il l'est.
4. Le délai attendu avant le prochain tome est déduit à partir des **délais entre les parutions précédentes**, en tenant compte des éventuels **trous dans la séquence de tomes** (un tome manquant dans la collection ne doit pas fausser le calcul du rythme).
5. Les **évolutions de rythme** (accélération ou décélération des parutions) doivent être prises en compte dans l'estimation.

### Intention d'achat

Gestion en **mode administration** (authentifié). Consultation de la liste en **mode consultation** (public, lecture seule).

Deux cas d'usage distincts :

1. **Album** : l'utilisateur souhaite acquérir un album en particulier, sans contrainte sur l'édition. L'intention porte sur l'album ; n'importe quelle édition satisfera l'intention.
2. **Édition spécifique** : l'utilisateur souhaite acquérir une édition précise d'un album (éditeur, collection, année, etc.), qu'il possède déjà cet album dans une autre édition ou non.

Ces deux cas sont représentés par une entité **Intention d'achat** distincte, liée soit à un Album soit à une Édition. Les autres règles métier sont à définir.

<!-- À compléter : cas d'usage, user stories, autres règles métier, flux applicatifs, etc. -->
