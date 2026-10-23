# Saisie d'un éditeur

Ce fichier décrit la présentation de la fiche de saisie d'un éditeur dans la partie Administration (`fonctionnel/index.md` § Structure de l'application) : la page dédiée où un éditeur se crée ou se modifie, avec ses collections, ouverte depuis la liste des éditeurs ([`listes.md`](listes.md)). Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation).

La fiche suit le modèle de la saisie d'une série ([`saisie-serie.md`](saisie-serie.md)), dont elle reprend l'en-tête, le titre et ses actions, la présentation des erreurs, les blocs, l'absence de colonne latérale, les contrôles et la barre d'enregistrement ; ce fichier ne décrit que ce qui lui est propre. Elle est déclinée selon chacun des thèmes en lice (cf. [`dashboard.md`](dashboard.md) § Alternatives), dans le ton sobre de l'administration (cf. [`administration.md`](administration.md) § Ton).

---

## Maquettes

Toutes les déclinaisons sont présentées côte à côte par la visionneuse [`maquettes/saisie-editeur/`](maquettes/saisie-editeur/index.html) (cf. [`charte-graphique.md`](charte-graphique.md) § Maquettes). Elles ont une source commune, `mockup.html`, que chacune affiche avec son thème, choisi par le paramètre `theme` (mêmes valeurs que pour la page d'accueil de l'administration, [`administration.md`](administration.md) § Maquettes).

Réglages de la source commune :

| Réglage | Valeurs |
| --- | --- |
| `exemple` | `modification` (par défaut) : un éditeur et ses collections, l'une ajoutée et une autre retirée depuis l'ouverture de la fiche ; `creation` : un nouvel éditeur, vide |
| `erreur` | `aucune` (par défaut), `metier`, `fonctionnelle` ou `technique` : l'erreur présentée après un enregistrement refusé |
| `menuOpen` | menu de l'administration ouvert |

Les données sont fictives.

## Contenu

- **En-tête et menu** de l'administration, l'entrée « Éditeurs » étant l'entrée courante du menu.
- **Fil d'Ariane** : Administration, Éditeurs, puis l'éditeur (« Nouvel éditeur » en création).
- **Titre** « Modifier l'éditeur » ou « Nouvel éditeur », suivi de son nom, recalculé au fil de la saisie (« <Sans nom> » tant que rien n'est saisi). La fiche ne propose ni lien vers la consultation, qui ne présenterait pas les données en cours de modification, ni suppression, ni fusion : on y modifie l'éditeur, et ces actions se font depuis la liste des éditeurs.
- **Erreur d'enregistrement** : celle de la saisie d'un album, en trois catégories ; l'erreur métier de l'exemple est un nom déjà porté par un autre éditeur, le champ étant bordé de la couleur d'alerte, le message rappelant qu'une différence de casse suffit à distinguer deux éditeurs (`modele-metier.md` § Éditeur).
- **Le formulaire**, en deux blocs :
  - **L'éditeur** : le **nom**, obligatoire, et le **site web**, avec, une fois renseigné, un bouton qui l'ouvre ; l'aide rappelle que dans la consultation le nom de l'éditeur mène à son site, et n'est pas un lien sans lui (`fonctionnel/index.md` § Structure de l'application).
  - **Les collections** : les collections éditeur n'ont ni liste ni page à elles ; elles se saisissent ici et s'enregistrent avec l'éditeur, dont elles font partie (`fonctionnel/regles-metier.md` § Suppression des entités), ce que le bloc rappelle. Une ligne par collection, par ordre alphabétique :
    - son **nom**, dans un champ, qui suffit à la renommer ;
    - ce qui la désigne : le nombre d'éditions et de séries (« 58 éditions · 3 séries », « Aucune édition ni série ») ;
    - **Retirer**, une corbeille de la couleur de suppression, lorsque rien ne la désigne ; sinon, à sa place, un cadenas atténué dit qu'elle est utilisée et ne peut pas être retirée (`fonctionnel/regles-metier.md` § Suppression des entités) ;
    - une collection **retirée** reste dans la liste jusqu'à l'enregistrement, barrée, « Retirée à l'enregistrement », avec **Rétablir** ; une collection **ajoutée** porte l'étiquette « Nouvelle » ;
    - **Ajouter une collection**, en tirets, sous la liste ; « Aucune collection » à défaut.
  - Sur smartphone, ce qui désigne une collection passe sous son nom, la corbeille ou le cadenas restant à droite.
- **Pas de colonne latérale** : comme pour une série ou un auteur, la fiche ne présente pas ce qui désigne l'éditeur, au-delà de ce qui désigne chacune de ses collections ; une suppression refusée le dit dans son dialogue. Le formulaire garde la largeur de celui de la saisie d'une série ; sous lui, en modification, les dates de création et de modification de la fiche.
- **La barre d'enregistrement** : celle de la saisie d'un album (« Créer l'éditeur » en création).

## Contrôles

Ceux de la liste des albums ([`liste-albums.md`](liste-albums.md) § Contrôles).

## Déclinaison par thème

Celle de la saisie d'un album ([`saisie-album.md`](saisie-album.md) § Déclinaison par thème). Les mentions au-dessus des titres de bloc sont : Identification (L'éditeur), Catalogue (Les collections).
