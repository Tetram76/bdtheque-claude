---
type: Référence visuelle
title: 'Contenu'
description: 'Contenu de la fiche d''un univers : univers parents, description, sous-univers, séries et albums rattachés.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-24T13:40:00Z }
---

# Contenu

Ce qui n'est pas renseigné n'est pas affiché ([Convivialité et efficacité](../charte-graphique/convivialite-et-efficacite.md)). Une fiche n'a pas de filtre : tout ce qui est rattaché à l'univers y figure ([Périmètre de la consultation](../../fonctionnel/regles-metier/perimetre-de-la-consultation.md)). De haut en bas (décision de l'utilisateur, #607, maquette à l'appui) :

- **En-tête** de la partie Consultation, sans entrée active : un univers n'a pas d'entrée dans le menu ([Structure de l'application](../../fonctionnel/structure-de-l-application.md)). Puis le **fil d'Ariane** : les univers parents, de la racine à son parent, chacun menant à sa fiche, puis l'univers ; un univers de premier niveau n'a que lui.
- **En-tête de la fiche** : la mention de sa nature, **« Univers »**, au-dessus de son visuel, comme celle de la fiche d'un album ([Contenu](../fiche-album/contenu.md)) ; son visuel, la **bulle de pensée** qui représente les univers ([Liens qui font sortir de l'application](../charte-graphique/liens-qui-font-sortir-de-l-application.md)), décoratif, à gauche du nom, comme le monogramme d'un auteur ([Contenu](../fiche-auteur/contenu.md)) ; son **nom** ; ses **sous-univers**, sous l'intitulé « Sous-univers », chacun menant à sa fiche ; des étiquettes qui donnent le nombre de séries et le nombre d'albums rattachés, sous-univers compris ([Hiérarchie des univers](../../fonctionnel/regles-metier/hierarchie-des-univers.md)).
- **De quoi s'agit-il ?** : sa description ([Univers](../../modele-metier/univers.md)) ; sans description, pas de bloc.
- **Quelles séries, quels albums ?** : ses séries et ses albums, sous-univers compris, présentés comme la bibliographie d'un auteur ([Contenu](../fiche-auteur/contenu.md), « Sur quels albums ? »), sans les rôles, que rien ne crédite ici :
  - séries et **albums sans série au premier niveau**, triés ensemble par ordre alphabétique (clé de tri) ; tout album a la même représentation, dans une série ou non : sa vignette de couverture, son nom et la mention qui le situe, son appréciation lorsqu'elle est renseignée, sa possession ([États d'un album](../../fonctionnel/regles-metier/appartenance-a-la-collection.md#états-dun-album)) ;
  - une **série** se distingue par sa petite pile de trois couvertures ; **rattachée elle-même** à l'univers, elle regroupe tous ses albums, et dit combien sont dans la collection, rapporté au nombre de ses albums (« 3 albums sur 4 dans la collection ») ; **rattachée par certains de ses albums seulement**, elle ne regroupe que ceux-là, et dit combien elle en a de rattachés sur le nombre de ses albums, puis combien sont dans la collection (« 2 albums rattachés sur 5 · aucun dans la collection », [Convivialité et efficacité](../charte-graphique/convivialite-et-efficacite.md)) ;
  - les colonnes alignées, sans celle des rôles (l'appréciation et la possession), le dépliage d'une série, « Tout déplier » et « Tout replier », la série repliée à l'arrivée au-delà de six albums, et le titre souligné en permanence sont ceux de la bibliographie d'un auteur ;
  - un univers auquel rien n'est rattaché le dit dans ses étiquettes, « Aucun album rattaché », un nombre nul restant une valeur, et n'a pas ce bloc.
- **Pied de page** : les dates de création et de modification de l'univers, introduites par ce qu'elles concernent (« Univers créé le …, modifié le … »), jamais par « Fiche ».
