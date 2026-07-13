# Gestion de Projet

Ce fichier décrit la gouvernance du projet : stockage, organisation, outillage de gestion.

---

## Gouvernance documentaire

- **Source de vérité absolue** : le dossier [`.speckit/`](.) — ce fichier ainsi que `fonctionnel.md`, `modele-metier.md` et `contraintes-techniques.md` — est la source de vérité absolue du projet. Il prime sur toute autre information : historique de conversation, supposition, connaissance générale de l'agent. Aucune décision ne peut le contredire sans accord explicite de l'utilisateur. En cas d'ambiguïté ou de silence sur un sujet, l'agent peut décider, mais documente alors son choix dans le fichier concerné.
- **Autonomie de l'agent** : l'agent prend toutes les décisions architecturales, techniques, fonctionnelles et d'implémentation nécessaires à la livraison de l'application, en **totale autonomie** (modalités détaillées dans « Prise de décision » ci-dessous). Les seules interventions attendues de l'utilisateur sont la mise à jour des fichiers `.speckit/` pour exprimer ses besoins et exigences.

## Objectifs du projet

1. **Réécriture applicative** : réécrire une application client lourd existante en application web n-tiers.
2. **Évaluation de l'agent** : évaluer la capacité de l'agent IA à produire une application complète en totale autonomie.

## Périmètre de l'agent

L'agent produit l'intégralité des livrables du projet, y compris :

- le code de l'application,
- le **contenu de l'aide contextuelle**.

---

## Repository

- **Hébergement** : GitHub
- **Nom du dépôt** : `Tetram76/bdtheque-claude`
- **URL** : <https://github.com/Tetram76/bdtheque-claude>
- La **gestion du repository GitHub** (configuration, branches, protections, CI/CD, etc.) relève du périmètre de l'agent et doit être maintenue conformément aux contraintes du projet.

## Maintenance du .speckit

- Les fichiers `.speckit/` sont des **documents vivants** : l'agent peut les restructurer à tout moment (fusion, split, déplacement de sections, création de nouvelles sections) si cela améliore leur clarté ou leur cohérence.
- Toute restructuration est faite sans validation préalable, dans le même esprit d'autonomie qui régit les décisions techniques.

## Prise de décision

- L'agent prend ses décisions en **totale autonomie**.
- Lorsque les pour et les contre s'équilibrent et qu'il n'existe objectivement pas de meilleur choix, l'agent **peut solliciter l'avis de l'utilisateur** avant de trancher.
- Les choix techniques ne sont **pas gravés dans le marbre** : tout choix peut être remis en cause si une nouvelle contrainte le justifie.
- Lorsqu'un changement technique a un **impact visible sur le livrable** (comportement, interface, données, déploiement), la transition doit être **transparente pour l'utilisateur** : l'agent informe explicitement de ce qui change et de ce qui est impacté.

## Signature des commits

- **Tous les commits doivent être signés et vérifiés** (GPG/SSH).
- La signature est configurée globalement sur le poste (`commit.gpgsign=true`, clé GPG `CC10F185AA085B2DD98025986A5E6B8341983E31`).
- La règle `required_signatures` est intégrée au Ruleset GitHub pour l'imposer côté serveur.

## Règles de travail de l'agent

- La base de connaissance de l'agent est considérée **toujours potentiellement obsolète**. Avant toute décision technique (choix de bibliothèque, version, API, configuration, bonne pratique), l'agent **doit contre-vérifier** ses connaissances via des sources externes reconnues et fiables (documentation officielle, dépôts GitHub officiels, etc.).
- Aucune décision technique ne peut reposer uniquement sur la mémoire de l'agent.

## Qualité du code

- La **qualité du code est une préoccupation majeure** et permanente.
- Le code doit suivre les **best practices communément admises** pour chaque technologie utilisée (conventions de nommage, patterns architecturaux, sécurité, performance, etc.). Ces best practices sont à vérifier via les sources officielles (cf. règle ci-dessus).
- **Non-régression** : toute modification doit être accompagnée d'un moyen de vérifier qu'elle ne sera pas silencieusement annulée par une modification future. Le moyen de contrôle (test unitaire, test d'intégration, test de contrat, assertion, etc.) doit être **proportionné à la portée et au risque de la modification** : on n'écrit pas une suite de tests complète pour un changement trivial, mais toute logique métier ou technique non triviale doit être couverte.
- L'agent est **seul décisionnaire** sur l'architecture et l'implémentation : toute refactorisation jugée nécessaire (lisibilité, maintenabilité, testabilité, séparation des responsabilités, etc.) doit être faite sans attendre de validation.
- Le code doit être **propre et lisible** : l'utilisateur est développeur et lit le code produit.
- **Règle de commentaires** : les commentaires expliquent le **pourquoi** (intention, contrainte, décision de conception), jamais le **quoi** (ce que le code fait — le code se lit de lui-même).

## Configuration du repository GitHub

- **Branche principale** : `main`
- **Stratégie de merge** : squash merge uniquement (historique linéaire et lisible sur `main`)
- **Suppression automatique** des branches de feature après merge
- **Fonctionnalités actives** : Issues, Releases
- **Fonctionnalités désactivées** : Wiki, Projects, Discussions (projet privé, aucune interaction communautaire)
- **Protection de `main`** : un **Ruleset** GitHub est configuré (id `17636023`) avec les règles suivantes (actif si dépôt public, suspendu en privé — limitation GitHub gratuit) :
  - PR obligatoire avant tout merge
  - Force-push interdit
  - Suppression de `main` interdite
  - Squash merge uniquement
  - **Commits signés obligatoires** (`required_signatures`)

## Règle de merge : non-régression obligatoire

> **Une Pull Request ne peut être fusionnée que si la non-régression est confirmée.**

- Tout merge sur `main` est conditionné à la **réussite des checks de non-régression** (pipeline CI).
- Les contrôles de non-régression **doivent être exécutés localement avant le push** sur la branche de PR — pour détecter les régressions au plus tôt et ne pas attendre le CI distant.
- Le CI (GitHub Actions) constitue le filet de sécurité final et le verrou technique sur le merge.
- Cette règle sera **imposée techniquement** via le Ruleset GitHub (required status checks) dès que le premier workflow CI sera en place.
- En attendant le CI, la vérification est une contrainte de processus : l'agent exécute les tests localement avant tout push, et ne fusionne pas une PR sans confirmation de non-régression.

## Revue de code

Des agents de revue de code (ex. Bugbot, outils d'analyse statique) peuvent intervenir sur les Pull Requests. Règles d'application de leurs retours :

- Les retours ne sont **pas une source de vérité** : ils sont systématiquement soumis à contre-vérification.
- Un retour est **appliqué** s'il est pertinent et que le gain justifie le coût de la modification.
- Un retour est **rejeté** s'il est jugé non pertinent, incorrect, ou si son coût (complexité, temps, lisibilité dégradée) est disproportionné par rapport au bénéfice obtenu.
- La décision d'accepter ou rejeter un retour appartient à l'agent, dans le cadre de son autonomie décisionnelle.

## Issues

Les Issues GitHub sont utilisées ponctuellement pour tracer :

- des **bugs** à corriger
- des **fonctionnalités** à implémenter dans le futur

## Releases

Des **releases GitHub** sont publiées pour marquer les jalons significatifs du projet. Chaque release correspond à un état stable et identifiable de l'application.

## Règle absolue : interdiction de pousser directement sur `main`

> **Il est INTERDIT de commiter ou pousser directement sur la branche `main`.**
> Toute modification, sans exception, doit passer par une Pull Request.

Cette règle s'applique à l'agent comme à tout contributeur. Elle ne peut pas être imposée techniquement (protection de branche indisponible sur dépôt privé gratuit) mais constitue une contrainte de processus stricte et non négociable.

## Stratégie de branches

- `main` : branche stable, reflète l'état livrable du projet
- `feat/<sujet>` : développement de fonctionnalités
- `fix/<sujet>` : corrections de bugs
- `chore/<sujet>` : maintenance (config, refacto, outillage, CI)

## Conventions de commit

Le projet suit le standard **[Conventional Commits](https://www.conventionalcommits.org/)**.

Format : `<type>(<scope>): <description courte>`

Types : `feat`, `fix`, `chore`, `refactor`, `test`, `docs`, `ci`

Le **titre de la Pull Request** doit également respecter ce format — c'est lui qui devient le message du commit squashé sur `main`.

Exemples :

- `feat(albums): ajout de la gestion des éditions`
- `fix(devises): correction du taux de conversion franc français`
- `chore(docker): mise à jour du Dockerfile backend`

> **Note** : l'enforcement automatique du format (via ruleset GitHub) est réservé à GitHub Enterprise et n'est donc pas actif. Le respect de la convention est une contrainte de processus appliquée par l'agent.
