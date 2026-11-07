---
type: Référence visuelle
title: 'Contenu'
description: 'Contenu de la fiche d''un album : visuels, identité, contributions, éditions, intentions, reste de la série.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Contenu

De haut en bas :

- **En-tête** de la partie Consultation : le logo, le menu, avec l'entrée « Albums » active, le champ de recherche et le bouton d'aide ([Apparence propre à chaque alternative](../charte-graphique/apparence-propre-a-chaque-alternative.md)).
- **Fil d'Ariane** : Albums, la série, puis l'album.
- **En-tête de la fiche**, sur deux colonnes, qui s'empilent sur smartphone :
  - **Visuels de l'édition** sélectionnée, dans l'ordre des visuels ([Ordre des visuels d'une édition](../../fonctionnel/regles-metier/ordre-des-visuels-d-une-edition.md)) : le visuel courant, son type (numéroté pour les planches) et sa position (« 6 sur 13 »), des boutons précédent et suivant, puis une **bande de vignettes** qui défile horizontalement, une par visuel, marquée de son type abrégé (« Couv. », « Garde », numéro de planche, « 4e »), la vignette courante encadrée de l'accent ; enfin le lien vers l'image originale.
  - **Identité de l'album** : le lien vers la série, le titre, puis des étiquettes : le tome ([Représentation du tome selon le type d'album](../../fonctionnel/presentation/libelle-d-un-album.md#représentation-du-tome-selon-le-type-dalbum)), la date de première publication et l'appartenance à la collection, avec le nombre d'éditions possédées. Puis l'appréciation, en cinq étoiles, ou « Pas encore noté ».
  - **Contributions** : un encadré annonce le nombre d'auteurs distincts crédités, puis une ligne par rôle (scénario, dessin, couleurs), avec le nombre d'auteurs du rôle et leurs noms en étiquettes, chacun menant à la fiche de l'auteur. Au-delà de six noms, le rôle est replié derrière un lien « et N autres », qui le déplie (« Réduire » le replie) : un album collectif ne repousse pas le reste de la fiche. Un rôle sans contribution indique « Non crédité ».
  - **Genres et univers** affichés ([Genres et univers d'un album](../../fonctionnel/regles-metier/genres-et-univers-d-un-album.md)). Chaque univers est présenté avec la **chaîne de ses ancêtres**, de la racine à l'univers rattaché (« Univers Hugo Pratt › Corto Maltese › Les Antilles ») : les ancêtres sont de simples liens, l'univers rattaché une étiquette colorée ; chacun mène à la liste de ses séries et albums, sous-univers compris, présentée comme la bibliographie d'un auteur ([Structure de l'application](../../fonctionnel/structure-de-l-application.md), [Hiérarchie des univers](../../fonctionnel/regles-metier/hierarchie-des-univers.md)).
- **Ça raconte quoi ?** : le résumé, puis les notes personnelles de l'album.
- **Dans quelles éditions ?** :
  - les éditions de l'album, en grille (quatre par ligne sur grand écran, une par ligne sur smartphone), chacune par son libellé ([Libellé d'une édition](../../fonctionnel/presentation/libelle-d-une-edition.md)) ; seules les éditions de la collection y figurent, une intention d'achat n'étant jamais présentée comme une édition ([Intention d'achat](../../fonctionnel/regles-metier/intention-d-achat.md)). Choisir une édition affiche son détail et ses visuels ;
  - le détail de l'édition choisie, en deux groupes : **l'édition** (éditeur, menant à son site web lorsqu'il est connu, collection, année d'édition, ISBN, catégorie, format, reliure, orientation, sens de lecture, pages, couleur) et **l'exemplaire** (mode d'acquisition, date et montant d'acquisition avec leurs libellés contextuels, valeur initiale, neuve ou d'occasion, état, dédicace, numérotation personnelle, notes personnelles). Chaque montant est suivi de sa valeur en euros du jour ; un montant d'acquisition absent est remplacé par la valeur estimée, lorsqu'elle existe ([Estimation de la valeur des éditions](../../fonctionnel/valorisation/estimation-de-la-valeur-des-editions.md)) ;
  - une donnée non renseignée reste affichée, avec la mention « Non renseigné » ;
  - en pied, les dates de création, d'entrée dans la collection et de modification de l'édition.
  - sous le détail, les **intentions d'achat** de l'album, s'il en a, en cadre en tirets, sous la mention « Intention d'achat » ou « Intentions d'achat » selon leur nombre : chacune par ce qu'elle vise, sans étiquette, le bloc étant celui des éditions — le libellé de l'édition visée, avec en petit dessous sa catégorie, son format, sa reliure et son nombre de pages, ou, pour une intention sur l'album lui-même, « Toute édition de l'album » — puis la date où elle a été posée. Une intention ne se choisit pas comme une édition et n'a pas de visuels à présenter ;
  - un album visé par une intention sur lui-même n'a aucune édition ([Intention d'achat](../../fonctionnel/regles-metier/intention-d-achat.md)) : la grille et le détail cèdent la place à « Aucune édition dans la collection », l'en-tête présente le visuel générique sans navigation entre visuels, et l'appartenance à la collection est « Intention d'achat ».
- **Et le reste de la série ?** : la liste dépliable des albums de la série, dans l'ordre de la série ([Ordre des albums dans une série](../../fonctionnel/regles-metier/ordre-des-albums-dans-une-serie.md)), chacun avec son tome et son appartenance à la collection, l'album affiché étant mis en avant ; et le lien vers la fiche de la série.
- **Pied de page** : les dates de création et de modification de l'album, introduites par ce qu'elles concernent (« Album créé le …, modifié le … »), jamais par « Fiche ».
