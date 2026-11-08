---
type: Choix d'implémentation
title: 'Plusieurs versions sur le NAS'
description: 'Un projet Compose par version, aucun nom fixé, aucune donnée partagée entre versions.'
applies_to:
  - 'docker-compose.yml'
  - '.env.example'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Plusieurs versions sur le NAS

- **Un projet Compose par version** ([Hébergement](../../contraintes-techniques/architecture-deploiement/hebergement.md) : production, version candidate, version de développement) : chacune a son dossier sur le NAS, avec le `docker-compose.yml` du commit de ses images et son propre `.env` (tag des images, port publié, dossier des visuels, adresse publique pour la seule production). Compose nommant d'après le projet ses conteneurs, ses réseaux et son volume, chaque version a sa base, son réseau interne et son secret interne, sans rien de commun avec les autres que les images.
  - `docker-compose.yml` ne fixe donc **aucun nom** (projet, conteneur, volume, réseau) : commun à tous les projets, il ferait prendre au second les conteneurs, ou la base, du premier.
  - **Alternative écartée (un fichier Compose par sorte de version, avec ses noms fixes)** : autant de fichiers à tenir alignés que de sortes de version, alors que le nom du projet suffit à isoler les versions avec le fichier unique, déjà commun aux déploiements et au développement ([Publication des images Docker](publication-des-images-docker.md)).
- **Aucune donnée partagée** : une pré-version a sa propre base et son propre dossier de visuels, vides à son premier déploiement, ou remplis d'une **copie** de ceux de la production, jamais des mêmes (récupération de cette copie : #267, sans objet avant la première mise en production).
  - **Alternative écartée (base ou dossier de visuels de la production partagés)** : `api` migre sa base à son démarrage ([Application du schéma au démarrage](application-du-schema-au-demarrage.md)), et une pré-version migrerait celle de la production vers un schéma que la production ne lit pas, sans retour possible ([Migrations EF Core](../persistance/migrations-ef-core.md)) ; la réconciliation des visuels supprime tout fichier qui ne correspond à aucun visuel de sa propre base ([Visuels : stockage et traitement](../domaine/visuels-stockage-et-traitement.md)), et deux versions supprimeraient chacune les visuels de l'autre.
