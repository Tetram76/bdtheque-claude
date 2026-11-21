---
type: Choix d'implémentation
title: 'Réseaux des conteneurs'
description: 'Un réseau par conteneur appelé, nommé comme lui, rejoint par ses seuls appelants, dans tous les fichiers Compose ; à suivre pour tout réseau ajouté ou conteneur qui en rejoint un.'
applies_to:
  - 'docker-compose.yml'
  - 'docker/validation/compose.yaml'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-21T12:00:00Z }
---

# Réseaux des conteneurs

L'isolation des tiers est imposée ([Architecture des tiers](../../contraintes-techniques/architecture-deploiement/architecture-des-tiers.md), [Déploiement](../../contraintes-techniques/architecture-deploiement/deploiement.md)) ; les réseaux Docker qui la réalisent sont un choix.

- **Un réseau par conteneur appelé, qui porte son nom, et que seuls ses appelants rejoignent** : le réseau `db` réunit `db` et `api`, son seul appelant ; le réseau `api` réunit `api` et ceux qui l'appellent (`frontend`, et le chargement des données de l'environnement de validation, [Environnement de validation fonctionnelle](environnement-de-validation-fonctionnelle.md)). Le nom d'un réseau dit ainsi quel conteneur il protège, et la liste des réseaux d'un service, quels conteneurs il appelle : un conteneur ajouté, comme l'outil pour agents IA ([Indépendance de l'outil pour agents IA](../../contraintes-techniques/architecture-deploiement/independance-de-l-outil-pour-agents-ia.md)), rejoint le réseau de chaque conteneur qu'il appelle, sans autre réseau à créer, et un réseau ajouté suit la même règle.
- **Aucun suffixe** : un nom de réseau n'apparaît que sous une clé `networks`, et Compose le préfixe du nom du projet (`<projet>_db`), comme son réseau par défaut (`<projet>_default`, documentation de Docker Compose, *Networking in Compose*) ; aucun nom n'est fixé ([Plusieurs versions sur le NAS](plusieurs-versions-sur-le-nas.md)). Réseaux et services ont des espaces de noms distincts : le service `api` reste joint par son nom de service, sur quelque réseau que ce soit.
- **Une seule règle pour tous les fichiers Compose** : l'environnement de validation surcharge le fichier de production et rejoint ses réseaux, sans en déclarer d'autres. Un test du dépôt vérifie la règle dans les deux fichiers, ainsi que l'accès à `db` par `api` seul.
- **Alternative écartée (réseaux nommés d'après la frontière qu'ils isolent, `backend` et `frontend`)** : sans règle, les deux réseaux d'origine avaient divergé, l'un nommé d'après une frontière (`backend`), l'autre d'après un conteneur avec un suffixe (`frontend-net`), suffixe ajouté pour ne pas reprendre le nom du service ; une frontière ne dit pas quels conteneurs elle sépare, et la liste n'en a pas de suite naturelle pour un conteneur de plus.
- **Alternative écartée (un réseau par paire d'appelant et d'appelé, ex. `frontend-api`)** : chaque appelant ajouté exigerait son réseau, pour isoler entre eux les appelants d'`api`, qu'`api` protège déjà par son secret interne ([Authentification : mise en œuvre](../socle/transverse/authentification-mise-en-oeuvre.md)).
- **Alternative écartée (suffixe `-net` sur chaque réseau)** : redondant, le contexte disant déjà qu'il s'agit d'un réseau.
