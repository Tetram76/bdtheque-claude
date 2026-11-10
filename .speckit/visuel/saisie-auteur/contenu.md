---
type: Référence visuelle
title: 'Contenu'
description: 'Contenu du panneau d''un auteur : ouverture, erreurs, identité, nom affiché et clé calculés, nationalité, biographie, actions.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-10T15:00:31Z }
---

# Contenu

- **Ouverture** : depuis la liste des auteurs, par **Nouvel auteur**, par le nom d'un auteur ou son **Modifier**, ou, pour un auteur qu'une recherche ne trouve pas, par « Créer l'auteur « … » », qui propose le texte cherché comme son nom, son premier mot en prénom et le reste en nom (un seul mot : le nom), comme l'auteur créé sur place depuis la saisie d'un album. Le panneau s'ouvre par-dessus la liste, qui garde ses critères (recherche, initiale, page), et **Annuler** ou Échap y ramène sans rien enregistrer.
- **Titre** « Modifier l'auteur » ou « Nouvel auteur ». Le panneau ne propose ni lien vers la consultation, ni suppression, ni fusion : ces actions se font depuis la ligne de l'auteur.
- **Erreur d'enregistrement**, présentée à sa place, en trois catégories ([Présentation des erreurs](../../fonctionnel/presentation-des-erreurs.md)) : un auteur sans nom ni pseudonyme ([Auteur / Artiste](../../modele-metier/auteur-artiste.md)) borde le nom et le pseudonyme de la couleur d'alerte, son message sous le nom, jusqu'à ce que l'un des noms change ; toute autre erreur (fiche modifiée entre-temps, texte trop long, problème technique) est présentée en tête du panneau.
- **Les champs**, à la suite :
  - le **prénom** et le **nom**, côte à côte (l'un sous l'autre sur smartphone), puis le **pseudonyme** ; l'aide rappelle qu'il faut au moins le nom ou le pseudonyme, et qu'un duo ou un collectif crédité sous un nom commun est un auteur, sous ce nom ;
  - sous eux, dans un cadre en tirets, ce qui en découle, calculé et non saisi, recalculé au fil de la saisie : le nom **affiché**, la clé où l'auteur est **classé** et son **initiale** ([Artistes](../../fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes)), un tiret tant qu'aucun nom ni pseudonyme ne les donne ; l'aide rappelle que le pseudonyme tient lieu de nom, et que sans lui l'auteur est classé à son nom, puis à son prénom. Contrairement au titre d'une série, cette clé n'a pas de mode manuel : le modèle ne la stocke pas ;
  - la **nationalité**, un champ dimensionné à son contenu ;
  - la **biographie**, une zone de texte ;
  - en modification, les dates de création et de modification de la fiche.
- **Pas de bibliographie** : le panneau ne présente pas les séries et albums de l'auteur, qui peuvent être nombreux ; un auteur se crédite sur la fiche de l'album ou de la série.
- **Les actions**, en bas du panneau : **Annuler** et **Enregistrer** (« Créer l'auteur » en création). L'enregistrement ramène à la liste, avec ses critères.
