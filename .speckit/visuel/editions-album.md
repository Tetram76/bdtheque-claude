# Éditions et intentions d'achat d'un album

Ce fichier décrit la présentation de la page des éditions et intentions d'achat d'un album dans la partie Administration (`fonctionnel.md` § Structure de l'application) : la page où se gèrent les éditions d'un album et ses intentions d'achat, ouverte depuis la liste des albums ([`liste-albums.md`](liste-albums.md)). Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation).

Ce n'est pas une fiche de saisie : rien ne s'y enregistre, elle n'a donc pas de barre d'enregistrement, et on la quitte sans rien perdre. Elle mène à la fiche de saisie de chaque édition ([`saisie-edition.md`](saisie-edition.md)), et à celle de l'album ([`saisie-album.md`](saisie-album.md)), qui ne présente pas ses éditions. Elle est déclinée selon chacun des thèmes en lice (cf. [`dashboard.md`](dashboard.md) § Alternatives), dans le ton sobre de l'administration (cf. [`administration.md`](administration.md) § Ton).

---

## Maquettes

Toutes les déclinaisons sont présentées côte à côte par la visionneuse [`maquettes/editions-album/`](maquettes/editions-album/index.html) (cf. [`charte-graphique.md`](charte-graphique.md) § Maquettes). Elles ont une source commune, `mockup.html`, que chacune affiche avec son thème, choisi par le paramètre `theme` (mêmes valeurs que pour la page d'accueil de l'administration, [`administration.md`](administration.md) § Maquettes).

Réglages de la source commune :

| Réglage | Valeurs |
| --- | --- |
| `exemple` | `sansEdition` (par défaut) : un album sans édition ni intention d'achat ; `editions` : un album, ses deux éditions et une intention d'achat sur une troisième ; `integrale` : une intégrale sans édition, visée par une intention d'achat sur l'album |
| `choixIntention` | choix de l'intention d'achat ouvert (exemple `sansEdition`) |
| `actions` | menu des autres actions de la première édition ouvert |
| `menuOpen` | menu de l'administration ouvert |

Les données sont fictives ; les couvertures sont des illustrations de substitution.

## Contenu

- **En-tête et menu** de l'administration, l'entrée « Albums » étant l'entrée courante du menu.
- **Fil d'Ariane** : Administration, Albums, puis l'album.
- **Titre** « Éditions et intentions d'achat », suivi du libellé de l'album (`fonctionnel.md` § Libellé d'un album). À droite : **Modifier l'album**, qui ouvre sa fiche de saisie, et **Voir dans la consultation** (réduits à leur icône sur smartphone).
- **L'album**, en bref, pour savoir de quel album il s'agit sans quitter la page : sa vignette de couverture (visuel générique sans couverture), son libellé, ses auteurs, sa date de première publication et son appartenance à la collection (« Dans la collection », « Hors collection » ou « Intention d'achat », comme dans la liste des albums) ; l'aide rappelle que l'album lui-même se modifie sur sa fiche de saisie.
- **Les éditions**, celles que possède la collection, une intention d'achat n'étant jamais présentée comme une édition (`fonctionnel.md` § Intention d'achat) :
  - le décompte (« 2 éditions », « Aucune édition »), et à droite **Nouvelle édition**, mise en relief, qui ouvre la fiche de saisie d'une nouvelle édition de l'album ; pour un album lui-même visé par une intention d'achat, qui n'a aucune édition, ce bouton cède la place à l'aide, qui rappelle que sa première édition s'ajoute en confirmant l'achat ;
  - chaque **édition**, une ligne, dense comme une table sur grand écran : sa vignette de couverture, son libellé (`fonctionnel.md` § Libellé d'une édition) avec, en petit dessous, sa catégorie, sa reliure et son nombre de pages, un détail (mode et année d'acquisition, nombre de visuels), puis ses actions : **Modifier**, qui ouvre sa fiche de saisie, et un menu « ⋯ » : **Voir dans la consultation** et, séparée par un filet, **Supprimer l'édition…** ;
  - sans édition, « Aucune édition dans la collection » ;
  - l'aide rappelle que chaque édition, avec ses visuels, se saisit sur sa propre fiche.
- **Les intentions d'achat** :
  - le décompte (« 1 intention d'achat », « Aucune intention d'achat »), et à droite le bouton qui en ajoute une (`fonctionnel.md` § Intention d'achat) :
    - pour un album **sans édition ni intention**, **Nouvelle intention d'achat**, qui propose deux choix : **Sur l'album…**, toute édition de l'album faisant l'affaire, et **Sur une nouvelle édition…** ; un clic en dehors des choix ouverts les referme ;
    - pour un album qui a une édition, possédée ou visée, seule une édition précise peut l'être : un bouton direct, **Nouvelle intention sur une édition…**, sans choix à faire ; plusieurs éditions d'un même album peuvent être visées, une intention chacune ;
    - dans les deux cas, viser une édition ouvre la fiche de saisie d'une nouvelle intention d'achat ([`saisie-edition.md`](saisie-edition.md)), sans exemplaire ni valeur ;
    - aucun bouton pour un album lui-même visé ;
  - chaque **intention**, une ligne, distinguée comme dans la liste des intentions d'achat ([`listes.md`](listes.md)) par une étiquette : **Album**, pleine, suivie de « toute édition », ou **Édition**, en contour, suivie du libellé de l'édition visée en gras, avec sa catégorie et sa reliure en petit dessous ; la date où elle a été posée ; puis ses actions : **Confirmer l'achat** (`fonctionnel.md` § Réalisation d'une intention) et un menu « ⋯ » : **Modifier l'intention**, pour une intention sur une édition ; **Viser plutôt une édition…** pour une intention sur l'album, ou **Viser plutôt l'album…** pour la seule intention d'un album sans édition, la conversion supprimant l'édition visée ; et, séparée par un filet, **Retirer l'intention…**, qui supprime avec elle l'édition visée (`fonctionnel.md` § Suppression des entités). Un clic en dehors d'un menu ouvert le referme ;
  - sans intention, « Aucune intention d'achat » ;
  - l'aide rappelle qu'une intention ne vise l'album entier que tant qu'il n'a aucune édition, possédée ou visée.
- Sous les blocs, les dates de création et de modification de la fiche de l'album.
- **Smartphone** : chaque édition tient sur deux lignes, la vignette à gauche : le libellé et les actions (Modifier réduit à son icône), puis le détail ; chaque intention de même, Confirmer l'achat réduit à son icône, la date passant sous ce qu'elle vise.

## Contrôles

Ceux de la liste des albums ([`liste-albums.md`](liste-albums.md) § Contrôles).

## Déclinaison par thème

Celle de la saisie d'un album ([`saisie-album.md`](saisie-album.md) § Déclinaison par thème). Les mentions au-dessus des titres de bloc sont : Identification (L'album), Exemplaires (Les éditions), Achats prévus (Les intentions d'achat).
