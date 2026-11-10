# Déploiement

* [Images de base des conteneurs](images-de-base-des-conteneurs.md) - Images de base épinglées, aspnet chiseled-extra, sonde de santé, contenu publié, connexion à db.
* [Frontend derrière le reverse proxy du NAS](frontend-derriere-le-reverse-proxy-du-nas.md) - Une image pour le reverse proxy et l'accès interne en HTTP, adresse publique déclarée, cookies selon la requête.
* [Plusieurs versions sur le NAS](plusieurs-versions-sur-le-nas.md) - Un projet Compose par version, aucun nom fixé, aucune donnée partagée entre versions.
* [Publication des images Docker](publication-des-images-docker.md) - Images publiées, tags, nightly unique, paire cohérente, analyse des vulnérabilités, architecture, workflow de publication.
* [Version de PostgreSQL](version-de-postgresql.md) - postgres 18 alpine, épinglé, identique en production et dans les tests.
* [Application du schéma au démarrage](application-du-schema-au-demarrage.md) - Migrations appliquées au démarrage d'api.
* [Environnement de validation fonctionnelle](environnement-de-validation-fonctionnelle.md) - Version d'une PR lancée sur le poste en une commande, images construites localement, données de validation fictives, édition à la une laissée au tirage ou, sur demande, couverture pâle.
