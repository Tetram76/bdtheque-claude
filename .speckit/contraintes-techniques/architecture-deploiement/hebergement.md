---
type: Contrainte technique
title: 'Hébergement'
description: 'NAS Synology, TLS par son reverse proxy, accès direct en HTTP en interne, plusieurs versions sur le NAS et données d''une pré-version.'
applies_to:
  - 'docker-compose.yml'
  - '.env.example'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Hébergement

- L'application est hébergée sur un **NAS Synology RS1221+** (processeur AMD Ryzen V1500B, architecture x86_64).
- Le déploiement Docker doit être compatible avec l'environnement Docker fourni par Synology (Container Manager).
- **Sécurisation SSL/TLS** : le chiffrement des échanges avec le navigateur (HTTPS, certificats) est assuré par le **reverse proxy du NAS Synology**, placé devant le conteneur `frontend` : il reçoit les requêtes en HTTPS et les relaie en HTTP au `frontend`, sans rediriger le HTTP vers le HTTPS. Le `frontend` ne gère pas lui-même la sécurisation SSL/TLS : il ne sert que du HTTP.
- **Accès direct en HTTP** : pendant le développement, les versions à valider ou à tester (validation fonctionnelle, versions candidates, RC, versions de développement) sont déployées **en interne**, sans reverse proxy et sans accès depuis l'extérieur : le `frontend` y est accessible directement en HTTP.
- **Plusieurs versions sur le même NAS** : le NAS qui héberge la version en **production** héberge aussi les **pré-versions** : la version candidate (RC) et une version de développement (alpha ou bêta, termes équivalents), appelée à devenir la version candidate suivante. À un instant donné, le NAS peut donc faire tourner jusqu'à **trois versions** de l'application en même temps.
- **Données d'une pré-version** : la mise en place d'une pré-version permet, et idéalement réalise elle-même, la **récupération des données de la production**.
  - **Non-contrainte** (ouverture de dernier recours) : si la récupération l'exige, la pré-version peut recevoir un accès **en lecture seule** aux données de la production (base et visuels), par exception à [Architecture des tiers](/contraintes-techniques/architecture-deploiement/architecture-des-tiers.md) (seul `api` accède à `db`). Une récupération qui s'en passe reste préférable.
