# Choix d'Implémentation

* [Outil pour agents IA](agents-ia.md) - Outil pour agents IA (Phase 6) : MCP, conteneur mcp, outils, jetons d'accès et de session, relevé de l'utilisation, archivage et réarmement.
* [API](api/index.md) - Organisation de l'API, navigation par initiale et recherche simple.
* [Déploiement](deploiement/index.md) - Images de base des conteneurs, frontend derrière le reverse proxy du NAS, plusieurs versions sur le NAS, publication des images Docker, version de PostgreSQL, application du schéma au démarrage et environnement de validation fonctionnelle.
* [Règles du domaine](domaine/index.md) - Intention d'achat, valeur d'une édition, devises, journal des modifications, paramétrage de l'application, édition « À la une », validation de l'ISBN et visuels.
* [Erreurs et suppressions](ecritures/index.md) - Erreurs métier, fonctionnelles et techniques, et suppression des entités.
* [Choix d'Implémentation](perimetre.md) - Ce que recense choix-implementation/ : choix de l'agent, leur raison et les alternatives écartées, rien de ce que le code montre ; remis en cause s'il le faut.
* [Organisation du code](organisation-du-code.md) - Projets de la solution et leur rôle, projets de tests ; à lire pour ajouter un projet ou placer du code.
* [Versions et bibliothèques](versions-et-bibliotheques.md) - .NET et EF Core 10 LTS, montée suivante vers .NET 12, Npgsql, ML.NET pour l'estimation.
* [Mesure d'audience](mesure-audience/index.md) - Mise en œuvre de la mesure d'audience de l'interface web : enregistrement des visites et des utilisations, compteurs mensuels, conservation et lecture des indicateurs, relevé par le frontend (visite, écrans et actions, appareil, information et opposition).
* [Outillage de développement](outillage.md) - Outillage .NET : packages centralisés, xunit, Testcontainers, bUnit, Playwright, couverture, commentaires XML, CI ; à lire pour tout test ou build.
* [Persistance](persistance/index.md) - Conventions EF Core, identifiants, cohérence entre tables, collation, migrations, concurrence d'accès, dates renseignées automatiquement et collections de navigation du domaine.
* [Recherche avancée](recherche-avancee/index.md) - Mise en œuvre de la recherche avancée et des recherches enregistrées.
* [Reprise des données](reprise-donnees/index.md) - Accès à la base existante (Firebird) et reprise des prix antérieurs à l'euro.
* [Socle applicatif](socle/index.md)
