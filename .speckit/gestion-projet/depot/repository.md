---
type: Gouvernance
title: 'Repository'
description: 'Dépôt GitHub et services rattachés, gérés par l''agent via leurs API ; documentation du dépôt (README.md, README.dev.md).'
tags: [socle]
applies_to:
  - 'README.md'
  - 'README.dev.md'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Repository

- **Hébergement** : GitHub
- **Nom du dépôt** : `Tetram76/bdtheque-claude`
- **URL** : <https://github.com/Tetram76/bdtheque-claude>
- La **gestion du repository GitHub** (configuration, branches, protections, sécurité, CI/CD, etc.) relève du périmètre de l'agent et doit être maintenue conformément aux contraintes du projet. Il en va de même des **services externes rattachés au dépôt** : paramétrage du projet SonarQube Cloud et marquage de ses problèmes ([Analyse de la qualité du code](/gestion-projet/depot/analyse-de-la-qualite-du-code.md)), dépôts Docker Hub de l'application.
  - L'agent accomplit lui-même ces actes ([Prise de décision](/gestion-projet/gouvernance/prise-de-decision.md), acte de gestion du projet), et ne les laisse jamais à l'utilisateur comme une tâche à faire de son côté. Il passe par l'**API** de chaque service, avec l'accès que l'utilisateur a donné au poste : `gh` pour GitHub ; pour SonarQube Cloud, un jeton personnel de l'utilisateur (l'offre gratuite n'en connaît pas d'autre) placé dans la variable d'environnement `SONAR_TOKEN` du poste. L'agent agit ainsi au nom de l'utilisateur, comme sur GitHub. Il n'agit jamais par la session de l'utilisateur dans un navigateur.
  - Seul ce qui exige l'utilisateur en personne lui revient : se connecter à un service, créer un secret ou un jeton et en placer la valeur (secret de dépôt, variable d'environnement du poste), qu'aucun agent ne lit ni ne manipule.
  - Un acte que l'outillage de l'agent lui refuse (ex. une commande bloquée par le mode automatique de Claude Code) n'est pas contourné : l'agent nomme l'acte refusé, la commande et la raison du refus, et demande à l'utilisateur de l'autoriser.
- **Documentation du dépôt**, en deux fichiers à la racine, selon leur lecteur :
  - `README.md` : pour qui découvre le projet, l'installe ou l'utilise — présentation, fonctionnalités, stack et architecture, déploiement et exploitation de sa version de production (procédure sur le NAS, mises à jour, HTTPS, dossier des visuels), indicateurs de qualité en tête ([Analyse de la qualité du code](/gestion-projet/depot/analyse-de-la-qualite-du-code.md)) ;
  - `README.dev.md` : pour qui maintient le projet ou rejoint l'équipe, ou veut valider une version avant de l'installer, ce qui demande des connaissances techniques dont l'utilisateur se passe — spécifications et démarche pilotée par elles, organisation du code, développement local (build, tests, captures de référence, lancement depuis les sources), validation fonctionnelle, publication des images, plusieurs versions sur le NAS (pré-versions à côté de la production).

  Chacun renvoie à l'autre ; une information ne figure que dans celui de son lecteur.
