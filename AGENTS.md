# .speckit — Source de Vérité Absolue

Le projet est piloté en **développement piloté par les spécifications** (*Spec-Driven Development*, SDD) : la spécification, le dossier `.speckit/`, précède et gouverne le code (cf. [Gouvernance documentaire](.speckit/gestion-projet/gouvernance/gouvernance-documentaire.md)).

Le dossier `.speckit/` est la **source de vérité absolue et contraignante** du projet : il prime sur toute autre information, y compris ce fichier, qui n'en est que le point d'entrée. Les règles de gouvernance (primauté du speckit, qui décide du contenu, mise à jour, rôle et autonomie de l'agent) sont dans [Gouvernance documentaire](.speckit/gestion-projet/gouvernance/gouvernance-documentaire.md) et [Prise de décision](.speckit/gestion-projet/gouvernance/prise-de-decision.md).

Le `.speckit/` est un bundle au format **OKF** (*Open Knowledge Format*) : une **carte**, l'`index.md` de chaque dossier, qui décrit chaque concept et dit quand il s'applique, et des **concepts**, un par fichier. Ce découpage limite ce qui entre dans le contexte de l'agent à ce qu'exige le chantier : c'est l'objet des règles de lecture ci-dessous (forme du bundle : [Maintenance du .speckit](.speckit/gestion-projet/gouvernance/maintenance-du-speckit.md)).

## Primauté sur les rules et les skills (critique)

Les règles et protocoles décrits dans le `.speckit/` ont une valeur **au moins égale, et en cas de conflit supérieure et prioritaire**, à celle de toute *rule* (`.claude/rules/`) ou de tout *skill* (`.claude/skills/`, plugins compris). Un skill ou une rule ne peut ni contredire, ni assouplir, ni remplacer une règle ou un protocole du speckit : en cas de divergence, le speckit s'applique et la divergence est signalée à l'utilisateur. Les skills et rules ne sont que des mises en œuvre opérationnelles de ce que le speckit prescrit.

## Règles de consultation

- **Lire la carte et le socle est la toute première action de chaque conversation** — avant toute autre lecture de fichier, recherche dans le code, réponse à l'utilisateur (y compris une simple question ou une clarification) ou action de quelque nature que ce soit. Cette lecture n'est ni différable ni conditionnée à la nature apparente de la demande : elle a lieu même si la demande semble triviale, hors-sujet par rapport au `.speckit`, ou déjà couverte par le contexte de conversation. Elle comprend, dans cet ordre :
  1. le listage de tous les `index.md` (`Glob` sur `.speckit/**/index.md`), seule opération permise avant la première lecture, et la lecture de chacun : la **carte** ;
  2. le relevé des concepts marqués `socle` (`Grep` de `tags: [socle]` dans `.speckit/`), et la lecture de chacun : le **socle**, qui s'applique quel que soit le chantier.
- **Lire ce qu'exige le chantier** : dès que l'objet de la demande est connu, et avant d'y répondre ou d'agir, chaque concept qu'elle touche est lu en entier :
  - les concepts que la carte désigne pour ce sujet, d'après leur titre et leur description ;
  - les concepts qui régissent les fichiers que le chantier modifiera (`applies_to`, que liste `scripts/Get-SpeckitConcepts.ps1`) ;
  - les concepts auxquels renvoie un passage lu, quand ce passage s'appuie sur eux.

  **En cas de doute sur l'utilité d'un concept, il est lu.** Au début d'un chantier, l'agent annonce à l'utilisateur la liste des concepts lus au-delà de la carte et du socle.
- **« Lire » un concept du `.speckit/` signifie en prendre connaissance en entier : chaque ligne, de la première à la dernière, et chaque ligne en entier.** C'est une obligation de résultat, quel que soit l'outil employé. Est **interdit**, sans exception, tout moyen de se dispenser d'en lire une partie : limiter le nombre de lignes lues, sauter des lignes, s'arrêter avant la fin, ou remplacer la lecture par un extrait (`head`, `tail`, `sed -n`, `grep`…), un résumé ou un souvenir du contenu. Aucune autorisation implicite n'existe : seul l'utilisateur peut déroger à cette règle, explicitement, dans le message concerné.
- **Vérifier la complétude** : si l'outil coupe sa sortie — avant la fin du fichier, ou au milieu d'une ligne trop longue —, la lecture est **complétée** par une lecture ciblée de ce qui manque, et de cela seulement (reprise à la première ligne non lue, lecture intégrale de la ligne coupée par un autre moyen), jusqu'à ce que tout ait été lu : compléter ainsi une sortie coupée n'est pas une lecture partielle. Un concept dont une partie n'a pas été lue est un concept **non lu** : aucune action qui en dépend n'est alors permise.
- **Avant toute décision** architecturale, technique ou fonctionnelle prise plus tard dans la conversation, la carte est relue, et chaque concept que la décision touche est lu ou relu en entier, même s'il l'a déjà été.
- **Avant chaque push**, `scripts/Get-SpeckitConcepts.ps1` liste les concepts qui régissent les fichiers modifiés de la branche ; chacun d'eux qui n'a pas été lu l'est avant le push.
- **Au moment d'agir** : certaines règles ne s'appliquent qu'à un moment précis du travail ; elles ne sont pas au socle, et la table des moments ci-dessous les désigne. Elle rappelle aussi un concept du socle dont une règle se joue à ce moment, faute de quoi elle passerait inaperçue (ex. la clôture d'un ticket parent à la fusion de la PR de son dernier sous-ticket). Avant chaque moment de la table, les concepts qu'elle lui associe sont lus en entier, s'ils ne l'ont pas été depuis leur dernière modification. Pour les moments qui ont une commande (ou l'outil équivalent d'un serveur MCP GitHub), le hook du dépôt (`.claude/settings.json`, `.claude/hooks/Show-SpeckitMoment.ps1`) rappelle ces concepts avant de l'exécuter, à partir de cette table, qui fait foi.

### Table des moments

| Moment | Commandes | Concepts à lire |
| --- | --- | --- |
| Créer un ticket | `gh issue create`, `create_issue`, `issue_write` | [`gestion-projet/revue-tickets/issues.md`](.speckit/gestion-projet/revue-tickets/issues.md), [`gestion-projet/depot/versions-et-releases.md`](.speckit/gestion-projet/depot/versions-et-releases.md) |
| Créer une Pull Request | `gh pr create`, `create_pull_request` | [`gestion-projet/revue-tickets/revue-codex-bloquante.md`](.speckit/gestion-projet/revue-tickets/revue-codex-bloquante.md), [`gestion-projet/depot/versions-et-releases.md`](.speckit/gestion-projet/depot/versions-et-releases.md) |
| Pousser sur une Pull Request | `git push`, `push_files`, `create_or_update_file` | [`gestion-projet/revue-tickets/revue-codex-bloquante.md`](.speckit/gestion-projet/revue-tickets/revue-codex-bloquante.md), [`gestion-projet/revue-tickets/analyse-sonarqube-cloud.md`](.speckit/gestion-projet/revue-tickets/analyse-sonarqube-cloud.md) |
| Passer une Pull Request en revue | `gh pr ready`, `update_pull_request` | [`gestion-projet/revue-tickets/revue-codex-bloquante.md`](.speckit/gestion-projet/revue-tickets/revue-codex-bloquante.md), [`gestion-projet/revue-tickets/analyse-sonarqube-cloud.md`](.speckit/gestion-projet/revue-tickets/analyse-sonarqube-cloud.md), [`gestion-projet/revue-tickets/validation-fonctionnelle.md`](.speckit/gestion-projet/revue-tickets/validation-fonctionnelle.md) |
| Commenter une Pull Request (demande de revue comprise) | `gh pr comment`, `add_issue_comment` | [`gestion-projet/revue-tickets/revue-codex-bloquante.md`](.speckit/gestion-projet/revue-tickets/revue-codex-bloquante.md) |
| Traiter un retour de revue (Codex, SonarQube Cloud) | — | [`gestion-projet/revue-tickets/revue-codex-bloquante.md`](.speckit/gestion-projet/revue-tickets/revue-codex-bloquante.md), [`gestion-projet/revue-tickets/analyse-sonarqube-cloud.md`](.speckit/gestion-projet/revue-tickets/analyse-sonarqube-cloud.md) |
| Réaliser un chantier dont le ticket exige la validation fonctionnelle | — | [`gestion-projet/revue-tickets/validation-fonctionnelle.md`](.speckit/gestion-projet/revue-tickets/validation-fonctionnelle.md) |
| Fusionner une Pull Request | `gh pr merge`, `merge_pull_request` | [`gestion-projet/revue-tickets/suivi-par-tickets.md`](.speckit/gestion-projet/revue-tickets/suivi-par-tickets.md), [`gestion-projet/revue-tickets/verrouillage-des-pull-requests-fusionnees.md`](.speckit/gestion-projet/revue-tickets/verrouillage-des-pull-requests-fusionnees.md), [`gestion-projet/revue-tickets/validation-fonctionnelle.md`](.speckit/gestion-projet/revue-tickets/validation-fonctionnelle.md), [`gestion-projet/depot/versions-et-releases.md`](.speckit/gestion-projet/depot/versions-et-releases.md) |
| Publier une version | `gh release create`, `create_release` | [`gestion-projet/depot/versions-et-releases.md`](.speckit/gestion-projet/depot/versions-et-releases.md) |
| Lancer un lot de chantiers en parallèle, ou l'un de ses chantiers | — | [`gestion-projet/gouvernance/chantiers-en-parallele.md`](.speckit/gestion-projet/gouvernance/chantiers-en-parallele.md) |
| Contrôle de cohérence fonctionnelle dû ou demandé | — | [`gestion-projet/revue-tickets/controle-de-coherence-fonctionnelle.md`](.speckit/gestion-projet/revue-tickets/controle-de-coherence-fonctionnelle.md) |
| Relevé des versions dû, montée de version | — | [`gestion-projet/depot/mises-a-jour-des-dependances.md`](.speckit/gestion-projet/depot/mises-a-jour-des-dependances.md) |
| Traitement du contrôle qualité dû | — | [`gestion-projet/depot/controle-qualite-hebdomadaire.md`](.speckit/gestion-projet/depot/controle-qualite-hebdomadaire.md), [`gestion-projet/revue-tickets/analyse-sonarqube-cloud.md`](.speckit/gestion-projet/revue-tickets/analyse-sonarqube-cloud.md) |
- **Relecture après modification** : un concept ou un `index.md` modifié depuis sa dernière lecture (notification de modification sur disque, `pull`, merge, changement de branche, édition, PR fusionnée) est **relu intégralement** avant toute action qui en dépend. Une édition ne se fait jamais à partir d'un contenu périmé.
- **Lecture intégrale sur demande** : à la demande de l'utilisateur, un dossier entier, ou tout le `.speckit/`, est lu en entier.
- **[`log.md`](.speckit/log.md)**, journal des changements du `.speckit/`, n'est pas lu systématiquement : il l'est lorsque l'on a besoin de savoir ce qui a changé dans le `.speckit/` (ex. depuis une version).
- **Le dossier `poc/`** (prototypes exploratoires, hors `.speckit/`) n'est pas lu systématiquement : il ne l'est que lorsque le contexte de la discussion le nécessite. Rien de ce qu'il contient n'est statué : il ne fait jamais foi face au `.speckit/`.

## Dossiers

Le périmètre détaillé de chaque dossier est exposé dans son concept `perimetre.md` lorsqu'il en a un, et son contenu dans son `index.md`, qui fait foi face au tableau ci-dessous.

| Dossier ou fichier | Contenu |
| --- | --- |
| `gestion-projet/` | Gouvernance du projet (documentaire, branches, commits, outillage, CI/CD), hors application elle-même. |
| `fonctionnel/` | Fonctionnalités et règles métier de l'application. |
| `modele-metier/` | Modèle du domaine : entités, attributs, relations, contraintes d'intégrité, glossaire. |
| `contraintes-techniques/` | Contraintes techniques **imposées** (et non-contraintes explicites). |
| `choix-implementation/` | Choix techniques de l'agent, de sa propre initiative. |
| `journal-evenements.md` | Journal des évènements externes au repo (règles propres, documentées dans le fichier). |
| `log.md` | Journal des changements du `.speckit/` (règles propres : `gestion-projet/gouvernance/maintenance-du-speckit.md`). |
| `suivi-implementation/` | Avancement du plan d'implémentation. |
| `visuel/` | Charte graphique, présentation des écrans et maquettes de référence. |
