# Listes de l'administration

Ce fichier décrit la présentation des listes de l'administration autres que celle des albums (`fonctionnel.md` § Structure de l'application) : séries, éditions, intentions d'achat, auteurs, éditeurs, genres et univers. Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation).

Toutes suivent le modèle de la liste des albums ([`liste-albums.md`](liste-albums.md)), dont elles reprennent l'en-tête, la recherche, les filtres, la navigation par initiale, la présentation en lignes, les actions, les dialogues (suppression, fusion) et les contrôles ; ce fichier ne décrit que ce qui leur est propre. Elles sont déclinées selon chacun des thèmes en lice (cf. [`dashboard.md`](dashboard.md) § Alternatives), dans le ton sobre de l'administration (cf. [`administration.md`](administration.md) § Ton).

---

## Maquettes

Toutes les déclinaisons sont présentées côte à côte par la visionneuse [`maquettes/listes/`](maquettes/listes/index.html) (cf. [`charte-graphique.md`](charte-graphique.md) § Maquettes). Elles ont une source commune, `mockup.html`, que chacune affiche avec son thème, choisi par le paramètre `theme` (mêmes valeurs que pour la page d'accueil de l'administration, [`administration.md`](administration.md) § Maquettes).

Réglages de la source commune :

| Réglage | Valeurs |
| --- | --- |
| `entite` | la liste présentée : `series` (par défaut), `editions`, `intentions`, `auteurs`, `editeurs`, `genres` ou `univers` |
| `filtres` | panneau des filtres ouvert (séries et éditions) |
| `actions` | menu des autres actions d'une ligne ouvert |
| `edition` | un genre en cours de modification dans sa ligne |
| `ajout` | cadre d'ajout d'un genre ouvert |
| `doublon` | un nouveau genre refusé, son libellé étant déjà pris (cadre d'ajout ouvert) |
| `panneau` | panneau d'un univers : `aucun` (par défaut), `modification`, `creation` ou `cycle` (parent refusé) |
| `menuOpen` | menu de l'administration ouvert |

Les données sont fictives.

## Principes communs

- **Ligne** : un visuel lorsque l'entité en a un, l'entrée (son nom ou son libellé, avec une précision en petit dessous), les colonnes propres à l'entité, puis les actions. Sur un écran moins large, les colonnes secondaires (date de modification, catégorie, nationalité, site web…) s'effacent ; sur smartphone, les autres colonnes passent juste sous l'entrée, alignées sur elle (sur le nom d'un sous-univers, au-delà de son retrait), un nombre y étant précédé du nom de sa colonne (« Albums 24 ») ; l'entrée est centrée sur la hauteur de ses actions, à droite, pour en rester proche, les boutons gardant la taille qu'ils ont sur les autres pages.
- **Actions** : l'action principale de l'entité en bouton, puis un menu « ⋯ » : voir l'entrée dans la consultation (ou le site web d'un éditeur), **Fusionner avec…** une autre fiche du même type (`fonctionnel.md` § Structure de l'application), et, séparée par un filet, la suppression. Un clic en dehors d'un menu ouvert le referme.
- **Création** : un bouton « Nouvel… » en tête de page, sauf pour les intentions d'achat, qui se posent depuis la page des éditions et intentions d'achat de leur album ([`editions-album.md`](editions-album.md) ; une mention le dit à sa place).
- **Données absentes** : affichées « Non renseigné(e) » ou « — », en texte atténué, jamais laissées vides.
- **Zone de texte** (ex. description d'un univers) : un arrondi modéré, même dans un thème dont les champs d'une ligne sont en pilule, dont l'arrondi rognerait ses coins et son texte.

## Saisie selon l'entité

La saisie d'une fiche s'ouvre selon le poids de l'entité :

| Entité | Saisie |
| --- | --- |
| Série, édition, auteur, éditeur | **Page dédiée**, comme l'album ; l'éditeur, qui porte ses collections, en est une. Série : [`saisie-serie.md`](saisie-serie.md) ; édition : [`saisie-edition.md`](saisie-edition.md) ; auteur : [`saisie-auteur.md`](saisie-auteur.md) ; éditeur : [`saisie-editeur.md`](saisie-editeur.md). |
| Genre | **Dans sa ligne** : « Modifier » change la ligne en champ, avec **Enregistrer** et **Annuler** ; un nouveau genre se saisit dans un cadre en tirets au-dessus de la liste, avec **Ajouter** et **Annuler** ; ce cadre est masqué par défaut, et le bouton **Nouveau genre** de l'en-tête l'ouvre. Un libellé déjà pris est refusé sur place, en couleur d'alerte, en rappelant que la casse et les accents ne distinguent pas deux genres (`modele-metier.md` § Genre). |
| Univers | **Panneau latéral**, qui glisse par-dessus la liste depuis la droite, la liste restant visible et grisée derrière : nom, univers parent (choisi en tapant son nom, présenté avec ses ancêtres, « Univers Hugo Pratt › Corto Maltese »), description, dates de la fiche, puis **Annuler** et **Enregistrer**. Un parent qui créerait un cycle est refusé sur place, avec sa raison (`modele-metier.md` § Univers — relations). |
| Intention d'achat | Pas de fiche propre : l'action principale est **Confirmer l'achat**, et le menu mène à la page des éditions et intentions d'achat de l'album, à la conversion vers l'album ou une édition, et au retrait de l'intention (`fonctionnel.md` § Intention d'achat). |

## Liste par liste

| Liste | Visuel | Entrée | Colonnes | Recherche, navigation, filtres, ordre |
| --- | --- | --- | --- | --- |
| Séries | petite pile de trois couvertures, comme en consultation | titre | statut, albums et albums dans la collection, « Complète » ou « À compléter », modification | titre ; navigation par initiale ; filtres auteur, éditeur, collection éditeur, genre, univers ; par titre |
| Éditions | vignette de couverture (visuel générique sans couverture) | libellé de l'édition, et l'album dessous | année, catégorie, possession (« Dans la collection » ou « Intention d'achat »), modification | ISBN ; filtres album, éditeur, collection éditeur ; dans l'ordre des albums, puis par année |
| Intentions d'achat | vignette de couverture de l'album | libellé de l'album visé | ce qu'elle vise (l'album, toute édition, ou une édition), appartenance de l'album à la collection, date | titre de l'album ou de sa série ; dans l'ordre des albums |
| Auteurs | monogramme | identifiant principal, et le prénom et le nom dessous sous un pseudonyme | nationalité, nombre d'albums crédités, rôles tenus, modification | nom, prénom ou pseudonyme ; navigation par initiale ; par clé de tri |
| Éditeurs | — | nom, et ses collections dessous | site web (lien), nombre de collections, nombre d'éditions, modification | nom ; par nom |
| Genres | — | libellé | nombre d'albums, nombre de séries, modification | libellé ; par libellé |
| Univers | — | nom, en retrait sous son parent, chaque univers suivi de ses sous-univers | nombre d'albums et de séries, sous-univers compris, nombre de sous-univers, modification | nom ; selon la hiérarchie |

## Déclinaison par thème

Celle de la liste des albums ([`liste-albums.md`](liste-albums.md) § Déclinaison par thème). La mention du bloc de la liste est « Fiches », ou « Référentiel » pour les genres et les univers.
