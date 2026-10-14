# Fiche d'auteur

Ce fichier décrit la présentation de la fiche d'un auteur dans la partie Consultation (`fonctionnel.md` § Structure de l'application). Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation).

La fiche est déclinée selon chacun des thèmes en lice pour la partie Consultation (cf. [`dashboard.md`](dashboard.md) § Alternatives) : comme toute page, elle suit l'identité visuelle du thème actif (cf. [`charte-graphique.md`](charte-graphique.md) § Portée).

---

## Maquettes

Toutes les déclinaisons sont présentées côte à côte par la visionneuse [`maquettes/fiche-auteur/`](maquettes/fiche-auteur/index.html) (cf. [`charte-graphique.md`](charte-graphique.md) § Maquettes). Elles ont une source commune, `mockup.html`, que chacune affiche avec son thème, choisi par le paramètre `theme` :

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

L'exemple présenté est un auteur sans biographie ni nationalité, crédité sur huit séries et deux albums sans série, dont une série de seize albums, une série sur laquelle il n'est crédité que sur la série elle-même, plusieurs rôles sur un même album et des albums hors de la collection.

## Contenu

De haut en bas :

- **En-tête** de la partie Consultation, avec l'entrée « Auteurs » active, puis le **fil d'Ariane** : Auteurs, puis l'auteur.
- **En-tête de la fiche** : un **monogramme** (initiales), décoratif ; l'identifiant principal de l'auteur (`fonctionnel.md` § Artistes), suivi de son prénom et de son nom lorsqu'il est connu sous un pseudonyme ; sa nationalité (« Non renseignée » sinon) ; des étiquettes qui donnent le nombre d'albums crédités, puis ce nombre par rôle.
- **Qui est-ce ?** : la biographie, ou « Aucune biographie pour l'instant. ».
- **Sur quels albums ?** : la **bibliographie complète** (`fonctionnel.md` § Structure de l'application), en une seule liste :
  - séries et **albums sans série au premier niveau**, triés ensemble par ordre alphabétique (clé de tri), comme le veut `fonctionnel.md` § Structure de l'application ;
  - **tout album a la même représentation**, qu'il appartienne à une série ou non : sa vignette de couverture, son titre (le tome seul pour un album sans titre propre), une mention qui le situe (« Série · T. 3 », ou « Album sans série »), le **rôle ou les rôles tenus sur l'album**, son appréciation lorsqu'elle est renseignée et son appartenance à la collection. Les vignettes des albums sans série sont alignées sur les piles des séries ; les albums d'une série dépliée sont réunis en retrait, sous la série, dans un **encadré** au fond teinté, marqué à gauche d'un filet de la couleur d'accent, pour qu'aucune entrée suivante (album sans série ou autre série) ne passe pour l'un d'eux ;
  - une **série** se distingue par une **petite pile de trois couvertures**, celles de ses premiers tomes, que l'auteur y soit crédité ou non, comme la pile de l'en-tête de la fiche d'une série (`fiche-serie.md`), et comme elle toujours de trois cartes, une place au-delà des tomes de la série étant une carte générique ; une série sur laquelle l'auteur n'est crédité que sur la série elle-même a donc sa pile, sans chevron ni album à déplier, avec la mention « crédité sur la série seulement » ;
  - une série indique le nombre d'albums sur lesquels l'auteur est crédité, rapporté au nombre d'albums de la série, et combien sont dans la collection ; les rôles tenus sur la série elle-même suivent la mention « Sur la série : ». Elle se déplie sur ses albums crédités, dans l'ordre de la série ;
  - chaque rôle garde sa couleur sur toute la page ;
  - un auteur sans aucun crédit le dit : « Crédité sur aucun album », et « Aucune série ni aucun album crédité pour l'instant. » à la place de la liste ;
  - une série se déplie par un **chevron** placé devant elle, sur une pastille ronde de la teinte de l'encadré de ses albums, sans cadre : visible sur smartphone sans dominer la ligne ; une série de plus de six albums crédités est repliée à l'arrivée ; « Tout déplier » et « Tout replier » agissent sur toute la liste.
- **Pied de page** : les dates de création et de modification de la fiche.

## Déclinaison par thème

Couleurs, typographie et touche propre de chaque thème : celles de la fiche d'un album (`fiche-album.md` § Déclinaison par thème), le monogramme reprenant le traitement du visuel de l'album.
