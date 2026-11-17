---
title: 'Consulter, construire, vérifier et maintenir'
description: 'Cette page explique comment cette documentation est écrite et organisée, comment la lire, la construire en site et la consulter en ligne, ce que vérifient ses contrôles automatiques et comment la tenir à jour à chaque changement ; à lire pour écrire ou modifier une page, ou comprendre l''échec d''un contrôle documentaire.'
code:
  - 'docs/tech/docfx.json'
  - 'docs/tech/template/**'
  - 'scripts/Build-TechnicalDocumentation.ps1'
  - 'scripts/Get-TechnicalDocumentationImpact.ps1'
  - 'scripts/Update-TechnicalReferences.ps1'
  - 'docker/validation-docs/**'
  - '.github/workflows/technical-documentation.yml'
  - 'tests/Bdtheque.Documentation.Tests/**'
  - 'tests/Bdtheque.Infrastructure.Tests/TechnicalDocumentation*.cs'
  - 'tests/Bdtheque.Infrastructure.Tests/DatabaseSchemaReference*.cs'
---

# Consulter, construire, vérifier et maintenir

Cette page décrit le fonctionnement de la documentation technique elle-même : comment ses pages sont écrites et organisées, comment on la lit, comment on la construit en site, ce que vérifient ses contrôles et comment on la tient à jour. Ses règles, c'est-à-dire ce qu'elle est, quand la lire et quand la mettre à jour, sont fixées par [Documentation technique](../../../.speckit/gestion-projet/gouvernance/documentation-technique.md). Ses choix d'outils et les alternatives écartées sont expliqués dans [Documentation technique : mise en œuvre](../../../.speckit/choix-implementation/documentation-technique.md).

## La forme des pages

La documentation est rangée sous `docs/tech/` en dossiers, un par domaine. Chaque dossier a une carte, son fichier `index.md`, sans en-tête, qui liste ses pages et ses sous-dossiers à raison d'une ligne chacun, sous la forme `* [Titre](page.md) - description`. La description d'une page dans sa carte est exactement celle de son en-tête : elle dit ce que traite la page et quand la lire. Les cartes permettent ainsi de choisir les pages utiles sans les ouvrir.

Une page traite un seul sujet et reste assez courte pour qu'on ne la lise que si elle sert. Elle commence par un en-tête YAML qui porte quatre champs :

- `title` est le titre de la page, que reprend son titre de niveau 1.
- `description` est la phrase que reprend la carte.
- `code`, facultatif, liste les motifs (`*`, `**`) des fichiers du dépôt que la page décrit. Chaque motif doit désigner au moins un fichier. C'est ce champ qui permet de retrouver la page à partir d'un fichier modifié.
- `generated`, réservé aux pages générées, nomme le script qui les produit.

Les liens sont relatifs, y compris ceux qui mènent au code, aux tests, à la configuration et au `.speckit/`, comme `[InternalApiKeyMiddleware](../../../src/...)`. Un lien vers un fichier de code dont le texte est un symbole en code, comme `` [`LoginThrottle`](…) ``, affirme que ce symbole se trouve dans ce fichier, et le contrôle de structure le vérifie. Pour le calcul de l'impact d'un changement, un lien vers un fichier compte comme un motif de `code`.

Une page décrit ce qui existe, vérifié contre le code, et rien d'autre. Ce que le `.speckit/` prévoit sans que ce soit encore réalisé n'y figure pas, même présenté comme prévu ; une page ne raconte pas non plus d'historique, et n'affirme jamais un fait « non vérifié ».

### La rédaction

La documentation s'adresse d'abord à un développeur qui découvre le projet ou le maintient, même si un agent peut aussi s'en servir pour limiter sa lecture du code. Elle est donc rédigée en vraies phrases, sans style télégraphique, et donne le minimum d'explication qu'il faut pour la comprendre sans ouvrir le code : à quoi sert le mécanisme, comment ses éléments s'enchaînent, ce que signifient les termes techniques ou propres au projet, et ce qui se passe dans les cas qui comptent. Ces règles sont détaillées dans la section « Rédaction » de [Documentation technique](../../../.speckit/gestion-projet/gouvernance/documentation-technique.md#rédaction).

### Les diagrammes

Les diagrammes sont écrits en Mermaid, dans un bloc ` ```mermaid `, si bien qu'ils se versionnent et se relisent comme du texte. Chacun répond à une seule question et ne porte qu'une seule nature d'information : un diagramme ne mêle pas, par exemple, la topologie des conteneurs et le parcours d'une requête, ni les actions de l'utilisateur, les appels entre composants et leurs effets en base. Un sujet qui réunit plusieurs natures d'information se présente en plusieurs diagrammes.

Chaque diagramme est introduit par un paragraphe, juste au-dessus de lui, qui pose sa question en une phrase interrogative et dit d'où il vient. Un diagramme tracé à la main est interprétatif : sa construction prouve qu'il est bien formé, pas qu'il est fidèle au code. Un diagramme généré reproduit au contraire une source qui fait autorité. Par exemple : « Le schéma suivant, tracé à la main d'après `ci.yml`, répond à une seule question : quel job attend la fin de quel autre avant de démarrer ? » Le contrôle de structure vérifie que ce paragraphe d'introduction contient un point d'interrogation. Un diagramme qui devient illisible se découpe.

## Lire la documentation

La documentation technique se lit dans un ordre précis :

1. On lit d'abord le `.speckit/`, selon [`AGENTS.md`](../../../AGENTS.md), car il prime sur elle.
2. Si le sujet touche l'implémentation, on lit la carte racine, [`docs/tech/index.md`](../index.md), puis la carte du domaine concerné, puis les seules pages utiles, jamais tout le dossier. Si aucune carte ne répond, on cherche dans le texte de `docs/tech/`, puis dans le code.
3. Une page oriente le lecteur, elle ne prouve rien : un fait précis, comme ce que fait une méthode ou ce qu'exécute un workflow, se vérifie dans le code ou par une exécution.

Pour retrouver les pages qui décrivent les fichiers modifiés de sa branche, on lance :

```powershell
./scripts/Get-TechnicalDocumentationImpact.ps1
```

## Construire et consulter le site

Le site n'est qu'une autre présentation des mêmes fichiers Markdown. Il est construit par DocFX, un générateur de sites de documentation installé comme outil .NET local dans [`dotnet-tools.json`](../../../dotnet-tools.json). DocFX y ajoute une navigation générée depuis les cartes, une recherche, et le rendu des diagrammes dans le navigateur par la copie de Mermaid qu'il embarque, sans aucune requête vers Internet. Dans le site, un lien qui sort de `docs/tech/` mène au même fichier sur GitHub, à la version du commit documenté, qu'indique le pied de chaque page.

Le site donne à tous les diagrammes un même style, plus sobre que celui de Mermaid par défaut : une palette déclinée en clair et en sombre, qui suit le thème choisi par le lecteur, et la police du site. Ce style est déclaré une seule fois, dans le modèle propre au projet, `docs/tech/template/public/main.js`, que DocFX applique par-dessus son modèle `modern` ; aucun diagramme n'a donc à porter de directive de style. GitHub, qui rend les diagrammes avec ses propres couleurs, ne le reprend pas.

La commande suivante construit la documentation de la copie de travail et la sert sur `http://localhost:8182` :

```powershell
./scripts/Build-TechnicalDocumentation.ps1 -Serve
```

Le script restaure d'abord les outils locaux (`dotnet tool restore`), si bien que la commande suffit sur un poste neuf, pourvu que le SDK .NET et PowerShell y soient installés. Le moindre avertissement de DocFX, un lien cassé par exemple, fait échouer la construction.

### Le site publié de `main`

Le site de `main` se consulte en ligne, sans rien lancer, sur <https://tetram76.github.io/bdtheque-claude/>. Le workflow [`technical-documentation.yml`](../../../.github/workflows/technical-documentation.yml) le reconstruit avec le même script à chaque push sur `main`, ou à la demande depuis l'onglet *Actions* de GitHub, puis le déploie sur GitHub Pages. Le commit indiqué au pied de chaque page dit de quelle version de `main` il s'agit, et les liens vers le code mènent à ce commit. Si une publication échoue, la précédente reste en ligne. Une Pull Request ne publie rien : son site se consulte sur le poste ou dans l'environnement de validation.

Le workflow compte deux jobs, séparés pour que seul celui qui déploie reçoive les droits de publication :

| Job | Ce qu'il fait | Droits qu'il reçoit |
| --- | --- | --- |
| `Build the site` | Il installe le SDK .NET des Dockerfiles, exécute `Build-TechnicalDocumentation.ps1` et dépose le site construit comme artefact de Pages. | Il ne peut que lire le dépôt. |
| `Deploy to GitHub Pages` | Il déploie cet artefact dans l'environnement `github-pages`, sur `main` seulement. | Il reçoit `pages: write` et `id-token: write`. |

### La documentation dans l'environnement de validation

La commande `./scripts/Start-Validation.ps1 -PullRequest <numéro>` construit et sert la documentation du commit validé sur `http://localhost:8182`, adresse qu'elle affiche avec celle de l'application ; `Stop-Validation.ps1` l'arrête avec le reste. C'est le service `docs`, dont l'image est construite par [`docker/validation-docs/Dockerfile`](../../../docker/validation-docs/Dockerfile), qui la sert, et il n'écoute que sur le poste. La documentation interactive de l'API, Scalar, reste servie par `api` lui-même, comme l'explique [Contrat HTTP de l'API](../references/api-http.md).

## Maintenir la documentation

La documentation change dans la même Pull Request que ce qu'elle décrit. Concrètement, chaque changement suit ces étapes :

1. Avant d'écrire du code, on lit les pages du sujet, puis le code qu'elles désignent.
2. Avant de pousser, on lance `./scripts/Get-TechnicalDocumentationImpact.ps1`. Le script liste les pages que le changement peut rendre fausses, ainsi que les fichiers modifiés qu'aucune page ne décrit. Chaque page listée est relue et mise à jour, diagrammes compris. Si aucune ne doit changer, un commit de la Pull Request le dit par un *trailer* Git, une ligne `Clé: valeur` placée en fin de message de commit, qui donne la raison :

   ```text
   Documentation-technique: sans effet sur les pages listées, renommage interne de variables.
   ```

3. Quand la source d'une référence générée change, on lance `./scripts/Update-TechnicalReferences.ps1` et on commite son résultat. Le script régénère aussi le contrat OpenAPI, depuis l'hôte de test de l'API, et le [schéma de la base](../references/schema-de-la-base.md), depuis une base qui a reçu les migrations. Ces deux références exigent Docker ; les options `-NoOpenApiContract` et `-NoDatabaseSchema` permettent de les omettre.
4. Lors d'un refactoring, les liens, les motifs `code` et les diagrammes suivent les fichiers déplacés ou supprimés. Une page devenue inutile est retirée, avec son entrée de carte.
5. Avant de livrer, on s'assure que les contrôles ci-dessous passent et que ce qui est écrit décrit l'état livré.

## Contrôles

Quatre contrôles automatiques vérifient la documentation. Ils vérifient tout ce qui peut l'être mécaniquement, mais aucun ne dit si une page est vraie.

| Contrôle | Où il s'exécute | Ce qu'il garantit |
| --- | --- | --- |
| Structure | `TechnicalDocumentationStructureTests`, parmi les tests du dépôt, à chaque Pull Request. | Il vérifie les cartes, les en-têtes, les titres, les liens, les motifs `code` et les symboles cités, et que chaque diagramme est introduit par un paragraphe qui pose sa question. Rien n'est cassé ni introuvable. |
| Impact d'un changement | `Get-TechnicalDocumentationImpact.ps1 -Check`, dans le job de la documentation du CI. | Il garantit qu'une page qui décrit un fichier modifié est modifiée elle aussi, ou qu'un commit dit pourquoi elle n'a pas à l'être. |
| Références générées | `Update-TechnicalReferences.ps1 -Check`, dans le même job. Le contrat OpenAPI est comparé par `OpenApiContractTests`, parmi les tests de l'API, et le schéma de la base par [`DatabaseSchemaReferenceTests`](../../../tests/Bdtheque.Infrastructure.Tests/DatabaseSchemaReferenceTests.cs), parmi ceux de l'infrastructure, quand une source ou la référence change. | Il garantit que les pages générées sont exactement celles que donnent leurs sources, sans page modifiée, manquante ou en trop. Le contrat versionné est celui que produisent les endpoints, et le schéma est celui que donne le catalogue d'une base qui a reçu les migrations du commit. La génération du schéma échoue si elle rencontre un objet qu'elle ne sait pas présenter, ou une table qu'aucun de ses diagrammes ne place. |
| Site et diagrammes | `Bdtheque.Documentation.Tests`, dans le même job, quand la documentation ou son outillage change. | Il garantit que le site se construit sans avertissement, y compris sur un poste où les outils locaux n'ont jamais été restaurés, que chaque diagramme est rendu sans erreur de Mermaid, et qu'aucun n'est réduit sous 0,75 de sa taille sur un écran de bureau, seuil sous lequel son texte passerait sous 12 pixels. Il vérifie aussi que le site n'émet aucune requête hors du poste et que le commit documenté figure sur chaque page. |

Ces contrôles ont des limites. Que le texte et les diagrammes décrivent fidèlement le code, que l'impact listé a bien été évalué et que la raison donnée par un *trailer* est juste, cela reste l'obligation de l'auteur du changement et un point de sa revue. De même, la lisibilité d'un diagramme au-delà de sa taille, c'est-à-dire sa disposition, ses croisements et ses libellés, s'inspecte sur son rendu, par l'auteur qui le crée ou le modifie.
