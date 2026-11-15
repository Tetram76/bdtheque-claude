---
type: Gouvernance
title: 'Repository'
description: 'Dépôt GitHub et services rattachés, gérés par l''agent via leurs API, sur GitHub sous son propre compte ; documentation du dépôt (README.md, README.dev.md).'
tags: [socle]
applies_to:
  - 'README.md'
  - 'README.dev.md'
  - '.claude/hooks/set-agent-identity.sh'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-15T12:30:00Z }
---

# Repository

- **Hébergement** : GitHub
- **Nom du dépôt** : `Tetram76/bdtheque-claude`
- **URL** : <https://github.com/Tetram76/bdtheque-claude>
- La **gestion du repository GitHub** (configuration, branches, protections, sécurité, CI/CD, etc.) relève du périmètre de l'agent et doit être maintenue conformément aux contraintes du projet. Il en va de même des **services externes rattachés au dépôt** : paramétrage du projet SonarQube Cloud et marquage de ses problèmes ([Analyse de la qualité du code](analyse-de-la-qualite-du-code.md)), dépôts Docker Hub de l'application.
  - L'agent accomplit lui-même ces actes ([Prise de décision](../gouvernance/prise-de-decision.md), acte de gestion du projet), et ne les laisse jamais à l'utilisateur comme une tâche à faire de son côté. Il passe par l'**API** de chaque service, avec l'accès que l'utilisateur a donné au poste : `gh` pour GitHub, sous le compte propre de l'agent (ci-dessous) ; pour SonarQube Cloud, un jeton personnel de l'utilisateur (l'offre gratuite n'en connaît pas d'autre) placé dans la variable d'environnement `SONAR_TOKEN` du poste, l'agent y agissant au nom de l'utilisateur. Il n'agit jamais par la session de l'utilisateur dans un navigateur.
  - **Compte GitHub de l'agent** : `Tetram76-bot`, compte machine de l'utilisateur, collaborateur `Write` du dépôt ([Configuration du repository GitHub](configuration-du-repository-github.md)). Tout ce que l'agent fait sur GitHub (push, Pull Requests, commentaires, réactions, résolutions, fusions, tickets, configuration du dépôt) l'est sous ce compte, jamais sous celui de l'utilisateur : l'utilisateur n'est ainsi ni l'auteur ni un participant des Pull Requests et des CI de l'agent, et GitHub ne lui en notifie plus l'activité, dont il recevait un courriel à chaque commentaire, revue, fusion et échec du CI (exigence de l'utilisateur). Les commits restent ceux de l'utilisateur (auteur, signature, [Signature des commits](signature-des-commits.md)) : seul change le compte qui les pousse.
    - **Séparation des identités** : `gh` connaît les deux comptes, celui de l'utilisateur restant actif pour ce qu'il fait lui-même. Le hook `SessionStart` du dépôt (`.claude/settings.json`, `.claude/hooks/set-agent-identity.sh`) donne aux seules commandes de l'agent le jeton du compte de l'agent (`GH_TOKEN`), lu dans le gestionnaire d'identifiants du poste où l'utilisateur l'a placé (`gh auth login`), jamais écrit dans le dépôt ni lu ou recopié par l'agent lui-même ; il fait aussi demander à `git` ses identifiants GitHub à `gh` dans le dépôt, si bien qu'un push suit `GH_TOKEN` comme toute commande `gh`. Faute de ce jeton, le hook en pose un invalide : une commande de l'agent échoue plutôt que de partir sous le compte de l'utilisateur.
    - **Vérification** : avant son premier acte sur GitHub dans une session, l'agent vérifie que `gh api user` répond `Tetram76-bot` ; sinon, il n'agit pas sur GitHub et le signale à l'utilisateur.
    - **Alternative écartée (désabonner l'utilisateur de chaque Pull Request de l'agent)** : tout commentaire posté ensuite sous son compte l'y réabonne (constaté), et l'agent commente presque chaque Pull Request.
    - **Alternative écartée (application GitHub)** : son jeton expire chaque heure et exige une clé privée sur le poste, sans rien apporter qu'un compte machine n'offre.
  - Seul ce qui exige l'utilisateur en personne lui revient : se connecter à un service, créer un secret ou un jeton et en placer la valeur (secret de dépôt, variable d'environnement du poste), qu'aucun agent ne lit ni ne manipule.
  - Un acte que l'outillage de l'agent lui refuse (ex. une commande bloquée par le mode automatique de Claude Code) n'est pas contourné : l'agent nomme l'acte refusé, la commande et la raison du refus, et demande à l'utilisateur de l'autoriser.
- **Documentation du dépôt**, en deux fichiers à la racine, selon leur lecteur :
  - `README.md` : pour qui découvre le projet, l'installe ou l'utilise — présentation, fonctionnalités, stack et architecture, déploiement et exploitation de sa version de production (procédure sur le NAS, mises à jour, HTTPS, dossier des visuels), indicateurs de qualité en tête ([Analyse de la qualité du code](analyse-de-la-qualite-du-code.md)) ;
  - `README.dev.md` : pour qui maintient le projet ou rejoint l'équipe, ou veut valider une version avant de l'installer, ce qui demande des connaissances techniques dont l'utilisateur se passe — spécifications et démarche pilotée par elles, organisation du code, développement local (build, tests, captures de référence, lancement depuis les sources), validation fonctionnelle, publication des images, plusieurs versions sur le NAS (pré-versions à côté de la production).

  Chacun renvoie à l'autre ; une information ne figure que dans celui de son lecteur.
