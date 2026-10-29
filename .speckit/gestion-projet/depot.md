# Dépôt

Dépôt GitHub : configuration, signature des commits, intégration continue, analyse de la qualité du code, mises à jour des dépendances, branches, conventions de commit, versions et releases.

---

## Repository

- **Hébergement** : GitHub
- **Nom du dépôt** : `Tetram76/bdtheque-claude`
- **URL** : <https://github.com/Tetram76/bdtheque-claude>
- La **gestion du repository GitHub** (configuration, branches, protections, sécurité, CI/CD, etc.) relève du périmètre de l'agent et doit être maintenue conformément aux contraintes du projet. Il en va de même des **services externes rattachés au dépôt** : paramétrage du projet SonarQube Cloud et marquage de ses problèmes (§ Analyse de la qualité du code), dépôts Docker Hub de l'application.
  - L'agent accomplit lui-même ces actes (`gestion-projet/gouvernance.md` § Prise de décision, acte de gestion du projet), et ne les laisse jamais à l'utilisateur comme une tâche à faire de son côté. Il passe par l'**API** de chaque service, avec l'accès que l'utilisateur a donné au poste : `gh` pour GitHub ; pour SonarQube Cloud, un jeton personnel de l'utilisateur (l'offre gratuite n'en connaît pas d'autre) placé dans la variable d'environnement `SONAR_TOKEN` du poste. L'agent agit ainsi au nom de l'utilisateur, comme sur GitHub. Il n'agit jamais par la session de l'utilisateur dans un navigateur.
  - Seul ce qui exige l'utilisateur en personne lui revient : se connecter à un service, créer un secret ou un jeton et en placer la valeur (secret de dépôt, variable d'environnement du poste), qu'aucun agent ne lit ni ne manipule.
  - Un acte que l'outillage de l'agent lui refuse (ex. une commande bloquée par le mode automatique de Claude Code) n'est pas contourné : l'agent nomme l'acte refusé, la commande et la raison du refus, et demande à l'utilisateur de l'autoriser.

## Signature des commits

- **Tous les commits doivent être signés et vérifiés** (GPG/SSH).
- La signature est configurée globalement sur le poste (`commit.gpgsign=true`, clé GPG `CC10F185AA085B2DD98025986A5E6B8341983E31`).
- La règle `required_signatures` est intégrée au Ruleset GitHub pour l'imposer côté serveur.
- **Vérification sur GitHub uniquement** : seul compte l'état que GitHub constate, celui que contrôle `required_signatures` : la mention « Verified » d'un commit, ou le champ `verification` de l'API des commits (`verified`, `reason`). La signature d'un commit poussé se vérifie donc toujours là, jamais en local : `git log --show-signature` ou `%G?` ne connaissent que les clés déclarées sur le poste (ex. `gpg.ssh.allowedSignersFile` pour une signature SSH), et peuvent ne pas reconnaître une signature que GitHub vérifie.

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
  - **Porte de qualité de SonarQube Cloud requise** : le check `SonarCloud Code Analysis`, que l'application GitHub de SonarQube Cloud (`integration_id` `12526`) publie sur chaque commit analysé, en échec si la porte de qualité échoue (cf. § « Analyse de la qualité du code »).
- **Sécurité du dépôt** (`security_and_analysis`, disponible gratuitement car dépôt public) :
  - **Secret scanning** : activé
  - **Push protection** (blocage des push contenant un secret détecté) : activée
  - **Dependabot alerts** : désactivées, faute de rien couvrir : le graphe de dépendances de GitHub ne lit pas les versions de `Directory.Packages.props`, et Dependabot n'alerte que sur les actions épinglées par version, non par empreinte (documentation GitHub, *About Dependabot alerts*) ; le contrôle de sécurité quotidien en tient lieu (§ Mises à jour des dépendances, vulnérabilités connues) ; **Dependabot security updates** : désactivées, leurs PR n'étant pas en brouillon
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

## Analyse de la qualité du code

- **SonarQube Cloud** (organisation `tetram76`, projet `Tetram76_bdtheque-claude`, gratuit pour un dépôt public) analyse le code et sa couverture par les tests, sur chaque Pull Request et sur `main`. Ses indicateurs (porte de qualité, couverture, fiabilité, sécurité, maintenabilité…) sont affichés en tête du `README.md`, et il commente chaque Pull Request de son analyse.
- **Workflow distinct** (`.github/workflows/code-quality.yml`), hors du workflow CI : le scanner (`dotnet-sonarscanner`, outil local épinglé de `dotnet-tools.json`) désactive `TreatWarningsAsErrors` dans la compilation qu'il instrumente ; il ne peut donc pas remplacer celle du CI, qui reste le contrôle de non-régression.
- **Porte de qualité, condition de fusion** (`gestion-projet/revue-tickets.md` § Règle de merge) : imposée par le Ruleset, qui requiert le check `SonarCloud Code Analysis` de l'application SonarQube Cloud (§ Configuration du repository GitHub), et non le job du workflow : ce check reflète le verdict de la porte, et n'existe que si l'analyse a abouti. Une analyse qui n'aboutit pas, faute de service ou de jeton, bloque donc la fusion.
- **Retours** : ceux de SonarQube Cloud sont ceux d'un outil d'analyse statique, traités dans le cycle de chaque Pull Request selon `gestion-projet/revue-tickets.md` § Analyse SonarQube Cloud.
- **Jetons** : l'analyse s'authentifie auprès de SonarQube Cloud avec le secret de dépôt GitHub `SONAR_TOKEN` ; l'agent, pour le marquage des problèmes et le paramétrage du projet, avec un jeton distinct, placé dans la variable d'environnement `SONAR_TOKEN` du poste (§ Repository), pour que chacun se révoque sans toucher à l'autre. Une Pull Request venant d'un **fork** n'y a pas accès : son analyse échoue et elle ne peut pas être fusionnée en l'état, comme pour l'analyse des vulnérabilités des images (§ Intégration continue (CI)). L'analyse automatique de SonarQube Cloud est désactivée sur le projet : elle exclut l'analyse par le CI, et ne mesure pas la couverture.
- **Périmètre** : le code de la solution, celui de `tests/` comme code de test ; les maquettes de `.speckit/`, les prototypes de `poc/` et les migrations générées par EF Core en sont exclus. Les scripts PowerShell de l'outillage (ex. `scripts/Get-DependencyUpdates.ps1`) et les scripts JavaScript du frontend (`src/Bdtheque.Frontend/wwwroot`, ex. `browser-time-zone.js`) sont analysés, mais exclus de la couverture, que seuls les tests .NET mesurent : ils y compteraient toujours comme non couverts. Exclure un script du frontend de cette mesure ne le dispense pas de tests : chacun de ses comportements est fixé par les tests de bout en bout, qui l'exécutent dans le navigateur (`choix-implementation/socle.md` § Outillage de développement .NET, tests du frontend). La couverture, que seule cette analyse mesure, suit `choix-implementation/socle.md` § Outillage de développement .NET.
- **Nouveau code** : celui qui a changé depuis la dernière version publiée (§ Versions et releases) : le projet est réglé sur la version précédente (*Previous version*), et chaque analyse porte le tag de la dernière version publiée ; la publication d'une version relance l'analyse de `main` pour que la période commence au commit publié (§ Versions et releases). Sur une Pull Request, le nouveau code est celui de la PR.

## Mises à jour des dépendances

Les montées de version relèvent de l'agent (point technique, `gestion-projet/gouvernance.md` § Prise de décision) : il les détecte et les traite sans attendre de demande de l'utilisateur.

- **Toutes les technologies surveillées** : chaque version que le dépôt utilise est **épinglée** à sa valeur exacte, pour qu'aucune ne change sans une modification du dépôt, donc sans montée de version visible :
  - packages NuGet (`Directory.Packages.props`), outil `dotnet-ef` (`dotnet-tools.json`) et SDK .NET (image `sdk` des Dockerfiles et workflows, à la même version, alignement vérifié par un test ; `global.json` n'en fixe que le minimum, pour les postes de développement) ;
  - images de base des Dockerfiles et image de `db` (`docker-compose.yml`, et l'image identique des tests, `PostgreSqlTestServer`) : version exacte du composant (ex. version corrective de .NET, de PostgreSQL et version d'Alpine), sans empreinte (*digest*) : l'éditeur reconstruit l'image sous le même tag pour les correctifs de son système, qui arrivent ainsi sans attendre (choix-implementation/deploiement.md § Images de base des conteneurs) ;
  - actions GitHub des workflows : empreinte complète du commit (*commit SHA*) de leur version exacte, cette version en commentaire (`uses: <action>@<empreinte> # v1.2.3`). Un tag de version reste déplaçable, par l'éditeur de l'action ou par qui compromet son dépôt, sauf pour une release immuable (documentation GitHub, *Immutable releases*), que peu d'actions publient : le code exécuté par les workflows, avec les secrets du dépôt, changerait sans modification du dépôt. Une empreinte, elle, ne désigne jamais un autre commit, et le relevé (ci-dessous) vérifie qu'elle est bien celle de la version du commentaire. À la différence d'une image, aucun correctif ne parvient par le déplacement d'un tag d'action : l'empreinte ne retarde rien.
    - **Alternative écartée (tag de la version seul)** : un tag déplacé, comme l'ont été ceux d'actions compromises, fait exécuter un autre code sans rien dans le dépôt qui le montre ni en déclenche la revue.

  Les images de l'application, publiées par le dépôt lui-même, n'en sont pas.
- **Relevé par l'agent** (`scripts/Get-DependencyUpdates.ps1`) : à chaque reprise du travail, l'agent vérifie si le relevé est dû (au plus un par contrôle de sécurité quotidien, ci-dessous, fréquence) et, s'il l'est, exécute ce script, qui interroge les sources officielles (NuGet, métadonnées de publication de .NET, registres Docker Hub et Microsoft, versions publiées des actions GitHub). Il liste, pour chaque technologie épinglée, la version la plus récente de sa version majeure et, à part, la version majeure la plus récente, pour qu'une version majeure écartée (ex. une version de .NET qui n'est pas LTS) ne masque pas les correctifs de celle en usage ; chacune avec sa portée (corrective, mineure, majeure) et sa date de publication, ainsi que les vulnérabilités connues, celles des packages NuGet et celles que tient le contrôle de sécurité quotidien (ci-dessous), sans rien modifier. Il s'arrête sur une action dont l'empreinte n'est pas celle de la version de son commentaire.
  - **Fréquence : un relevé par contrôle de sécurité quotidien.** Entre deux exécutions du contrôle quotidien (ci-dessous), rien de ce qui décide d'une montée ne change : les vulnérabilités connues sont celles du contrôle, et une version tout juste publiée attend 7 jours (ci-dessous). Plusieurs chantiers lancés le même jour ne refont donc ni le relevé ni sa revue. Chaque relevé laisse une **trace** dans la **description** du ticket des vulnérabilités connues, que l'agent modifie : une section « Relevé des versions du `<date>` », ajoutée après la liste des vulnérabilités, qui donne sa conclusion (rien à appliquer, ou la Pull Request de la correction ou de la montée). Le contrôle quotidien réécrit cette description en entier à chaque exécution, si bien que la section disparaît avec lui : à la reprise du travail, le relevé n'est dû que si la description du ticket ne porte pas cette section. Sinon, la reprise passe directement au chantier, la conclusion du relevé restant valable jusqu'au contrôle suivant (une Pull Request qu'il a ouverte se poursuit selon ses propres règles).
    - **Alternative écartée (commentaire du ticket à chaque relevé)** : les commentaires s'accumuleraient sans fin sur un ticket qui reste ouvert tant qu'une vulnérabilité est connue, alors que seule compte la conclusion du dernier relevé.
    - **Alternative écartée (trace dans un fichier du dépôt)** : elle exigerait un commit sur `main` à chaque relevé, ce qu'interdit § Règle absolue : interdiction de pousser directement sur `main`.
    - **Alternative écartée (relevé exécuté par le contrôle quotidien lui-même)** : le script n'est que la moitié du travail ; décider, pour chaque vulnérabilité et chaque version, de ce qui est applicable reste la revue de l'agent, qu'il faudrait tracer de toute façon.
  - **Aucun outil externe** : le relevé et le contrôle de sécurité n'emploient que les sources officielles, l'outillage du SDK .NET, Docker Scout (déjà employé par le CI) et la base des vulnérabilités de GitHub (*GitHub Advisory Database*), qui n'ouvre aucune PR. Aucun robot ne modifie le contenu du dépôt ni n'ouvre de Pull Request, le contrôle de sécurité ne tenant qu'un ticket : une montée est une Pull Request de l'agent, faite de ses commits signés.
    - **Alternative écartée (mises à jour de Dependabot, *version updates* et *security updates*)** : leurs PR, prêtes pour la revue dès leur ouverture, déclencheraient une revue Codex avant même la revue de l'agent (`gestion-projet/gouvernance.md` § Budget IA).
    - **Alternative écartée (Renovate, application hébergée ou auto-hébergée)** : un outil externe de plus, doté d'un accès en écriture au dépôt ; auto-hébergé, il exige en outre une application GitHub et ses secrets, seule identité sous laquelle ses commits seraient signés (`required_signatures`). Son apport, ouvrir lui-même les PR, est sans objet : la montée n'est traitée qu'à la reprise du travail, par l'agent, qui fait sa revue et ses adaptations.
  - **Vulnérabilités connues** (`contraintes-techniques/index.md` § Suivi des versions et des vulnérabilités), surveillées chaque jour, que le dépôt fasse ou non l'objet de travaux, par le contrôle de sécurité quotidien (`.github/workflows/security-audit.yml`), avec pour chaque technologie l'outil qui lit réellement les versions en usage :
    - packages NuGet, transitifs compris : audit NuGet (*GitHub Advisory Database*, `dotnet list package --vulnerable --include-transitive`), également exécuté par le relevé ;
    - images de l'application publiées (système, runtime .NET) et image de `db` (PostgreSQL) : Docker Scout, toutes vulnérabilités connues, corrigées ou non ;
    - actions GitHub : *GitHub Advisory Database*, interrogée pour chaque version en usage (API `advisories`, `affects=<action>@<version>`), celle du commentaire de son empreinte.

    Le contrôle tient la liste des vulnérabilités connues dans un ticket (« Vulnérabilités connues », étiquette `security-audit`), ouvert tant que la liste n'est pas vide, et que le relevé affiche. Il n'échoue que lorsqu'il trouve une vulnérabilité absente de cette liste : GitHub notifie alors l'échec à l'utilisateur qui a créé le workflow, une seule fois par vulnérabilité, et non chaque jour tant qu'elle reste sans correction.
  - **Correction dès qu'elle est disponible** : à chaque relevé, l'agent cherche, pour chaque vulnérabilité, une correction applicable (version corrigée d'un package, tag plus récent ou reconstruit d'une image) ; il l'applique en tête, sans attendre la montée mensuelle ni le délai de 7 jours. Une vulnérabilité sans correction disponible reste suivie par le contrôle jusqu'à la parution de sa correction.
  - **Aucun blocage par une vulnérabilité qu'on ne peut pas encore corriger** : le contrôle quotidien n'est pas un check requis, et l'audit NuGet n'est qu'un avertissement dans les constructions (`WarningsNotAsErrors`, `Directory.Build.props`) malgré `TreatWarningsAsErrors` ; ni le développement ni la fusion d'une PR n'attendent donc une correction qui n'existe pas.
  - **Autres versions** : une montée par mois au plus, au premier relevé d'un mois qui n'en a pas encore connu : les versions mineures et correctives disponibles y sont réunies en une seule Pull Request, pour limiter les revues Codex et les exécutions du CI (quotas, § Intégration continue (CI)).
  - **Délai de 7 jours** avant de retenir une version tout juste publiée (colonne `TooRecent` du relevé), pour qu'une version retirée ou corrigée aussitôt ne soit pas prise.
  - **Versions majeures** : celle d'une technologie de la stack est un chantier à part (ci-dessous) ; celle d'une autre dépendance rejoint la montée mensuelle.
  - **Hors de la revue Codex jusqu'à la revue de l'agent** : la Pull Request d'une montée est ouverte en brouillon (`gestion-projet/revue-tickets.md` § Ouverture de la PR) ; l'agent ne la passe en « prête pour la revue » qu'une fois sa revue de l'existant faite (ci-dessous), ce qui déclenche la seule revue Codex de la montée.
- **Aucune montée sans sa revue** : toute montée de version, quelle qu'en soit la portée (correctif de sécurité compris), et quelle que soit la technologie (bibliothèque, SDK, image Docker, PostgreSQL, action GitHub…), est une montée de version au sens de `contraintes-techniques/index.md` § Exploitation des technologies retenues. Sa **revue de l'existant**, au regard de la nouvelle version, est donc une condition de sa fusion, au même titre que la non-régression et la revue Codex (`gestion-projet/revue-tickets.md` § Règle de merge) : l'agent la mène **avant** de passer la PR en « prête pour la revue », et une PR dont la revue n'est pas faite n'est pas fusionnée.
  - **Moment** : à chaque relevé (§ Mises à jour des dépendances, fréquence), juste après lui et avant le chantier suivant du plan ; une alerte de sécurité passe en tête.
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

## Versions et releases

Les versions de l'application sont connues du dépôt avant leur publication : chaque version à venir est un **jalon** GitHub (*milestone*), auquel sont rattachés les tickets et les Pull Requests qu'elle livre ; chaque version publiée est un **tag** et une **release** GitHub. Une version correspond à un état stable et identifiable de l'application, sur `main`.

- **Numérotation** : [Semantic Versioning](https://semver.org/) (`MAJEUR.MINEUR.CORRECTIF`), tag `v<version>` (ex. `v0.1.0`) :
  - `0.0.x` : les phases qui ne livrent pas encore d'application utilisable, une version par phase (phases 0 à 2) ;
  - `0.1.0` : la première application utilisable (Phase 3, partie 1), puis une version **mineure** par phase ou partie de phase ;
  - `1.0.0` : la version de la première mise en production (déploiement consigné dans `journal-evenements.md`) ; les versions suivantes gardent une version mineure par phase ou partie de phase ;
  - une version **corrective** (ex. `v0.1.1`) livre une correction publiée entre deux de ces versions.

  Une **version candidate** (RC), testée sur le déploiement interne avant la publication de la version (`contraintes-techniques/architecture-deploiement.md` § Hébergement), n'a pas de numéro propre : elle est désignée par les images d'une publication à la demande, sous leur tag immuable `sha-<commit>` (`choix-implementation/deploiement.md` § Publication des images Docker).
  - **Alternative écartée (numéro de pré-version, ex. `v0.1.0-rc.1`, avec son tag et sa release)** : un nom plus lisible que `sha-<commit>`, pour un coût réel : chaque version publiée fait repartir la période du nouveau code de SonarQube Cloud (§ Analyse de la qualité du code), qu'une version candidate amputerait du code ajouté depuis la version précédente, et jalons, tags et releases devraient distinguer les versions candidates des versions publiées.

  La version que livre chaque phase, ou partie de phase, est indiquée dans `suivi-implementation.md`.
- **Jalon** : chaque version à venir est un jalon de même nom (`v0.1.0`), dont la description désigne la phase ou la partie qu'elle livre. Il est créé au découpage de cette phase ou de cette partie, avec ses tickets, ou avec le ticket d'une version corrective. Il ne porte pas d'échéance : le projet ne planifie pas de dates.
- **Rattachement** : chaque ticket est rattaché au jalon de la version qui le livre (`gestion-projet/revue-tickets.md` § Suivi par tickets) ; un ticket hors plan l'est dès que sa réalisation est prévue dans une version, et reste sans jalon tant qu'elle ne l'est pas (ex. une montée de version majeure encore lointaine, le ticket des vulnérabilités connues, que ne réalise aucune PR). Chaque Pull Request fusionnée sur `main` est rattachée au jalon de son ticket de référence ; sans ticket, à celui de la version en cours, la première version non publiée. Un ticket fermé sans être réalisé (*not planned*) quitte son jalon.
- **Publication** : une version est publiée quand tous les tickets de son jalon sont fermés :
  - **tag** annoté et signé (clé GPG de § Signature des commits), posé sur le commit de `main` qui la complète ;
  - **release** GitHub sur ce tag, dont les notes sont générées par GitHub à partir des Pull Requests fusionnées depuis le tag de la version précédente, et vérifiées avant publication comme tout texte publié (`gestion-projet/gouvernance.md` § Budget IA, mention de Codex). Une version `0.0.x`, qui ne livre pas d'application utilisable, est marquée **pré-release** ;
  - **analyse SonarQube Cloud** de `main`, relancée dès le tag posé (`gh workflow run code-quality.yml --ref main`) : la période du nouveau code commence à la première analyse qui porte la version (§ Analyse de la qualité du code), qui doit être celle du commit publié, et non celle de la fusion suivante, dont les changements en seraient exclus ;
  - **images Docker** de la version, publiées à la demande depuis son tag (`choix-implementation/deploiement.md` § Publication des images Docker) ; les versions `0.0.x` n'en ont pas, faute d'interface ;
  - **fermeture du jalon**.
- La publication d'une version est un acte de gestion du projet (`gestion-projet/gouvernance.md` § Prise de décision) : l'agent l'accomplit dès que la règle ci-dessus la prescrit, puis en informe l'utilisateur. Le déploiement de la version, lui, appartient à l'utilisateur.
