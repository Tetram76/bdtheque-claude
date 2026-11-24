---
type: Référence visuelle
title: 'Contenu'
description: 'Contenu de la page des éditions et intentions d''acquisition d''un album.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-24T09:45:00Z }
---

# Contenu

- **En-tête et menu** de l'administration, l'entrée « Albums » étant l'entrée courante du menu.
- **Fil d'Ariane** : Administration, Intentions d'acquisition, qui mène à leur liste ([`listes/`](../listes/index.md)), puis l'album : comme tout fil d'Ariane de l'administration, il ramène à une liste.
- **Titre** « Éditions et intentions d'acquisition », suivi du libellé de l'album ([Libellé d'un album](../../fonctionnel/presentation/libelle-d-un-album.md)).
- **L'album**, en bref, pour savoir de quel album il s'agit sans quitter la page : sa vignette de couverture (visuel générique sans couverture), son libellé, ses auteurs, sa date de première publication et sa possession (« Possédé », « Catalogué » ou « Intention », comme dans la liste des albums) ; à droite, l'un sous l'autre, **Voir dans la consultation** puis **Modifier**, qui ouvre la fiche de saisie de l'album (réduits à leur icône sur smartphone) ; l'aide rappelle que l'album lui-même se modifie sur sa fiche de saisie.
- **Les éditions**, celles que possède la collection, une intention d'acquisition n'étant jamais présentée comme une édition ([Intention d'acquisition](../../fonctionnel/regles-metier/intention-d-acquisition.md)) :
  - le décompte (« 2 éditions », « Aucune édition »), et à droite **Nouvelle édition**, mise en relief, qui ouvre la fiche de saisie d'une nouvelle édition de l'album ; pour un album lui-même visé par une intention d'acquisition, qui n'a aucune édition, ce bouton cède la place à l'aide, qui rappelle que sa première édition s'ajoute en confirmant l'acquisition ;
  - chaque **édition**, une ligne, dense comme une table sur grand écran : sa vignette de couverture, son libellé ([Libellé d'une édition](../../fonctionnel/presentation/libelle-d-une-edition.md)) avec, en petit dessous, sa catégorie, sa reliure et son nombre de pages, un détail (mode et année d'acquisition, nombre de visuels), puis ses actions : **Modifier**, qui ouvre sa fiche de saisie, et un menu « ⋯ » : **Voir dans la consultation** et, séparée par un filet, **Supprimer l'édition…** ;
  - sans édition, « Aucune édition dans la collection » ;
  - l'aide rappelle que chaque édition, avec ses visuels, se saisit sur sa propre fiche.
- **Les intentions d'acquisition** :
  - le décompte (« 1 intention d'acquisition », « Aucune intention d'acquisition »), et à droite le bouton qui en ajoute une ([Intention d'acquisition](../../fonctionnel/regles-metier/intention-d-acquisition.md)) :
    - pour un album **sans édition ni intention**, **Nouvelle intention d'acquisition**, qui propose deux choix : **Sur l'album…**, toute édition de l'album faisant l'affaire, qui pose l'intention dès le clic, sans dialogue de confirmation, rien n'étant à saisir pour elle, la page se mettant à jour, et **Sur une nouvelle édition…** ; un clic en dehors des choix ouverts les referme ;
    - pour un album qui a une édition, possédée ou visée, seule une édition précise peut l'être : un bouton direct, **Nouvelle intention sur une édition…**, sans choix à faire ; plusieurs éditions d'un même album peuvent être visées, une intention chacune ;
    - dans les deux cas, viser une édition ouvre la fiche de saisie d'une nouvelle intention d'acquisition ([`saisie-edition/`](../saisie-edition/index.md)), sans exemplaire ni valeur ;
    - aucun bouton pour un album lui-même visé ;
  - chaque **intention**, une ligne, distinguée comme dans la liste des intentions d'acquisition ([`listes/`](../listes/index.md)) par une étiquette : **Album**, pleine, suivie de « toute édition », ou **Édition**, en contour, suivie du libellé de l'édition visée en gras, avec sa catégorie et sa reliure en petit dessous ; la date où elle a été posée ; puis ses actions, une action principale présentée comme le **Modifier** d'une ligne d'édition, et un menu « ⋯ » (décisions de l'utilisateur, #444) :
    - une intention sur une **édition** a pour action principale **Modifier**, qui ouvre sa fiche de saisie ([`saisie-edition/`](../saisie-edition/index.md)) ; son menu propose **Viser plutôt l'album…** pour la seule intention d'un album sans édition, la conversion supprimant l'édition visée, puis, séparée par un filet, **Retirer l'intention…** ;
    - une intention sur l'**album**, qui n'a rien à modifier, n'a pas de **Modifier** : son action principale est **Viser une édition…**, qui la convertit en intention sur une édition, et son menu ne propose que **Retirer l'intention…** ;
    - **Retirer l'intention…** supprime avec elle l'édition visée ([Suppression des entités](../../fonctionnel/regles-metier/suppression-des-entites.md)) ;
    - la confirmation de l'acquisition ne se fait pas depuis cette page, mais depuis la liste des intentions d'acquisition ([`listes/`](../listes/index.md)) et le lien « Confirmer les acquisitions » de l'accueil de l'administration ([Réalisation d'une intention](../../fonctionnel/regles-metier/intention-d-acquisition.md#réalisation-dune-intention)). L'action principale d'une intention suit ainsi ce pour quoi on vient sur la page : sur cette page, qui présente les éditions d'un album, on modifie, d'où **Modifier**, comme pour une édition ; dans la liste des intentions d'acquisition, où mène le lien « Confirmer les acquisitions », on confirme, d'où **Confirmer** (décision de l'utilisateur, [Saisie selon l'entité](../listes/saisie-selon-l-entite.md)) ;
    - un clic en dehors d'un menu ouvert le referme ;
  - sans intention, « Aucune intention d'acquisition » ;
  - l'aide rappelle qu'une intention ne vise l'album entier que tant qu'il n'a aucune édition, possédée ou visée.
- Au bas du bloc de l'album, sous son aide, les dates de création et de modification de l'album, introduites par ce qu'elles concernent (« Album créé le …, modifié le … »), jamais par « Fiche ».
- **Smartphone** : chaque édition tient sur deux lignes, la vignette à gauche : le libellé et les actions (Modifier réduit à son icône), puis le détail ; chaque intention de même, Modifier ou Viser une édition… réduit à son icône, la date passant sous ce qu'elle vise.
