---
type: Choix d'implémentation
title: 'Environnement de validation fonctionnelle'
description: 'Version d''une PR lancée sur le poste en une commande, images construites localement, données de validation fictives, édition à la une laissée au tirage ou, sur demande, couverture pâle.'
applies_to:
  - 'scripts/Start-Validation.ps1'
  - 'scripts/Stop-Validation.ps1'
  - 'docker/validation/**'
  - 'docker/validation-data/**'
  - 'src/Bdtheque.ValidationData/**'
  - 'tests/Bdtheque.ValidationData.Tests/**'
  - 'src/Bdtheque.Frontend/Theming/ChosenThemeSelector.cs'
  - 'src/Bdtheque.Frontend/Hosting/**'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-10T20:26:20Z }
---

# Environnement de validation fonctionnelle

- **La version d'une PR, lancée sur le poste en une commande** ([Validation fonctionnelle](../../gestion-projet/revue-tickets/validation-fonctionnelle.md)), qui n'exige que PowerShell, git et un service Docker : ni SDK .NET, ni fichier à éditer, ni secret à générer. Marche à suivre : `README.dev.md`.
- **Images construites sur le poste**, avec les Dockerfiles de production, depuis les sources du commit de tête de la PR extraites hors de la copie de travail : ni les modifications locales ni la branche courante n'entrent dans ce qui est validé. Quelques minutes au premier lancement, les suivants reprenant le cache, et seules des images de base épinglées sont téléchargées.
  - **Alternative écartée (images de la PR publiées sur Docker Hub)** : chaque validation consommerait les quotas de Docker Hub et de l'API GitHub ([Intégration continue (CI)](../../gestion-projet/depot/integration-continue-ci.md)), les dépôts privés exigeraient un jeton sur le poste, et un workflow de plus devrait supprimer les tags de chaque PR à sa fermeture ([Déploiement](../../contraintes-techniques/architecture-deploiement/deploiement.md)). Un coût sans justification pour éviter quelques minutes de construction.
- **Un projet Compose distinct**, surchargeant celui de production sans le modifier, avec des valeurs fixes et fictives : ses conteneurs, réseaux, volumes et images locales ne touchent ni un déploiement local ni les images publiées, et ses ports ne s'ouvrent qu'au poste lui-même.
- **Environnement ASP.NET Core `Validation`**, qui se comporte comme la production, avec deux ajouts : la documentation de l'API ([Documentation de l'API](../socle/transverse/documentation-de-l-api.md)), et le **thème choisi par l'adresse**, qui devient celui de la visite, pour juger chaque thème sans attendre qu'il soit tiré ([Design et charte graphique](../../fonctionnel/design-et-charte-graphique.md)).
  - **Alternative écartée (environnement `Development`)** : il change d'autres comportements (page d'exception du développeur, liaison des requêtes, validation des services), que l'utilisateur validerait à tort pour ceux de la production.
- **Données de validation fictives, chargées par l'API d'administration**, comme le ferait un client : chaque règle de l'application s'y applique, le journal des modifications les consigne, et elles suivent le schéma sans rien à tenir aligné. Elles **évoluent avec les écrans** : le chantier d'un écran y ajoute les cas qu'il présente. Leur chargement est vérifié par les tests : une évolution de l'API qui les refuserait fait échouer le CI.
  - **Alternative écartée (sauvegarde SQL restaurée)** : à régénérer à chaque évolution du schéma, et écrite hors du domaine, sans ses règles ni le journal.
- **Édition à la une laissée au tirage de l'application** : le chargement n'en met aucune à la une ; le premier affichage de la journée la tire parmi toutes les éditions possédées des données, comme en production ([Page d'accueil](../../fonctionnel/page-accueil.md)). Les données comptent une couverture pâle, à l'accent de ton moyen, cas le plus exigeant du thème « La couverture du jour » ([La couverture du jour](../../visuel/dashboard/la-couverture-du-jour.md)), qu'un tirage au hasard ne présenterait qu'une fois sur trente : une option du lancement la met à la une des jours qui entourent le chargement, en temps universel, qui couvrent la date du navigateur quel que soit son fuseau, pour juger un écran dans ce thème sur ce cas. La lisibilité d'une couverture pâle reste vérifiée par les tests du thème, indépendamment des données ([Thèmes](../socle/themes.md)).
  - **Alternative écartée (couverture pâle à la une à chaque lancement)** : chaque lancement repartant d'une base vide le jour de la validation, celle-ci ne montrait jamais le tirage réel, ni le thème dans d'autres couleurs : chaque écran se jugeait sur un seul jeu de couleurs, qui n'est pas celui que la production présente le plus souvent.
- **Chaque lancement repart d'une base vide**, sauf demande contraire : rien ne passe d'une validation à l'autre.
- **Compte administrateur fixe et fictif**, affiché au lancement : la validation de l'administration ne demande aucun secret ([Authentification : mise en œuvre](../socle/transverse/authentification-mise-en-oeuvre.md)).
