---
type: Gouvernance
title: 'Intégration continue (CI)'
description: 'Workflows CI, checks requis tenus alignés sur le Ruleset, secrets, quotas de Docker Hub et de l''API GitHub consommés par chaque push.'
tags: [socle]
applies_to:
  - '.github/workflows/**'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Intégration continue (CI)

- **GitHub Actions** héberge le pipeline de non-régression (`.github/workflows/ci.yml`), déclenché sur chaque Pull Request et sur push vers `main`.
- Le contenu du pipeline (étapes, outillage de build et de test) est un choix d'implémentation : cf. [Outillage de développement .NET](../../choix-implementation/outillage.md).
- Le workflow CI (`ci.yml`) constitue le **check de statut requis** de la règle de merge ci-dessous : ses jobs sont imposés par le Ruleset GitHub (cf. [Configuration du repository GitHub](configuration-du-repository-github.md)). Le Ruleset les désigne par leur nom : toute modification de la liste des jobs du workflow est reportée dans le Ruleset dans le même mouvement. Sans cela, un job ajouté n'est pas requis — son échec n'empêche pas la fusion —, et un job renommé ou retiré reste attendu sous son ancien nom sans jamais se présenter, ce qui bloque toute fusion.
- **Publication des images Docker** sur Docker Hub ([Déploiement](../../contraintes-techniques/architecture-deploiement/deploiement.md)) : workflow distinct, `.github/workflows/docker-publish.yml` (cf. [Publication des images Docker](../../choix-implementation/deploiement/publication-des-images-docker.md)). Il s'authentifie auprès de Docker Hub avec le compte `tetram76` et un jeton d'accès (*Personal Access Token* Docker Hub, droits *Read, Write & Delete*, la suppression de l'image `nightly` remplacée l'exigeant) stocké dans le secret de dépôt GitHub `DOCKERHUB_TOKEN`.
- **Analyse des vulnérabilités dans le CI** ([Analyse des vulnérabilités des images Docker](../revue-tickets/analyse-des-vulnerabilites-des-images-docker.md)) : le CI lit les images publiées sur Docker Hub avec le même jeton (`DOCKERHUB_TOKEN`). Le code de la branche et de ses dépendances ne s'exécute que dans les conteneurs de construction des images, qui ne reçoivent pas les identifiants du registre, et seuls l'utilisateur et l'agent agissant en son nom ouvrent des Pull Requests qui accèdent aux secrets. Une PR venant d'un **fork** n'y a pas accès : elle échoue à ce contrôle et ne peut pas être fusionnée en l'état. C'est assumé : il revient à son auteur de s'en accommoder, et aucun chemin de contrôle sans secret n'est prévu pour elle.
- **Quotas consommés par le CI** :
  - **API GitHub** : le jeton du workflow (`GITHUB_TOKEN`) est limité à **1 000 requêtes par heure et par dépôt** (documentation GitHub, *Rate limits for the REST API*), partagées par toutes les exécutions des workflows du dépôt ; l'analyse des vulnérabilités l'utilise notamment pour télécharger son outil. Au-delà, GitHub répond `403` ou `429`.
  - **Docker Hub** : Docker Hub limite les téléchargements d'images (*pulls*) à **200 par période de 6 heures** pour un compte *Personal* authentifié comme `tetram76`, et à **100 par période de 6 heures et par adresse IP** sans authentification ; une image multi-architecture compte un téléchargement par architecture (documentation Docker Hub, *Usage and limits*). Au-delà, Docker Hub répond `429 Too Many Requests`, quel que soit le contenu de la PR. Chaque exécution du CI lit des images sur Docker Hub : l'image publiée, que l'analyse des vulnérabilités lit avec le compte `tetram76`, et l'image PostgreSQL des tests, téléchargée sans authentification.

  Face à un échec dû à l'un de ces quotas, l'agent ne pousse pas de nouveau commit, qui le consommerait à son tour : il relance les seuls jobs en échec (`gh run rerun --failed`) une fois la période du quota écoulée, et signale à l'utilisateur un échec qui persiste.
