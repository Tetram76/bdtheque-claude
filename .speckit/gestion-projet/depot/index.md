# Dépôt

* [Repository](repository.md) - Dépôt GitHub et services rattachés, gérés par l'agent via leurs API ; documentation du dépôt (README.md, README.dev.md).
* [Signature des commits](signature-des-commits.md) - Commits signés et vérifiés, vérification sur GitHub seulement ; s'applique à tout commit.
* [Configuration du repository GitHub](configuration-du-repository-github.md) - Réglages du dépôt GitHub, Ruleset de main, sécurité du dépôt, merge réservé à l'utilisateur ; à lire pour toute modification de la configuration GitHub.
* [Intégration continue (CI)](integration-continue-ci.md) - Workflows CI, checks requis tenus alignés sur le Ruleset, secrets, quotas de Docker Hub et de l'API GitHub consommés par chaque push.
* [Analyse de la qualité du code](analyse-de-la-qualite-du-code.md) - Analyse SonarQube Cloud : workflow, porte de qualité, jetons, périmètre et exclusions, nouveau code ; à lire pour modifier l'analyse ou sa configuration.
* [Mises à jour des dépendances](mises-a-jour-des-dependances.md) - Versions épinglées, relevé des versions à chaque reprise du travail, vulnérabilités connues, montées et leur revue de l'existant.
* [Règle absolue : interdiction de pousser directement sur `main`](regle-absolue-interdiction-de-pousser-directement-sur-main.md) - Interdiction de commiter ou pousser sur main : toute modification passe par une Pull Request.
* [Stratégie de branches](strategie-de-branches.md) - Nommage des branches (feat/, fix/, chore/).
* [Conventions de commit](conventions-de-commit.md) - Conventional Commits pour les commits et titres de PR, description de PR devenue corps du commit squashé, template de PR.
* [Versions et releases](versions-et-releases.md) - Numérotation semver, jalons, rattachement des tickets et PR, publication d'une version (tag, release, analyse, images).
