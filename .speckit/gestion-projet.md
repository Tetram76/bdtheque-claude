# Gestion de Projet

Ce fichier décrit la gouvernance du projet : stockage, organisation, outillage de gestion.

---

## Gouvernance documentaire

- **Source de vérité absolue** : le dossier [`.speckit/`](.) est la source de vérité absolue du projet. Il prime sur toute autre information : historique de conversation, supposition, connaissance générale de l'agent. Aucune décision ne peut le contredire sans accord explicite de l'utilisateur. En cas d'ambiguïté ou de silence sur un sujet, l'agent peut décider, mais documente alors son choix dans le fichier concerné.
- **Autonomie de l'agent** : l'agent prend toutes les décisions architecturales, techniques, fonctionnelles et d'implémentation nécessaires à la livraison de l'application, en **totale autonomie** (modalités détaillées dans « Prise de décision » ci-dessous). Les seules interventions attendues de l'utilisateur sont la mise à jour des fichiers `.speckit/` pour exprimer ses besoins et exigences.
- **Suivi d'implémentation** : l'avancement du plan d'implémentation (phases, découpage en Pull Requests, statut) est tracé dans [`suivi-implementation.md`](suivi-implementation.md), distinct des quatre fichiers de spécification ci-dessus car il décrit l'état d'avancement du projet plutôt que son contenu cible.

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
- **Le speckit n'est pas un historique de décisions** : les fichiers ne décrivent que l'état actuel et cible du projet, jamais son évolution passée. Ils ne contiennent ni historique, ni dates, ni traces de décisions successives. En cas de changement de décision, l'ancienne information est **remplacée**, jamais conservée à côté de la nouvelle avec une mention du type « anciennement », « auparavant » ou « suite à ».

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
- Le projet est guidé par les principes **KISS, DRY, YAGNI et SOLID**. Toute décision (architecture, conception, implémentation, revue) doit s'y conformer :
  - **KISS** (*Keep It Simple, Stupid*) : privilégier la solution la plus simple qui répond au besoin réel, éviter la complexité non justifiée.
  - **DRY** (*Don't Repeat Yourself*) : éviter la duplication de logique ou de connaissance ; factoriser lorsque c'est pertinent, sans sur-factoriser prématurément.
  - **YAGNI** (*You Aren't Gonna Need It*) : ne pas implémenter de fonctionnalité, abstraction ou paramétrage anticipant un besoin futur non avéré.
  - **SOLID** : respecter les cinq principes de conception orientée objet (responsabilité unique, ouvert/fermé, substitution de Liskov, ségrégation des interfaces, inversion des dépendances) dans l'organisation du code.
- Le code doit suivre les **best practices communément admises** pour chaque technologie utilisée (conventions de nommage, patterns architecturaux, sécurité, performance, etc.). Ces best practices sont à vérifier via les sources officielles (cf. règle ci-dessus).
- **Non-régression** : toute modification doit être accompagnée d'un moyen de vérifier qu'elle ne sera pas silencieusement annulée par une modification future. Le moyen de contrôle (test unitaire, test d'intégration, test de contrat, assertion, etc.) doit être **proportionné à la portée et au risque de la modification** : on n'écrit pas une suite de tests complète pour un changement trivial, mais toute logique métier ou technique non triviale doit être couverte.
- L'agent est **seul décisionnaire** sur l'architecture et l'implémentation : toute refactorisation jugée nécessaire (lisibilité, maintenabilité, testabilité, séparation des responsabilités, etc.) doit être faite sans attendre de validation.
- Le code doit être **propre et lisible** : l'utilisateur est développeur et lit le code produit.
- **Règle de commentaires** : les commentaires expliquent le **pourquoi** (intention, contrainte, décision de conception), jamais le **quoi** (ce que le code fait — le code se lit de lui-même).
- **Langue des fichiers techniques** : tout le contenu technique (code, noms de fonctions/classes/variables/constantes, commentaires, messages de log, noms de fichiers de configuration) est rédigé en **anglais**. Seule la documentation du projet (`.speckit/`, `README.md`, etc.) est rédigée en **français**. Les messages destinés à l'utilisateur final sont produits par le système de traduction du frontend (cf. `contraintes-techniques.md`), pas codés en dur dans une langue donnée.

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

## Outillage .NET

- **Gestion centralisée des packages NuGet** via `Directory.Packages.props` (Central Package Management) : toutes les versions sont déclarées à la racine de la solution, les fichiers `.csproj` ne référencent que les noms de package.
- **Tests unitaires et d'intégration** : `xunit`, exécutés via `dotnet test`. Couverture de code collectée avec `coverlet.collector`.
- **Tests d'intégration de l'API** : `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`), base de données remplacée par SQLite en mémoire pour isoler les tests du conteneur PostgreSQL.

## Intégration continue (CI)

- **GitHub Actions** héberge le pipeline de non-régression (`.github/workflows/ci.yml`), déclenché sur chaque Pull Request et sur push vers `main`.
- Étapes du pipeline : restauration, build en mode `Release`, exécution de la totalité des tests (`dotnet test`).
- Ce workflow constitue le **check de statut requis** évoqué dans la règle de merge ci-dessous, dès qu'il est activé dans le Ruleset GitHub.

## Règle de merge : non-régression et revue Codex obligatoires

> **Une Pull Request ne peut être fusionnée que si la non-régression est confirmée ET que Codex l'a approuvée.**

- Tout merge sur `main` est conditionné à la **réussite des checks de non-régression** (pipeline CI) **et** à l'**approbation de la revue Codex** (voir « Revue de code » ci-dessous).
- Les contrôles de non-régression **doivent être exécutés localement avant le push** sur la branche de PR — pour détecter les régressions au plus tôt et ne pas attendre le CI distant.
- Le CI (GitHub Actions) constitue le filet de sécurité final et le verrou technique sur le merge.
- Cette règle sera **imposée techniquement** via le Ruleset GitHub (required status checks) dès que le premier workflow CI sera en place.
- En attendant le CI, la vérification est une contrainte de processus : l'agent exécute les tests localement avant tout push, et ne fusionne pas une PR sans confirmation de non-régression ni approbation de Codex.

## Revue de code

Des agents de revue de code (ex. Bugbot, outils d'analyse statique) peuvent intervenir sur les Pull Requests. Règles générales d'application de leurs retours :

- Les retours ne sont **pas une source de vérité** : ils sont systématiquement soumis à contre-vérification.
- Un retour est **appliqué** s'il est pertinent et que le gain justifie le coût de la modification.
- Un retour est **rejeté** s'il est jugé non pertinent, incorrect, ou si son coût (complexité, temps, lisibilité dégradée) est disproportionné par rapport au bénéfice obtenu.
- La décision d'accepter ou rejeter un retour appartient à l'agent, dans le cadre de son autonomie décisionnelle.
- L'objectif de robustesse est une application **user-proof** (parcours réels, usages légitimes, erreurs utilisateur plausibles), pas un code **proof** contre tout scénario théorique ou détourné. Le code est écrit pour le contexte de cette application — il n'a pas vocation à devenir une librairie publique ni à être réutilisé hors projet.
- Les scénarios invoqués par une revue (edge cases, abus, chemins d'exécution) doivent être **pertinents, valides et possibles** dans ce contexte applicatif ; un scénario hypothétique, irréaliste ou hors périmètre ne justifie pas une complexification du code.

### Revue Codex (bloquante)

Une revue **Codex** se déclenche automatiquement à chaque commit poussé sur une Pull Request. À la différence des autres agents de revue, son approbation est une **condition bloquante du merge** : une PR ne peut être fusionnée que si Codex a réagi par un 👍 sur la PR pour le commit de tête, signe qu'une revue n'a rien trouvé à corriger.

Traitement de chaque retour d'une revue Codex :

1. **Contre-vérification** du retour (pertinence vis-à-vis de l'objectif de la PR, du contenu du `.speckit/`, des bonnes pratiques applicables, et faisabilité du scénario dans le contexte réel de l'application) — selon les règles générales ci-dessus, y compris le critère user-proof.
2. **Commit dédié** pour chaque retour validé (un commit par retour appliqué).
3. **Réponse systématique** à chaque retour, qu'il soit appliqué (avec le commit correspondant) ou rejeté (avec la justification du rejet).
4. **Résolution** de chaque conversation de revue une fois tous ses retours traités.
5. **Attente de la revue suivante** : après le push des commits, Codex relance une revue (avec un léger délai) ; l'agent attend son résultat avant de poursuivre.

Ce cycle (revue → contre-vérification → commits → réponses → résolution des conversations → attente de la revue suivante) est répété jusqu'à réaction 👍 de Codex sur le commit de tête. Le merge n'intervient qu'une fois cette approbation obtenue, en complément de la réussite du CI.

La procédure opérationnelle détaillée (commandes `gh`, requêtes GraphQL de résolution de conversation, etc.) est décrite dans le skill `.claude/skills/codex-review-loop/`.

## Issues

Les Issues GitHub sont utilisées ponctuellement pour tracer :

- des **bugs** à corriger
- des **fonctionnalités** à implémenter dans le futur

## Lien entre Pull Requests et Issues

- Une Pull Request n'a **pas systématiquement** vocation à résoudre une ou plusieurs Issues.
- Si le contenu d'une PR **répond** à une ou plusieurs Issues (correction d'un bug tracé, implémentation d'une fonctionnalité tracée), la PR **doit référencer** ces Issues (ex. mention `#<numéro>` dans la description).
- Si une Issue est **entièrement traitée** par la PR, la référence utilise un mot-clé de fermeture automatique GitHub (`Closes`, `Fixes`, `Resolves #<numéro>`), afin que l'Issue soit **automatiquement clôturée au merge** de la PR.
- Si une PR ne traite une Issue que **partiellement**, celle-ci est référencée sans mot-clé de fermeture (elle reste ouverte après le merge).

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

Le **titre de la Pull Request** doit également respecter ce format — c'est lui qui devient le titre du commit squashé sur `main`.

La quasi-totalité des PR étant fusionnées en **squash merge**, la **description de la PR devient le corps du commit** (réglage repository `squash_merge_commit_message = PR_BODY`). La description doit donc être rédigée comme un **message de commit à part entière** : contenu clair, pertinent et durable, exploitable dans l'historique Git sans avoir à consulter la PR d'origine.

- Il n'existe **pas de template de Pull Request** dans ce repository : chaque description est rédigée librement, avec **contexte, changements effectués et impact**.

Exemples :

- `feat(albums): ajout de la gestion des éditions`
- `fix(devises): correction du taux de conversion franc français`
- `chore(docker): mise à jour du Dockerfile backend`

> **Note** : l'enforcement automatique du format (via ruleset GitHub) est réservé à GitHub Enterprise et n'est donc pas actif. Le respect de la convention est une contrainte de processus appliquée par l'agent.
