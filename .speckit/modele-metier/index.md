# Modèle Métier

* [Domaine](domaine.md) - Le domaine : une collection de bandes dessinées.
* [Glossaire des entités](glossaire-des-entites.md) - Définition de chaque entité du domaine ; point d'entrée du modèle.
* [Relations et cardinalités](relations-et-cardinalites.md) - Relations et cardinalités de chaque entité, contraintes des intentions d'achat, du suivi de l'outil et des visites.
* [Schéma des relations](schema-des-relations.md) - Diagramme des relations, collection utilisateur, contrainte des contributions.

## Attributs des entités

* [Attributs communs à toutes les entités](attributs-communs-a-toutes-les-entites.md) - Dates de création et de dernière modification, renseignées automatiquement.
* [Album](album.md) - Attributs d'un album : titre, clé de tri, type, hors-série, tomes, date de première publication, résumé, notes, appréciation.
* [Série](serie.md) - Attributs d'une série : titre, clé de tri, statut, tomes prévus, indicateurs, valeurs template.
* [Édition](edition.md) - Attributs d'une édition : publication, fabrication, acquisition, montants, gratuité, valeurs proposées, contraintes d'intégrité.
* [Auteur / Artiste](auteur-artiste.md) - Attributs d'un auteur, au moins un nom ou un pseudonyme.
* [Contribution *(relation Album ↔ Auteur/Artiste)*](contribution-relation-album-auteur-artiste.md) - Rôle et artiste d'une contribution.
* [Éditeur](editeur.md) - Attributs d'un éditeur, nom unique sans casse ni accents.
* [Collection éditeur](collection-editeur.md) - Nom d'une collection éditeur, unique chez son éditeur sans casse ni accents.
* [Genre](genre.md) - Libellé d'un genre, unique sans casse ni accents.
* [Univers](univers.md) - Nom et description d'un univers, nom unique sous son parent sans casse ni accents.
* [Visuel d'édition](visuel-d-edition.md) - Type, fichier, ordre et couleurs d'un visuel d'édition.
* [Intention d'achat](intention-d-achat.md) - Cible album ou édition d'une intention d'achat et ses contraintes.
* [Recherche enregistrée](recherche-enregistree.md) - Nom, entité, critères et variables d'une recherche enregistrée.
* [Paramétrage de l'application](parametrage-de-l-application.md) - Format du libellé des albums, culture d'affichage ; fiche unique.
* [Jeton d'accès](jeton-d-acces.md) - Attributs d'un jeton d'accès de l'outil pour agents IA (Phase 6).
* [Session de l'outil pour agents IA](session-de-l-outil-pour-agents-ia.md) - Ouverture et expiration d'une session de l'outil (Phase 6).
* [Objectif de session](objectif-de-session.md) - Texte et date d'un objectif de session (Phase 6).
* [Utilisation de l'outil](utilisation-de-l-outil.md) - Trace d'un appel de l'outil pour agents IA (Phase 6).
* [Retour écrit](retour-ecrit.md) - Limite signalée par un agent (Phase 6).
* [Visite de l'interface web](visite-de-l-interface-web.md) - Attributs d'une visite de la mesure d'audience.
* [Utilisation d'une fonction](utilisation-d-une-fonction.md) - Attributs d'une utilisation de la mesure d'audience.
* [Compteur d'audience mensuel](compteur-d-audience-mensuel.md) - Attributs et recomposition des compteurs d'audience mensuels.
* [Entrée du journal des modifications](entree-du-journal-des-modifications.md) - Attributs d'une entrée du journal des modifications et ses règles.
