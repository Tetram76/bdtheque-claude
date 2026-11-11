# Dépôt

* [Repository](repository.md) - Dépôt GitHub et services rattachés, gérés par l'agent via leurs API ; documentation du dépôt (README.md, README.dev.md).
* [Signature des commits](signature-des-commits.md) - Commits signés et vérifiés, vérification sur GitHub seulement ; s'applique à tout commit.
* [Configuration du repository GitHub](configuration-du-repository-github.md) - Réglages du dépôt GitHub, Ruleset de main, sécurité du dépôt, merge fait par l'agent au nom de l'utilisateur, jamais par un collaborateur ; à lire pour toute modification de la configuration GitHub.
* [Intégration continue (CI)](integration-continue-ci.md) - Workflows CI, tests selon ce que modifie la Pull Request, aucun check sur une Pull Request en conflit avec sa branche de destination, checks requis tenus alignés sur le Ruleset, secrets, quotas de Docker Hub et de l'API GitHub consommés par chaque push.
* [Analyse de la qualité du code](analyse-de-la-qualite-du-code.md) - Analyse SonarQube Cloud : workflow, porte de qualité, jetons, périmètre et exclusions, nouveau code ; à lire pour modifier l'analyse ou sa configuration.
* [Mises à jour des dépendances](mises-a-jour-des-dependances.md) - Versions épinglées, relevé des versions, vulnérabilités connues, montées et leur revue de l'existant ; à lire quand le relevé est dû ou pour toute montée de version.
* [Relevé des versions à la reprise du travail](releve-des-versions-a-la-reprise-du-travail.md) - À chaque reprise du travail : quand le relevé des versions est dû et où sa trace se lit ; s'il est dû, lire Mises à jour des dépendances.
* [Règle absolue : interdiction de pousser directement sur `main`](regle-absolue-interdiction-de-pousser-directement-sur-main.md) - Interdiction de commiter ou pousser sur main : toute modification passe par une Pull Request.
* [Stratégie de branches](strategie-de-branches.md) - Branches persistantes (main et branches de release seules), nommage des branches (feat/, fix/, chore/), branche de travail tenue à jour de sa branche de destination.
* [Conventions de commit](conventions-de-commit.md) - Conventional Commits pour les commits et titres de PR, description de PR devenue corps du commit squashé, template de PR.
* [Versions et releases](versions-et-releases.md) - Numérotation semver, jalons, rattachement des tickets et PR, publication d'une version (tag, release, analyse, images).
