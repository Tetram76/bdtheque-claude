# Charte graphique

Ce fichier décrit l'identité visuelle de l'application : ton, couleurs, typographie et éléments graphiques. Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation). L'exigence de rendu est posée par `fonctionnel.md` § Design et charte graphique ; la présentation de chaque écran est décrite dans son propre fichier de ce dossier, avec ses maquettes.

---

## Portée

La charte ci-dessous est issue de la maquette du dashboard (cf. [`dashboard.md`](dashboard.md)), premier écran conçu : elle s'applique à la partie **Consultation** (`fonctionnel.md` § Structure de l'application).

La charte de la partie **Administration** reste à définir. Elle s'écartera probablement un peu de celle-ci, dans un sens plus sobre (moins de fantaisie et de désordre), sans que ce soit statué.

## Ton général

- **Joyeux et chaleureux**, ni sérieux ni sévère : l'application présente une collection de BD, avec un clin d'œil à l'univers de la BD (**cases et bulles**).
- **Sans pastiche de comics** : ni police d'onomatopée, ni encarts de narration jaunes, ni gros contours noirs carrés, ni trame de points.
- **Sobriété du désordre** : le côté « pas aligné au cordeau » (éléments légèrement inclinés ou décalés) fait partie de l'identité, mais reste **ponctuel** : il est réservé au logo, aux bulles de titre, aux compteurs mis en avant et aux cadres de couverture. Les blocs et leur contenu (listes, étiquettes, chiffres) restent droits et alignés.

## Couleurs

- **Thème** : sombre, sur un fond **violet profond**. Deux teintes restent en lice, le choix étant reporté aux maquettes des écrans suivants :

  | Teinte | Fond | Blocs | Blocs en relief | Bordures | Texte secondaire | Texte atténué |
  | --- | --- | --- | --- | --- | --- | --- |
  | **Violine** | `#2B1030` | `#3A1641` | `#431B4B` | `#6A3375` | `#EBD3EF` | `#CFA9D6` |
  | **Mûre** | `#1F1033` | `#2A1844` | `#321D4F` | `#533A78` | `#DCD1EE` | `#B9A8D8` |

- **Texte principal et bulles** : crème `#FBF3EA`.
- **Couleur d'accent** : ambre `#FFB547` — élément actif de la navigation, montants en euros du jour, chiffre principal.
- **Palette secondaire**, vive, pour varier les couleurs sans hiérarchie entre elles (catégories comme les genres et les éditeurs, compteurs, cadres de couverture, pastilles) : turquoise `#4FD1C5`, corail `#FF7A6B`, lilas `#B69CFF`, vert `#9BE07A`, bleu `#7FB8FF`, rose `#F7A8D0`, jaune `#FFD166`. Un texte posé sur ces couleurs est de la couleur du fond.

## Typographie

- **Titres et chiffres mis en avant** : serif doux et gras (*Fraunces*).
- **Corps de texte** : sans-serif lisible (*DM Sans*).

## Éléments graphiques

- **Cases** : chaque bloc de contenu est une case aux **coins arrondis irréguliers** (rayons différents à chaque coin, comme tracés à la main), à fine bordure, posée droite. Les cases ne sont pas animées au survol.
- **Bulles** : le titre d'une case est une **bulle** crème avec sa queue, légèrement inclinée, à cheval sur le bord supérieur de la case. Il est formulé comme une **phrase parlée** (question ou exclamation : « Plutôt quel genre ? », « Tout juste arrivés ! »), jamais comme un simple intitulé.
- **Couvertures** : les couvertures d'albums sont le principal élément visuel. Chacune est présentée dans un **cadre coloré** (bordure tirée de la palette secondaire), légèrement incliné ; au survol, la couverture se redresse et se soulève.
- **Logo** : le nom de l'application sur une étiquette crème inclinée, ponctuée de l'accent.
- **Navigation** : menu en forme de pilule, l'entrée active en accent ; champ de recherche et bouton d'aide arrondis, toujours visibles dans l'en-tête.

## Maquettes

Les maquettes sont réalisées dans un canevas *Design* de Claude, dont les sources sont conservées dans [`maquettes/`](maquettes/), un sous-dossier par écran ; elles ne s'ouvrent qu'à travers ce canevas. Leurs données sont fictives. Le fichier de chaque écran indique ses sources et le lien de son canevas.
