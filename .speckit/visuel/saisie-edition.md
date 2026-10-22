# Saisie d'une édition

Ce fichier décrit la présentation de la fiche de saisie d'une édition et de ses visuels dans la partie Administration (`fonctionnel.md` § Structure de l'application) : la page dédiée où une édition se crée ou se modifie, ouverte depuis la page des éditions et intentions d'achat de son album ([`editions-album.md`](editions-album.md)) ou depuis la liste des éditions ([`listes.md`](listes.md)). Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation).

La fiche suit les principes de la fiche de saisie d'un album ([`saisie-album.md`](saisie-album.md)) : blocs de taille comparable, champs dimensionnés à leur contenu, cases à cocher pour les données oui/non, mention de bloc distincte de son titre, erreurs distinguées au premier coup d'œil, barre d'enregistrement figée. Elle est déclinée selon chacun des thèmes en lice (cf. [`dashboard.md`](dashboard.md) § Alternatives), dans le ton sobre de l'administration (cf. [`administration.md`](administration.md) § Ton).

---

## Maquettes

Toutes les déclinaisons sont présentées côte à côte par la visionneuse [`maquettes/saisie-edition/`](maquettes/saisie-edition/index.html) (cf. [`charte-graphique.md`](charte-graphique.md) § Maquettes). Elles ont une source commune, `mockup.html`, que chacune affiche avec son thème, choisi par le paramètre `theme` (mêmes valeurs que pour la page d'accueil de l'administration, [`administration.md`](administration.md) § Maquettes).

Réglages de la source commune :

| Réglage | Valeurs |
| --- | --- |
| `exemple` | `modification` (par défaut) : une édition possédée, avec onze visuels ; `creation` : une nouvelle édition, pré-remplie d'après la série de l'album ; `nouvelleIntention` : une nouvelle édition visée par une intention d'achat, créée avec elle ; `intention` : une édition visée par une intention d'achat, donc non possédée |
| `mode` | `achat` (par défaut) ou `offerte` : le mode d'acquisition, dont suivent les libellés de la date et du montant |
| `gratuite` | édition gratuite (avec le mode `offerte`) |
| `isbn` | `valide` (par défaut) ou `incoherent` : chiffre de contrôle incohérent |
| `televersement` | `aucun` (par défaut), `encours` (un envoi en cours, un autre en attente) ou `refus` (deux fichiers refusés) |
| `visuelMenu` | menu des actions d'un visuel ouvert |
| `erreur` | `aucune` (par défaut), `metier`, `fonctionnelle` ou `technique` |
| `menuOpen` | menu de l'administration ouvert |

Les données sont fictives ; les visuels sont des illustrations de substitution.

## Contenu

- **En-tête et menu** de l'administration, l'entrée « Albums » étant l'entrée courante du menu.
- **Fil d'Ariane** : Administration, Albums, l'album, qui mène à la page de ses éditions et intentions d'achat, puis l'édition (« Nouvelle édition » en création, « Nouvelle intention d'achat » pour une édition visée).
- **Titre** « Modifier l'édition » ou « Nouvelle édition » (« Modifier l'intention d'achat » ou « Nouvelle intention d'achat » pour une édition visée par une intention d'achat, ce qui suffit à la distinguer d'une édition possédée), suivi du **libellé de l'édition** (`fonctionnel.md` § Libellé d'une édition), recalculé au fil de la saisie, et du lien vers la fiche de saisie de l'**album**. En modification, à droite : **Voir dans la consultation** (la fiche de l'album, qui présente l'édition). La fiche ne propose ni suppression de l'édition, ni confirmation de l'achat, ni retrait de l'intention : on y modifie l'édition, et ces actions se font depuis la page des éditions et intentions d'achat de l'album ou depuis les listes.
- **Erreur d'enregistrement**, en tête de page, présentée comme sur la fiche de saisie d'un album ; l'erreur métier pointe en outre le champ en cause (ex. un prix sans date de référence).
- **Pré-remplissage** : en création, un encadré indique que la fiche est pré-remplie d'après la série de l'album, et quels champs, ces valeurs n'étant que des propositions (`fonctionnel.md` § Initialisation d'une nouvelle édition depuis la série).
- **Le formulaire**, en blocs :
  - **L'édition** : l'**éditeur**, obligatoire, et la **collection**, parmi celles de l'éditeur, chacun choisi en tapant son nom ; puis l'**année d'édition**, l'**ISBN** et la **catégorie**. Le chiffre de contrôle de l'ISBN est vérifié pendant la saisie : correct, une coche ; incohérent, un cadre et un message d'avertissement, qui rappellent que l'ISBN peut être enregistré tel quel (`fonctionnel.md` § Validation de l'ISBN).
  - **Fabrication** : format, reliure, orientation et sens de lecture, en listes courtes où « — » laisse la donnée vide, puis le nombre de **pages** et la case **En couleur**.
  - **L'exemplaire**, pour une édition possédée : le **mode d'acquisition**, obligatoire ; la **date**, dont le libellé suit le mode (« Date d'achat », « Date d'acquisition », `fonctionnel.md` § Libellés contextuels sur l'édition) ; l'**état** ; les cases **D'occasion** et **Dédicacée** ; la **numérotation personnelle** ; les **notes personnelles**.
  - **Valeur**, pour une édition possédée : le **montant d'acquisition**, dont le libellé suit le mode (« Prix d'achat », « Valeur d'acquisition »), et la **valeur initiale**, chacun en montant et devise côte à côte (devise d'origine, ancien franc compris), avec ce qui le date ; puis la case **Gratuite**, qui vide et désactive les deux montants, et que le mode « Achat » désactive (une édition achetée n'est jamais gratuite).
  - pour une édition visée par une intention d'achat, ni l'exemplaire ni la valeur : la fiche présente le minimum de champs (`fonctionnel.md` § Réalisation d'une intention), seul l'éditeur étant obligatoire. L'exemplaire se saisit en confirmant l'achat, depuis la page des éditions et intentions d'achat de l'album ([`editions-album.md`](editions-album.md)) ou depuis la liste des intentions d'achat, où se retire aussi l'intention. Une nouvelle édition visée, ouverte par les boutons d'intention d'achat de cette page, est pré-remplie d'après la série comme toute nouvelle édition.
- **Les visuels**, en colonne latérale sur grand écran (après le formulaire sur smartphone), car ils ne s'enregistrent pas avec le formulaire : chaque visuel est enregistré dès son ajout, son ordre et son type dès qu'on les change, ce que le bloc rappelle.
  - une **zone de dépôt**, avec le choix de fichiers et le type des visuels ajoutés (« Couverture » proposé), les formats acceptés et la taille maximale ;
  - les **envois en cours**, traités un à la fois : progression de l'envoi, « En attente » pour les suivants ; un fichier refusé (format, taille) l'est avec sa raison, en couleur d'alerte, et se retire de la liste ;
  - le **nombre de visuels**, puis tous les visuels en **une seule grille**, dans l'ordre des types (`fonctionnel.md` § Ordre des visuels d'une édition), chaque vignette marquée de son type (« Couv. », « Garde », « Pl. 3 », « 4e »), pour qu'un type d'un seul visuel n'occupe pas une ligne à lui seul ;
  - sur chaque vignette, un bouton « ⋯ » qui ouvre son menu : **Changer le type**, **Ouvrir l'original** et **Supprimer le visuel…** ; une vignette se glisse pour la réordonner parmi celles de son type ;
  - en création, le bloc indique que les visuels s'ajoutent une fois l'édition enregistrée ;
  - sous le bloc, les dates de création, d'entrée dans la collection et de modification de l'édition.
- **La barre d'enregistrement**, comme sur la fiche de saisie d'un album (« Créer l'édition » en création, « Créer l'intention d'achat » pour une nouvelle édition visée).
- **Smartphone** : les blocs s'empilent ; les champs passent à la ligne en gardant leur taille, et les cases à cocher s'alignent sur le bas des champs voisins.

## Contrôles

Ceux de la liste des albums ([`liste-albums.md`](liste-albums.md) § Contrôles).

## Déclinaison par thème

Celle de la fiche de saisie d'un album ([`saisie-album.md`](saisie-album.md) § Déclinaison par thème). Mentions des blocs : Publication (L'édition), Objet (Fabrication), Possession (L'exemplaire), Montants (Valeur), Images (Les visuels).
