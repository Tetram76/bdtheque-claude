---
type: Référence visuelle
title: 'Contenu'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Contenu

- **En-tête et menu** de l'administration, l'entrée « Auteurs » étant l'entrée courante du menu.
- **Fil d'Ariane** : Administration, Auteurs, puis l'auteur (« Nouvel auteur » en création).
- **Titre** « Modifier l'auteur » ou « Nouvel auteur », suivi de son identifiant principal ([Artistes](/fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes)), recalculé au fil de la saisie (« <Sans nom> » tant que rien n'est saisi). La fiche ne propose ni lien vers la consultation, qui ne présenterait pas les données en cours de modification, ni suppression, ni fusion : on y modifie l'auteur, et ces actions se font depuis la liste des auteurs.
- **Erreur d'enregistrement** : celle de la saisie d'un album, en trois catégories ; l'erreur métier de l'exemple est un auteur sans nom ni pseudonyme, les deux champs étant bordés de la couleur d'alerte ([Auteur / Artiste](/modele-metier/auteur-artiste.md)).
- **Le formulaire**, en deux blocs :
  - **L'auteur** :
    - le **prénom** et le **nom**, côte à côte (l'un sous l'autre sur smartphone), puis le **pseudonyme** ; l'aide rappelle qu'il faut au moins le nom ou le pseudonyme, et qu'un duo ou un collectif crédité sous un nom commun est un auteur, sous ce nom ;
    - sous eux, dans un cadre en tirets, ce qui en découle, calculé et non saisi : le nom **affiché**, la clé où l'auteur est **classé** et son **initiale** ([Artistes](/fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes)) ; l'aide rappelle que le pseudonyme tient lieu de nom, et que sans lui l'auteur est classé à son nom, puis son prénom. Contrairement au titre d'une série, cette clé n'a pas de mode manuel : le modèle ne la stocke pas ;
    - la **nationalité**, un champ dimensionné à son contenu.
  - **Biographie** : une zone de texte.
- **Pas de colonne latérale** : la fiche ne présente pas la bibliographie de l'auteur, qui peut compter beaucoup de séries et d'albums ; un auteur se crédite sur la fiche de l'album ou de la série. Le formulaire garde la largeur de celui de la saisie d'une série ; sous lui, en modification, les dates de création et de modification de la fiche.
- **La barre d'enregistrement** : celle de la saisie d'un album (« Créer l'auteur » en création).
