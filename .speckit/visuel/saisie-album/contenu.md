---
type: Référence visuelle
title: 'Contenu'
description: 'Contenu de la saisie d''un album : erreurs, blocs du formulaire, barre d''enregistrement.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Contenu

- **En-tête et menu** de l'administration ([En-tête et menu](/visuel/administration/en-tete-et-menu.md)), l'entrée « Albums » étant l'entrée courante du menu.
- **Fil d'Ariane** : Administration, Albums, puis l'album (« Nouvel album » en création).
- **Titre** « Modifier l'album » ou « Nouvel album », suivi du **libellé de l'album** ([Libellé d'un album](/fonctionnel/presentation/libelle-d-un-album.md)), recalculé au fil de la saisie (« <Sans titre> » tant que rien n'est saisi). La fiche ne propose ni lien vers la consultation, qui ne présenterait pas les données en cours de modification, ni suppression, ni fusion : on y modifie l'album, et ces actions se font depuis la liste des albums.
- **Erreur d'enregistrement**, en tête de page, au-dessus du formulaire. Les trois catégories ([Présentation des erreurs](/fonctionnel/presentation-des-erreurs.md)) se distinguent au premier coup d'œil, chacune par sa mention, son icône, sa couleur et son cadre :
  - **À corriger** (erreur métier) : cadre plein de la couleur d'alerte, point d'exclamation ; le message dit quoi corriger, et le champ en cause est en outre bordé de la couleur d'alerte, avec son message sous lui ;
  - **Modifiée entre-temps** (erreur fonctionnelle) : cadre en tirets d'une couleur d'information, flèches de rechargement ; le message explique que rien n'a été écrasé, avec **Recharger la fiche** et **Voir mes changements** ;
  - **Problème technique** : cadre pointillé neutre, clé à molette ; le message rassure sur les changements saisis, avec **Réessayer**.
- **Le formulaire**, en blocs, sur la colonne principale :
  - **L'album** :
    - la **série**, choisie en tapant son titre, que l'on retire pour faire de l'album un album isolé ; l'aide rappelle ce que change la série pour le titre ;
    - le **titre**, dont la mention suit la série : « facultatif dans une série », « obligatoire sans série » ;
    - la **clé de tri**, toujours visible, marquée **Auto** ou **Manuelle** ; en mode manuel, **Réinitialiser** la recalcule à partir du titre ([Calcul et stockage de la clé de tri](/fonctionnel/presentation/tri-et-navigation-par-initiale.md#calcul-et-stockage-de-la-clé-de-tri)) ; sans titre propre, l'aide indique que l'album est rangé à la clé de sa série.
  - **Tomaison** et **Parution**, deux petits blocs côte à côte sous « L'album » (empilés sur smartphone) : deux notions sans rapport, la place de l'album dans la numérotation et sa date, que rien ne doit faire confondre, sans que l'une ou l'autre alourdisse « L'album » face aux blocs suivants. « Parution » prend la largeur de son contenu, « Tomaison » le reste.
    - **Tomaison**, dont les champs tiennent sur une ligne, qui passe à la ligne au besoin :
      - le **type** : deux cases à cocher, **Intégrale** et **Hors-série**, indépendantes l'une de l'autre ; sans case cochée, l'album est régulier. Une donnée à deux valeurs se saisit par une case à cocher, jamais par un sélecteur à boutons ;
      - le **tome** ; pour une intégrale, son **numéro** et, à côté, les **tomes couverts**, regroupés sous leur propre intitulé (« 1 à 5 »), la règle qui les lie (« Les deux, ou aucun ») placée juste sous eux.
    - **Parution** : la **première publication**, une liste de mois (« — » pour un mois inconnu) et l'année, côte à côte, jamais le jour.
  - Dans toute la fiche, chaque champ est **dimensionné à ce qu'il contient** (quelques chiffres pour un tome ou une année, un nom de mois), jamais étiré sur toute la largeur ; seuls les textes libres (série, titre, clé de tri, résumé, notes) l'occupent.
  - **Les auteurs** : une ligne par rôle (scénario, dessin, couleurs), chaque auteur en étiquette retirable, « Personne » sans auteur, et **Ajouter**, qui trouve un auteur en tapant son nom ou le crée sur place. Quand ils viennent d'être repris de la série ([Initialisation des contributions depuis la série](/fonctionnel/regles-metier/initialisation-des-contributions-depuis-la-serie.md)), un encadré le dit, et rappelle qu'ils appartiennent désormais à l'album.
  - **Genres et univers** : ceux de l'album, en étiquettes retirables, avec **Ajouter** ; en dessous, **par la série**, ceux qu'il affiche par sa série, en étiquettes en tirets, non modifiables ici ([Genres et univers d'un album](/fonctionnel/regles-metier/genres-et-univers-d-un-album.md)). Un univers est présenté avec ses ancêtres.
  - **Résumé, notes et appréciation** : le résumé et les notes personnelles en zones de texte, puis l'appréciation en cinq étoiles, avec son libellé (« Bien », « Pas encore noté ») et **Retirer**.
- **Pas de colonne des éditions** : les éditions et les intentions d'achat de l'album ne se gèrent pas depuis sa fiche de saisie, qu'on ne quitte ainsi jamais pour une autre fiche avec des changements non enregistrés ; elles ont leur propre page, **Éditions et intentions d'achat** ([`editions-album.md`](/visuel/editions-album/index.md)), ouverte depuis la liste des albums. Le formulaire garde la largeur qu'il a à côté d'une colonne latérale sur la fiche d'une édition ; sous lui, en modification, les dates de création et de modification de la fiche.
- **La barre d'enregistrement**, figée en bas de l'écran : l'état de la fiche (« Modifications non enregistrées », signalé d'un point de couleur d'accent ; « Nouvelle fiche »), **Annuler** et **Enregistrer** (« Créer l'album » en création), mis en relief. Sur smartphone, elle tient sur une ligne, l'état réduit à quelques mots.
- **Smartphone** : les blocs s'empilent sur une colonne ; « Tomaison » et « Parution » aussi ; les champs de la tomaison passent à la ligne en gardant leur taille, les tomes couverts et la date de première publication restant chacun sur une ligne ; les rôles des auteurs passent au-dessus de leurs étiquettes.
