---
type: Choix d'implémentation
title: 'Documentation technique : mise en œuvre'
description: 'Markdown sous docs/tech en cartes et pages, diagrammes Mermaid, site DocFX, contrôles (structure, impact par trailer Git, références générées, rendu dans Chromium), job du CI, service de l''environnement de validation ; à lire pour modifier l''outillage de la documentation technique.'
applies_to:
  - 'docs/tech/docfx.json'
  - 'scripts/Build-TechnicalDocumentation.ps1'
  - 'scripts/Get-TechnicalDocumentationImpact.ps1'
  - 'scripts/Update-TechnicalReferences.ps1'
  - 'docker/validation-docs/**'
  - 'tests/Bdtheque.Documentation.Tests/**'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-16T18:20:07Z }
---

# Documentation technique : mise en œuvre

Mise en œuvre de [Documentation technique](../gestion-projet/gouvernance/documentation-technique.md).

## Forme

- **Markdown sous `docs/tech/`**, lu tel quel par les agents et sur GitHub, organisé comme le `.speckit/` : une **carte** (`index.md`) par dossier, qui liste ses pages avec leur description, et des **pages** courtes, chacune avec un en-tête (titre, description, motifs des fichiers qu'elle décrit). Une forme déjà familière à tout agent du projet, que les mêmes tests savent vérifier, et qui ne fait charger que ce que le sujet exige. `docs/` laisse la place, à côté, à une documentation utilisateur dont rien n'est fixé.
  - **Alternative écartée (documentation technique dans le `.speckit/`)** : elle y mêlerait la description de l'exécution aux exigences et aux raisons, que [Choix d'Implémentation](perimetre.md) réserve.
  - **Alternative écartée (pages sans carte, découvertes par recherche)** : chaque question chargerait les pages pour savoir lesquelles lire.
  - **Alternative écartée (index de recherche sémantique, base vectorielle)** : un service de plus, sans besoin démontré, pour un corpus que ses cartes parcourent.
- **Correspondance fichier → pages** par les motifs de l'en-tête et par les liens vers le code de chaque page, sans annuaire à tenir à côté : un fichier modifié désigne les pages qui le décrivent.

## Diagrammes

- **Mermaid**, seul (MIT, maintenu) : GitHub le rend dans le Markdown, DocFX l'embarque dans son site, et un agent l'écrit et le relit comme du texte.
  - **Alternative écartée (Structurizr DSL, modèle C4)** : un modèle riche, mais ni GitHub ni DocFX ne le rendent ; il exigerait Java ou un conteneur pour produire des images à versionner.
  - **Alternative écartée (PlantUML)** : rendu par Java et Graphviz, ou par un serveur ; GitHub ne le rend pas.
  - **Alternative écartée (D2)** : non rendu par GitHub, un binaire de plus à épingler.
  - **Alternative écartée (images SVG ou PNG versionnées)** : une seconde source à tenir alignée sur le texte, et un diff illisible.

## Site

- **DocFX** (MIT, .NET Foundation, outil .NET local épinglé dans `dotnet-tools.json` comme les autres) : navigation, recherche et rendu de Mermaid embarqués dans son modèle `modern`, sans requête vers Internet ; un lien cassé est un avertissement, que la construction tient pour une erreur. Aucun nouvel environnement : le SDK .NET et PowerShell, déjà requis.
  - **Alternative écartée (Material for MkDocs)** : en maintenance depuis novembre 2025, ses correctifs mêmes arrêtés en novembre 2026, son équipe passée à un nouvel outil encore jeune (Zensical) ; et une chaîne Python à installer partout.
  - **Alternative écartée (Docusaurus, VitePress, Astro Starlight)** : une chaîne Node.js, absente du projet.
  - **Alternative écartée (Hugo)** : Mermaid n'y est pas embarqué, son script serait à fournir et à tenir à jour.
  - **Alternative écartée (docsify, rendu à la volée)** : sans version publiée depuis longtemps.
- **Navigation générée depuis les cartes**, à chaque construction, sur une copie : les cartes restent la seule table des matières écrite.
  - **Alternative écartée (`toc.yml` versionnés)** : une seconde table des matières à tenir alignée sur les cartes.
- **Liens qui sortent de `docs/tech/` menés au même fichier sur GitHub, au commit documenté**, inscrit au pied de chaque page : le site ne contient que la documentation, et aucun lien n'y mène à une autre version.
  - **Alternative écartée (sources et `.speckit/` copiés dans le site)** : un site plusieurs fois plus gros, pour des fichiers que GitHub présente déjà.

## Contrôles

- **Structure, dans les tests du dépôt** (`Bdtheque.Infrastructure.Tests`, catégorie `Repository`), qui s'exécutent à chaque Pull Request : un fichier de code supprimé ou renommé par n'importe quel changement fait échouer le lien ou le motif qui le désigne.
- **Impact d'un changement par un *trailer* Git** : le CI liste les pages qui décrivent un fichier modifié par la Pull Request ; l'une d'elles inchangée, il exige qu'un commit de la Pull Request porte `Documentation-technique:` avec sa raison. La trace vit dans l'historique de la branche, avec le changement, et ne coûte aucune exécution de plus.
  - **Alternative écartée (toute Pull Request modifie `docs/tech/`)** : une contrainte artificielle, contournée par des modifications de pure forme.
  - **Alternative écartée (mention dans la description de la Pull Request)** : la modifier ne relance pas le CI, sauf à le déclencher à chaque modification de la description, chaque fois en entier ([Intégration continue (CI)](../gestion-projet/depot/integration-continue-ci.md), quotas).
- **Références générées** par un script, versionnées, et vérifiées par sa régénération comparée au dossier lui-même, fichiers non suivis compris. Générée aujourd'hui : les références entre projets, depuis les fichiers de projet. Le contrat OpenAPI et le schéma de la base ne le sont pas encore (tickets de suivi, couverture de la documentation) : le premier se consulte exact pour chaque commit dans l'environnement de validation, le second exige une base migrée.
- **Rendu dans Chromium** (`Bdtheque.Documentation.Tests`, Playwright, comme les tests de bout en bout) : le site construit, chaque page est ouverte sur un écran de bureau, chaque diagramme écrit doit être rendu sans erreur de Mermaid, sans être réduit sous 0,75 de sa taille (Mermaid rétrécit un diagramme trop large à la colonne de la page, son texte avec lui), et sans requête hors du poste. Une analyse de la seule syntaxe ne verrait pas cette réduction, cause constatée des diagrammes illisibles ; la disposition elle-même (croisements, libellés), que Mermaid calcule, reste inspectée sur le rendu par l'agent.
  - **Alternative écartée (détection géométrique des chevauchements)** : Mermaid place lui-même les formes sans les superposer ; un tel contrôle serait du code à tenir pour un défaut que l'outil ne produit pas.
- **Job du CI à part** (« Technical documentation »), toujours exécuté, ses étapes omises selon les fichiers modifiés : impact et références à chaque Pull Request ; construction et rendu quand la documentation ou son outillage change. Une Pull Request documentaire seule n'exécute ni les tests de l'application, ni ceux de bout en bout, ni la construction des images.

## Environnement de validation

- **Service `docs` du projet Compose de validation**, image construite sur le poste depuis le commit validé, comme `api` et `frontend` : le SDK construit le site, l'image d'exécution d'`api` (déjà sur le poste) le sert par DocFX, sur `127.0.0.1` seulement. La documentation consultée est donc celle du commit, et disparaît avec l'environnement.
  - **Alternative écartée (site servi par `api` en environnement `Validation`)** : l'image de production devrait embarquer la documentation, ou `api` servir des fichiers qui ne la concernent pas.
  - **Alternative écartée (serveur web dédié, ex. nginx)** : une image de plus à télécharger et à surveiller ([Mises à jour des dépendances](../gestion-projet/depot/mises-a-jour-des-dependances.md)).
- **Scalar conservé**, servi par `api` comme avant ([Documentation de l'API](socle/transverse/documentation-de-l-api.md)) : il explore le contrat réel du commit ; la documentation technique y renvoie sans le reproduire.
  - **Alternative écartée (contrat OpenAPI rendu par DocFX)** : DocFX ne lit que Swagger 2.0, pas OpenAPI 3 ; et une seconde présentation du même contrat, non interactive, ne remplacerait pas l'essai d'un appel.
