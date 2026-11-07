---
type: Gouvernance
title: 'Versions et releases'
description: 'Numérotation semver, jalons, rattachement des tickets et PR, publication d''une version (tag, release, analyse, images).'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T12:55:48Z }
---

# Versions et releases

Les versions de l'application sont connues du dépôt avant leur publication : chaque version à venir est un **jalon** GitHub (*milestone*), auquel sont rattachés les tickets et les Pull Requests qu'elle livre ; chaque version publiée est un **tag** et une **release** GitHub. Une version correspond à un état stable et identifiable de l'application, sur `main`.

- **Numérotation** : [Semantic Versioning](https://semver.org/) (`MAJEUR.MINEUR.CORRECTIF`), tag `v<version>` (ex. `v0.1.0`) :
  - `0.0.x` : les phases qui ne livrent pas encore d'application utilisable, une version par phase (phases 0 à 2) ;
  - `0.1.0` : la première application utilisable (Phase 3, partie 1), puis une version **mineure** par phase ou partie de phase ;
  - `1.0.0` : la version de la première mise en production (déploiement consigné dans [`journal-evenements.md`](../../journal-evenements.md)) ; les versions suivantes gardent une version mineure par phase ou partie de phase ;
  - une version **corrective** (ex. `v0.1.1`) livre une correction publiée entre deux de ces versions.

  Une **version candidate** (RC), testée sur le déploiement interne avant la publication de la version ([Hébergement](../../contraintes-techniques/architecture-deploiement/hebergement.md)), n'a pas de numéro propre : elle est désignée par les images d'une publication à la demande, sous leur tag immuable `sha-<commit>` ([Publication des images Docker](../../choix-implementation/deploiement/publication-des-images-docker.md)). Une **version de développement** (alpha ou bêta), appelée à devenir la version candidate suivante, est désignée de même, et déployée en interne à côté de la production et de la version candidate ([Plusieurs versions sur le NAS](../../choix-implementation/deploiement/plusieurs-versions-sur-le-nas.md)).
  - **Alternative écartée (numéro de pré-version, ex. `v0.1.0-rc.1`, avec son tag et sa release)** : un nom plus lisible que `sha-<commit>`, pour un coût réel : chaque version publiée fait repartir la période du nouveau code de SonarQube Cloud ([Analyse de la qualité du code](analyse-de-la-qualite-du-code.md)), qu'une version candidate amputerait du code ajouté depuis la version précédente, et jalons, tags et releases devraient distinguer les versions candidates des versions publiées.

  La version que livre chaque phase, ou partie de phase, est indiquée dans [`suivi-implementation/`](../../suivi-implementation/index.md).
- **Jalon** : chaque version à venir est un jalon de même nom (`v0.1.0`), dont la description désigne la phase ou la partie qu'elle livre. Il est créé au découpage de cette phase ou de cette partie, avec ses tickets, ou avec le ticket d'une version corrective. Il ne porte pas d'échéance : le projet ne planifie pas de dates.
- **Rattachement** : chaque ticket est rattaché au jalon de la version qui le livre ([Suivi par tickets](../revue-tickets/suivi-par-tickets.md)) ; un ticket hors plan l'est dès que sa réalisation est prévue dans une version, et reste sans jalon tant qu'elle ne l'est pas (ex. une montée de version majeure encore lointaine, le ticket des vulnérabilités connues, que ne réalise aucune PR). Chaque Pull Request fusionnée sur `main` est rattachée au jalon de son ticket de référence ; sans ticket, à celui de la version en cours, la première version non publiée. Un ticket fermé sans être réalisé (*not planned*) quitte son jalon.
- **Publication** : une version est publiée quand tous les tickets de son jalon sont fermés :
  - **tag** annoté et signé (clé GPG de [Signature des commits](signature-des-commits.md)), posé sur le commit de `main` qui la complète ;
  - **release** GitHub sur ce tag, dont les notes sont générées par GitHub à partir des Pull Requests fusionnées depuis le tag de la version précédente, et vérifiées avant publication comme tout texte publié ([Budget IA](../gouvernance/budget-ia.md), mention de Codex). Une version `0.0.x`, qui ne livre pas d'application utilisable, est marquée **pré-release** ;
  - **analyse SonarQube Cloud** de `main`, relancée dès le tag posé : la période du nouveau code commence à la première analyse qui porte la version ([Analyse de la qualité du code](analyse-de-la-qualite-du-code.md)), qui doit être celle du commit publié, et non celle de la fusion suivante, dont les changements en seraient exclus ;
  - **images Docker** de la version, publiées à la demande depuis son tag ([Publication des images Docker](../../choix-implementation/deploiement/publication-des-images-docker.md)) ; les versions `0.0.x` n'en ont pas, faute d'interface ;
  - **fermeture du jalon**.
- La publication d'une version est un acte de gestion du projet ([Prise de décision](../gouvernance/prise-de-decision.md)) : l'agent l'accomplit dès que la règle ci-dessus la prescrit, puis en informe l'utilisateur. Le déploiement de la version, lui, appartient à l'utilisateur.
