# Liste des albums

Ce fichier décrit la présentation de la liste des albums dans la partie Administration (`fonctionnel.md` § Structure de l'application) : l'écran où les albums se retrouvent, puis se créent, se modifient, se fusionnent et se suppriment. Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation).

La liste est déclinée selon chacun des thèmes en lice pour la partie Consultation (cf. [`dashboard.md`](dashboard.md) § Alternatives) : comme toute page de l'administration, elle suit l'identité visuelle du thème actif, dans le ton sobre de l'administration (cf. [`administration.md`](administration.md) § Ton).

---

## Maquettes

Toutes les déclinaisons sont présentées côte à côte par la visionneuse [`maquettes/liste-albums/`](maquettes/liste-albums/index.html) (cf. [`charte-graphique.md`](charte-graphique.md) § Maquettes). Elles ont une source commune, `mockup.html`, que chacune affiche avec son thème, choisi par le paramètre `theme` (mêmes valeurs que pour la page d'accueil de l'administration, [`administration.md`](administration.md) § Maquettes).

Réglages de la source commune :

| Réglage | Valeurs |
| --- | --- |
| `exemple` | `liste` (par défaut) : la liste ouverte sur la lettre B ; `filtree` : une liste restreinte par deux filtres ; `vide` : une recherche sans résultat |
| `filtres` | panneau des filtres ouvert |
| `actions` | menu des autres actions d'une ligne ouvert |
| `dialogue` | `aucun` (par défaut), `confirmation` (suppression confirmée), `refus` (suppression refusée), `fusion` (choix de l'autre album) |
| `menuOpen` | menu de l'administration ouvert |

Les données sont fictives ; les couvertures sont des illustrations de substitution.

## Contenu

De haut en bas :

- **En-tête et menu** de l'administration ([`administration.md`](administration.md) § En-tête et menu), l'entrée « Albums » étant l'entrée courante du menu.
- **Fil d'Ariane** : Administration, puis Albums.
- **Titre** « Albums », suivi du nombre total d'albums, et le bouton **Nouvel album**, mis en relief (réduit à son icône sur smartphone).
- **La liste**, dans un bloc unique :
  - un **champ de recherche** sur le titre de l'album et sur celui de sa série, et le bouton **Filtres**, qui porte le nombre de filtres actifs ;
  - le **panneau des filtres**, replié par défaut : série, auteur, éditeur, collection éditeur, genre et univers, choisis en tapant leur nom, et l'appartenance à la collection, choisie dans une liste ; un univers retient aussi les albums de ses sous-univers, et genres et univers sont ceux qu'affiche l'album (`fonctionnel.md` § Genres et univers d'un album, § Hiérarchie des univers) ;
  - les **filtres actifs**, en étiquettes retirables une à une, et « Tout effacer » ;
  - la **navigation par initiale** (« Toutes », A à Z, # et @, `fonctionnel.md` § Entrées de la navigation par initiale), qui défile horizontalement sans passer à la ligne, comme le menu de la consultation, l'entrée courante en couleur d'accent ;
  - le **décompte** (« 143 albums sous la lettre B », « 38 albums sur 2 847 ») et l'ordre de la liste, rappelé en clair : par titre, la série tenant lieu de titre aux albums qui n'en ont pas (`fonctionnel.md` § Tri et navigation par initiale) ;
  - les **albums**, une ligne chacun, dense comme une table sur grand écran : la **vignette de couverture** (visuel générique sans couverture, `fonctionnel.md` § Couverture d'un album, § Visuel par défaut), le **libellé** de l'album (`fonctionnel.md` § Libellé d'un album) avec ses auteurs en petit dessous, la **date de première publication** (« Non renseignée » sinon), le nombre d'**éditions** et d'éditions possédées, en lien vers la page des éditions et intentions d'achat de l'album, l'**appartenance à la collection** (étiquette pleine « Dans la collection », en tirets « Hors collection » ou « Intention d'achat »), la date de **dernière modification**, puis les **actions** ;
  - la **pagination** : la position dans la liste (« Albums 1 à 50 sur 143 »), les pages, précédente et suivante.
- **Actions d'une ligne** : le bouton **Modifier**, qui ouvre la fiche de saisie de l'album sur une page dédiée, et un bouton « ⋯ » qui ouvre le menu des autres actions : **Éditions et intentions d'achat**, qui ouvre la page où se gèrent les éditions de l'album et ses intentions d'achat ([`editions-album.md`](editions-album.md)), **Voir dans la consultation**, **Fusionner avec un autre album…** et, séparée par un filet, **Supprimer…**, de la couleur de suppression du thème. Un clic en dehors d'un menu ouvert le referme. Les listes n'offrent pas d'action groupée (`fonctionnel.md` § Structure de l'application).
- **Suppression** : un dialogue qui reprend le libellé de l'album et annonce l'impact de la suppression par type de fiche (`fonctionnel.md` § Suppression des entités) : fiches supprimées avec l'album, associations retirées. Le bouton de confirmation, « Supprimer définitivement », a la couleur d'alerte du thème. Une suppression refusée le dit dans le même dialogue, avec ce qui la bloque (ex. une recherche enregistrée qui désigne l'album) et la marche à suivre, sans autre bouton que « Fermer ».
- **Fusion** : un dialogue qui part de l'album choisi et demande l'autre album, retrouvé par une recherche, chaque candidat avec son libellé, ses auteurs, sa date de première publication et ses éditions. « Continuer » mène à l'étape suivante, dont la présentation attend les modalités de la fusion (`fonctionnel.md` § Structure de l'application).
- **Liste vide** : une recherche sans résultat le dit, rappelle sur quoi porte la recherche, et propose d'effacer la recherche ou de créer l'album cherché.
- **Smartphone** : chaque ligne tient sur deux lignes, la vignette à gauche : le libellé et les actions (Modifier réduit à son icône), puis la date, les éditions et l'appartenance à la collection ; la date de modification n'y figure pas. Le panneau des filtres passe sur deux colonnes.

## Contrôles

Dans tous les thèmes, le texte indicatif des champs garde la couleur du texte atténué du thème, sans opacité, et l'indicateur de focus clavier est un contour de la couleur du texte, détaché du contrôle par une bande de la couleur de la surface qui l'entoure : il contraste avec le contrôle, coloré ou non, comme avec cette surface (`suivi-implementation.md`, Phase 3, note sur le style des contrôles).

## Déclinaison par thème

Couleurs, typographie et touche propre de chaque thème : celles de la page d'accueil de l'administration ([`administration.md`](administration.md) § Déclinaison par thème), la liste étant présentée dans un seul bloc, sans titre visible.
