---
type: Référence visuelle
title: 'Contenu'
description: 'Contenu de la page des éditions et intentions d''achat d''un album.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-09T23:06:13Z }
---

# Contenu

- **En-tête et menu** de l'administration, l'entrée « Albums » étant l'entrée courante du menu.
- **Fil d'Ariane** : Administration, Intentions d'achat, qui mène à leur liste ([`listes/`](../listes/index.md)), puis l'album : comme tout fil d'Ariane de l'administration, il ramène à une liste.
- **Titre** « Éditions et intentions d'achat », suivi du libellé de l'album ([Libellé d'un album](../../fonctionnel/presentation/libelle-d-un-album.md)).
- **L'album**, en bref, pour savoir de quel album il s'agit sans quitter la page : sa vignette de couverture (visuel générique sans couverture), son libellé, ses auteurs, sa date de première publication et son appartenance à la collection (« Dans la collection », « Hors collection » ou « Intention d'achat », comme dans la liste des albums) ; à droite, l'un sous l'autre, **Voir dans la consultation** puis **Modifier**, qui ouvre la fiche de saisie de l'album (réduits à leur icône sur smartphone) ; l'aide rappelle que l'album lui-même se modifie sur sa fiche de saisie.
- **Les éditions**, celles que possède la collection, une intention d'achat n'étant jamais présentée comme une édition ([Intention d'achat](../../fonctionnel/regles-metier/intention-d-achat.md)) :
  - le décompte (« 2 éditions », « Aucune édition »), et à droite **Nouvelle édition**, mise en relief, qui ouvre la fiche de saisie d'une nouvelle édition de l'album ; pour un album lui-même visé par une intention d'achat, qui n'a aucune édition, ce bouton cède la place à l'aide, qui rappelle que sa première édition s'ajoute en confirmant l'achat ;
  - chaque **édition**, une ligne, dense comme une table sur grand écran : sa vignette de couverture, son libellé ([Libellé d'une édition](../../fonctionnel/presentation/libelle-d-une-edition.md)) avec, en petit dessous, sa catégorie, sa reliure et son nombre de pages, un détail (mode et année d'acquisition, nombre de visuels), puis ses actions : **Modifier**, qui ouvre sa fiche de saisie, et un menu « ⋯ » : **Voir dans la consultation** et, séparée par un filet, **Supprimer l'édition…** ;
  - sans édition, « Aucune édition dans la collection » ;
  - l'aide rappelle que chaque édition, avec ses visuels, se saisit sur sa propre fiche.
- **Les intentions d'achat** :
  - le décompte (« 1 intention d'achat », « Aucune intention d'achat »), et à droite le bouton qui en ajoute une ([Intention d'achat](../../fonctionnel/regles-metier/intention-d-achat.md)) :
    - pour un album **sans édition ni intention**, **Nouvelle intention d'achat**, qui propose deux choix : **Sur l'album…**, toute édition de l'album faisant l'affaire, qui pose l'intention dès le clic, sans dialogue de confirmation, rien n'étant à saisir pour elle, la page se mettant à jour, et **Sur une nouvelle édition…** ; un clic en dehors des choix ouverts les referme ;
    - pour un album qui a une édition, possédée ou visée, seule une édition précise peut l'être : un bouton direct, **Nouvelle intention sur une édition…**, sans choix à faire ; plusieurs éditions d'un même album peuvent être visées, une intention chacune ;
    - dans les deux cas, viser une édition ouvre la fiche de saisie d'une nouvelle intention d'achat ([`saisie-edition/`](../saisie-edition/index.md)), sans exemplaire ni valeur ;
    - aucun bouton pour un album lui-même visé ;
  - chaque **intention**, une ligne, distinguée comme dans la liste des intentions d'achat ([`listes/`](../listes/index.md)) par une étiquette : **Album**, pleine, suivie de « toute édition », ou **Édition**, en contour, suivie du libellé de l'édition visée en gras, avec sa catégorie et sa reliure en petit dessous ; la date où elle a été posée ; puis ses actions : **Confirmer l'achat** ([Réalisation d'une intention](../../fonctionnel/regles-metier/intention-d-achat.md#réalisation-dune-intention)) et un menu « ⋯ » : **Modifier l'intention**, pour une intention sur une édition ; **Viser plutôt une édition…** pour une intention sur l'album, ou **Viser plutôt l'album…** pour la seule intention d'un album sans édition, la conversion supprimant l'édition visée ; et, séparée par un filet, **Retirer l'intention…**, qui supprime avec elle l'édition visée ([Suppression des entités](../../fonctionnel/regles-metier/suppression-des-entites.md)). Un clic en dehors d'un menu ouvert le referme ;
  - sans intention, « Aucune intention d'achat » ;
  - l'aide rappelle qu'une intention ne vise l'album entier que tant qu'il n'a aucune édition, possédée ou visée.
- Au bas du bloc de l'album, sous son aide, les dates de création et de modification de l'album, introduites par ce qu'elles concernent (« Album créé le …, modifié le … »), jamais par « Fiche ».
- **Smartphone** : chaque édition tient sur deux lignes, la vignette à gauche : le libellé et les actions (Modifier réduit à son icône), puis le détail ; chaque intention de même, Confirmer l'achat réduit à son icône, la date passant sous ce qu'elle vise.
