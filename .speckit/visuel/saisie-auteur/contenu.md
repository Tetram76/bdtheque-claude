---
type: Référence visuelle
title: 'Contenu'
description: 'Contenu du panneau d''un auteur : ouverture, erreurs, dates en tête, noms, nationalité, biographie, actions.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-10T15:32:35Z }
---

# Contenu

- **Ouverture** : depuis la liste des auteurs, par **Nouvel auteur**, par le nom d'un auteur ou son **Modifier**, ou, pour un auteur qu'une recherche ne trouve pas, par « Créer l'auteur « … » », qui propose le texte cherché comme son nom, son premier mot en prénom et le reste en nom (un seul mot : le nom), comme l'auteur créé sur place depuis la saisie d'un album. Le panneau s'ouvre par-dessus la liste, qui garde ses critères (recherche, initiale, page), et **Annuler** ou Échap y ramène sans rien enregistrer.
- **Titre** « Modifier l'auteur » ou « Nouvel auteur ». Le panneau ne propose ni lien vers la consultation, ni suppression, ni fusion : ces actions se font depuis la ligne de l'auteur.
- **Erreur d'enregistrement**, présentée à sa place, en trois catégories ([Présentation des erreurs](../../fonctionnel/presentation-des-erreurs.md)) :
  - un auteur sans nom ni pseudonyme ([Auteur / Artiste](../../modele-metier/auteur-artiste.md)) borde le nom et le pseudonyme de la couleur d'alerte, son message sous le nom ;
  - un nom affiché déjà porté par un autre auteur, sans tenir compte de la casse ni des accents ([Auteur / Artiste](../../modele-metier/auteur-artiste.md)), est refusé au champ dont il vient, le pseudonyme s'il est renseigné, le nom sinon, avec un message qui le cite ;
  - toute autre erreur (fiche modifiée entre-temps, texte trop long, problème technique) est présentée en tête du panneau.
- **Dates** : en modification, les dates de création et de modification de la fiche, en tête, sous une éventuelle erreur et au-dessus du premier champ, comme sur une fiche de saisie.
- **Les champs**, à la suite :
  - le **prénom** et le **nom**, côte à côte (l'un sous l'autre sur smartphone), puis le **pseudonyme** ; l'aide rappelle qu'il faut au moins le nom ou le pseudonyme, qu'un duo ou un collectif crédité sous un nom commun est un auteur, sous ce nom, et que le pseudonyme tient lieu de nom, l'auteur étant sans lui classé à son nom, puis son prénom ([Artistes](../../fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes)) ;
  - la **nationalité**, un champ dimensionné à son contenu ;
  - la **biographie**, une zone de texte.
- **Pas de nom affiché, de clé de classement ni d'initiale calculés** : ils découlent des noms par une règle simple, que l'aide rappelle, et la liste montre l'auteur à sa place dès l'enregistrement (décision de l'utilisateur).
- **Pas de bibliographie** : le panneau ne présente pas les séries et albums de l'auteur, qui peuvent être nombreux ; un auteur se crédite sur la fiche de l'album ou de la série.
- **Les actions**, en bas du panneau : **Annuler** et **Enregistrer** (« Créer l'auteur » en création). L'enregistrement ramène à la liste, avec ses critères.
