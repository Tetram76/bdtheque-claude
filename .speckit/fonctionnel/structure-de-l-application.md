---
type: Règle métier
title: 'Structure de l''application'
description: 'Consultation publique (fiches, navigation, recherches, statistiques), administration authentifiée, aide contextuelle ; à lire pour tout écran ou endpoint.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Structure de l'application

L'application se compose de trois parties distinctes :

1. **Consultation** — accessible **publiquement** (sans authentification) :
   - Affichage d'une **fiche détaillée** pour les seuls **albums**, **séries** et **auteurs** : toutes les informations publiques disponibles y sont présentées. Les notes personnelles, la numérotation personnelle, les données d'acquisition (mode, date, prix, occasion, gratuité) et l'appréciation sont des informations **publiques**.
     - Les **éditions**, leurs **visuels** et les **intentions d'achat** sont présentés par la fiche de leur album : chaque édition avec toutes ses informations et ses visuels (avec accès à l'image originale), chaque intention avec l'album ou l'édition qu'elle vise. La liste publique des intentions mène à la fiche de l'album visé.
     - Les **éditeurs**, **collections éditeur**, **genres** et **univers** n'ont pas de fiche, ni d'entrée dans le menu de la consultation.
       - Un **éditeur** dont le **site web** est renseigné mène à ce site ; sinon, son nom n'est pas un lien. Une **collection éditeur**, sans site web, n'est pas un lien.
       - Un **genre** ou un **univers** mène à la liste de ses séries et de ses albums, présentée **comme la bibliographie d'un auteur** : séries et albums sans série au premier niveau, triés ensemble par ordre alphabétique, chaque série regroupant ses albums qui s'y rattachent, dans l'ordre de la série. Pour un univers, la liste comprend ses sous-univers (cf. [Hiérarchie des univers](regles-metier/hierarchie-des-univers.md)).
   - La fiche d'une **série** présente ses contributions template comme les **auteurs de la série**, et son éditeur et sa collection éditeur template comme son **éditeur** et sa **collection**. Ses autres valeurs template d'édition (catégorie, état, reliure, orientation, sens de lecture, format, couleur) et ses indicateurs **Exclure des manquants** et **Exclure des estimations de sortie**, paramètres de la saisie et des calculs, n'y figurent pas : ils ne décrivent pas la série.
   - La fiche d'un **auteur** présente sa **bibliographie complète**, en une seule liste qui réconcilie les **séries** et les **albums** auxquels il a participé, chacun avec le ou les **rôles** qu'il y a tenus. Elle ne reprend pas le détail de ces fiches, auxquelles elle mène.
     - Une **série** y figure dès que l'auteur est crédité sur la série ou sur l'un de ses albums ; elle regroupe les albums de la série auxquels il a participé, dans l'ordre de la série (cf. [Ordre des albums dans une série](regles-metier/ordre-des-albums-dans-une-serie.md)).
     - **Albums sans série** : ils forment, avec les séries, le **premier niveau** de la liste, triés ensemble par **ordre alphabétique** (cf. [Tri et navigation par initiale](presentation/tri-et-navigation-par-initiale.md)) ; une série y est visuellement distinguée d'un album.
   - **Navigation inter-entités** : depuis une fiche, il est possible de naviguer vers les fiches des entités associées, ou vers la liste d'un genre ou d'un univers (ex. album → série, album → auteur, auteur → bibliographie, album → liste d'un genre, etc.).
   - **Recherche facilement accessible** à tout moment depuis n'importe quelle page de la partie consultation.
   - **Recherche simple** : par type d'entité (ex. rechercher des albums, des auteurs, des séries, etc.).
   - **Recherche avancée** : recherche de n'importe quelle entité sur n'importe quelle combinaison de critères, portant sur les données du modèle métier et sur les liens entre entités, et rejeu des recherches enregistrées par l'administrateur (cf. [Recherche avancée](recherche-avancee/recherche-avancee.md)).
   - **États et statistiques** : rapports et indicateurs sur la collection (à préciser), selon le niveau d'accès de chacun (cf. [Règles d'accès](regles-d-acces.md)).
2. **Administration** — protégée par **authentification** :
   - CRUD sur toutes les entités, paramétrage de l'application, gestion des référentiels, gestion des recherches enregistrées (cf. [Recherche avancée](recherche-avancee/recherche-avancee.md)), gestion des jetons d'accès de l'outil pour agents IA et suivi de son utilisation (cf. [Interface pour agents IA](agents-ia/interface-pour-agents-ia.md)), mesure d'audience de l'interface web (cf. [Mesure d'audience de l'interface web](mesure-audience.md)), etc.
   - **Fusion** de deux fiches d'un même type, proposée depuis la liste de leur type, en plus des opérations CRUD (ex. deux albums saisis en double). Ses modalités restent à définir : rien n'est statué sur ce que la fusion conserve de chaque fiche.
   - Les listes n'offrent pas, dans un premier temps, d'opération groupée (plusieurs fiches sélectionnées à la fois) ; l'idée n'est pas écartée, mais rien n'est statué (YAGNI).
   - La **saisie des données est manuelle**, mais assistée par des **imports depuis des sources externes** : APIs, extraction de données de sites web, etc. Cette aide à la saisie permet de **compléter une fiche** à partir de ces sources. Le **[catalogue général de la BnF](https://api.bnf.fr/fr/api-sru-catalogue-general)** (API SRU) en fait partie, comme **source d'information complémentaire** ; les autres sources restent à définir.
   - L'accès est protégé par un **compte administrateur unique** (login + mot de passe). Pas de gestion multi-utilisateurs.
3. **Aide contextuelle** — accessible à tout moment, depuis n'importe quelle page de l'application :
   - Affiche des informations d'aide **relatives à la page en cours** (aide sensible au contexte).
   - Le contenu peut être **riche** : texte, captures d'écran, tableaux, exemples, etc. — pas seulement de courts textes explicatifs.
