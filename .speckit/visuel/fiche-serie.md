# Fiche de série

Ce fichier décrit la présentation de la fiche d'une série dans la partie Consultation (`fonctionnel.md` § Structure de l'application). Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation).

La fiche est déclinée selon chacun des thèmes en lice pour la partie Consultation (cf. [`dashboard.md`](dashboard.md) § Alternatives) : comme toute page, elle suit l'identité visuelle du thème actif (cf. [`charte-graphique.md`](charte-graphique.md) § Portée).

---

## Maquettes

Toutes les déclinaisons sont présentées côte à côte par la visionneuse [`maquettes/fiche-serie/`](maquettes/fiche-serie/index.html) (cf. [`charte-graphique.md`](charte-graphique.md) § Maquettes). Elles ont une source commune, `mockup.html`, que chacune affiche avec son thème, choisi par le paramètre `theme` :

| Thème | Paramètre `theme` |
| --- | --- |
| Cases et bulles, violine | `violine` |
| Cases et bulles, mûre | `mure` |
| La gazette | `gazette` |
| Le sommaire de magazine | `sommaire` |
| Le festival en métro | `festival` |
| Le fanzine en risographie | `riso` |
| La vitrine de librairie | `librairie` |
| L'album de vignettes | `vignettes` |

Quatre exemples, au choix du réglage « exemple » de la source commune : `longue` (par défaut), une série en cours de 32 albums (28 tomes, deux intégrales, deux hors-séries), dont cinq hors de la collection et un visé par une intention d'achat, dessinée par neuf auteurs, rattachée à un sous-univers ; `heads`, une série terminée et complète de quatre tomes sans titre propre ; `courte`, une série d'un seul album, sans éditeur, collection, nombre de tomes prévu ni résumé ; `vide`, une série tout juste créée, sans album, auteur, genre ni statut. Les couvertures sont des illustrations de substitution.

## Contenu

De haut en bas :

- **En-tête** de la partie Consultation, avec l'entrée « Séries » active, puis le **fil d'Ariane** : Séries, puis la série.
- **En-tête de la fiche**, sur deux colonnes, qui s'empilent sur smartphone :
  - une **pile de trois couvertures**, décorative : celles des premiers tomes de la série ; la pile garde toujours trois cartes, pour qu'une série ne se confonde jamais avec un album, une place au-delà des tomes de la série étant une carte générique ;
  - le titre, puis des étiquettes : le **statut**, le **nombre de tomes prévus** (nombre de tomes théorique) et la mention **Complète** ou **À compléter** ;
  - l'**avancement de la collection** : « N albums sur M dans la collection », avec une barre de progression ;
  - l'**éditeur** et la **collection** de la série (éditeur et collection template) ; l'éditeur mène à son site web lorsqu'il est connu, signalé comme un lien externe, la collection n'est pas un lien (`fonctionnel.md` § Structure de l'application) ;
  - les **auteurs de la série** (contributions template), présentés comme les contributions d'un album (`fiche-album.md` § Contenu) : nombre d'auteurs distincts, une ligne par rôle, rôle replié au-delà de six noms ;
  - les **genres** et les **univers**, chaque univers avec la chaîne de ses ancêtres, comme sur la fiche d'un album.
  - Une donnée non renseignée (statut, nombre de tomes prévus, éditeur, collection, résumé) reste affichée, avec la mention « Non renseigné » en texte atténué, jamais sous la forme d'un lien vide, comme sur la fiche d'un album. Une série sans album, sans auteur ou sans genre le dit de même (« Aucun album dans la série pour l'instant », sans barre de progression ; « Aucun auteur crédité sur la série » ; « Aucun genre »).
  - Les autres valeurs template et les indicateurs d'exclusion n'y figurent pas (`fonctionnel.md` § Structure de l'application).
- **De quoi ça parle ?** : le résumé, puis les notes personnelles de la série (« Aucune note » sinon).
- **Combien d'albums ?** : le décompte (tomes, intégrales, hors-séries), puis les albums **dans l'ordre de la série** (`fonctionnel.md` § Ordre des albums dans une série), en deux groupes : **Tomes et intégrales**, puis **Hors-séries**. Chaque album est une carte, en grille, qui mène à sa fiche :
  - sa couverture, son tome (`fonctionnel.md` § Représentation du tome selon le type d'album) et son titre ; un album sans titre propre est nommé par son tome seul (« Tome 3 ») ;
  - son appréciation, en étoiles, lorsqu'elle est renseignée ;
  - son **appartenance à la collection** : étiquette pleine « Dans la collection », ou étiquette en tirets « Hors collection » ou « Intention d'achat ».
- **Pied de page** : les dates de création et de modification de la série, introduites par ce qu'elles concernent (« Série créée le …, modifiée le … »), jamais par « Fiche ».

## Déclinaison par thème

Couleurs, typographie et touche propre de chaque thème : celles de la fiche d'un album (`fiche-album.md` § Déclinaison par thème), la pile de couvertures reprenant le traitement du visuel de l'album.
