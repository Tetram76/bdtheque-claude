# Présentation des données

Langue, culture et fuseau horaire d'affichage, tri et navigation par initiale, libellés des albums et des éditions.

---

## Langue et culture d'affichage

La **culture d'affichage** (ex. français de France), et non une simple langue, est un paramètre de l'application : ce choix pilote la langue des textes de l'interface, mais aussi le **formatage des dates, nombres et montants** et l'**ordre de tri** des listes et résultats de recherche (voir « Tri et navigation par initiale » ci-dessous).

La culture s'applique à tout texte **au moment où il est présenté** : un texte que l'application produit à partir de données enregistrées (ex. le journal des modifications, cf. `modele-metier.md` § Entrée du journal des modifications) n'est jamais figé dans la culture en vigueur lors de leur enregistrement.

**Fuseau horaire** : toute présentation à l'utilisateur et toute interaction avec lui suivent le **fuseau horaire de son navigateur**, celui en cours au moment de la présentation ou de l'interaction (un navigateur qui change de fuseau pendant une visite, ex. lors d'un voyage, suit son nouveau fuseau) : dates et heures présentées (ex. dates de création et de modification d'une fiche), comme dates et heures saisies ou recherchées (une date et heure est comparée par son jour dans ce fuseau, cf. `fonctionnel/recherche-avancee.md` § Comparaisons). Côté serveur, les dates et heures sont enregistrées et traitées en **temps universel**.

Seule la culture **française** est proposée actuellement ; l'application est conçue pour pouvoir en proposer d'autres ultérieurement sans refonte.

## Tri et navigation par initiale

### Règle générale

Le tri alphabétique des **séries**, **albums** et **artistes** repose sur une **clé de tri** distincte de la valeur affichée. L'affichage respecte toujours la forme naturelle ; seule la clé de tri est transformée.

### Entrées de la navigation par initiale

La navigation par initiale propose les entrées **A** à **Z**, une entrée **#** et une entrée **@** :

- Une initiale qui est une lettre ayant une **lettre de base** de `A` à `Z` dans l'ordre alphabétique (lettre accentuée, en minuscule, ligature…) est rangée sous cette lettre de base en majuscule : `É`, `È`, `Ê` → **E** ; `à` → **A** ; `Ç` → **C** ; `Œ` → **O**. À l'intérieur d'une entrée, l'ordre de tri linguistique habituel s'applique.
- Une initiale **numérique** (chiffre) est rangée sous **#** (ex. `13`, `2001 Nights`).
- Toute **autre** initiale (ponctuation, symbole, lettre sans lettre de base de `A` à `Z`, comme celles d'un autre alphabet que le latin) est rangée sous **@** (ex. `...Et après`, `Ωmega`).

### Titres (séries et albums)

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

### Calcul et stockage de la clé de tri

La clé de tri des titres est **calculée automatiquement** à partir du titre et **stockée explicitement** en base. Elle est toujours visible dans le formulaire de saisie et modifiable par l'utilisateur.

La clé possède deux états :

- **Auto** : calculée depuis le titre, jamais modifiée manuellement. Recalculée automatiquement à chaque modification du titre (mise à jour en temps réel dans le formulaire). C'est l'état par défaut.
- **Manuelle** : l'utilisateur a explicitement saisi une valeur. Elle est conservée telle quelle même si le titre change. Un indicateur visuel signale cet état dans le formulaire ; un bouton "réinitialiser" permet de repasser en mode Auto.

La clé stockée fait foi pour le tri et la navigation par initiale.

### Artistes

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

## Libellé d'un album

Le libellé d'un album utilise son titre en forme naturelle (cf. § Tri et navigation par initiale).

Il peut optionnellement inclure le **titre de la série**, utilisé quand le contexte ne rend pas la série évidente (ex. résultat de recherche multi-séries). Quand la série est incluse et que l'album n'a pas de titre propre, le titre de la série tient lieu de titre.

### Représentation du tome selon le type d'album

| Type | Hors-série | Format du tome (avec titre propre) | Format du tome (sans titre propre) |
| --- | --- | --- | --- |
| Régulier | non | `T. {N}` | `Tome {N}` |
| Régulier | oui | `HS[ {N}]` | `Hors-série[ {N}]` |
| Intégrale | non | `INT.[ - {N}][ [{Début} à {Fin}]]` | `Intégrale[ - {N}][ [{Début} à {Fin}]]` |
| Intégrale | oui | `INT.HS[ - {N}][ [{Début} à {Fin}]]` | `Intégrale hors-série[ - {N}][ [{Début} à {Fin}]]` |

Le numéro de tome n'apparaît que s'il est renseigné. Pour les intégrales, la séquence `[Début à Fin]` n'apparaît que si les deux bornes sont renseignées.

### Deux formats configurables (paramètre de l'application)

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

### Fallback

Si toutes les composantes sont vides : `<Sans titre>`.

### Exemples (format 0, avec série)

| Album | Résultat |
| --- | --- |
| Titre + série + tome 5 | `Le Lotus bleu (Tintin - T. 5)` |
| Sans titre, série + tome 5 | `Tintin - T. 5` |
| Titre + hors-série 2 | `Titre (Série - HS 2)` |
| Titre + intégrale tomes 1 à 6 | `Titre (Série - INT. [1 à 6])` |

## Libellé d'une édition

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

## Libellés contextuels sur l'édition

Le libellé de la date et du montant d'acquisition s'adapte au mode d'acquisition :

| Mode | Libellé de la date | Libellé du montant |
| --- | --- | --- |
| `Achat` | Date d'achat | Prix d'achat |
| `Offerte` | Date d'acquisition | Valeur d'acquisition |
| `Échange` | Date d'acquisition | Valeur d'acquisition |
| `Gagnée` | Date d'acquisition | Valeur d'acquisition |
| `Héritée` | Date d'acquisition | Valeur d'acquisition |

Dans tous les cas, le champ montant est affiché et reste optionnel : même sans transaction financière, une édition peut avoir une valeur marchande connue. Si l'édition est marquée **Gratuite**, le champ montant est désactivé et vidé, de même que la valeur initiale. Une édition achetée (mode `Achat`) ne peut pas être marquée gratuite.
