# Saisie d'un auteur

Ce fichier décrit la présentation de la fiche de saisie d'un auteur dans la partie Administration (`fonctionnel.md` § Structure de l'application) : la page dédiée où un auteur se crée ou se modifie, ouverte depuis la liste des auteurs ([`listes.md`](listes.md)). Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation).

La fiche suit le modèle de la saisie d'une série ([`saisie-serie.md`](saisie-serie.md)), dont elle reprend l'en-tête, le titre et ses actions, la présentation des erreurs, les blocs, la colonne latérale en lecture seule, les contrôles et la barre d'enregistrement ; ce fichier ne décrit que ce qui lui est propre. Elle est déclinée selon chacun des thèmes en lice (cf. [`dashboard.md`](dashboard.md) § Alternatives), dans le ton sobre de l'administration (cf. [`administration.md`](administration.md) § Ton).

---

## Maquettes

Toutes les déclinaisons sont présentées côte à côte par la visionneuse [`maquettes/saisie-auteur/`](maquettes/saisie-auteur/index.html) (cf. [`charte-graphique.md`](charte-graphique.md) § Maquettes). Elles ont une source commune, `mockup.html`, que chacune affiche avec son thème, choisi par le paramètre `theme` (mêmes valeurs que pour la page d'accueil de l'administration, [`administration.md`](administration.md) § Maquettes).

Réglages de la source commune :

| Réglage | Valeurs |
| --- | --- |
| `exemple` | `modification` (par défaut) : un auteur connu sous un pseudonyme, crédité sur quatre séries et un album sans série ; `creation` : un nouvel auteur, vide |
| `erreur` | `aucune` (par défaut), `metier`, `fonctionnelle` ou `technique` : l'erreur présentée après un enregistrement refusé |
| `menuOpen` | menu de l'administration ouvert |

Les données sont fictives ; les couvertures sont des illustrations de substitution.

## Contenu

- **En-tête et menu** de l'administration, l'entrée « Auteurs » étant l'entrée courante du menu.
- **Fil d'Ariane** : Administration, Auteurs, puis l'auteur (« Nouvel auteur » en création).
- **Titre** « Modifier l'auteur » ou « Nouvel auteur », suivi de son identifiant principal (`fonctionnel.md` § Artistes), recalculé au fil de la saisie (« <Sans nom> » tant que rien n'est saisi). En modification, à droite : **Voir dans la consultation**, et le menu « ⋯ » : **Fusionner avec un autre auteur…** et, séparée par un filet, **Supprimer l'auteur…**, qui mènent aux dialogues de la liste (un auteur crédité ne se supprime pas, `fonctionnel.md` § Suppression des entités).
- **Erreur d'enregistrement** : celle de la saisie d'un album, en trois catégories ; l'erreur métier de l'exemple est un auteur sans nom ni pseudonyme, les deux champs étant bordés de la couleur d'alerte (`modele-metier.md` § Auteur / Artiste).
- **Le formulaire**, en deux blocs :
  - **L'auteur** :
    - le **prénom** et le **nom**, côte à côte (l'un sous l'autre sur smartphone), puis le **pseudonyme** ; l'aide rappelle qu'il faut au moins le nom ou le pseudonyme, et qu'un duo ou un collectif crédité sous un nom commun est un auteur, sous ce nom ;
    - sous eux, dans un cadre en tirets, ce qui en découle, calculé et non saisi : le nom **affiché**, la clé où l'auteur est **classé** et son **initiale** (`fonctionnel.md` § Artistes) ; l'aide rappelle que le pseudonyme tient lieu de nom, et que sans lui l'auteur est classé à son nom, puis son prénom. Contrairement au titre d'une série, cette clé n'a pas de mode manuel : le modèle ne la stocke pas ;
    - la **nationalité**, un champ dimensionné à son contenu.
  - **Biographie** : une zone de texte.
- **Bibliographie**, en modification seulement, en colonne latérale sur grand écran (après le formulaire sur smartphone), en **lecture seule**, comme les albums d'une série : un auteur se crédite sur la fiche de l'album ou de la série, ce que le bloc rappelle, en ajoutant qu'il ne peut pas être supprimé tant qu'il est crédité.
  - le décompte (« 4 séries et 1 album sans série, soit 22 albums ») ;
  - séries et albums sans série, triés ensemble par ordre alphabétique, comme sur la fiche de l'auteur en consultation (`fonctionnel.md` § Structure de l'application) : la vignette (une petite pile pour une série), le titre, « Série » ou « Album », les rôles tenus, et le nombre d'albums d'une série où l'auteur est crédité ou l'année d'un album ;
  - sur grand écran, la colonne tient dans la hauteur de l'écran et la liste y défile seule, comme les albums d'une série ;
  - sous le bloc, les dates de création et de modification de la fiche.
- **La barre d'enregistrement** : celle de la saisie d'un album (« Créer l'auteur » en création).

## Contrôles

Ceux de la liste des albums ([`liste-albums.md`](liste-albums.md) § Contrôles).

## Déclinaison par thème

Celle de la saisie d'un album ([`saisie-album.md`](saisie-album.md) § Déclinaison par thème). Les mentions au-dessus des titres de bloc sont : Identification (L'auteur), Présentation (Biographie), Crédits (Bibliographie).
