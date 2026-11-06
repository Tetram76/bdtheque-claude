---
type: Plan d'implémentation
title: 'Phase 3 — Frontend Blazor'
description: 'Phase 3, frontend Blazor : partie 1 (v0.1.0) et ses chantiers, notes des parties suivantes.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Phase 3 — Frontend Blazor

Pages de consultation (public) et d'administration (authentifié), composants partagés, aide contextuelle. Le rendu repose sur un thème de base neutre, qui définit les contrats des thèmes ; aucune alternative des maquettes n'en sert de base ([Thème de base](/visuel/charte-graphique/theme-de-base.md)).

La phase est réalisée en plusieurs parties, découpées l'une après l'autre ; seule la partie 1 l'est à ce stade.

- **Pages non implémentées** : tant qu'une page n'est pas implémentée, tout lien qui y mène affiche une page « Pas encore implémenté » (#238). Le chantier qui réalise une page remplace par le vrai lien tous les liens qui y mènent.
- **Validation fonctionnelle** : les chantiers qui réalisent ce que l'utilisateur voit et utilise sont validés par lui avant fusion ([Validation fonctionnelle](/gestion-projet/revue-tickets/validation-fonctionnelle.md)), avec l'environnement de validation (#214), réalisé avant le premier d'entre eux ; chaque ticket précise s'il y est soumis.
- **Thèmes** : la partie 1 réalise le thème de base et deux thèmes, « La planche encrée » et « La gazette », retenus pour s'opposer sur presque tout (blocs encadrés et ouverts, une couleur par bloc et une seule surface, bords dessinés et filets, titre en bulle et mention au-dessus du titre, désordre ponctuel et sobriété), pour que les contrats du thème de base couvrent ces extrêmes. Un thème ajouté par une partie ultérieure est décliné sur tous les écrans déjà réalisés.

## Partie 1 — Éléments de base

**Version** : `v0.1.0`, avec les ajustements des phases 0 à 2 qui suivent la consolidation de la Phase 2.

Socle du frontend, environnement de validation fonctionnelle, thème de base et deux thèmes, et les écrans qui ont une maquette : dashboard (collection vide comprise), accès à l'administration, listes de l'administration, fiches de saisie et fiches de consultation. Le chantier #161 (accès du frontend au volume des visuels) est tranché avant tout écran qui affiche un visuel ; #185 et #186 précèdent les écrans qui en dépendent. Le socle du frontend (ticket parent #191) est réalisé par trois sous-tickets, #238 à #240 ; le thème de base neutre, les contrats des thèmes et le thème de la visite (ticket parent #194), par cinq sous-tickets, #254 à #258.

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
| #200 | Liste des albums de l'administration |
| #201 | Listes des séries, des auteurs et des éditeurs de l'administration |
| #202 | Listes des éditions et des intentions d'achat de l'administration |
| #203 | Genres et univers : listes et saisie de l'administration |
| #204 | Page des éditions et intentions d'achat d'un album |
| #205 | Saisie d'un album |
| #206 | Saisie d'une série |
| #207 | Saisie d'un auteur |
| #208 | Saisie d'un éditeur et de ses collections |
| #209 | Saisie d'une édition, de ses visuels et d'une intention d'achat |
| #210 | Fiche d'un album dans la consultation |
| #211 | Fiche d'une série dans la consultation |
| #212 | Fiche d'un auteur dans la consultation |

## Parties suivantes

Contenu, nombre et version à établir au découpage de chacune.

- **Note — contraste des thèmes** : le texte courant du thème « fanzine en risographie » (bleu `#0078BF` sur papier `#F4EFE4`, [`visuel/dashboard/`](/visuel/dashboard/index.md)) n'atteint que 4,1:1, sous les 4,5:1 attendus pour du texte courant. La couleur relevant de l'identité du thème, sa correction est soumise à l'utilisateur lors du découpage de la partie qui le réalise, avec la vérification des contrastes des thèmes qu'elle ajoute.
- **Note — style des contrôles** : dans chaque thème, le texte indicatif (*placeholder*) des champs garde un contraste d'au moins 4,5:1, et l'indicateur de focus clavier contraste à la fois avec le contrôle et avec la surface qui l'entoure, y compris sur un bouton à fond coloré. Les maquettes de l'administration (`visuel/maquettes/administration/`) ne respectent pas ces deux points (texte indicatif atténué par une opacité, contour de focus dans la couleur du texte du contrôle). Le style des contrôles relève des contrats du thème de base (#194, réalisé par #256), que tout thème ajouté respecte.
- **Note — paramétrage de l'application** : le modèle de domaine et l'API du paramétrage sont réalisés par la partie 1 (#193), dont la page d'accueil de l'administration présente les valeurs ; l'écran qui le modifie reste à réaliser.
- **Note — mesure d'audience de l'interface web** : la partie 1 réalise le relevé, les visites, l'information des visiteurs et leur opposition (#197), et chaque écran ajoute ses fonctions à la liste des fonctions suivies ; l'écran de l'administration qui présente les indicateurs reste à réaliser.
- **Note — recherche avancée** : les écrans de la recherche avancée ([Recherche avancée](/fonctionnel/recherche-avancee/recherche-avancee.md)) sont intégrés à cette phase : formulaire de construction des critères, utilisable sans aucune connaissance de SQL et qui met en évidence le chemin suivi entre entités, y compris vers l'autre donnée d'une comparaison entre deux données ([Recherche avancée : mise en œuvre](/choix-implementation/recherche-avancee/recherche-avancee-mise-en-oeuvre.md)), rejeu des recherches enregistrées avec la saisie de leurs variables, et page d'administration des recherches enregistrées.
