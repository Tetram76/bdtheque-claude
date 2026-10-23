# Fonctionnel

Ce dossier décrit les fonctionnalités de l'application, ainsi que les éléments de design et la charte graphique.
Il ne doit contenir **aucun détail d'implémentation laissé à la discrétion de l'agent** (stack, architecture, mécanique de calcul, structure de données, bibliothèque, etc.) : ceux-ci relèvent de `contraintes-techniques/` ou `choix-implementation/`. En revanche, un élément d'apparence technique mais **explicitement imposé par l'utilisateur comme règle métier** (ex. l'algorithme d'estimation retenu) reste documenté ici. Les règles sont formulées au niveau métier — le comportement attendu et les cas visibles de l'utilisateur.

| Fichier | Contenu |
| --- | --- |
| `index.md` | Contexte, nom de l'application, design et charte graphique, structure de l'application, règles d'accès, présentation des erreurs. |
| `regles-metier.md` | Appartenance à la collection, périmètre de la consultation, initialisation des saisies depuis la série, genres et univers, ordres de présentation, visuels et couverture, ISBN, séquence de tomes, suppression des entités, intention d'achat. |
| `presentation.md` | Langue, culture et fuseau horaire d'affichage, tri et navigation par initiale, libellés des albums et des éditions. |
| `valorisation.md` | Calcul des estimations, estimation de la valeur des éditions, coût de remplacement, gestion des devises. |
| `page-accueil.md` | Dashboard de la page d'accueil et écran d'accueil d'une collection vide. |
| `recherche-avancee.md` | Recherche avancée et recherches enregistrées. |
| `agents-ia.md` | Interface pour agents IA : accès par jeton, suivi de l'utilisation. |
| `mesure-audience.md` | Mesure d'audience de l'interface web. |
| `second-plan.md` | Identification des albums manquants, estimation de sortie d'un nouvel album. |

---

## Contexte

L'application est la réécriture d'une application client lourd existante sous forme d'application web n-tiers. L'application existante est une **BDthèque** : un gestionnaire de collection de bandes dessinées.

## Nom de l'application

Le nom définitif de l'application n'est pas encore défini. Le **nom de travail** est **`bdtheque-claude`** (nom du dépôt git).

## Design et charte graphique

L'interface doit offrir un rendu **soigné et personnel**, clairement au-dessus du rendu par défaut des frameworks UI. Un template de base (Bootstrap, Material out-of-the-box, etc.) n'est pas acceptable.

La charte graphique, la présentation de chaque écran et leurs maquettes de référence sont décrites dans le dossier [`visuel/`](../visuel/) : [`charte-graphique.md`](../visuel/charte-graphique.md), puis un fichier par écran (ex. [`dashboard.md`](../visuel/dashboard.md)).

**Thème actif** : l'application propose plusieurs thèmes (`visuel/charte-graphique.md` § Thème de base). Le thème est **tiré au hasard** au début d'une **visite** (cf. `fonctionnel/mesure-audience.md` § Mesure d'audience de l'interface web, Visites), parmi les thèmes proposés, et conservé toute la durée de la visite, dans la consultation comme dans l'administration, que la visite soit relevée par la mesure d'audience ou non. Seule exception : une visite qui se prolonge au-delà de sa date calendaire (navigateur resté ouvert, notamment sur tablette ou smartphone) se poursuit, mais un nouveau thème est tiré pour chaque nouvelle date, dès la première page affichée à cette date ; la date est celle du fuseau horaire en cours du navigateur (cf. `fonctionnel/presentation.md` § Langue et culture d'affichage).

Le dossier `visuel/` est une **référence visuelle et ergonomique** (apparence, mise en page, maquettes) : il ne définit **pas le contenu** des écrans, qui relève de ce fichier. Un écart entre le contenu d'une maquette et les règles fonctionnelles n'est pas une contradiction : les règles fonctionnelles s'appliquent, la maquette n'illustrant qu'un rendu possible.

## Structure de l'application

L'application se compose de trois parties distinctes :

1. **Consultation** — accessible **publiquement** (sans authentification) :
   - Affichage d'une **fiche détaillée** pour les seuls **albums**, **séries** et **auteurs** : toutes les informations publiques disponibles y sont présentées. Les notes personnelles, la numérotation personnelle, les données d'acquisition (mode, date, prix, occasion, gratuité) et l'appréciation sont des informations **publiques**.
     - Les **éditions**, leurs **visuels** et les **intentions d'achat** sont présentés par la fiche de leur album : chaque édition avec toutes ses informations et ses visuels (avec accès à l'image originale), chaque intention avec l'album ou l'édition qu'elle vise. La liste publique des intentions mène à la fiche de l'album visé.
     - Les **éditeurs**, **collections éditeur**, **genres** et **univers** n'ont pas de fiche, ni d'entrée dans le menu de la consultation.
       - Un **éditeur** dont le **site web** est renseigné mène à ce site ; sinon, son nom n'est pas un lien. Une **collection éditeur**, sans site web, n'est pas un lien.
       - Un **genre** ou un **univers** mène à la liste de ses séries et de ses albums, présentée **comme la bibliographie d'un auteur** : séries et albums sans série au premier niveau, triés ensemble par ordre alphabétique, chaque série regroupant ses albums qui s'y rattachent, dans l'ordre de la série. Pour un univers, la liste comprend ses sous-univers (cf. `fonctionnel/regles-metier.md` § Hiérarchie des univers).
   - La fiche d'une **série** présente ses contributions template comme les **auteurs de la série**, et son éditeur et sa collection éditeur template comme son **éditeur** et sa **collection**. Ses autres valeurs template d'édition (catégorie, état, reliure, orientation, sens de lecture, format, couleur) et ses indicateurs **Exclure des manquants** et **Exclure des estimations de sortie**, paramètres de la saisie et des calculs, n'y figurent pas : ils ne décrivent pas la série.
   - La fiche d'un **auteur** présente sa **bibliographie complète**, en une seule liste qui réconcilie les **séries** et les **albums** auxquels il a participé, chacun avec le ou les **rôles** qu'il y a tenus. Elle ne reprend pas le détail de ces fiches, auxquelles elle mène.
     - Une **série** y figure dès que l'auteur est crédité sur la série ou sur l'un de ses albums ; elle regroupe les albums de la série auxquels il a participé, dans l'ordre de la série (cf. `fonctionnel/regles-metier.md` § Ordre des albums dans une série).
     - **Albums sans série** : ils forment, avec les séries, le **premier niveau** de la liste, triés ensemble par **ordre alphabétique** (cf. `fonctionnel/presentation.md` § Tri et navigation par initiale) ; une série y est visuellement distinguée d'un album.
   - **Navigation inter-entités** : depuis une fiche, il est possible de naviguer vers les fiches des entités associées, ou vers la liste d'un genre ou d'un univers (ex. album → série, album → auteur, auteur → bibliographie, album → liste d'un genre, etc.).
   - **Recherche facilement accessible** à tout moment depuis n'importe quelle page de la partie consultation.
   - **Recherche simple** : par type d'entité (ex. rechercher des albums, des auteurs, des séries, etc.).
   - **Recherche avancée** : recherche de n'importe quelle entité sur n'importe quelle combinaison de critères, portant sur les données du modèle métier et sur les liens entre entités, et rejeu des recherches enregistrées par l'administrateur (cf. `fonctionnel/recherche-avancee.md` § Recherche avancée).
   - **États et statistiques** : rapports et indicateurs sur la collection (à préciser), selon le niveau d'accès de chacun (cf. § Règles d'accès).
2. **Administration** — protégée par **authentification** :
   - CRUD sur toutes les entités, paramétrage de l'application, gestion des référentiels, gestion des recherches enregistrées (cf. `fonctionnel/recherche-avancee.md` § Recherche avancée), gestion des jetons d'accès de l'outil pour agents IA et suivi de son utilisation (cf. `fonctionnel/agents-ia.md` § Interface pour agents IA), mesure d'audience de l'interface web (cf. `fonctionnel/mesure-audience.md` § Mesure d'audience de l'interface web), etc.
   - **Fusion** de deux fiches d'un même type, proposée depuis la liste de leur type, en plus des opérations CRUD (ex. deux albums saisis en double). Ses modalités restent à définir : rien n'est statué sur ce que la fusion conserve de chaque fiche.
   - Les listes n'offrent pas, dans un premier temps, d'opération groupée (plusieurs fiches sélectionnées à la fois) ; l'idée n'est pas écartée, mais rien n'est statué (YAGNI).
   - La **saisie des données est manuelle**, mais assistée par des **imports depuis des sources externes** : APIs, extraction de données de sites web, etc. Cette aide à la saisie permet de **compléter une fiche** à partir de ces sources. Le **[catalogue général de la BnF](https://api.bnf.fr/fr/api-sru-catalogue-general)** (API SRU) en fait partie, comme **source d'information complémentaire** ; les autres sources restent à définir.
   - L'accès est protégé par un **compte administrateur unique** (login + mot de passe). Pas de gestion multi-utilisateurs.
3. **Aide contextuelle** — accessible à tout moment, depuis n'importe quelle page de l'application :
   - Affiche des informations d'aide **relatives à la page en cours** (aide sensible au contexte).
   - Le contenu peut être **riche** : texte, captures d'écran, tableaux, exemples, etc. — pas seulement de courts textes explicatifs.

## Règles d'accès

Ces règles sont **globales** : elles s'appliquent à l'interface web comme à l'interface pour agents IA, une fonctionnalité ayant les mêmes restrictions d'accès quelle que soit l'interface qui la propose. Seul le moyen de s'authentifier diffère : l'interface pour agents IA ne connaît que ses jetons d'accès, dont chacun donne le niveau public ou celui du compte administrateur (cf. `fonctionnel/agents-ia.md` § Interface pour agents IA, Accès par jeton).

- **Recherche et consultation** : publiques. L'authentification n'est pas requise, mais n'est pas interdite pour autant.
- **Analyses et statistiques** : deux niveaux d'accès, **public** et **privé** (réservé au compte administrateur). Le niveau est choisi **au cas par cas**, en fonction des données accédées et exposées par chaque analyse ou statistique.
- **Écriture de données** (saisie, modification, suppression, paramétrage) : réservée au **compte administrateur**, seul compte autorisé à modifier les données.

## Présentation des erreurs

Toute erreur présentée à l'utilisateur doit lui permettre de distinguer **très facilement**, au premier coup d'œil, s'il s'agit :

- d'une **erreur métier** : l'action demandée enfreint une règle de gestion de l'application (ex. une donnée obligatoire manquante, une incohérence entre deux champs, une intention d'achat déjà existante pour cet album). L'utilisateur peut la résoudre lui-même en corrigeant sa saisie ou sa demande ;
- d'une **erreur fonctionnelle** : la demande n'enfreint aucune règle de gestion et l'application fonctionne normalement, mais la demande ne peut pas aboutir en l'état des données au moment où elle est traitée. Cas type : l'enregistrement d'une fiche **modifiée entre-temps** (depuis un autre onglet ou un autre appareil), refusé pour ne pas écraser silencieusement cette autre modification. L'utilisateur peut la résoudre lui-même en tenant compte du nouvel état des données. Cette catégorie regroupe les erreurs de même nature ;
- d'une **erreur technique** : l'application n'a pas pu traiter la demande pour une raison indépendante de la saisie de l'utilisateur (dysfonctionnement, service indisponible, etc.).

Cette distinction s'applique à toutes les parties de l'application (consultation, administration, aide contextuelle).


<!-- À compléter : cas d'usage, user stories, autres règles métier, flux applicatifs, etc. -->
