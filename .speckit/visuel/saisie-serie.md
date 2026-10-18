# Saisie d'une série

Ce fichier décrit la présentation de la fiche de saisie d'une série dans la partie Administration (`fonctionnel.md` § Structure de l'application) : la page dédiée où une série se crée ou se modifie, ouverte depuis la liste des séries ([`listes.md`](listes.md)). Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation).

La fiche suit le modèle de la saisie d'un album ([`saisie-album.md`](saisie-album.md)), dont elle reprend l'en-tête, le titre et ses actions, la présentation des erreurs, les blocs, les contrôles et la barre d'enregistrement ; ce fichier ne décrit que ce qui lui est propre. Elle est déclinée selon chacun des thèmes en lice (cf. [`dashboard.md`](dashboard.md) § Alternatives), dans le ton sobre de l'administration (cf. [`administration.md`](administration.md) § Ton).

---

## Maquettes

Toutes les déclinaisons sont présentées côte à côte par la visionneuse [`maquettes/saisie-serie/`](maquettes/saisie-serie/index.html) (cf. [`charte-graphique.md`](charte-graphique.md) § Maquettes). Elles ont une source commune, `mockup.html`, que chacune affiche avec son thème, choisi par le paramètre `theme` (mêmes valeurs que pour la page d'accueil de l'administration, [`administration.md`](administration.md) § Maquettes).

Réglages de la source commune :

| Réglage | Valeurs |
| --- | --- |
| `exemple` | `modification` (par défaut) : une série en cours, avec ses huit albums, dont une intégrale visée par une intention d'achat ; `creation` : une nouvelle série, vide, les valeurs proposées aux nouvelles éditions pré-remplies |
| `cleManuelle` | clé de tri saisie à la main (exemple `modification`) |
| `erreur` | `aucune` (par défaut), `metier`, `fonctionnelle` ou `technique` : l'erreur présentée après un enregistrement refusé |
| `menuOpen` | menu de l'administration ouvert |

Les données sont fictives ; les couvertures sont des illustrations de substitution.

## Contenu

- **En-tête et menu** de l'administration, l'entrée « Séries » étant l'entrée courante du menu.
- **Fil d'Ariane** : Administration, Séries, puis la série (« Nouvelle série » en création).
- **Titre** « Modifier la série » ou « Nouvelle série », suivi du titre de la série, recalculé au fil de la saisie (« <Sans titre> » tant que rien n'est saisi). En modification, à droite : **Voir dans la consultation**, et le menu « ⋯ » : **Fusionner avec une autre série…** et, séparée par un filet, **Supprimer la série…**, qui mènent aux dialogues de la liste (une série qui contient des albums ne se supprime pas, `fonctionnel.md` § Suppression des entités).
- **Erreur d'enregistrement** : celle de la saisie d'un album, en trois catégories ; l'erreur métier de l'exemple est un titre vidé, le champ étant bordé de la couleur d'alerte.
- **Le formulaire**, en blocs, sur la colonne principale :
  - **La série** : le **titre**, obligatoire ; la **clé de tri**, marquée **Auto** ou **Manuelle**, avec **Réinitialiser** en mode manuel (`fonctionnel.md` § Calcul et stockage de la clé de tri) ; le **statut**, une liste (« — Non renseigné », En cours, Terminée, Abandonnée), l'aide rappelant ce que veut dire « Terminée ».
  - **Avancement** et **Calculs**, deux blocs de même largeur côte à côte (empilés sur smartphone), deux sujets sans rapport :
    - **Avancement** : le nombre de **tomes numérotés prévus**, un champ de quelques chiffres, l'aide disant qu'il ne compte pas les hors-série et sert à identifier les albums manquants ; puis la case **Complète dans la collection**, l'aide rappelant que c'est un jugement de l'utilisateur, indépendant du nombre d'albums possédés (`modele-metier.md` § Série) ;
    - **Calculs** : les cases **Exclure des albums manquants** et **Exclure des estimations de sortie**, l'aide rappelant qu'elles ne figurent pas sur la fiche de la consultation (`fonctionnel.md` § Structure de l'application) et que seule une série en cours reçoit une estimation de sortie.
  - **Les auteurs** : une ligne par rôle, comme pour un album ; l'aide rappelle qu'ils sont recopiés sur un album qu'on rattache à la série s'il n'en a encore aucun (`fonctionnel.md` § Initialisation des contributions depuis la série).
  - **Éditeur et collection** : l'**éditeur** et la **collection éditeur**, côte à côte (l'un sous l'autre sur smartphone), choisis en tapant leur nom ; la collection, l'une de celles de l'éditeur, reste grisée, en tirets, tant qu'aucun éditeur n'est choisi (« Choisissez d'abord l'éditeur »), et changer d'éditeur la retire. L'aide rappelle qu'ils décrivent la série et pré-remplissent les nouvelles éditions de ses albums.
  - **Genres et univers** : ceux de la série, en étiquettes retirables, avec **Ajouter** ; l'aide rappelle que chaque album de la série les affiche en plus des siens (`fonctionnel.md` § Genres et univers d'un album).
  - **Résumé et notes** : deux zones de texte.
  - **Nouvelles éditions** : les valeurs proposées aux nouvelles éditions des albums de la série (`fonctionnel.md` § Initialisation d'une nouvelle édition depuis la série) : catégorie, état, reliure, format, orientation, sens de lecture et couleur, chacune une liste dimensionnée à son plus long choix, à la suite sur autant de lignes qu'il faut. « — » ne propose rien. La couleur, qui peut aussi ne rien proposer, est une liste à trois choix (« — », En couleur, Noir et blanc), non une case à cocher. L'aide rappelle que ces valeurs ne décrivent pas la série et n'apparaissent pas dans la consultation. En création, elles sont pré-remplies avec les valeurs proposées pour toute nouvelle édition (`modele-metier.md` § Série), ce qu'un encadré dit.
- **Les albums**, en modification seulement (une nouvelle série n'en a évidemment aucun), en colonne latérale sur grand écran (après le formulaire sur smartphone), en **lecture seule** : un album désigne sa série sans lui appartenir ; il se modifie, et se rattache à une série, sur sa propre fiche, ce que le bloc rappelle. La fiche de la série ne mène donc ni à la modification ni à la création d'un album, ce qui évite aussi de la quitter avec des changements non enregistrés. La liste sert de repère pour saisir la série (tomes prévus, série complète) et montre ce qui empêche sa suppression. Sur grand écran, la colonne tient dans la hauteur de l'écran, au-dessus de la barre d'enregistrement, et reste en vue quand la page défile ; la liste des albums y défile seule, sans qu'il faille d'abord faire défiler toute la fiche.
  - le décompte (« 8 albums, dont 6 dans la collection ») ;
  - chaque album, dans l'ordre de la série (`fonctionnel.md` § Ordre des albums dans une série) : sa vignette de couverture, son tome et son titre, son appartenance à la collection (« Dans la collection », « Pas dans la collection » ou « Intention d'achat », comme dans la liste des albums), et l'année de sa première publication, sans lien ni action ;
  - sous le bloc, les dates de création et de modification de la fiche.
- **La barre d'enregistrement** : celle de la saisie d'un album (« Créer la série » en création).
- **Smartphone** : les blocs s'empilent sur une colonne ; « Avancement » et « Calculs » aussi, comme l'éditeur et la collection ; les listes des nouvelles éditions passent à la ligne en gardant leur taille.

## Contrôles

Ceux de la liste des albums ([`liste-albums.md`](liste-albums.md) § Contrôles).

## Déclinaison par thème

Celle de la saisie d'un album ([`saisie-album.md`](saisie-album.md) § Déclinaison par thème). Les mentions au-dessus des titres de bloc sont : Identification (La série), Collection (Avancement), Réglages (Calculs), Contributions (Les auteurs), Publication (Éditeur et collection), Classement (Genres et univers), Lecture (Résumé et notes), Pré-remplissage (Nouvelles éditions), Contenu (Les albums).
