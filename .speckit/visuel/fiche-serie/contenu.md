---
type: Référence visuelle
title: 'Contenu'
description: 'Contenu de la fiche d''une série : pile de couvertures, avancement, auteurs, albums dans l''ordre de la série.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-09T10:30:00Z }
---

# Contenu

De haut en bas :

- **En-tête** de la partie Consultation, avec l'entrée « Séries » active, puis le **fil d'Ariane** : Séries, puis la série.
- **En-tête de la fiche**, sur deux colonnes, qui s'empilent sur smartphone :
  - une **pile de trois couvertures**, décorative : celles des premiers tomes de la série ; la pile garde toujours trois cartes, pour qu'une série ne se confonde jamais avec un album, une place au-delà des tomes de la série étant une carte générique ;
  - le titre, puis des étiquettes : le **statut**, le **nombre de tomes prévus** (nombre de tomes théorique) et la mention **Complète** ou **À compléter** ;
  - l'**avancement de la collection** : « N albums sur M dans la collection », avec une barre de progression ;
  - l'**éditeur** et la **collection** de la série (éditeur et collection template) ; l'éditeur mène à son site web lorsqu'il est connu, signalé comme un lien externe, la collection n'est pas un lien ([Structure de l'application](../../fonctionnel/structure-de-l-application.md)) ;
  - les **auteurs de la série** (contributions template), présentés comme les contributions d'un album ([Contenu](../fiche-album/contenu.md)) : nombre d'auteurs distincts, une ligne par rôle, repliée au-delà de ce que tient la ligne ;
  - les **genres** et les **univers**, chaque univers avec la chaîne de ses ancêtres, comme sur la fiche d'un album.
  - Une donnée non renseignée (statut, nombre de tomes prévus, éditeur, collection, résumé) reste affichée, avec la mention « Non renseigné » en texte atténué, jamais sous la forme d'un lien vide, comme sur la fiche d'un album. Une série sans album, sans auteur ou sans genre le dit de même (« Aucun album dans la série pour l'instant », sans barre de progression ; « Aucun auteur crédité sur la série » ; « Aucun genre »).
  - Les autres valeurs template et les indicateurs d'exclusion n'y figurent pas ([Structure de l'application](../../fonctionnel/structure-de-l-application.md)).
- **De quoi ça parle ?** : le résumé, puis les notes personnelles de la série (« Aucune note » sinon).
- **Combien d'albums ?** : le décompte (tomes, intégrales, hors-séries), puis les albums **dans l'ordre de la série** ([Ordre des albums dans une série](../../fonctionnel/regles-metier/ordre-des-albums-dans-une-serie.md)), en deux groupes : **Tomes et intégrales**, puis **Hors-séries**. Chaque album est une carte, en grille, qui mène à sa fiche :
  - sa couverture, son tome ([Représentation du tome selon le type d'album](../../fonctionnel/presentation/libelle-d-un-album.md#représentation-du-tome-selon-le-type-dalbum)) et son titre ; un album sans titre propre est nommé par son tome seul (« Tome 3 ») ;
  - son appréciation, en étoiles, lorsqu'elle est renseignée ;
  - son **appartenance à la collection** : étiquette pleine « Dans la collection », ou étiquette en tirets « Hors collection » ou « Intention d'achat ».
- **Pied de page** : les dates de création et de modification de la série, introduites par ce qu'elles concernent (« Série créée le …, modifiée le … »), jamais par « Fiche ».
