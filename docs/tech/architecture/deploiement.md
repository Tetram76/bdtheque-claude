---
title: 'Déploiement et environnement de validation'
description: 'Production sur le NAS, pré-versions, images publiées et environnement de validation d''une Pull Request sur le poste ; pour toute question de mise en service, de ports ou de validation.'
code:
  - 'docker-compose.yml'
  - '.env.example'
  - 'docker/**'
  - 'scripts/Start-Validation.ps1'
  - 'scripts/Stop-Validation.ps1'
  - '.github/workflows/docker-publish.yml'
---

# Déploiement et environnement de validation

Le même fichier [`docker-compose.yml`](../../../docker-compose.yml) sert la production, avec les images publiées, et l'environnement de validation, qui le surcharge. Exigences : [Déploiement](../../../.speckit/contraintes-techniques/architecture-deploiement/deploiement.md), [Hébergement](../../../.speckit/contraintes-techniques/architecture-deploiement/hebergement.md) ; procédure d'installation : [`README.md`](../../../README.md).

## Production et pré-versions

Question du schéma : *d'où viennent les images, et comment plusieurs versions cohabitent-elles sur le NAS ?* Schéma interprétatif.

```mermaid
flowchart TB
    main["Branche main"]
    publish["Workflow docker-publish.yml<br/>chaque nuit ou à la demande"]
    hub[("Docker Hub<br/>tetram76/bdtheque-api<br/>tetram76/bdtheque-frontend")]
    main --> publish --> hub

    subgraph nas["NAS Synology, Container Manager"]
        proxy["Reverse proxy<br/>HTTPS public"]
        prod["Projet bdtheque<br/>tag de la version"]
        rc["Projet bdtheque-rc<br/>tag sha-commit"]
        dev["Projet bdtheque-dev<br/>tag sha-commit"]
    end
    hub --> prod
    hub --> rc
    hub --> dev
    proxy -- "HTTP" --> prod
```

- **Un projet Compose par version**, chacun dans son dossier, avec son `.env` : ses conteneurs, réseaux et volume `db-data` lui sont propres ; seul le `frontend` de la production est derrière le reverse proxy, les pré-versions sont jointes en HTTP interne ([Plusieurs versions sur le NAS](../../../.speckit/choix-implementation/deploiement/plusieurs-versions-sur-le-nas.md)).
- **Images** : `api` et `frontend`, construites par [`docker/api/Dockerfile`](../../../docker/api/Dockerfile) et [`docker/frontend/Dockerfile`](../../../docker/frontend/Dockerfile) (SDK .NET pour la compilation, ASP.NET Core *chiseled* pour l'exécution), publiées par [`docker-publish.yml`](../../../.github/workflows/docker-publish.yml) ; `db` est l'image PostgreSQL officielle épinglée.
- **Schéma de la base** : appliqué par `api` à son démarrage (migrations EF Core), avant de servir.
- **Variables** : modèle dans [`.env.example`](../../../.env.example) ; `docker compose` refuse de démarrer sans les valeurs obligatoires (mot de passe de la base, secret interne, compte administrateur), et `api` comme `frontend` valident leur configuration au démarrage.

## Environnement de validation

La version d'une Pull Request (ou de tout commit) lancée sur le poste par une commande, pour sa validation fonctionnelle et pour en consulter la documentation technique. Raisons et alternatives : [Environnement de validation fonctionnelle](../../../.speckit/choix-implementation/deploiement/environnement-de-validation-fonctionnelle.md) ; marche à suivre : [`README.dev.md`](../../../README.dev.md).

Question du schéma : *qu'est-ce que la commande de validation construit et démarre, et qu'ouvre-t-elle au navigateur du poste ?* Schéma interprétatif.

```mermaid
flowchart TB
    start["Start-Validation.ps1<br/>-PullRequest N"]
    sources["Sources du commit<br/>git archive, hors copie de travail"]
    start --> sources

    subgraph project["Projet Compose bdtheque-validation"]
        frontend["frontend<br/>127.0.0.1:8180"]
        api["api<br/>127.0.0.1:8181<br/>Scalar, OpenAPI"]
        db[("db")]
        data["validation-data<br/>chargement unique"]
        docs["docs<br/>127.0.0.1:8182<br/>documentation technique"]
    end
    sources -- "docker compose up --build" --> project
    frontend --> api --> db
    data -- "API d'administration" --> api

    browser["Navigateur du poste"]
    browser --> frontend
    browser --> api
    browser --> docs
```

- **Construit depuis le commit validé** : les sources sont extraites par `git archive` dans un dossier temporaire, puis les images sont construites localement sous des noms propres à la validation (`bdtheque-validation-*`), jamais celles de Docker Hub.
- **Surcharge** : [`docker/validation/compose.yaml`](../../../docker/validation/compose.yaml), avec les valeurs fixes et fictives de [`validation.env`](../../../docker/validation/validation.env) ; `api` et `frontend` y tournent dans l'environnement ASP.NET Core `Validation`, qui ajoute la documentation de l'API et le thème choisi par l'adresse.
- **Ports ouverts au seul poste** (`127.0.0.1`) : application (`FRONTEND_PORT`), documentation de l'API par Scalar (`API_PORT`, `/scalar/v1`), documentation technique du commit (`DOCS_PORT`).
- **Documentation technique** : le service `docs`, image construite par [`docker/validation-docs/Dockerfile`](../../../docker/validation-docs/Dockerfile) depuis le `docs/tech/` du commit validé, dont l'empreinte figure au pied de chaque page. Détail : [Consulter, construire, vérifier et maintenir](../documentation/consulter-et-maintenir.md).
- **Arrêt** : [`Stop-Validation.ps1`](../../../scripts/Stop-Validation.ps1) supprime conteneurs, réseaux, volumes et sources extraites ; chaque lancement repart d'une base vide et reconstruit ce que le commit a changé.
