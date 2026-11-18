---
type: Référence visuelle
title: 'Liste par liste'
description: 'Visuel, entrée, colonnes et filtres de chaque liste de l''administration.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-18T11:00:00Z }
---

# Liste par liste

| Liste | Visuel | Entrée | Colonnes | Recherche, navigation, filtres, ordre |
| --- | --- | --- | --- | --- |
| Séries | petite pile de trois couvertures, comme en consultation | titre | statut, albums et albums dans la collection, en une phrase : « 7 albums dans la collection » quand tous y sont, « 9 albums dont 4 dans la collection » quand une partie y est, « 9 albums » quand aucun n'y est (décision de l'utilisateur, #416), « Complète » ou « À compléter » (colonne « Avancement », décision de l'utilisateur, #425), modification | titre ; navigation par initiale ; filtres auteur, éditeur, collection (« Collection »), genre, univers ; par titre |
| Éditions | vignette de couverture (visuel générique sans couverture) | libellé de l'édition, et l'album dessous | année, catégorie, acquisition (mode et année), modification ; seules les éditions de la collection, une édition visée figurant parmi les intentions d'achat ([Intention d'achat](../../fonctionnel/regles-metier/intention-d-achat.md)) | ISBN ; filtres série, album, éditeur, collection (« Collection ») ; dans l'ordre des albums, puis par année |
| Intentions d'achat | vignette de couverture de l'album | libellé de l'album visé | ce qu'elle vise, distingué au premier coup d'œil par une étiquette : **Album**, pleine, de la couleur d'accent, suivie de « toute édition » en texte atténué, ou **Édition**, en contour, suivie du libellé de l'édition en gras, chacune avec sa propre icône ; possession de l'album (colonne « Possession »), « Possédé » ou « Intention d'achat », étiquettes de la liste des albums ([États d'un album](../../fonctionnel/regles-metier/appartenance-a-la-collection.md#états-dun-album)) ; date | titre de l'album ou de sa série ; dans l'ordre des albums |
| Auteurs | monogramme | identifiant principal, et le prénom et le nom dessous sous un pseudonyme | nationalité, nombre d'albums crédités, rôles tenus, modification | nom, prénom ou pseudonyme ; navigation par initiale ; par clé de tri |
| Éditeurs | — | nom, et ses collections dessous | site web, lien qui fait sortir de l'application ([Liens qui font sortir de l'application](../charte-graphique/liens-qui-font-sortir-de-l-application.md)), sur une seule ligne quelle que soit la longueur de l'adresse : présentée sans `https://` ni `www.`, l'adresse se termine par des points de suspension quand elle ne tient pas dans la colonne (« example.com/planche-n… »), et reste accessible en entier, en infobulle et par le lien (décision de l'utilisateur) ; nombre de collections, nombre d'éditions, modification | nom ; par nom |
| Genres | — | libellé | nombre de séries, nombre d'albums, modification | libellé ; par libellé |
| Univers | — | nom, en retrait sous son parent, chaque univers suivi de ses sous-univers | nombre de séries et d'albums, sous-univers compris, nombre de sous-univers, modification | nom ; selon la hiérarchie |
