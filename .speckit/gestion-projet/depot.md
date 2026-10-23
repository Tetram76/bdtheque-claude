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
  - **Dependabot security updates** : activé
  - *Vérification de validité des secrets détectés* (`secret_scanning_validity_checks`) : désactivée, cause non identifiée — à vérifier manuellement dans les paramètres GitHub du dépôt si besoin.
- **Merge réservé à l'utilisateur, y compris pour de futurs collaborateurs** : le dépôt est la propriété d'un compte **personnel** (`Tetram76`), pas d'une organisation — la restriction de push/merge par utilisateur ou équipe (fonctionnalité GitHub de branch protection) n'est **pas disponible** sur ce type de dépôt, elle ne peut donc pas être imposée techniquement via un Ruleset ou une protection de branche. La garantie repose donc sur la gestion des droits d'accès : **aucun collaborateur ne doit recevoir un accès `Write` (ou supérieur)** au dépôt. Toute contribution externe future passe par un **fork** + Pull Request ; le merge de cette PR reste effectué par l'utilisateur (ou par l'agent agissant en son nom), jamais par le contributeur externe lui-même.

## Intégration continue (CI)

- **GitHub Actions** héberge le pipeline de non-régression (`.github/workflows/ci.yml`), déclenché sur chaque Pull Request et sur push vers `main`.
- Le contenu du pipeline (étapes, outillage de build et de test) est un choix d'implémentation : cf. `choix-implementation/socle.md` § Outillage de développement .NET.
- Le workflow CI (`ci.yml`) constitue le **check de statut requis** de la règle de merge ci-dessous : ses jobs sont imposés par le Ruleset GitHub (cf. § « Configuration du repository GitHub »). Le Ruleset les désigne par leur nom : toute modification de la liste des jobs du workflow est reportée dans le Ruleset dans le même mouvement. Sans cela, un job ajouté n'est pas requis — son échec n'empêche pas la fusion —, et un job renommé ou retiré reste attendu sous son ancien nom sans jamais se présenter, ce qui bloque toute fusion.
- **Publication des images Docker** sur Docker Hub (`contraintes-techniques/architecture-deploiement.md` § Déploiement) : workflow distinct, `.github/workflows/docker-publish.yml` (cf. `choix-implementation/deploiement.md` § Publication des images Docker). Il s'authentifie auprès de Docker Hub avec le compte `tetram76` et un jeton d'accès (*Personal Access Token* Docker Hub, droits *Read, Write & Delete*, la suppression de l'image `nightly` remplacée l'exigeant) stocké dans le secret de dépôt GitHub `DOCKERHUB_TOKEN`.
- **Analyse des vulnérabilités dans le CI** (`gestion-projet/revue-tickets.md` § « Analyse des vulnérabilités des images Docker ») : le CI lit les images publiées sur Docker Hub avec le même jeton (`DOCKERHUB_TOKEN`). Le code de la branche et de ses dépendances ne s'exécute que dans les conteneurs de construction des images, qui ne reçoivent pas les identifiants du registre, et seuls l'utilisateur, l'agent agissant en son nom et Dependabot ouvrent des Pull Requests qui accèdent aux secrets. Une PR venant d'un **fork** n'y a pas accès : elle échoue à ce contrôle et ne peut pas être fusionnée en l'état. C'est assumé : il revient à son auteur de s'en accommoder, et aucun chemin de contrôle sans secret n'est prévu pour elle. Les PR de Dependabot n'ayant accès qu'aux secrets Dependabot, le jeton y est aussi déclaré comme secret Dependabot, sous le même nom.
- **Quotas consommés par le CI** :
  - **API GitHub** : le jeton du workflow (`GITHUB_TOKEN`) est limité à **1 000 requêtes par heure et par dépôt** (documentation GitHub, *Rate limits for the REST API*), partagées par toutes les exécutions des workflows du dépôt ; l'analyse des vulnérabilités l'utilise notamment pour télécharger son outil. Au-delà, GitHub répond `403` ou `429`.
  - **Docker Hub** : Docker Hub limite les téléchargements d'images (*pulls*) à **200 par période de 6 heures** pour un compte *Personal* authentifié comme `tetram76`, et à **100 par période de 6 heures et par adresse IP** sans authentification ; une image multi-architecture compte un téléchargement par architecture (documentation Docker Hub, *Usage and limits*). Au-delà, Docker Hub répond `429 Too Many Requests`, quel que soit le contenu de la PR. Chaque exécution du CI lit des images sur Docker Hub : l'image publiée, que l'analyse des vulnérabilités lit avec le compte `tetram76`, et l'image PostgreSQL des tests, téléchargée sans authentification.

  Face à un échec dû à l'un de ces quotas, l'agent ne pousse pas de nouveau commit, qui le consommerait à son tour : il relance les seuls jobs en échec (`gh run rerun --failed`) une fois la période du quota écoulée, et signale à l'utilisateur un échec qui persiste.

## Mises à jour des dépendances

Les montées de version relèvent de l'agent (point technique, `gestion-projet/gouvernance.md` § Prise de décision) : il les détecte et les traite sans attendre de demande de l'utilisateur.

- **Détection par Dependabot** : une version disponible arrive sous la forme d'une Pull Request, que l'agent n'a pas à chercher lui-même.
  - **Correctifs de sécurité** (*Dependabot security updates*, § Configuration du repository GitHub) : proposés dès la publication de l'alerte.
  - **Autres versions** (*version updates*, `.github/dependabot.yml`) : proposées **une fois par mois**, pour chaque écosystème du dépôt — packages NuGet (outil `dotnet-ef` compris), SDK .NET (`global.json`), images de base des Dockerfiles, image de `db` (`docker-compose.yml`), actions GitHub. Les images de l'application, publiées par le dépôt lui-même, n'en sont pas.
  - **Coût maîtrisé** : chaque Pull Request consomme une revue Codex et les quotas du CI (`gestion-projet/gouvernance.md` § Budget IA). Les versions mineures et correctives d'un écosystème sont donc réunies en une seule PR, et Dependabot ne met jamais une PR à jour de lui-même (*rebase*), chaque mise à jour étant un push : l'agent la demande au besoin, pour une PR qu'il va fusionner (commentaire `@dependabot rebase`).
  - **Délai de 7 jours** avant de proposer une version tout juste publiée (*cooldown*), pour qu'une version retirée ou corrigée aussitôt ne soit pas proposée ; il ne s'applique pas aux correctifs de sécurité.
  - **Plateforme .NET** : ses versions majeures (SDK, images de base, packages alignés sur la version de .NET, fournisseur Npgsql) ne sont pas proposées, la plateforme ne suivant que les versions LTS (`choix-implementation/index.md` § Versions et bibliothèques) : la montée vers la LTS suivante est tracée par son propre ticket, ouvert d'avance.
- **Traitement en priorité** : à chaque reprise du travail, avant le chantier suivant du plan, l'agent traite les Pull Requests de Dependabot ouvertes, comme toute PR (§ Règle de merge de `gestion-projet/revue-tickets.md`, revue Codex comprise) :
  - **version mineure ou corrective** : contre-vérification des notes de version (changement de comportement, dépréciation, nouveauté utile à l'existant, `contraintes-techniques/index.md` § Exploitation des technologies retenues), adaptation éventuelle du code poussée sur la branche de la PR, puis fusion. La description, qui devient le corps du commit (§ Conventions de commit), est réécrite selon le template avant la fusion ;
  - **version majeure d'une technologie de la stack** (`contraintes-techniques/index.md` § Stack cible, et les composants qu'elle retient : runtime et SDK .NET, ASP.NET Core, EF Core, Npgsql, PostgreSQL, images de base) : c'est un chantier, avec son ticket, qui porte la montée elle-même, puis la revue de l'existant qu'exige `contraintes-techniques/index.md` § Exploitation des technologies retenues ; la PR de Dependabot est fermée au profit de ce ticket. Une montée qui a un impact visible sur le déploiement (ex. version majeure de PostgreSQL après le premier déploiement, qui impose une sauvegarde et une restauration des données) est annoncée à l'utilisateur (`gestion-projet/gouvernance.md` § Prise de décision) ;
  - **version majeure d'une autre dépendance** (actions GitHub, outils et bibliothèques de test…) : traitée dans sa PR, comme une version mineure, la revue de l'usage qu'en fait le dépôt comprise.

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
