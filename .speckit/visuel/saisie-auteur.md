# Saisie d'un auteur

Ce fichier décrit la présentation de la fiche de saisie d'un auteur dans la partie Administration (`fonctionnel.md` § Structure de l'application) : la page dédiée où un auteur se crée ou se modifie, ouverte depuis la liste des auteurs ([`listes.md`](listes.md)). Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation).

La fiche suit le modèle de la saisie d'une série ([`saisie-serie.md`](saisie-serie.md)), dont elle reprend l'en-tête, le titre et ses actions, la présentation des erreurs, les blocs, l'absence de colonne latérale, les contrôles et la barre d'enregistrement ; ce fichier ne décrit que ce qui lui est propre. Elle est déclinée selon chacun des thèmes en lice (cf. [`dashboard.md`](dashboard.md) § Alternatives), dans le ton sobre de l'administration (cf. [`administration.md`](administration.md) § Ton).

---

## Maquettes

Toutes les déclinaisons sont présentées côte à côte par la visionneuse [`maquettes/saisie-auteur/`](maquettes/saisie-auteur/index.html) (cf. [`charte-graphique.md`](charte-graphique.md) § Maquettes). Elles ont une source commune, `mockup.html`, que chacune affiche avec son thème, choisi par le paramètre `theme` (mêmes valeurs que pour la page d'accueil de l'administration, [`administration.md`](administration.md) § Maquettes).

Réglages de la source commune :

| Réglage | Valeurs |
| --- | --- |
| `exemple` | `modification` (par défaut) : un auteur connu sous un pseudonyme ; `creation` : un nouvel auteur, vide |
| `erreur` | `aucune` (par défaut), `metier`, `fonctionnelle` ou `technique` : l'erreur présentée après un enregistrement refusé |
| `menuOpen` | menu de l'administration ouvert |

Les données sont fictives ; les couvertures sont des illustrations de substitution.

## Contenu

- **En-tête et menu** de l'administration, l'entrée « Auteurs » étant l'entrée courante du menu.
- **Fil d'Ariane** : Administration, Auteurs, puis l'auteur (« Nouvel auteur » en création).
- **Titre** « Modifier l'auteur » ou « Nouvel auteur », suivi de son identifiant principal (`fonctionnel.md` § Artistes), recalculé au fil de la saisie (« <Sans nom> » tant que rien n'est saisi). La fiche ne propose ni lien vers la consultation, qui ne présenterait pas les données en cours de modification, ni suppression, ni fusion : on y modifie l'auteur, et ces actions se font depuis la liste des auteurs.
- **Erreur d'enregistrement** : celle de la saisie d'un album, en trois catégories ; l'erreur métier de l'exemple est un auteur sans nom ni pseudonyme, les deux champs étant bordés de la couleur d'alerte (`modele-metier.md` § Auteur / Artiste).
- **Le formulaire**, en deux blocs :
  - **L'auteur** :
    - le **prénom** et le **nom**, côte à côte (l'un sous l'autre sur smartphone), puis le **pseudonyme** ; l'aide rappelle qu'il faut au moins le nom ou le pseudonyme, et qu'un duo ou un collectif crédité sous un nom commun est un auteur, sous ce nom ;
    - sous eux, dans un cadre en tirets, ce qui en découle, calculé et non saisi : le nom **affiché**, la clé où l'auteur est **classé** et son **initiale** (`fonctionnel.md` § Artistes) ; l'aide rappelle que le pseudonyme tient lieu de nom, et que sans lui l'auteur est classé à son nom, puis son prénom. Contrairement au titre d'une série, cette clé n'a pas de mode manuel : le modèle ne la stocke pas ;
    - la **nationalité**, un champ dimensionné à son contenu.
  - **Biographie** : une zone de texte.
- **Pas de colonne latérale** : la fiche ne présente pas la bibliographie de l'auteur, qui peut compter beaucoup de séries et d'albums ; un auteur se crédite sur la fiche de l'album ou de la série. Le formulaire garde la largeur de celui de la saisie d'une série ; sous lui, en modification, les dates de création et de modification de la fiche.
- **La barre d'enregistrement** : celle de la saisie d'un album (« Créer l'auteur » en création).

## Contrôles

Ceux de la liste des albums ([`liste-albums.md`](liste-albums.md) § Contrôles).

## Déclinaison par thème

Celle de la saisie d'un album ([`saisie-album.md`](saisie-album.md) § Déclinaison par thème). Les mentions au-dessus des titres de bloc sont : Identification (L'auteur), Présentation (Biographie).
