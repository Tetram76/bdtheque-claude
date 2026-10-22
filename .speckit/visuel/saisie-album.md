# Saisie d'un album

Ce fichier décrit la présentation de la fiche de saisie d'un album dans la partie Administration (`fonctionnel.md` § Structure de l'application) : la page dédiée où un album se crée ou se modifie, ouverte depuis la liste des albums ([`liste-albums.md`](liste-albums.md)). Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation).

La fiche est déclinée selon chacun des thèmes en lice pour la partie Consultation (cf. [`dashboard.md`](dashboard.md) § Alternatives) : comme toute page de l'administration, elle suit l'identité visuelle du thème actif, dans le ton sobre de l'administration (cf. [`administration.md`](administration.md) § Ton).

---

## Maquettes

Toutes les déclinaisons sont présentées côte à côte par la visionneuse [`maquettes/saisie-album/`](maquettes/saisie-album/index.html) (cf. [`charte-graphique.md`](charte-graphique.md) § Maquettes). Elles ont une source commune, `mockup.html`, que chacune affiche avec son thème, choisi par le paramètre `theme` (mêmes valeurs que pour la page d'accueil de l'administration, [`administration.md`](administration.md) § Maquettes).

Réglages de la source commune :

| Réglage | Valeurs |
| --- | --- |
| `exemple` | `modification` (par défaut) : un album d'une série ; `creation` : un nouvel album, vide ; `integrale` : une intégrale sans titre propre, dont les auteurs viennent d'être repris de sa série |
| `cleManuelle` | clé de tri saisie à la main (exemple `modification`) |
| `erreur` | `aucune` (par défaut), `metier`, `fonctionnelle` ou `technique` : l'erreur présentée après un enregistrement refusé |
| `menuOpen` | menu de l'administration ouvert |

Les données sont fictives ; les couvertures sont des illustrations de substitution.

## Contenu

- **En-tête et menu** de l'administration ([`administration.md`](administration.md) § En-tête et menu), l'entrée « Albums » étant l'entrée courante du menu.
- **Fil d'Ariane** : Administration, Albums, puis l'album (« Nouvel album » en création).
- **Titre** « Modifier l'album » ou « Nouvel album », suivi du **libellé de l'album** (`fonctionnel.md` § Libellé d'un album), recalculé au fil de la saisie (« <Sans titre> » tant que rien n'est saisi). La fiche ne propose ni lien vers la consultation, qui ne présenterait pas les données en cours de modification, ni suppression, ni fusion : on y modifie l'album, et ces actions se font depuis la liste des albums.
- **Erreur d'enregistrement**, en tête de page, au-dessus du formulaire. Les trois catégories (`fonctionnel.md` § Présentation des erreurs) se distinguent au premier coup d'œil, chacune par sa mention, son icône, sa couleur et son cadre :
  - **À corriger** (erreur métier) : cadre plein de la couleur d'alerte, point d'exclamation ; le message dit quoi corriger, et le champ en cause est en outre bordé de la couleur d'alerte, avec son message sous lui ;
  - **Modifiée entre-temps** (erreur fonctionnelle) : cadre en tirets d'une couleur d'information, flèches de rechargement ; le message explique que rien n'a été écrasé, avec **Recharger la fiche** et **Voir mes changements** ;
  - **Problème technique** : cadre pointillé neutre, clé à molette ; le message rassure sur les changements saisis, avec **Réessayer**.
- **Le formulaire**, en blocs, sur la colonne principale :
  - **L'album** :
    - la **série**, choisie en tapant son titre, que l'on retire pour faire de l'album un album isolé ; l'aide rappelle ce que change la série pour le titre ;
    - le **titre**, dont la mention suit la série : « facultatif dans une série », « obligatoire sans série » ;
    - la **clé de tri**, toujours visible, marquée **Auto** ou **Manuelle** ; en mode manuel, **Réinitialiser** la recalcule à partir du titre (`fonctionnel.md` § Calcul et stockage de la clé de tri) ; sans titre propre, l'aide indique que l'album est rangé à la clé de sa série.
  - **Tomaison** et **Parution**, deux petits blocs côte à côte sous « L'album » (empilés sur smartphone) : deux notions sans rapport, la place de l'album dans la numérotation et sa date, que rien ne doit faire confondre, sans que l'une ou l'autre alourdisse « L'album » face aux blocs suivants. « Parution » prend la largeur de son contenu, « Tomaison » le reste.
    - **Tomaison**, dont les champs tiennent sur une ligne, qui passe à la ligne au besoin :
      - le **type** : deux cases à cocher, **Intégrale** et **Hors-série**, indépendantes l'une de l'autre ; sans case cochée, l'album est régulier. Une donnée à deux valeurs se saisit par une case à cocher, jamais par un sélecteur à boutons ;
      - le **tome** ; pour une intégrale, son **numéro** et, à côté, les **tomes couverts**, regroupés sous leur propre intitulé (« 1 à 5 »), la règle qui les lie (« Les deux, ou aucun ») placée juste sous eux.
    - **Parution** : la **première publication**, une liste de mois (« — » pour un mois inconnu) et l'année, côte à côte, jamais le jour.
  - Dans toute la fiche, chaque champ est **dimensionné à ce qu'il contient** (quelques chiffres pour un tome ou une année, un nom de mois), jamais étiré sur toute la largeur ; seuls les textes libres (série, titre, clé de tri, résumé, notes) l'occupent.
  - **Les auteurs** : une ligne par rôle (scénario, dessin, couleurs), chaque auteur en étiquette retirable, « Personne » sans auteur, et **Ajouter**, qui trouve un auteur en tapant son nom ou le crée sur place. Quand ils viennent d'être repris de la série (`fonctionnel.md` § Initialisation des contributions depuis la série), un encadré le dit, et rappelle qu'ils appartiennent désormais à l'album.
  - **Genres et univers** : ceux de l'album, en étiquettes retirables, avec **Ajouter** ; en dessous, **par la série**, ceux qu'il affiche par sa série, en étiquettes en tirets, non modifiables ici (`fonctionnel.md` § Genres et univers d'un album). Un univers est présenté avec ses ancêtres.
  - **Résumé, notes et appréciation** : le résumé et les notes personnelles en zones de texte, puis l'appréciation en cinq étoiles, avec son libellé (« Bien », « Pas encore noté ») et **Retirer**.
- **Pas de colonne des éditions** : les éditions et les intentions d'achat de l'album ne se gèrent pas depuis sa fiche de saisie, qu'on ne quitte ainsi jamais pour une autre fiche avec des changements non enregistrés ; elles ont leur propre page, **Éditions et intentions d'achat** ([`editions-album.md`](editions-album.md)), ouverte depuis la liste des albums. Le formulaire garde la largeur qu'il a à côté d'une colonne latérale sur la fiche d'une édition ; sous lui, en modification, les dates de création et de modification de la fiche.
- **La barre d'enregistrement**, figée en bas de l'écran : l'état de la fiche (« Modifications non enregistrées », signalé d'un point de couleur d'accent ; « Nouvelle fiche »), **Annuler** et **Enregistrer** (« Créer l'album » en création), mis en relief. Sur smartphone, elle tient sur une ligne, l'état réduit à quelques mots.
- **Smartphone** : les blocs s'empilent sur une colonne ; « Tomaison » et « Parution » aussi ; les champs de la tomaison passent à la ligne en gardant leur taille, les tomes couverts et la date de première publication restant chacun sur une ligne ; les rôles des auteurs passent au-dessus de leurs étiquettes.

## Contrôles

Ceux de la liste des albums ([`liste-albums.md`](liste-albums.md) § Contrôles) : texte indicatif de la couleur du texte atténué, sans opacité, et focus clavier détaché du contrôle par une bande de la couleur de la surface qui l'entoure.

## Déclinaison par thème

Couleurs, typographie et touche propre de chaque thème : celles de la page d'accueil de l'administration ([`administration.md`](administration.md) § Déclinaison par thème), chaque bloc du formulaire ayant son titre de bloc. Dans les thèmes qui portent une mention au-dessus du titre (gazette, festival, album de vignettes), cette mention dit la nature des données du bloc, sans répéter son titre : Identification (L'album), Dans la série (Tomaison), Calendrier (Parution), Contributions (Les auteurs), Classement (Genres et univers), Lecture (Résumé, notes et appréciation). La couleur d'information de l'erreur fonctionnelle est une couleur propre au thème, distincte de sa couleur d'alerte. Dans les thèmes « Cases et bulles », la bulle du titre de bloc chevauche le bord supérieur du bloc, comme sur le dashboard, toujours sans queue ni inclinaison.
