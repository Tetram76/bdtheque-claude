# Gestion de Projet

Ce fichier décrit la gouvernance du projet : stockage, organisation, outillage de gestion.

---

## Gouvernance documentaire

- **Source de vérité absolue** : le dossier [`.speckit/`](.) est la source de vérité absolue et contraignante du projet. Il prime sur toute autre information : historique de conversation, supposition, connaissance générale de l'agent, y compris `AGENTS.md`, qui n'en est que le point d'entrée. Chaque règle qu'il contient — gouvernance, fonctionnel, modèle métier ou contraintes techniques — a la **même force obligatoire**, sans distinction de « majeur » ou « mineur » : un manquement, même ponctuel ou sur un point jugé secondaire, est une violation, pas un détail.
- **Primauté sans entrave** : cette primauté ne doit pas entraver l'efficacité de l'agent. Elle s'applique dans le respect des contraintes et des modalités que le speckit définit lui-même, notamment l'autonomie de l'agent sur les points techniques (§ « Prise de décision »).
- **Aucune contradiction** : aucune décision, ligne de code ou réponse ne peut contredire le `.speckit` sans accord explicite de l'utilisateur — sans exception, sans oubli, sans arbitrage silencieux de l'agent au profit de sa propre appréciation. En cas d'ambiguïté ou de silence : technique → l'agent décide et documente son choix dans le fichier concerné ; fonctionnel ou métier → décision de l'utilisateur (§ « Prise de décision »).
- **Rôle de l'agent** : ce projet vise à produire une **application n-tiers web** et tous les éléments nécessaires à sa mise en production. L'agent décide **seul** des points techniques et des points fonctionnels ou métier déjà tranchés par le `.speckit` (§ « Prise de décision »), et doit **toujours être en mesure de justifier** ses choix lorsqu'il est challengé : une contrainte imposée est traçable dans `contraintes-techniques.md`, un choix propre à l'agent dans `choix-implementation.md`, une règle de gouvernance dans ce fichier.
- **Suivi d'implémentation** : le plan d'implémentation (phases, découpage en chantiers) est décrit dans [`suivi-implementation.md`](suivi-implementation.md), distinct des cinq fichiers de spécification car il décrit le découpage du projet plutôt que son contenu cible. L'**état d'avancement** d'un chantier y est celui de son ticket GitHub, signalé par la mise en forme du numéro du ticket (cf. § « Suivi par tickets »). Toute Pull Request qui précise ou corrige le découpage d'un chantier inclut, dans le même commit ou la même PR, la mise à jour de `suivi-implementation.md` **et** du ticket correspondant.

### Qui décide du contenu

- **`contraintes-techniques.md` et `fonctionnel.md`** : l'utilisateur est le **seul décisionnaire** de leur contenu. L'agent n'y modifie, n'y ajoute et n'en retire rien de sa propre initiative : tout changement exige l'accord explicite préalable de l'utilisateur.
- **`modele-metier.md`** : l'utilisateur en est le **décisionnaire principal**, puisqu'il choisit les données à utiliser et les règles qui les régissent (entités, attributs, relations, contraintes d'intégrité) : l'agent n'y modifie rien de cet ordre sans son accord explicite. L'agent peut en revanche le **compléter avec des informations plus techniques** (ex. type de stockage d'un attribut, précisions de représentation) sans accord préalable, à condition de ne modifier ni les données ni les règles choisies par l'utilisateur.
- **`choix-implementation.md`** : l'agent peut **à tout moment remettre en cause** ce qui y est noté (changer, remplacer ou retirer un choix) sans accord préalable, dès lors que cela respecte `contraintes-techniques.md` et `fonctionnel.md`, qui prévalent. Il documente alors le nouveau choix (cf. § « Prise de décision » pour l'information de l'utilisateur lorsque l'impact est visible sur le livrable).

### Mise à jour du .speckit

Mettre à jour le fichier concerné **immédiatement et sans attendre** dès que l'utilisateur fournit une exigence, une contrainte, une règle ou toute information relevant du périmètre d'un des fichiers. Ne jamais laisser une information pertinente uniquement dans le fil de conversation.

- **Le speckit n'est pas un historique de décisions.** Les fichiers `.speckit/` sont des **documents de référence**, pas des journaux : ils ne contiennent que l'état actuel et cible du projet — jamais d'historique, de dates, ni de traces de décisions successives ou de leur évolution.
- Toute nouvelle information est **intégrée dans le contenu existant** (mise à jour d'une section, enrichissement d'une définition, ajout dans la liste appropriée), jamais ajoutée en bas de fichier comme une entrée de log.
- En cas de changement de décision, l'**ancienne information est remplacée**, pas conservée à côté de la nouvelle avec une mention de type « anciennement », « auparavant » ou « suite à ».
- Le fichier doit rester cohérent, lisible et structuré comme une documentation vivante.
- **Exception : `journal-evenements.md`** est un journal, pas un document de référence : il déroge à ces règles, qui sont documentées dans le fichier lui-même.

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
- Toute restructuration est faite sans validation préalable, dans le même esprit d'autonomie qui régit les décisions techniques. Elle porte sur la **forme** uniquement : elle ne modifie, n'ajoute ni ne retire aucun contenu dont l'utilisateur est seul décisionnaire (cf. § « Qui décide du contenu » ci-dessus).

## Prise de décision

- La décision d'agir seul ou de solliciter l'utilisateur dépend de la nature du point traité :
  - **Point technique** (architecture, implémentation, outillage, choix de bibliothèque, performance, sécurité, etc.) : l'agent décide en **totale autonomie** ; cette autonomie **exclut de lui poser une question technique** au seul motif d'un doute, de plusieurs options ou d'un `.speckit` incomplet : l'agent décide et documente son choix dans `choix-implementation.md`, sans demander confirmation.
  - **Point fonctionnel ou métier** (règle métier, comportement attendu, contenu applicatif — y compris le contenu de `fonctionnel.md`/`modele-metier.md`) :
    - Si le `.speckit` existant **permet déjà de trancher** (application d'une exigence déjà documentée, sans changement de règle) : l'agent décide en autonomie, en s'appuyant explicitement sur le passage du `.speckit` qui tranche.
    - Si le point constitue un **véritable changement d'exigence**, ou reste **ambigu** au regard du `.speckit` existant : décision **explicite de l'utilisateur**. L'agent effectue la contre-vérification (pertinence, faits vérifiés) mais **ne tranche pas seul** — il soumet le point à l'utilisateur, **un point à la fois**, et applique la décision reçue avant de passer au point suivant.
  - Cette règle s'applique aussi bien au traitement des retours de revue de PR (cf. « Revue de code ») qu'à toute évolution du contenu fonctionnel/métier du `.speckit/` proposée à l'initiative de l'agent.
- Lorsque, sur un point technique, les pour et les contre s'équilibrent et qu'il n'existe objectivement pas de meilleur choix, l'agent **peut solliciter l'avis de l'utilisateur** avant de trancher — exception étroite, qui ne couvre ni le simple doute ni la présence de plusieurs options dès que l'une est objectivement préférable.
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
- **Méthodologie TDD (Test-Driven Development)** : toute implémentation de logique métier ou technique non triviale suit le cycle **Red → Green → Refactor** : écrire d'abord un test qui échoue pour le comportement visé (Red), écrire ensuite le code de production minimal qui le fait passer (Green), puis refactoriser le code — production et tests — une fois au vert (Refactor). Le test est **toujours écrit avant** le code qu'il vérifie, jamais après-coup pour documenter un comportement déjà implémenté.
- **Non-régression** : toute modification doit être accompagnée d'un moyen de vérifier qu'elle ne sera pas silencieusement annulée par une modification future. Le moyen de contrôle (test unitaire, test d'intégration, test de contrat, assertion, etc.) doit être **proportionné à la portée et au risque de la modification** : on n'écrit pas une suite de tests complète pour un changement trivial, mais toute logique métier ou technique non triviale doit être couverte — et développée selon le cycle TDD ci-dessus.
- L'agent est **seul décisionnaire** sur l'architecture et l'implémentation : toute refactorisation jugée nécessaire (lisibilité, maintenabilité, testabilité, séparation des responsabilités, etc.) doit être faite sans attendre de validation.
- Le code doit être **propre et lisible** : l'utilisateur est développeur et lit le code produit.
- **Règle de commentaires** : les commentaires expliquent le **pourquoi** (intention, contrainte, décision de conception), jamais le **quoi** (ce que le code fait — le code se lit de lui-même).
- **Langue des fichiers techniques** : tout le contenu technique (code, noms de fonctions/classes/variables/constantes, commentaires, messages de log, noms de fichiers de configuration) est rédigé en **anglais**. Seule la documentation du projet (`.speckit/`, `README.md`, etc.) est rédigée en **français**. Les messages destinés à l'utilisateur final sont produits par le système de traduction du frontend (cf. `choix-implementation.md` § Internationalisation), pas codés en dur dans une langue donnée.

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
- **Sécurité du dépôt** (`security_and_analysis`, disponible gratuitement car dépôt public) :
  - **Secret scanning** : activé
  - **Push protection** (blocage des push contenant un secret détecté) : activée
  - **Dependabot security updates** : activé
  - *Vérification de validité des secrets détectés* (`secret_scanning_validity_checks`) : désactivée, cause non identifiée — à vérifier manuellement dans les paramètres GitHub du dépôt si besoin.
- **Merge réservé à l'utilisateur, y compris pour de futurs collaborateurs** : le dépôt est la propriété d'un compte **personnel** (`Tetram76`), pas d'une organisation — la restriction de push/merge par utilisateur ou équipe (fonctionnalité GitHub de branch protection) n'est **pas disponible** sur ce type de dépôt, elle ne peut donc pas être imposée techniquement via un Ruleset ou une protection de branche. La garantie repose donc sur la gestion des droits d'accès : **aucun collaborateur ne doit recevoir un accès `Write` (ou supérieur)** au dépôt. Toute contribution externe future passe par un **fork** + Pull Request ; le merge de cette PR reste effectué par l'utilisateur (ou par l'agent agissant en son nom), jamais par le contributeur externe lui-même.

## Outillage .NET

- **Gestion centralisée des packages NuGet** via `Directory.Packages.props` (Central Package Management) : toutes les versions sont déclarées à la racine de la solution, les fichiers `.csproj` ne référencent que les noms de package.
- **Tests unitaires et d'intégration** : `xunit`, exécutés via `dotnet test`. Couverture de code collectée avec `coverlet.collector`.
- **Tests de persistance et d'intégration de l'API sur PostgreSQL réel** : `Testcontainers.PostgreSql` démarre, par exécution de tests, un conteneur de la **même image** que le service `db` de `docker-compose.yml` (alignement vérifié par un test) ; le schéma y est produit par les **migrations** (jamais par `EnsureCreated`), et chaque test dispose de sa propre base, clonée d'une base modèle migrée une fois (`CREATE DATABASE … TEMPLATE`), dont les connexions sont libérées à la fin du test (une base par test, donc un pool de connexions par test : conservées, elles épuiseraient `max_connections`). Les tests de l'API (`Microsoft.AspNetCore.Mvc.Testing`, `WebApplicationFactory`) démarrent sur une base vide, comme un premier déploiement. Docker est donc requis pour exécuter les tests, localement comme en CI.
  - **Alternative écartée (SQLite en mémoire)** : un autre moteur que celui de production ne vérifie ni les migrations réellement appliquées, ni la collation (tri linguistique), ni la précision des `decimal`, ni la forme exacte des contraintes PostgreSQL — précisément ce que ces tests doivent garantir. Son seul avantage (pas de dépendance à Docker) ne compense pas des tests verts sur un schéma qui n'est pas celui livré.
- **Contrôle de cohérence modèle ↔ migrations** : la CI exécute `dotnet ef migrations has-pending-model-changes`, qui échoue si une modification du modèle EF a été commitée sans sa migration.

## Intégration continue (CI)

- **GitHub Actions** héberge le pipeline de non-régression (`.github/workflows/ci.yml`), déclenché sur chaque Pull Request et sur push vers `main`.
- Étapes du pipeline : restauration, build en mode `Release`, contrôle de cohérence modèle ↔ migrations, exécution de la totalité des tests (`dotnet test`, sur PostgreSQL via Testcontainers — Docker est disponible sur les runners `ubuntu-latest`).
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
- La décision d'accepter ou de rejeter un retour suit la règle générale de « Prise de décision » ci-dessus : un retour **technique** relève de l'agent ; un retour **fonctionnel ou métier** relève de l'agent si le `.speckit` existant permet déjà de trancher, ou d'une décision explicite de l'utilisateur (un point à la fois) s'il s'agit d'un véritable changement d'exigence ou d'une ambiguïté.
- L'objectif de robustesse est une application **à l'épreuve de l'utilisateur** (parcours réels, usages légitimes, erreurs utilisateur plausibles), pas un code **blindé** contre tout scénario théorique ou détourné. Le code est écrit pour le contexte de cette application — il n'a pas vocation à devenir une librairie publique ni à être réutilisé hors projet.
- Les scénarios invoqués par une revue (cas limites, abus, chemins d'exécution) doivent être **pertinents, valides et possibles** dans ce contexte applicatif ; un scénario hypothétique, irréaliste ou hors périmètre ne justifie pas une complexification du code.

### Revue Codex (bloquante)

Une revue **Codex** se déclenche automatiquement à chaque commit poussé sur une Pull Request. À la différence des autres agents de revue, son approbation est une **condition bloquante du merge** : une PR ne peut être fusionnée que si Codex a donné sa **validation finale** par une réaction 👍 sur la **description de la PR** pour le commit de tête, signe qu'une revue n'a rien trouvé à corriger.

#### Statut des retours de Codex

Codex ne connaît pas le contexte : il se contente de relire les diffs. L'agent qui traite les retours est le **seul à connaître le contexte**, donc le seul à pouvoir décider ; et, comme pour toute décision technique (cf. § Prise de décision), il en est le **seul décisionnaire**. Les retours de Codex ne sont que des **indications**, à intégrer à sa réflexion — jamais des ordres.

#### Traiter un retour

1. **Contre-vérifier**, toujours et sans exception : pertinence vis-à-vis de l'objectif de la PR, du contenu du `.speckit/` et des bonnes pratiques applicables, faisabilité du scénario dans le contexte réel de l'application, critère « à l'épreuve de l'utilisateur » (cf. règles générales ci-dessus). Un retour non vérifié n'est ni appliqué ni rejeté.
2. **Corriger à la racine** un retour validé : établir la **cause racine** du problème, et la corriger, jamais un correctif de surface (« sparadrap ») qui ne traite que le symptôme relevé. Un **commit dédié** par retour appliqué.
3. **Répondre à chaque retour**, toujours, qu'il soit appliqué (avec le commit correspondant) ou rejeté (avec la justification du rejet).
4. **Apprécier chaque commentaire** de Codex, toujours, par une réaction : 👍 s'il était utile, 👎 sinon (Codex le demande dans chacun de ses commentaires).
5. **Résoudre** chaque conversation de revue une fois tous ses retours traités.

#### Cycle

Le traitement est répété **tant que Codex émet des retours**, jusqu'à sa validation finale (👍 sur la description de la PR pour le commit de tête). Le merge n'intervient qu'ensuite, en complément de la réussite du CI.

- Après le push de commits, Codex relance une revue (avec un léger délai) ; l'agent attend son résultat avant de poursuivre.
- Si **tous les retours sont rejetés sans ajouter de commit**, Codex ne relance pas la revue : l'agent la **force** par un commentaire `@codex review` sur la PR.
- **Faux positif sur la signature des commits** : Codex peut signaler des commits non signés. Si la vérification (statut « Verified » côté GitHub) établit un faux positif, il se traite comme tout retour rejeté (réponse justifiée, appréciation 👎, résolution), et Codex est alors considéré comme n'ayant **plus rien d'autre à remonter**. L'agent relance la revue (`@codex review`) jusqu'à **3 fois** ; si Codex persiste sur ce seul faux positif, **sans autre commentaire**, la PR est considérée comme **validée par Codex**, en l'absence du 👍.

La procédure opérationnelle détaillée (commandes `gh`, requêtes GraphQL de résolution de conversation, etc.) est décrite dans le skill `.claude/skills/codex-review-loop/`.

## Issues

Les Issues GitHub (« tickets ») servent à :

- tracer **chaque chantier** du plan d'implémentation (cf. § « Suivi par tickets » ci-dessous) ;
- tracer ponctuellement des **bugs** à corriger ou des **fonctionnalités** à implémenter dans le futur, hors plan d'implémentation.

## Suivi par tickets

Chaque chantier listé dans [`suivi-implementation.md`](suivi-implementation.md) **doit** faire l'objet d'un ticket GitHub.

- **Correspondance** : un ticket correspond à une seule Pull Request ; une Pull Request correspond à zéro ou un ticket (jamais plusieurs). « Ticket » désigne toute Issue GitHub : la règle vaut pour les chantiers du plan comme pour les bugs et fonctionnalités tracés hors plan.
- **Chantiers hors suivi** : un chantier mené hors du plan d'implémentation peut faire l'objet d'un ticket, sans que ce soit obligatoire. Seuls les chantiers listés dans le suivi **doivent** avoir un ticket, à l'exception des chantiers de consolidation (ci-dessous).
- **Création** : les tickets sont créés au moment du **découpage de la phase** en chantiers.
- **Consolidation d'une phase** : lors de la consolidation d'une phase, un ticket est créé pour **chaque chantier identifié**, sans que le suivi ait besoin d'être mis à jour : ces chantiers ont un ticket obligatoire même s'ils ne sont pas listés dans le suivi.
- **Pull Request** : la PR est associée à son ticket et le **ferme automatiquement au merge** (mot-clé `Closes #<numéro>` dans la description, cf. § « Lien entre Pull Requests et Issues »). Le ticket est donc clôturé quand la PR est fusionnée.
- **Commentaire de clôture** : à la fusion de la PR, un commentaire est ajouté au ticket pour indiquer **comment l'implémentation a répondu au ticket** (ce qui a été réalisé au regard du contenu du ticket, y compris les écarts éventuels).
- **Traçabilité jusqu'au ticket** : le commit final de la PR doit permettre de remonter, directement ou indirectement, jusqu'au ticket, par les seuls mécanismes natifs de GitHub (aucun dispositif ad hoc). La description de la PR devenant le corps du commit squashé (cf. § « Conventions de commit »), le mot-clé de fermeture qu'elle contient figure dans le commit : GitHub y résout le ticket, et le suffixe `(#<PR>)` ajouté au titre par le squash merge mène à la PR. La référence au ticket est donc **toujours dans la description de la PR**, jamais seulement dans un commentaire ou dans le titre.
- **Statut** : l'état d'un chantier (à faire / fait) est **défini par l'état de son ticket** (ouvert / fermé). Le suivi le reflète uniquement par la mise en forme du numéro du ticket : **barré** (`~~#<numéro>~~`) lorsque le ticket est fermé, normal lorsqu'il est ouvert ; aucune autre colonne ou mention de statut.
- **Contenu du suivi** : pour chaque chantier, le suivi indique le **numéro du ticket** et en décrit succinctement le contenu (en pratique, son titre).
- **Cohérence suivi ↔ tickets** : toute correction apportée au découpage dans le suivi est **reportée dans le ticket concerné**, dans le respect de la définition du contenu d'un ticket ci-dessous.

### Contenu d'un ticket

Le ticket est plus précis que la ligne du suivi, sans pour autant être un plan d'implémentation. Il reprend **tout ce qui est connu au moment du découpage de la phase et peut influencer l'implémentation du chantier** :

- la liste des **éléments à implémenter** ;
- les **contraintes** applicables (règles du `.speckit/`, dépendances avec d'autres chantiers, périmètre exclu) ;
- les **éléments techniques déjà statués** pendant ou avant le découpage de la phase.

Le ticket **ne décrit jamais comment implémenter** le chantier : la rédaction d'un ticket n'implique **pas** la création du plan d'implémentation, qui relève de l'agent au moment de réaliser le chantier.

## Lien entre Pull Requests et Issues

- Une Pull Request référence **zéro ou un** ticket (cf. § « Suivi par tickets »), jamais plusieurs ; un ticket n'est traité que par une seule PR.
- Une PR qui réalise un chantier du plan d'implémentation est associée à **son ticket**, qu'elle ferme au merge. Hors plan d'implémentation, une PR peut n'avoir aucun ticket.
- Si une PR **répond** à un ticket (correction d'un bug tracé, implémentation d'une fonctionnalité tracée), elle **doit le référencer** (ex. mention `#<numéro>` dans la description).
- Le ticket étant **entièrement traité** par sa PR, la référence utilise un mot-clé de fermeture automatique GitHub (`Closes`, `Fixes`, `Resolves #<numéro>`), afin qu'il soit **automatiquement clôturé au merge** de la PR.
- Un ticket qui ne pourrait être traité que **partiellement** par une PR est à découper en plusieurs tickets, chacun traité entièrement par sa propre PR.

## Releases

Des **releases GitHub** sont publiées pour marquer les jalons significatifs du projet. Chaque release correspond à un état stable et identifiable de l'application.

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

**Ce réglage ne doit pas être modifié** : il porte à la fois la qualité de l'historique et la traçabilité jusqu'au ticket (cf. § « Suivi par tickets »). Avec une autre valeur, le corps du commit squashé n'est plus la description de la PR (`COMMIT_MESSAGES` : concaténation des messages des commits intermédiaires ; `BLANK` : corps vide). Le mot-clé de fermeture (`Closes #<numéro>`) n'apparaît alors plus dans le commit final : celui-ci ne mène plus directement au ticket, seul le suffixe `(#<PR>)` du titre mène à la PR, et il faut passer par elle pour retrouver le ticket. La description, rédigée comme un message de commit, ne figure plus non plus dans l'historique de `main`. Toute modification de ce réglage exige donc de revoir ces règles.

La description de toute Pull Request **suit toujours le template** du projet `.github` : le dépôt [`Tetram76/.github`](https://github.com/Tetram76/.github) du compte, qui porte les fichiers de santé communautaire par défaut de tous ses dépôts, dont `.github/pull_request_template.md`. Sections et consignes du template sont à suivre. Il n'est pas copié dans ce dépôt : il est relu à chaque PR.

Exemples :

- `feat(albums): ajout de la gestion des éditions`
- `fix(devises): correction du taux de conversion franc français`
- `chore(docker): mise à jour du Dockerfile backend`

> **Note** : l'enforcement automatique du format (via ruleset GitHub) est réservé à GitHub Enterprise et n'est donc pas actif. Le respect de la convention est une contrainte de processus appliquée par l'agent.
