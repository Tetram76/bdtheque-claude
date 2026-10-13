# Fiche d'auteur

Ce fichier décrit la présentation de la fiche d'un auteur dans la partie Consultation (`fonctionnel.md` § Structure de l'application). Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation).

La fiche est déclinée selon chacun des thèmes en lice pour la partie Consultation (cf. [`dashboard.md`](dashboard.md) § Alternatives) : comme toute page, elle suit l'identité visuelle du thème actif (cf. [`charte-graphique.md`](charte-graphique.md) § Portée).

---

## Maquettes

Toutes les déclinaisons forment la page « Fiche d'auteur » du canevas des maquettes (cf. [`charte-graphique.md`](charte-graphique.md) § Maquettes). Leurs sources sont dans [`maquettes/fiche-auteur/`](maquettes/fiche-auteur/).

| Thème | Fichiers |
| --- | --- |
| Cases et bulles, violine | `Auteur-Violine.dc.html` |
| Cases et bulles, mûre | `Auteur-Mure.dc.html` |
| La gazette | `Auteur-Gazette.dc.html` |
| Le sommaire de magazine | `Auteur-Sommaire.dc.html` |
| Le festival en métro | `Auteur-Festival.dc.html` |
| Le fanzine en risographie | `Auteur-Riso.dc.html` |
| La vitrine de librairie | `Auteur-Librairie.dc.html` |
| L'album de vignettes | `Auteur-Vignettes.dc.html` |

`Main.dc.html` n'est pas une déclinaison : c'est la source commune, que chaque planche affiche avec son thème. Son nom est imposé par le canevas, dont c'est le fichier d'entrée.

L'exemple présenté est un auteur sans biographie ni nationalité, crédité sur sept séries et deux albums sans série, dont une série de seize albums, plusieurs rôles sur un même album et des albums hors de la collection.

## Contenu

De haut en bas :

- **En-tête** de la partie Consultation, avec l'entrée « Auteurs » active, puis le **fil d'Ariane** : Auteurs, puis l'auteur.
- **En-tête de la fiche** : un **monogramme** (initiales), décoratif ; l'identifiant principal de l'auteur (`fonctionnel.md` § Artistes), suivi de son prénom et de son nom lorsqu'il est connu sous un pseudonyme ; sa nationalité (« Non renseignée » sinon) ; des étiquettes qui donnent le nombre d'albums crédités, puis ce nombre par rôle.
- **Qui est-ce ?** : la biographie, ou « Aucune biographie pour l'instant. ».
- **Sur quels albums ?** : la **bibliographie complète** (`fonctionnel.md` § Structure de l'application), en une seule liste :
  - séries et **albums sans série au même niveau**, rangés ensemble par ordre alphabétique (clé de tri) (piste envisagée par `fonctionnel.md`) ;
  - **tout album a la même représentation**, qu'il appartienne à une série ou non : sa vignette de couverture, son titre (le tome seul pour un album sans titre propre), une mention qui le situe (« Série · T. 3 », ou « Album sans série »), le **rôle ou les rôles tenus sur l'album**, son appréciation lorsqu'elle est renseignée et son appartenance à la collection. Les vignettes des albums sans série sont alignées sur les piles des séries ; les albums d'une série dépliée sont réunis en retrait, sous la série, dans un **encadré** au fond teinté, marqué à gauche d'un filet de la couleur d'accent, pour qu'aucune entrée suivante (album sans série ou autre série) ne passe pour l'un d'eux ;
  - une **série** se distingue par une **petite pile de trois couvertures**, celles de ses premiers albums listés, comme la pile de l'en-tête de la fiche d'une série (`fiche-serie.md`) : elle indique le nombre d'albums sur lesquels l'auteur est crédité, rapporté au nombre d'albums de la série, et combien sont dans la collection ; les rôles tenus sur la série elle-même suivent la mention « Sur la série : ». Elle se déplie sur ses albums crédités, dans l'ordre de la série ;
  - chaque rôle garde sa couleur sur toute la page ;
  - une série se déplie par un **chevron** placé devant elle, sur une pastille ronde de la teinte de l'encadré de ses albums, sans cadre : visible sur smartphone sans dominer la ligne ; une série de plus de six albums crédités est repliée à l'arrivée ; « Tout déplier » et « Tout replier » agissent sur toute la liste.
- **Pied de page** : les dates de création et de modification de la fiche.

## Déclinaison par thème

Couleurs, typographie et touche propre de chaque thème : celles de la fiche d'un album (`fiche-album.md` § Déclinaison par thème), le monogramme reprenant le traitement du visuel de l'album.
