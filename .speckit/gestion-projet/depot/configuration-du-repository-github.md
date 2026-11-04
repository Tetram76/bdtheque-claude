---
type: Gouvernance
title: 'Configuration du repository GitHub'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Configuration du repository GitHub

- **Branche principale** : `main`
- **Stratégie de merge** : squash merge uniquement (historique linéaire et lisible sur `main`)
- **Suppression automatique** des branches de feature après merge
- **Fonctionnalités actives** : Issues, Releases
- **Visibilité** : dépôt **public**.
- **Fonctionnalités désactivées** : Wiki, Projects, Discussions (aucune interaction communautaire souhaitée)
- **Protection de `main`** : un **Ruleset** GitHub est configuré (id `17636023`, **actif** — le dépôt étant public) avec les règles suivantes :
  - PR obligatoire avant tout merge
  - Force-push interdit
  - Suppression de `main` interdite
  - Squash merge uniquement
  - **Commits signés obligatoires** (`required_signatures`)
  - **Checks de la CI requis** (`required_status_checks`) : chaque job du workflow CI est un check requis (cf. [Intégration continue (CI)](/gestion-projet/depot/integration-continue-ci.md)). La branche n'a pas à être à jour avec `main` avant le merge : les PR sont fusionnées en squash une à une, et exiger cette mise à jour imposerait une resynchronisation systématique sans gain proportionné.
  - **Porte de qualité de SonarQube Cloud requise** : le check `SonarCloud Code Analysis`, que l'application GitHub de SonarQube Cloud (`integration_id` `12526`) publie sur chaque commit analysé, en échec si la porte de qualité échoue (cf. [Analyse de la qualité du code](/gestion-projet/depot/analyse-de-la-qualite-du-code.md)).
- **Sécurité du dépôt** (`security_and_analysis`, disponible gratuitement car dépôt public) :
  - **Secret scanning** : activé
  - **Push protection** (blocage des push contenant un secret détecté) : activée
  - **Dependabot alerts** : désactivées, faute de rien couvrir : le graphe de dépendances de GitHub ne lit pas les versions de `Directory.Packages.props`, et Dependabot n'alerte que sur les actions épinglées par version, non par empreinte (documentation GitHub, *About Dependabot alerts*) ; le contrôle de sécurité quotidien en tient lieu ([Mises à jour des dépendances](/gestion-projet/depot/mises-a-jour-des-dependances.md), vulnérabilités connues) ; **Dependabot security updates** : désactivées, leurs PR n'étant pas en brouillon
  - *Vérification de validité des secrets détectés* (`secret_scanning_validity_checks`) : désactivée, cause non identifiée — à vérifier manuellement dans les paramètres GitHub du dépôt si besoin.
- **Merge réservé à l'utilisateur, y compris pour de futurs collaborateurs** : le dépôt est la propriété d'un compte **personnel** (`Tetram76`), pas d'une organisation — la restriction de push/merge par utilisateur ou équipe (fonctionnalité GitHub de branch protection) n'est **pas disponible** sur ce type de dépôt, elle ne peut donc pas être imposée techniquement via un Ruleset ou une protection de branche. La garantie repose donc sur la gestion des droits d'accès : **aucun collaborateur ne doit recevoir un accès `Write` (ou supérieur)** au dépôt. Toute contribution externe future passe par un **fork** + Pull Request ; le merge de cette PR reste effectué par l'utilisateur (ou par l'agent agissant en son nom), jamais par le contributeur externe lui-même.
