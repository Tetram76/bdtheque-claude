---
type: Référence visuelle
title: 'Contenu'
description: 'Contenu de la saisie d''une série : avancement, calculs, auteurs, éditeur, valeurs des nouvelles éditions.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Contenu

- **En-tête et menu** de l'administration, l'entrée « Séries » étant l'entrée courante du menu.
- **Fil d'Ariane** : Administration, Séries, puis la série (« Nouvelle série » en création).
- **Titre** « Modifier la série » ou « Nouvelle série », suivi du titre de la série, recalculé au fil de la saisie (« <Sans titre> » tant que rien n'est saisi). La fiche ne propose ni lien vers la consultation, qui ne présenterait pas les données en cours de modification, ni suppression, ni fusion : on y modifie la série, et ces actions se font depuis la liste des séries.
- **Erreur d'enregistrement** : celle de la saisie d'un album, en trois catégories ; l'erreur métier de l'exemple est un titre vidé, le champ étant bordé de la couleur d'alerte.
- **Le formulaire**, en blocs, sur la colonne principale :
  - **La série** : le **titre**, obligatoire ; la **clé de tri**, marquée **Auto** ou **Manuelle**, avec **Réinitialiser** en mode manuel ([Calcul et stockage de la clé de tri](/fonctionnel/presentation/tri-et-navigation-par-initiale.md#calcul-et-stockage-de-la-clé-de-tri)) ; le **statut**, une liste (« — Non renseigné », En cours, Terminée, Abandonnée), l'aide rappelant ce que veut dire « Terminée ».
  - **Avancement** et **Calculs**, deux blocs de même largeur côte à côte (empilés sur smartphone), deux sujets sans rapport :
    - **Avancement** : le nombre de **tomes numérotés prévus**, un champ de quelques chiffres, l'aide disant qu'il ne compte pas les hors-série et sert à identifier les albums manquants ; puis la case **Complète dans la collection**, l'aide rappelant que c'est un jugement de l'utilisateur, indépendant du nombre d'albums possédés ([Série](/modele-metier/serie.md)) ;
    - **Calculs** : les cases **Exclure des albums manquants** et **Exclure des estimations de sortie**, l'aide rappelant qu'elles ne figurent pas sur la fiche de la consultation ([Structure de l'application](/fonctionnel/structure-de-l-application.md)) et que seule une série en cours reçoit une estimation de sortie.
  - **Les auteurs** : une ligne par rôle, comme pour un album ; l'aide rappelle qu'ils sont recopiés sur un album qu'on rattache à la série s'il n'en a encore aucun ([Initialisation des contributions depuis la série](/fonctionnel/regles-metier/initialisation-des-contributions-depuis-la-serie.md)).
  - **Éditeur et collection** : l'**éditeur** et la **collection éditeur**, côte à côte (l'un sous l'autre sur smartphone), choisis en tapant leur nom ; la collection, l'une de celles de l'éditeur, reste grisée, en tirets, tant qu'aucun éditeur n'est choisi (« Choisissez d'abord l'éditeur »), et changer d'éditeur la retire. L'aide rappelle qu'ils décrivent la série et pré-remplissent les nouvelles éditions de ses albums.
  - **Genres et univers** : ceux de la série, en étiquettes retirables, avec **Ajouter** ; l'aide rappelle que chaque album de la série les affiche en plus des siens ([Genres et univers d'un album](/fonctionnel/regles-metier/genres-et-univers-d-un-album.md)).
  - **Résumé et notes** : deux zones de texte.
  - **Nouvelles éditions** : les valeurs proposées aux nouvelles éditions des albums de la série ([Initialisation d'une nouvelle édition depuis la série](/fonctionnel/regles-metier/initialisation-d-une-nouvelle-edition-depuis-la-serie.md)) : catégorie, état, reliure, format, orientation, sens de lecture et couleur, chacune une liste dimensionnée à son plus long choix, à la suite sur autant de lignes qu'il faut. « — » ne propose rien. La couleur, qui peut aussi ne rien proposer, est une liste à trois choix (« — », En couleur, Noir et blanc), non une case à cocher. L'aide rappelle que ces valeurs ne décrivent pas la série et n'apparaissent pas dans la consultation. En création, elles sont pré-remplies avec les valeurs proposées pour toute nouvelle édition ([Série](/modele-metier/serie.md)), ce qu'un encadré dit.
- **Pas de colonne latérale** : la fiche ne présente pas les albums de la série, ni pour les modifier ni pour mémoire. Un album se rattache à une série et se modifie sur sa propre fiche, et une série peut en compter beaucoup. Le formulaire garde la largeur qu'il a sur la fiche d'un album ; sous lui, en modification, les dates de création et de modification de la fiche.
- **La barre d'enregistrement** : celle de la saisie d'un album (« Créer la série » en création).
- **Smartphone** : les blocs s'empilent sur une colonne ; « Avancement » et « Calculs » aussi, comme l'éditeur et la collection ; les listes des nouvelles éditions passent à la ligne en gardant leur taille.
