# Dépôt

Dépôt GitHub : configuration, signature des commits, intégration continue, mises à jour des dépendances, branches, conventions de commit et releases.

---

## Repository

- **Hébergement** : GitHub
- **Nom du dépôt** : `Tetram76/bdtheque-claude`
- **URL** : <https://github.com/Tetram76/bdtheque-claude>
- La **gestion du repository GitHub** (configuration, branches, protections, CI/CD, etc.) relève du périmètre de l'agent et doit être maintenue conformément aux contraintes du projet.

## Signature des commits

- **Tous les commits doivent être signés et vérifiés** (GPG/SSH).
- La signature est configurée globalement sur le poste (`commit.gpgsign=true`, clé GPG `CC10F185AA085B2DD98025986A5E6B8341983E31`).
- La règle `required_signatures` est intégrée au Ruleset GitHub pour l'imposer côté serveur.

## Configuration du repository GitHub

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
  - **Checks de la CI requis** (`required_status_checks`) : chaque job du workflow CI est un check requis (cf. § « Intégration continue (CI) »). La branche n'a pas à être à jour avec `main` avant le merge : les PR sont fusionnées en squash une à une, et exiger cette mise à jour imposerait une resynchronisation systématique sans gain proportionné.
- **Sécurité du dépôt** (`security_and_analysis`, disponible gratuitement car dépôt public) :
  - **Secret scanning** : activé
  - **Push protection** (blocage des push contenant un secret détecté) : activée
  - **Dependabot alerts** : activées, lues par le relevé des versions de l'agent (§ Mises à jour des dépendances) ; **Dependabot security updates** : désactivées, leurs PR n'étant pas en brouillon
  - *Vérification de validité des secrets détectés* (`secret_scanning_validity_checks`) : désactivée, cause non identifiée — à vérifier manuellement dans les paramètres GitHub du dépôt si besoin.
- **Merge réservé à l'utilisateur, y compris pour de futurs collaborateurs** : le dépôt est la propriété d'un compte **personnel** (`Tetram76`), pas d'une organisation — la restriction de push/merge par utilisateur ou équipe (fonctionnalité GitHub de branch protection) n'est **pas disponible** sur ce type de dépôt, elle ne peut donc pas être imposée techniquement via un Ruleset ou une protection de branche. La garantie repose donc sur la gestion des droits d'accès : **aucun collaborateur ne doit recevoir un accès `Write` (ou supérieur)** au dépôt. Toute contribution externe future passe par un **fork** + Pull Request ; le merge de cette PR reste effectué par l'utilisateur (ou par l'agent agissant en son nom), jamais par le contributeur externe lui-même.

## Intégration continue (CI)

- **GitHub Actions** héberge le pipeline de non-régression (`.github/workflows/ci.yml`), déclenché sur chaque Pull Request et sur push vers `main`.
- Le contenu du pipeline (étapes, outillage de build et de test) est un choix d'implémentation : cf. `choix-implementation/socle.md` § Outillage de développement .NET.
- Le workflow CI (`ci.yml`) constitue le **check de statut requis** de la règle de merge ci-dessous : ses jobs sont imposés par le Ruleset GitHub (cf. § « Configuration du repository GitHub »). Le Ruleset les désigne par leur nom : toute modification de la liste des jobs du workflow est reportée dans le Ruleset dans le même mouvement. Sans cela, un job ajouté n'est pas requis — son échec n'empêche pas la fusion —, et un job renommé ou retiré reste attendu sous son ancien nom sans jamais se présenter, ce qui bloque toute fusion.
- **Publication des images Docker** sur Docker Hub (`contraintes-techniques/architecture-deploiement.md` § Déploiement) : workflow distinct, `.github/workflows/docker-publish.yml` (cf. `choix-implementation/deploiement.md` § Publication des images Docker). Il s'authentifie auprès de Docker Hub avec le compte `tetram76` et un jeton d'accès (*Personal Access Token* Docker Hub, droits *Read, Write & Delete*, la suppression de l'image `nightly` remplacée l'exigeant) stocké dans le secret de dépôt GitHub `DOCKERHUB_TOKEN`.
- **Analyse des vulnérabilités dans le CI** (`gestion-projet/revue-tickets.md` § « Analyse des vulnérabilités des images Docker ») : le CI lit les images publiées sur Docker Hub avec le même jeton (`DOCKERHUB_TOKEN`). Le code de la branche et de ses dépendances ne s'exécute que dans les conteneurs de construction des images, qui ne reçoivent pas les identifiants du registre, et seuls l'utilisateur et l'agent agissant en son nom ouvrent des Pull Requests qui accèdent aux secrets. Une PR venant d'un **fork** n'y a pas accès : elle échoue à ce contrôle et ne peut pas être fusionnée en l'état. C'est assumé : il revient à son auteur de s'en accommoder, et aucun chemin de contrôle sans secret n'est prévu pour elle.
- **Quotas consommés par le CI** :
  - **API GitHub** : le jeton du workflow (`GITHUB_TOKEN`) est limité à **1 000 requêtes par heure et par dépôt** (documentation GitHub, *Rate limits for the REST API*), partagées par toutes les exécutions des workflows du dépôt ; l'analyse des vulnérabilités l'utilise notamment pour télécharger son outil. Au-delà, GitHub répond `403` ou `429`.
  - **Docker Hub** : Docker Hub limite les téléchargements d'images (*pulls*) à **200 par période de 6 heures** pour un compte *Personal* authentifié comme `tetram76`, et à **100 par période de 6 heures et par adresse IP** sans authentification ; une image multi-architecture compte un téléchargement par architecture (documentation Docker Hub, *Usage and limits*). Au-delà, Docker Hub répond `429 Too Many Requests`, quel que soit le contenu de la PR. Chaque exécution du CI lit des images sur Docker Hub : l'image publiée, que l'analyse des vulnérabilités lit avec le compte `tetram76`, et l'image PostgreSQL des tests, téléchargée sans authentification.

  Face à un échec dû à l'un de ces quotas, l'agent ne pousse pas de nouveau commit, qui le consommerait à son tour : il relance les seuls jobs en échec (`gh run rerun --failed`) une fois la période du quota écoulée, et signale à l'utilisateur un échec qui persiste.

## Mises à jour des dépendances

Les montées de version relèvent de l'agent (point technique, `gestion-projet/gouvernance.md` § Prise de décision) : il les détecte et les traite sans attendre de demande de l'utilisateur.

- **Toutes les technologies surveillées** : chaque version que le dépôt utilise est **épinglée** à sa valeur exacte, pour qu'aucune ne change sans une modification du dépôt, donc sans montée de version visible :
  - packages NuGet (`Directory.Packages.props`), outil `dotnet-ef` (`dotnet-tools.json`) et SDK .NET (image `sdk` des Dockerfiles et CI, à la même version ; `global.json` n'en fixe que le minimum, pour les postes de développement) ;
  - images de base des Dockerfiles et image de `db` (`docker-compose.yml`, et l'image identique des tests, `PostgreSqlTestServer`) : version exacte du composant (ex. version corrective de .NET, de PostgreSQL et version d'Alpine), sans empreinte (*digest*) : l'éditeur reconstruit l'image sous le même tag pour les correctifs de son système, qui arrivent ainsi sans attendre (choix-implementation/deploiement.md § Images de base des conteneurs) ;
  - actions GitHub des workflows.

  Les images de l'application, publiées par le dépôt lui-même, n'en sont pas.
- **Relevé par l'agent** (`scripts/Get-DependencyUpdates.ps1`) : à chaque reprise du travail, l'agent exécute ce script, qui interroge les sources officielles (NuGet, métadonnées de publication de .NET, registres Docker Hub et Microsoft, versions publiées des actions GitHub) et les alertes Dependabot du dépôt. Il liste chaque version plus récente que celle épinglée, avec sa portée (corrective, mineure, majeure) et sa date de publication, sans rien modifier.
  - **Aucun outil externe** : le relevé n'emploie que les sources officielles et une fonction native de GitHub, les alertes Dependabot, qui n'ouvrent aucune PR. Aucun robot n'écrit dans le dépôt : une montée est une Pull Request de l'agent, faite de ses commits signés.
    - **Alternative écartée (mises à jour de Dependabot, *version updates* et *security updates*)** : leurs PR, prêtes pour la revue dès leur ouverture, déclencheraient une revue Codex avant même la revue de l'agent (`gestion-projet/gouvernance.md` § Budget IA).
    - **Alternative écartée (Renovate, application hébergée ou auto-hébergée)** : un outil externe de plus, doté d'un accès en écriture au dépôt ; auto-hébergé, il exige en outre une application GitHub et ses secrets, seule identité sous laquelle ses commits seraient signés (`required_signatures`). Son apport, ouvrir lui-même les PR, est sans objet : la montée n'est traitée qu'à la reprise du travail, par l'agent, qui fait sa revue et ses adaptations.
  - **Correctifs de sécurité** : une alerte Dependabot ouverte est traitée en tête, sans attendre la montée mensuelle ni le délai de 7 jours.
  - **Autres versions** : une montée par mois au plus, au premier relevé d'un mois qui n'en a pas encore connu : les versions mineures et correctives disponibles y sont réunies en une seule Pull Request, pour limiter les revues Codex et les exécutions du CI (quotas, § Intégration continue (CI)).
  - **Délai de 7 jours** avant de retenir une version tout juste publiée (colonne `TooRecent` du relevé), pour qu'une version retirée ou corrigée aussitôt ne soit pas prise.
  - **Versions majeures** : celle d'une technologie de la stack est un chantier à part (ci-dessous) ; celle d'une autre dépendance rejoint la montée mensuelle.
  - **Hors de la revue Codex jusqu'à la revue de l'agent** : la Pull Request d'une montée est ouverte en brouillon (`gestion-projet/revue-tickets.md` § Ouverture de la PR) ; l'agent ne la passe en « prête pour la revue » qu'une fois sa revue de l'existant faite (ci-dessous), ce qui déclenche la seule revue Codex de la montée.
- **Aucune montée sans sa revue** : toute montée de version, quelle qu'en soit la portée (correctif de sécurité compris), et quelle que soit la technologie (bibliothèque, SDK, image Docker, PostgreSQL, action GitHub…), est une montée de version au sens de `contraintes-techniques/index.md` § Exploitation des technologies retenues. Sa **revue de l'existant**, au regard de la nouvelle version, est donc une condition de sa fusion, au même titre que la non-régression et la revue Codex (`gestion-projet/revue-tickets.md` § Règle de merge) : l'agent la mène **avant** de passer la PR en « prête pour la revue », et une PR dont la revue n'est pas faite n'est pas fusionnée.
  - **Moment** : à chaque reprise du travail, avant le chantier suivant du plan, après le relevé ; une alerte de sécurité passe en tête.
  - **Contenu**, pour chaque dépendance montée, à partir de ses notes de version et de la documentation officielle, depuis la version remplacée : changements de comportement, dépréciations et changements cassants qui touchent le dépôt, à corriger dans la PR ; nouveautés qui amélioreraient l'existant (performances, lisibilité, sécurité, concision, maintenabilité…), confrontées à l'usage réel qu'en fait le dépôt — code, schéma et requêtes de la base, Dockerfiles et fichier Compose, workflows, configuration —, selon les mêmes exigences que la revue de l'existant de toute la stack : gain réel, contre-vérification sur la documentation officielle.
  - **Proportion** : la revue suit la portée de la montée — les seules notes d'une version corrective, toutes les nouveautés d'une version mineure, l'ensemble de l'existant pour une version majeure.
  - **Trace** : la description de la PR, qui devient le corps du commit (§ Conventions de commit) et que l'agent réécrit selon le template avant de la passer en « prête pour la revue », présente la revue dépendance par dépendance, et ses conclusions : correction faite dans la PR, chantier d'amélioration identifié (un ticket chacun, rédigé selon `gestion-projet/revue-tickets.md` § Contenu d'un ticket, réalisé par sa propre PR), ou absence de chantier, constatée explicitement.
- **Version majeure d'une technologie de la stack** (`contraintes-techniques/index.md` § Stack cible, et les composants qu'elle retient : runtime et SDK .NET, ASP.NET Core, EF Core, Npgsql, PostgreSQL, images de base) : la montée est un chantier, avec son ticket, qui porte la montée et son adaptation (PR de l'agent), puis, à sa fusion, le ticket de la revue de l'existant au regard de la nouvelle version, menée comme celle de toute la stack. La décision d'y procéder et son moment relèvent de l'agent, dans le respect des versions retenues (ex. les seules versions LTS de .NET, `choix-implementation/index.md` § Versions et bibliothèques). Une montée qui a un impact visible sur le déploiement (ex. version majeure de PostgreSQL après le premier déploiement, qui impose une sauvegarde et une restauration des données) est annoncée à l'utilisateur (`gestion-projet/gouvernance.md` § Prise de décision).
- **Toute autre montée** (version mineure ou corrective, version majeure d'une autre dépendance : actions GitHub, outils et bibliothèques de test…) : traitée dans la PR de la montée, mensuelle ou de sécurité, revue et adaptations comprises, poussées tant qu'elle est en brouillon.

## Règle absolue : interdiction de pousser directement sur `main`

> **Il est INTERDIT de commiter ou pousser directement sur la branche `main`.**
> Toute modification, sans exception, doit passer par une Pull Request.

Cette règle s'applique à l'agent comme à tout contributeur. Elle est **imposée techniquement** par le Ruleset GitHub (cf. « Configuration du repository GitHub » ci-dessus) et constitue par ailleurs une contrainte de processus stricte et non négociable.

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

**Ce réglage ne doit pas être modifié** : il porte à la fois la qualité de l'historique et la traçabilité jusqu'au ticket (cf. `gestion-projet/revue-tickets.md` § « Suivi par tickets »). Avec une autre valeur, le corps du commit squashé n'est plus la description de la PR (`COMMIT_MESSAGES` : concaténation des messages des commits intermédiaires ; `BLANK` : corps vide). Le mot-clé de fermeture (`Closes #<numéro>`) n'apparaît alors plus dans le commit final : celui-ci ne mène plus directement au ticket, seul le suffixe `(#<PR>)` du titre mène à la PR, et il faut passer par elle pour retrouver le ticket. La description, rédigée comme un message de commit, ne figure plus non plus dans l'historique de `main`. Toute modification de ce réglage exige donc de revoir ces règles.

La description de toute Pull Request **suit toujours le template** du projet `.github` : le dépôt [`Tetram76/.github`](https://github.com/Tetram76/.github) du compte, qui porte les fichiers de santé communautaire par défaut de tous ses dépôts, dont `.github/pull_request_template.md`. Sections et consignes du template sont à suivre. Il n'est pas copié dans ce dépôt : il est relu à chaque PR.

Exemples :

- `feat(albums): ajout de la gestion des éditions`
- `fix(devises): correction du taux de conversion franc français`
- `chore(docker): mise à jour du Dockerfile backend`

> **Note** : l'enforcement automatique du format (via ruleset GitHub) est réservé à GitHub Enterprise et n'est donc pas actif. Le respect de la convention est une contrainte de processus appliquée par l'agent.

## Releases

Des **releases GitHub** sont publiées pour marquer les jalons significatifs du projet. Chaque release correspond à un état stable et identifiable de l'application.
