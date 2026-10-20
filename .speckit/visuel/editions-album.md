# Éditions et intentions d'achat d'un album

Ce fichier décrit la présentation de la page des éditions et intentions d'achat d'un album dans la partie Administration (`fonctionnel.md` § Structure de l'application) : la page où se gèrent les éditions d'un album et ses intentions d'achat, ouverte depuis la liste des albums ([`liste-albums.md`](liste-albums.md)). Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation).

Ce n'est pas une fiche de saisie : rien ne s'y enregistre, elle n'a donc pas de barre d'enregistrement, et on la quitte sans rien perdre. Elle mène à la fiche de saisie de chaque édition ([`saisie-edition.md`](saisie-edition.md)), et à celle de l'album ([`saisie-album.md`](saisie-album.md)), qui ne présente pas ses éditions. Elle est déclinée selon chacun des thèmes en lice (cf. [`dashboard.md`](dashboard.md) § Alternatives), dans le ton sobre de l'administration (cf. [`administration.md`](administration.md) § Ton).

---

## Maquettes

Toutes les déclinaisons sont présentées côte à côte par la visionneuse [`maquettes/editions-album/`](maquettes/editions-album/index.html) (cf. [`charte-graphique.md`](charte-graphique.md) § Maquettes). Elles ont une source commune, `mockup.html`, que chacune affiche avec son thème, choisi par le paramètre `theme` (mêmes valeurs que pour la page d'accueil de l'administration, [`administration.md`](administration.md) § Maquettes).

Réglages de la source commune :

| Réglage | Valeurs |
| --- | --- |
| `exemple` | `sansIntention` (par défaut) : un album et son édition, sans intention d'achat ; `editions` : un album et ses trois éditions, dont une visée par une intention d'achat ; `integrale` : une intégrale sans édition, visée par une intention d'achat sur l'album |
| `choixIntention` | choix de l'intention d'achat ouvert |
| `actions` | menu des autres actions d'une édition ouvert |
| `menuOpen` | menu de l'administration ouvert |

Les données sont fictives ; les couvertures sont des illustrations de substitution.

## Contenu

- **En-tête et menu** de l'administration, l'entrée « Albums » étant l'entrée courante du menu.
- **Fil d'Ariane** : Administration, Albums, puis l'album.
- **Titre** « Éditions et intentions d'achat », suivi du libellé de l'album (`fonctionnel.md` § Libellé d'un album). À droite : **Modifier l'album**, qui ouvre sa fiche de saisie, et **Voir dans la consultation** (réduits à leur icône sur smartphone).
- **L'album**, en bref, pour savoir de quel album il s'agit sans quitter la page : sa vignette de couverture (visuel générique sans couverture), son libellé, ses auteurs, sa date de première publication et son appartenance à la collection (« Dans la collection », « Pas dans la collection » ou « Intention d'achat », comme dans la liste des albums) ; l'aide rappelle que l'album lui-même se modifie sur sa fiche de saisie.
- **Les éditions** :
  - le décompte (« 3 éditions, dont 2 dans la collection », « Aucune édition »), et à droite **Nouvelle édition**, mise en relief, qui ouvre la fiche de saisie d'une nouvelle édition de l'album, et **Intention d'achat**, qui propose de poser l'intention :
    - **Sur l'album…**, toute édition de l'album faisant l'affaire, tant qu'aucune de ses éditions n'est visée ;
    - **Sur une nouvelle édition…**, une édition précise, qui ouvre la fiche de saisie d'une édition visée par une intention d'achat, sans exemplaire ni valeur (`fonctionnel.md` § Intention d'achat) ; plusieurs éditions d'un même album peuvent être visées, une intention chacune ;
    - le bouton n'apparaît pas pour un album lui-même visé (`fonctionnel.md` § Intention d'achat) ; un clic en dehors des choix ouverts les referme ;
  - l'**intention d'achat sur l'album**, lorsqu'elle existe, en cadre en tirets, avec sa date, **Viser plutôt une édition…** et **Retirer l'intention…** ;
  - chaque **édition**, une ligne, dense comme une table sur grand écran : sa vignette de couverture, son libellé (`fonctionnel.md` § Libellé d'une édition) avec, en petit dessous, sa catégorie, sa reliure et son nombre de pages, son appartenance à la collection (« Dans la collection », ou « Intention d'achat » en tirets), un détail (mode et année d'acquisition, nombre de visuels), puis ses actions ;
  - **actions d'une édition** : **Modifier**, qui ouvre sa fiche de saisie, et un menu « ⋯ ». Pour une édition possédée : **Voir dans la consultation** et, séparée par un filet, **Supprimer l'édition…**. Pour une édition visée par une intention d'achat : **Confirmer l'achat…** (`fonctionnel.md` § Réalisation d'une intention), **Viser plutôt l'album…** et, séparée par un filet, **Retirer l'intention…**, qui supprime l'édition avec elle (`fonctionnel.md` § Suppression des entités). Un clic en dehors d'un menu ouvert le referme ;
  - sans édition ni intention, « Aucune édition pour l'instant » ;
  - l'aide rappelle que chaque édition, avec ses visuels, se saisit sur sa propre fiche, et qu'un album visé par une intention ne peut pas avoir l'une de ses éditions visée aussi, et inversement.
- Sous les blocs, les dates de création et de modification de la fiche de l'album.
- **Smartphone** : chaque édition tient sur deux lignes, la vignette à gauche : le libellé et les actions (Modifier réduit à son icône), puis l'appartenance à la collection et le détail.

## Contrôles

Ceux de la liste des albums ([`liste-albums.md`](liste-albums.md) § Contrôles).

## Déclinaison par thème

Celle de la saisie d'un album ([`saisie-album.md`](saisie-album.md) § Déclinaison par thème). Les mentions au-dessus des titres de bloc sont : Identification (L'album), Exemplaires (Les éditions).
