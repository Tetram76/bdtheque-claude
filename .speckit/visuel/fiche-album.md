# Fiche d'album

Ce fichier décrit la présentation de la fiche d'un album dans la partie Consultation (`fonctionnel.md` § Structure de l'application). Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation).

La fiche est déclinée selon chacun des thèmes en lice pour la partie Consultation (cf. [`dashboard.md`](dashboard.md) § Alternatives) : comme toute page, elle suit l'identité visuelle du thème actif (cf. [`charte-graphique.md`](charte-graphique.md) § Portée).

---

## Maquettes

Toutes les déclinaisons sont présentées côte à côte par la visionneuse [`maquettes/fiche-album/`](maquettes/fiche-album/index.html) (cf. [`charte-graphique.md`](charte-graphique.md) § Maquettes). Elles ont une source commune, `mockup.html`, que chacune affiche avec son thème, choisi par le paramètre `theme` :

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

L'exemple présenté est un album d'une série, sans appréciation, qui pousse la fiche dans ses cas chargés : trois éditions (une par album en moyenne, deux au plus dans la base existante) et une intention d'achat sur une quatrième ; une vingtaine d'auteurs (réglage « contributions » de la source commune : `nombreuses` ou `courantes`) ; un rattachement à un sous-univers ; une édition à treize visuels (couverture, page de garde, dix planches, 4e de couverture). Les visuels sont des illustrations de substitution.

## Contenu

De haut en bas :

- **En-tête** de la partie Consultation : le logo, le menu, avec l'entrée « Albums » active, le champ de recherche et le bouton d'aide (`charte-graphique.md` § Apparence propre à chaque alternative).
- **Fil d'Ariane** : Albums, la série, puis l'album.
- **En-tête de la fiche**, sur deux colonnes, qui s'empilent sur smartphone :
  - **Visuels de l'édition** sélectionnée, dans l'ordre des visuels (`fonctionnel.md` § Ordre des visuels d'une édition) : le visuel courant, son type (numéroté pour les planches) et sa position (« 6 sur 13 »), des boutons précédent et suivant, puis une **bande de vignettes** qui défile horizontalement, une par visuel, marquée de son type abrégé (« Couv. », « Garde », numéro de planche, « 4e »), la vignette courante encadrée de l'accent ; enfin le lien vers l'image originale.
  - **Identité de l'album** : le lien vers la série, le titre, puis des étiquettes : le tome (`fonctionnel.md` § Représentation du tome selon le type d'album), la date de première publication et l'appartenance à la collection, avec le nombre d'éditions possédées. Puis l'appréciation, en cinq étoiles, ou « Pas encore noté ».
  - **Contributions** : un encadré annonce le nombre d'auteurs distincts crédités, puis une ligne par rôle (scénario, dessin, couleurs), avec le nombre d'auteurs du rôle et leurs noms en étiquettes, chacun menant à la fiche de l'auteur. Au-delà de six noms, le rôle est replié derrière un lien « et N autres », qui le déplie (« Réduire » le replie) : un album collectif ne repousse pas le reste de la fiche. Un rôle sans contribution indique « Non crédité ».
  - **Genres et univers** affichés (`fonctionnel.md` § Genres et univers d'un album). Chaque univers est présenté avec la **chaîne de ses ancêtres**, de la racine à l'univers rattaché (« Univers Hugo Pratt › Corto Maltese › Les Antilles ») : les ancêtres sont de simples liens, l'univers rattaché une étiquette colorée ; chacun mène à la liste de ses séries et albums, sous-univers compris, présentée comme la bibliographie d'un auteur (`fonctionnel.md` § Structure de l'application, § Hiérarchie des univers).
- **Ça raconte quoi ?** : le résumé, puis les notes personnelles de l'album.
- **Dans quelles éditions ?** :
  - les éditions de l'album, en grille (quatre par ligne sur grand écran, une par ligne sur smartphone), chacune par son libellé (`fonctionnel.md` § Libellé d'une édition) ; seules les éditions de la collection y figurent, une intention d'achat n'étant jamais présentée comme une édition (`fonctionnel.md` § Intention d'achat). Choisir une édition affiche son détail et ses visuels ;
  - le détail de l'édition choisie, en deux groupes : **l'édition** (éditeur, menant à son site web lorsqu'il est connu, collection, année d'édition, ISBN, catégorie, format, reliure, orientation, sens de lecture, pages, couleur) et **l'exemplaire** (mode d'acquisition, date et montant d'acquisition avec leurs libellés contextuels, valeur initiale, neuve ou d'occasion, état, dédicace, numérotation personnelle, notes personnelles). Chaque montant est suivi de sa valeur en euros du jour ; un montant d'acquisition absent est remplacé par la valeur estimée, lorsqu'elle existe (`fonctionnel.md` § Estimation de la valeur des éditions) ;
  - une donnée non renseignée reste affichée, avec la mention « Non renseigné » ;
  - en pied, les dates de création, d'entrée dans la collection et de modification de l'édition.
  - sous le détail, l'**intention d'achat** de l'album, s'il en a une, en cadre en tirets, distinguée comme dans la liste des intentions d'achat par une étiquette : **Album**, suivie de « toute édition », ou **Édition**, suivie du libellé de l'édition visée et, en petit dessous, de sa catégorie, son format, sa reliure et son nombre de pages ; puis la date où elle a été posée. Une intention ne se choisit pas comme une édition et n'a pas de visuels à présenter.
- **Et le reste de la série ?** : la liste dépliable des albums de la série, dans l'ordre de la série (`fonctionnel.md` § Ordre des albums dans une série), chacun avec son tome et son appartenance à la collection, l'album affiché étant mis en avant ; et le lien vers la fiche de la série.
- **Pied de page** : les dates de création et de modification de l'album, introduites par ce qu'elles concernent (« Album créé le …, modifié le … »), jamais par « Fiche ».

## Déclinaison par thème

Couleurs et typographie de chaque thème : cf. [`dashboard.md`](dashboard.md) § Alternatives. Leur touche propre dans la fiche :

| Thème | Touche propre |
| --- | --- |
| Cases et bulles (violine, mûre) | Titres de bloc en bulle crème avec sa queue, légèrement inclinée ; visuel dans un cadre coloré incliné ; coins arrondis irréguliers des blocs. |
| La gazette | Double filet sous l'en-tête ; entrée active du menu soulignée de l'accent ; blocs ouverts par un filet d'encre, petite mention en capitales au-dessus de chaque titre ; visuel incliné, sur fond blanc. |
| Le sommaire de magazine | Gros filets noirs, titres en *Anton* soulignés d'un filet épais ; visuel dans un cadre rouge incliné, à l'ombre noire décalée. |
| Le festival en métro | Station devant chaque titre de bloc, précédé de « Station … » ; visuel dans un cadre bleu arrondi, sans inclinaison. |
| Le fanzine en risographie | Cadres en pointillés ; ombre rose décalée des titres et du visuel, tenu par un morceau de scotch jaune. |
| La vitrine de librairie | En-tête vert bouteille filet de laiton, entrée active soulignée de laiton ; titres de bloc en serif d'enseigne italique ; visuel posé sur une tablette en noyer. |
| L'album de vignettes | En-tête bleu canard ; pastille « Page n · … » à cheval sur le bord de chaque bloc ; visuel en vignette à liseré blanc, légèrement inclinée. |
