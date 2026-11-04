---
type: Référence visuelle
title: 'Contenu'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Contenu

- **En-tête et menu** de l'administration, l'entrée « Éditeurs » étant l'entrée courante du menu.
- **Fil d'Ariane** : Administration, Éditeurs, puis l'éditeur (« Nouvel éditeur » en création).
- **Titre** « Modifier l'éditeur » ou « Nouvel éditeur », suivi de son nom, recalculé au fil de la saisie (« <Sans nom> » tant que rien n'est saisi). La fiche ne propose ni lien vers la consultation, qui ne présenterait pas les données en cours de modification, ni suppression, ni fusion : on y modifie l'éditeur, et ces actions se font depuis la liste des éditeurs.
- **Erreur d'enregistrement** : celle de la saisie d'un album, en trois catégories ; l'erreur métier de l'exemple est un nom déjà porté par un autre éditeur, le champ étant bordé de la couleur d'alerte, le message rappelant qu'une différence de casse suffit à distinguer deux éditeurs ([Éditeur](/modele-metier/editeur.md)).
- **Le formulaire**, en deux blocs :
  - **L'éditeur** : le **nom**, obligatoire, et le **site web**, avec, une fois renseigné, un bouton qui l'ouvre ; l'aide rappelle que dans la consultation le nom de l'éditeur mène à son site, et n'est pas un lien sans lui ([Structure de l'application](/fonctionnel/structure-de-l-application.md)).
  - **Les collections** : les collections éditeur n'ont ni liste ni page à elles ; elles se saisissent ici et s'enregistrent avec l'éditeur, dont elles font partie ([Suppression des entités](/fonctionnel/regles-metier/suppression-des-entites.md)), ce que le bloc rappelle. Une ligne par collection, par ordre alphabétique :
    - son **nom**, dans un champ, qui suffit à la renommer ;
    - ce qui la désigne : le nombre d'éditions et de séries (« 58 éditions · 3 séries », « Aucune édition ni série ») ;
    - **Retirer**, une corbeille de la couleur de suppression, lorsque rien ne la désigne ; sinon, à sa place, un cadenas atténué dit qu'elle est utilisée et ne peut pas être retirée ([Suppression des entités](/fonctionnel/regles-metier/suppression-des-entites.md)) ;
    - une collection **retirée** reste dans la liste jusqu'à l'enregistrement, barrée, « Retirée à l'enregistrement », avec **Rétablir** ; une collection **ajoutée** porte l'étiquette « Nouvelle » ;
    - **Ajouter une collection**, en tirets, sous la liste ; « Aucune collection » à défaut.
  - Sur smartphone, ce qui désigne une collection passe sous son nom, la corbeille ou le cadenas restant à droite.
- **Pas de colonne latérale** : comme pour une série ou un auteur, la fiche ne présente pas ce qui désigne l'éditeur, au-delà de ce qui désigne chacune de ses collections ; une suppression refusée le dit dans son dialogue. Le formulaire garde la largeur de celui de la saisie d'une série ; sous lui, en modification, les dates de création et de modification de la fiche.
- **La barre d'enregistrement** : celle de la saisie d'un album (« Créer l'éditeur » en création).
