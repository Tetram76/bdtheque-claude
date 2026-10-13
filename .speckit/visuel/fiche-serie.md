# Fiche de série

Ce fichier décrit la présentation de la fiche d'une série dans la partie Consultation (`fonctionnel.md` § Structure de l'application). Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation).

La fiche est déclinée selon chacun des thèmes en lice pour la partie Consultation (cf. [`dashboard.md`](dashboard.md) § Alternatives) : comme toute page, elle suit l'identité visuelle du thème actif (cf. [`charte-graphique.md`](charte-graphique.md) § Portée).

---

## Maquettes

Toutes les déclinaisons forment la page « Fiche de série » du canevas des maquettes (cf. [`charte-graphique.md`](charte-graphique.md) § Maquettes). Leurs sources sont dans [`maquettes/fiche-serie/`](maquettes/fiche-serie/).

| Thème | Fichiers |
| --- | --- |
| Cases et bulles, violine | `Serie-Violine.dc.html` |
| Cases et bulles, mûre | `Serie-Mure.dc.html` |
| La gazette | `Serie-Gazette.dc.html` |
| Le sommaire de magazine | `Serie-Sommaire.dc.html` |
| Le festival en métro | `Serie-Festival.dc.html` |
| Le fanzine en risographie | `Serie-Riso.dc.html` |
| La vitrine de librairie | `Serie-Librairie.dc.html` |
| L'album de vignettes | `Serie-Vignettes.dc.html` |

`Main.dc.html` n'est pas une déclinaison : c'est la source commune, que chaque planche affiche avec son thème. Son nom est imposé par le canevas, dont c'est le fichier d'entrée.

Deux exemples, au choix du réglage « exemple » de la source commune : `longue` (par défaut), une série en cours de 32 albums (28 tomes, deux intégrales, deux hors-séries), dont cinq hors de la collection et un visé par une intention d'achat, dessinée par neuf auteurs, rattachée à un sous-univers ; `heads`, une série terminée et complète de quatre tomes sans titre propre. Les couvertures sont des illustrations de substitution.

## Contenu

De haut en bas :

- **En-tête** de la partie Consultation, avec l'entrée « Séries » active, puis le **fil d'Ariane** : Séries, puis la série.
- **En-tête de la fiche**, sur deux colonnes, qui s'empilent sur smartphone :
  - une **pile des premières couvertures** de la série, décorative ;
  - le titre, puis des étiquettes : le **statut**, le **nombre de tomes prévus** (nombre de tomes théorique) et la mention **Complète** ou **À compléter** ;
  - l'**avancement de la collection** : « N albums sur M dans la collection », avec une barre de progression ;
  - l'**éditeur** et la **collection** de la série (éditeur et collection template), chacun menant à sa fiche ;
  - les **auteurs de la série** (contributions template), présentés comme les contributions d'un album (`fiche-album.md` § Contenu) : nombre d'auteurs distincts, une ligne par rôle, rôle replié au-delà de six noms ;
  - les **genres** et les **univers**, chaque univers avec la chaîne de ses ancêtres, comme sur la fiche d'un album.
  - Les autres valeurs template et les indicateurs d'exclusion n'y figurent pas (`fonctionnel.md` § Structure de l'application).
- **De quoi ça parle ?** : le résumé, puis les notes personnelles de la série (« Aucune note » sinon).
- **Combien d'albums ?** : le décompte (tomes, intégrales, hors-séries), puis les albums **dans l'ordre de la série** (`fonctionnel.md` § Ordre des albums dans une série), en deux groupes : **Tomes et intégrales**, puis **Hors-séries**. Chaque album est une carte, en grille, qui mène à sa fiche :
  - sa couverture, son tome (`fonctionnel.md` § Représentation du tome selon le type d'album) et son titre ; un album sans titre propre est nommé par son tome seul (« Tome 3 ») ;
  - son appréciation, en étoiles, lorsqu'elle est renseignée ;
  - son **appartenance à la collection** : étiquette pleine « Dans la collection », ou étiquette en tirets « Pas dans la collection » ou « Intention d'achat ».
- **Pied de page** : les dates de création et de modification de la fiche.

## Déclinaison par thème

Couleurs, typographie et touche propre de chaque thème : celles de la fiche d'un album (`fiche-album.md` § Déclinaison par thème), la pile de couvertures reprenant le traitement du visuel de l'album.
