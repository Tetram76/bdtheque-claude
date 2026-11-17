---
title: 'Consulter, construire, vérifier et maintenir'
description: 'Forme des pages et des cartes, lecture guidée, construction et consultation du site, contrôles automatiques et leurs limites, mise à jour à chaque changement ; pour écrire ou modifier une page, ou comprendre un échec d''un contrôle documentaire.'
code:
  - 'docs/tech/docfx.json'
  - 'scripts/Build-TechnicalDocumentation.ps1'
  - 'scripts/Get-TechnicalDocumentationImpact.ps1'
  - 'scripts/Update-TechnicalReferences.ps1'
  - 'docker/validation-docs/**'
  - 'tests/Bdtheque.Documentation.Tests/**'
  - 'tests/Bdtheque.Infrastructure.Tests/TechnicalDocumentation*.cs'
---

# Consulter, construire, vérifier et maintenir

Le fonctionnement de la documentation technique elle-même. Ses règles (ce qu'elle est, quand la lire, quand la mettre à jour) sont celles de [Documentation technique](../../../.speckit/gestion-projet/gouvernance/documentation-technique.md) ; ses choix d'outils et leurs alternatives, celles de [Documentation technique : mise en œuvre](../../../.speckit/choix-implementation/documentation-technique.md).

## Forme

- **Cartes** : chaque dossier de `docs/tech/` a un `index.md`, sans en-tête, qui liste ses pages et ses sous-dossiers, une ligne chacun : `* [Titre](page.md) - description`. La description d'une page est celle de son en-tête, mot pour mot : elle dit ce que traite la page et quand la lire.
- **Page** : un sujet, assez court pour n'être lu que s'il sert. Son en-tête YAML porte :
  - `title`, repris par son titre de niveau 1 ;
  - `description`, celle de sa carte ;
  - `code`, facultatif : les motifs (`*`, `**`) des fichiers du dépôt qu'elle décrit, chacun désignant au moins un fichier, pour retrouver la page à partir d'un fichier modifié ;
  - `generated`, pour une page générée seulement : le script qui la produit.
- **Liens** : relatifs, y compris vers le code, les tests, la configuration et le `.speckit/` (`[InternalApiKeyMiddleware](../../../src/...)`). Un lien vers un fichier de code dont le texte est un symbole en code (`` [`LoginThrottle`](…) ``) affirme que ce symbole est dans ce fichier. Un lien vers un fichier compte, pour l'impact d'un changement, comme un motif de `code`.
- **Diagrammes** : Mermaid, dans un bloc ` ```mermaid `, précédé de la question à laquelle il répond (« Question du schéma : … ») et de sa nature : *interprétatif* (tenu à la main, sa compilation ne prouve pas sa fidélité au code) ou *généré* (reproduit une source qui fait autorité). Un diagramme qui devient illisible se découpe.
- **État décrit** : ce qui est présent, vérifié contre le code, et rien d'autre : ce que le `.speckit/` prévoit sans qu'il soit réalisé n'y figure pas, même annoncé comme tel. Ni historique, ni « non vérifié ».

## Lire

1. Le `.speckit/`, selon [`AGENTS.md`](../../../AGENTS.md), qui prime.
2. Si le sujet touche l'implémentation : la carte racine, [`docs/tech/index.md`](../index.md), puis la carte du domaine, puis les pages utiles, jamais tout le dossier. À défaut de carte qui réponde, une recherche textuelle dans `docs/tech/`, puis dans le code.
3. Une page oriente, elle ne prouve pas : un fait précis (ce que fait une méthode, ce qu'exécute un workflow) se vérifie dans le code ou par une exécution.

Pour retrouver les pages qui décrivent des fichiers modifiés :

```powershell
./scripts/Get-TechnicalDocumentationImpact.ps1
```

## Construire et consulter le site

Le site n'est qu'une autre vue des mêmes fichiers Markdown, construite par DocFX (outil .NET local de [`dotnet-tools.json`](../../../dotnet-tools.json)) : navigation générée depuis les cartes, recherche, diagrammes rendus dans le navigateur par le Mermaid que DocFX embarque, sans aucune requête vers Internet. Un lien qui sort de `docs/tech/` y mène au même fichier sur GitHub, au commit documenté, inscrit au pied de chaque page.

```powershell
./scripts/Build-TechnicalDocumentation.ps1 -Serve
```

construit la documentation de la copie de travail et la sert sur `http://localhost:8182`. Le script restaure d'abord les outils locaux (`dotnet tool restore`), si bien que la commande suffit sur un poste neuf, avec le SDK .NET et PowerShell. Un avertissement de DocFX (lien cassé…) fait échouer la construction.

**Environnement de validation** : `./scripts/Start-Validation.ps1 -PullRequest <numéro>` construit et sert la documentation du commit validé sur `http://localhost:8182`, adresse qu'il affiche avec celle de l'application ; `Stop-Validation.ps1` l'arrête avec le reste. Le service `docs`, image construite par [`docker/validation-docs/Dockerfile`](../../../docker/validation-docs/Dockerfile), n'écoute que sur le poste. La documentation interactive de l'API (Scalar) reste servie par `api` lui-même : [Contrat HTTP de l'API](../references/api-http.md).

## Maintenir

À chaque changement, dans la même Pull Request que lui :

1. **Avant d'écrire du code** : lire les pages du sujet, puis le code qu'elles désignent.
2. **Avant de pousser** : `./scripts/Get-TechnicalDocumentationImpact.ps1` liste les pages que le changement peut rendre fausses, et les fichiers modifiés qu'aucune page ne décrit. Chaque page listée est relue et mise à jour, diagrammes compris ; si aucune ne doit changer, un commit de la Pull Request porte le *trailer* Git qui le dit, avec sa raison :

   ```text
   Documentation-technique: sans effet sur les pages listées, renommage interne de variables.
   ```

3. **Référence générée** : après un changement de sa source, `./scripts/Update-TechnicalReferences.ps1`, dont le résultat est commité.
4. **Refactoring** : liens, motifs `code` et diagrammes suivent les fichiers déplacés ou supprimés ; une page devenue inutile est retirée, avec son entrée de carte.
5. **Avant de livrer** : les contrôles ci-dessous passent, et ce qui est écrit décrit l'état livré.

## Contrôles

| Contrôle | Où | Ce qu'il garantit |
| --- | --- | --- |
| Structure : cartes, en-têtes, titres, liens, motifs `code`, symboles cités, question de chaque diagramme | `TechnicalDocumentationStructureTests`, tests du dépôt, à chaque Pull Request | Ce qui se vérifie mécaniquement : rien de cassé ni d'introuvable. |
| Impact d'un changement | `Get-TechnicalDocumentationImpact.ps1 -Check`, job du CI de la documentation | Une page qui décrit un fichier modifié est modifiée elle aussi, ou un commit dit pourquoi elle n'a pas à l'être. |
| Références générées | `Update-TechnicalReferences.ps1 -Check`, même job | Les pages générées sont celles que donnent leurs sources : ni modifiée, ni manquante, ni en trop. |
| Site et diagrammes | `Bdtheque.Documentation.Tests`, même job, quand la documentation ou son outillage change | Le site se construit sans avertissement, y compris sur un poste dont les outils locaux n'ont jamais été restaurés ; chaque diagramme écrit est rendu, sans erreur de Mermaid, sans être réduit sous 0,75 de sa taille sur un écran de bureau (texte sous 12 pixels), sans requête hors du poste ; le commit documenté figure sur chaque page. |

**Limites** : aucun contrôle ne dit qu'une page est vraie. Que le texte et les diagrammes décrivent fidèlement le code, que l'impact listé a bien été évalué et que la raison d'un *trailer* est juste, cela reste une obligation de l'auteur du changement et un point de sa revue. La lisibilité d'un diagramme au-delà de sa taille (disposition, croisements, libellés) s'inspecte sur son rendu, par l'auteur qui le crée ou le modifie.
