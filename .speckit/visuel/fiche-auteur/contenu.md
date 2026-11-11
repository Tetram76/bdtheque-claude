---
type: Référence visuelle
title: 'Contenu'
description: 'Contenu de la fiche d''un auteur : identité, biographie, bibliographie complète.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-11T12:00:00Z }
---

# Contenu

Une donnée non renseignée de l'auteur n'est pas affichée, sans mention « Non renseigné » à sa place. De haut en bas :

- **En-tête** de la partie Consultation, avec l'entrée « Auteurs » active, puis le **fil d'Ariane** : Auteurs, puis l'auteur.
- **En-tête de la fiche** : un petit **monogramme** (initiales), décoratif, à gauche du nom, y compris sur smartphone ; l'identifiant principal de l'auteur ([Artistes](../../fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes)), suivi de son prénom et de son nom lorsqu'il est connu sous un pseudonyme ; sa nationalité, lorsqu'elle est renseignée ; des étiquettes qui donnent le nombre d'albums crédités, puis ce nombre par rôle.
- **Qui est-ce ?** : la biographie ; sans biographie, pas de bloc.
- **Sur quels albums ?** : la **bibliographie complète** ([Structure de l'application](../../fonctionnel/structure-de-l-application.md)), en une seule liste :
  - séries et **albums sans série au premier niveau**, triés ensemble par ordre alphabétique (clé de tri), comme le veut [Structure de l'application](../../fonctionnel/structure-de-l-application.md) ;
  - **tout album a la même représentation**, qu'il appartienne à une série ou non : sa vignette de couverture, son titre (le tome seul pour un album sans titre propre), une mention qui le situe (« Série · T. 3 », ou « Album sans série »), le **rôle ou les rôles tenus sur l'album**, son appréciation lorsqu'elle est renseignée et son appartenance à la collection. Les vignettes des albums sans série sont alignées sur les piles des séries ; les albums d'une série dépliée sont réunis en retrait, sous la série, dans un **encadré** au fond teinté, marqué à gauche d'un filet de la couleur d'accent, pour qu'aucune entrée suivante (album sans série ou autre série) ne passe pour l'un d'eux ;
  - une **série** se distingue par une **petite pile de trois couvertures**, celles de ses premiers tomes, que l'auteur y soit crédité ou non, comme la pile de l'en-tête de la fiche d'une série (`fiche-serie.md`), et comme elle toujours de trois cartes, une place au-delà des tomes de la série étant une carte générique ; une série sur laquelle l'auteur n'est crédité que sur la série elle-même a donc sa pile, sans chevron ni album à déplier, avec la mention « crédité sur la série seulement » ;
  - une série indique le nombre d'albums sur lesquels l'auteur est crédité, rapporté au nombre d'albums de la série, et combien sont dans la collection ; les rôles tenus sur la série elle-même suivent la mention « Sur la série : ». Elle se déplie sur ses albums crédités, dans l'ordre de la série ;
  - chaque rôle garde sa couleur sur toute la page ;
  - un auteur sans aucun crédit le dit : « Crédité sur aucun album », et « Aucune série ni aucun album crédité pour l'instant. » à la place de la liste ;
  - une série se déplie par un **chevron** placé devant elle, sur une pastille ronde de la teinte de l'encadré de ses albums, sans cadre : visible sur smartphone sans dominer la ligne ; une série de plus de six albums crédités est repliée à l'arrivée ; « Tout déplier » et « Tout replier » agissent sur toute la liste.
- **Pied de page** : les dates de création et de modification de l'auteur, introduites par ce qu'elles concernent (« Auteur créé le …, modifié le … »), jamais par « Fiche ».
