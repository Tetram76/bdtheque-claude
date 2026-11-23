---
type: Plan d'implémentation
title: 'Phase 3 — Frontend Blazor'
description: 'Phase 3, frontend Blazor : partie 1 (v0.1.0) et partie 2, consultation et écrans restants de l''administration (v0.2.0), et leurs chantiers ; objet des parties suivantes (recherche avancée, documentation utilisateur, reste éventuel), première version livrable à leur issue, et leurs notes.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-22T20:40:00Z }
---

# Phase 3 — Frontend Blazor

Pages de consultation (public) et d'administration (authentifié), composants partagés, aide contextuelle. Le rendu repose sur un thème de base neutre, qui définit les contrats des thèmes ; aucune alternative des maquettes n'en sert de base ([Thème de base](../visuel/charte-graphique/theme-de-base.md)).

La phase est réalisée en quatre parties, ou cinq si nécessaire, découpées l'une après l'autre ; les parties 1 et 2 le sont à ce stade. À l'issue de la partie 4, ou de la partie 5 si elle a lieu, la **première version de l'application est livrable** (décision de l'utilisateur).

- **Pages non implémentées** : tant qu'une page n'est pas implémentée, tout lien qui y mène affiche une page « Pas encore implémenté » (#238). Le chantier qui réalise une page remplace par le vrai lien tous les liens qui y mènent.
- **Validation fonctionnelle** : les chantiers qui réalisent ce que l'utilisateur voit et utilise sont validés par lui avant fusion ([Validation fonctionnelle](../gestion-projet/revue-tickets/validation-fonctionnelle.md)), avec l'environnement de validation (#214), réalisé avant le premier d'entre eux ; chaque ticket précise s'il y est soumis.
- **Thèmes** : la partie 1 réalise le thème de base et trois thèmes. « La planche encrée » et « La gazette » sont retenus pour s'opposer sur presque tout (blocs encadrés et ouverts, une couleur par bloc et une seule surface, bords dessinés et filets, titre en bulle et mention au-dessus du titre, désordre ponctuel et sobriété), pour que les contrats du thème de base couvrent ces extrêmes. « La couverture du jour » l'est pour ses couleurs, tirées chaque jour de la couverture de l'édition à la une, que les contrats accueillent sans les connaître d'avance ; il est réalisé avant les écrans qui restent, écrits ainsi sur des contrats qui le couvrent déjà. Un thème ajouté après des écrans déjà réalisés, dans la partie 1 comme dans une partie ultérieure, est décliné sur chacun d'eux.

## Partie 1 — Éléments de base

**Version** : `v0.1.0`, avec les ajustements des phases 0 à 2 qui suivent la consolidation de la Phase 2.

Socle du frontend, environnement de validation fonctionnelle, thème de base et trois thèmes, et les écrans qui ont une maquette : dashboard (collection vide comprise), accès à l'administration, listes de l'administration, fiches de saisie et fiches de consultation. Le chantier #161 (accès du frontend au volume des visuels) est tranché avant tout écran qui affiche un visuel ; #185 et #186 précèdent les écrans qui en dépendent. Le socle du frontend (ticket parent #191) est réalisé par trois sous-tickets, #238 à #240 ; le thème de base neutre, les contrats des thèmes et le thème de la visite (ticket parent #194), par cinq sous-tickets, #254 à #258 ; les listes des séries, des auteurs et des éditeurs de l'administration (ticket parent #201), par trois sous-tickets, #301 à #303 ; le thème « La couverture du jour » (ticket parent #309), par deux sous-tickets, les couleurs des couvertures (#311) puis le thème lui-même (#312) ; la fiche d'un album dans la consultation (ticket parent #210), par trois sous-tickets, la structure de la fiche et les éléments que réutilisent les fiches d'une série et d'un auteur (#321), puis les visuels et les éditions (#322), enfin les intentions d'acquisition et le reste de la série (#323) ; les listes des éditions et des intentions d'acquisition de l'administration (ticket parent #202), par deux sous-tickets, la liste des éditions (#333) puis celle des intentions d'acquisition (#334) ; la saisie d'une édition, de ses visuels et d'une intention d'acquisition (ticket parent #209), par deux sous-tickets, la fiche de saisie d'une édition et d'une intention d'acquisition (#361) puis ses visuels (#362).

| Ticket | Titre |
| --- | --- |
| ~~#161~~ | Accès du frontend au volume des visuels |
| ~~#238~~ | Structure du frontend, page « Pas encore implémenté » et outillage de non-régression du frontend |
| ~~#239~~ | Appels à l'API, présentation des erreurs et localisation du frontend |
| ~~#240~~ | Fuseau horaire du navigateur et alignement de la recherche avancée |
| ~~#214~~ | Environnement de validation fonctionnelle |
| ~~#192~~ | Données du dashboard dans l'API avant la Phase 4 |
| ~~#193~~ | Paramétrage de l'application dans le modèle de domaine et l'API |
| ~~#254~~ | Contrats des thèmes, thème de base neutre et captures de référence des écrans |
| ~~#255~~ | Composants de présentation : blocs, boutons, étiquettes, couvertures et mention « Non renseigné » |
| ~~#256~~ | Contrôles de saisie et composants interactifs : champs, choix d'une fiche, menu « ⋯ », dialogue, barre d'enregistrement |
| ~~#257~~ | En-têtes, menus et navigation : consultation, administration, fil d'Ariane, navigation par initiale, pagination |
| ~~#258~~ | Tirage du thème de la visite |
| ~~#195~~ | Thème « La planche encrée » |
| ~~#196~~ | Thème « La gazette » |
| ~~#197~~ | Mesure d'audience dans l'interface web : relevé, visites, information et opposition |
| ~~#198~~ | Dashboard et écran d'accueil d'une collection vide |
| ~~#199~~ | Connexion et page d'accueil de l'administration |
| ~~#200~~ | Liste des albums de l'administration |
| ~~#301~~ | Liste des séries de l'administration |
| ~~#311~~ | Couleurs des couvertures : dominante, profonde et accent de chaque visuel |
| ~~#312~~ | Thème « La couverture du jour » : identité, déclinaison et captures |
| ~~#302~~ | Liste des auteurs de l'administration |
| ~~#303~~ | Liste des éditeurs de l'administration |
| ~~#333~~ | Liste des éditions de l'administration |
| ~~#334~~ | Liste des intentions d'acquisition de l'administration |
| ~~#203~~ | Genres et univers : listes et saisie de l'administration |
| ~~#204~~ | Page des éditions et intentions d'acquisition d'un album |
| ~~#205~~ | Saisie d'un album |
| ~~#206~~ | Saisie d'une série |
| ~~#207~~ | Saisie d'un auteur |
| ~~#208~~ | Saisie d'un éditeur et de ses collections |
| ~~#361~~ | Saisie d'une édition et d'une intention d'acquisition |
| ~~#362~~ | Visuels d'une édition sur sa fiche de saisie |
| ~~#321~~ | Fiche d'un album : structure, identité, contributions, genres et univers, résumé |
| ~~#322~~ | Fiche d'un album : visuels de l'édition, éditions de la collection et détail de l'édition choisie |
| ~~#323~~ | Fiche d'un album : intentions d'acquisition et reste de la série |
| ~~#211~~ | Fiche d'une série dans la consultation |
| ~~#212~~ | Fiche d'un auteur dans la consultation |

## Partie 2 — Consultation

**Version** : `v0.2.0`.

Les fonctions de la consultation, recherche avancée exceptée ([Structure de l'application](../fonctionnel/structure-de-l-application.md)), et les écrans de l'administration qui restent à réaliser, fusion de deux fiches comprise (décision de l'utilisateur). Objectif : à son issue, l'application est **fonctionnellement complète**, hors recherche avancée et hors recherche d'une édition par son ISBN dans les sources externes, reportée à la partie 5 (décision de l'utilisateur).

La liste des albums de la consultation (#603) est réalisée la première : elle fixe le modèle des listes de la consultation et le filtre qui limite la consultation à la collection, encore à préciser avec l'utilisateur ([Périmètre de la consultation](../fonctionnel/regles-metier/perimetre-de-la-consultation.md)). Les listes des séries, des auteurs et des intentions d'acquisition (#604 à #606), et celle d'un genre ou d'un univers (#607), la suivent ; la recherche simple (#608), après les listes des albums, des séries et des auteurs. Les modalités de la fusion (#614), que rien ne statue encore, sont définies avec l'utilisateur au démarrage de son chantier. Chaque chantier de la partie est soumis à la validation fonctionnelle.

| Ticket | Titre |
| --- | --- |
| #603 | Liste des albums de la consultation |
| #604 | Liste des séries de la consultation |
| #605 | Liste des auteurs de la consultation |
| #606 | Liste publique des intentions d'acquisition |
| #607 | Séries et albums d'un genre ou d'un univers dans la consultation |
| #608 | Recherche simple de la consultation |
| ~~#609~~ | Recherche d'une fiche à modifier depuis l'en-tête de l'administration |
| ~~#610~~ | Écran du paramétrage de l'application |
| #611 | Indicateurs de la mesure d'audience dans l'administration |
| #612 | Confirmation d'une acquisition et conversion d'une intention d'acquisition |
| #613 | Éditions dont l'ISBN est à vérifier |
| #614 | Fusion de deux fiches d'un même type |

## Parties suivantes

L'objet de chaque partie est fixé (décision de l'utilisateur) ; son contenu détaillé et sa version sont établis à son découpage, qui intègre les notes ci-dessous qui la concernent.

- **Partie 3 — Recherche avancée** : les écrans de la recherche avancée (note ci-dessous).
- **Partie 4 — Documentation utilisateur** :
  - rédaction des pages nécessaires ;
  - liens vers elles dans l'application (aide contextuelle, [Structure de l'application](../fonctionnel/structure-de-l-application.md)) ;
  - harnais « documentation utilisateur », hebdomadaire ;
  - règle du `.speckit/` : tout ajout ou toute modification d'une fonction visible dans l'application entraîne la création ou la modification de la documentation utilisateur correspondante.
- **Partie 5 — Le reste, si nécessaire** : ce qui manquerait encore à la première version livrable, à inventorier, dont déjà la recherche d'une édition par son ISBN dans les sources externes, qui crée ou complète les fiches à partir d'elles ([Contenu](../visuel/administration/contenu.md), « Saisir à partir d'un ISBN » ; [Structure de l'application](../fonctionnel/structure-de-l-application.md)) (décision de l'utilisateur).

- **Note — contraste des thèmes** : le texte courant du thème « fanzine en risographie » (bleu `#0078BF` sur papier `#F4EFE4`, [`visuel/dashboard/`](../visuel/dashboard/index.md)) n'atteint que 4,1:1, sous les 4,5:1 attendus pour du texte courant. La couleur relevant de l'identité du thème, sa correction est soumise à l'utilisateur lors du découpage de la partie qui le réalise, avec la vérification des contrastes des thèmes qu'elle ajoute.
- **Note — style des contrôles** : dans chaque thème, le texte indicatif (*placeholder*) des champs garde un contraste d'au moins 4,5:1, et l'indicateur de focus clavier contraste à la fois avec le contrôle et avec la surface qui l'entoure, y compris sur un bouton à fond coloré. Les maquettes de l'administration (`visuel/maquettes/administration/`) ne respectent pas ces deux points (texte indicatif atténué par une opacité, contour de focus dans la couleur du texte du contrôle). Le style des contrôles relève des contrats du thème de base (#194, réalisé par #256), que tout thème ajouté respecte.
- **Note — paramétrage de l'application** : le modèle de domaine et l'API du paramétrage sont réalisés par la partie 1 (#193), dont la page d'accueil de l'administration présente les valeurs ; l'écran qui le modifie est réalisé par la partie 2 (#610).
- **Note — mesure d'audience de l'interface web** : la partie 1 réalise le relevé, les visites, l'information des visiteurs et leur opposition (#197), et chaque écran ajoute ses fonctions à la liste des fonctions suivies ; l'écran de l'administration qui présente les indicateurs est réalisé par la partie 2 (#611).
- **Note — recherche avancée** : les écrans de la recherche avancée ([Recherche avancée](../fonctionnel/recherche-avancee/recherche-avancee.md)) sont intégrés à cette phase, dont ils sont l'**objet de la partie 3** (décision de l'utilisateur) : formulaire de construction des critères, utilisable sans aucune connaissance de SQL et qui met en évidence le chemin suivi entre entités, y compris vers l'autre donnée d'une comparaison entre deux données ([Recherche avancée : mise en œuvre](../choix-implementation/recherche-avancee/recherche-avancee-mise-en-oeuvre.md)), rejeu des recherches enregistrées avec la saisie de leurs variables, et page d'administration des recherches enregistrées.
