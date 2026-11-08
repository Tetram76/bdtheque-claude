---
type: Choix d'implémentation
title: 'Recherches enregistrées : mise en œuvre'
description: 'Critères stockés en texte, variables par pointeur JSON, fiches désignées protégées par clé étrangère, administration et rejeu.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/SavedSearch.cs'
  - 'src/Bdtheque.Api/Endpoints/SavedSearchEndpoints.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/SavedSearch*.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Recherches enregistrées : mise en œuvre

- **Critères stockés en texte**, tels qu'écrits, dans la forme JSON du contrat de la recherche avancée ([Recherches enregistrées](../../fonctionnel/recherche-avancee/recherches-enregistrees.md)). Le domaine n'y voit qu'un arbre JSON ; l'API les confronte au schéma de la recherche avancée à chaque écriture, en les traduisant sans les exécuter, si bien qu'une erreur y est la même qu'à l'exécution. Stockée par les recherches et par le journal, jamais purgé, cette forme est figée par des tests, comme les clés du schéma ([Recherche avancée : mise en œuvre](recherche-avancee-mise-en-oeuvre.md)).
  - **Alternative écartée (`jsonb`)** : il réordonne les clés et l'espacement ; les mêmes critères réécrits ne seraient plus égaux à ceux lus, et le journal, qui compare les valeurs ([Journal des modifications : mise en œuvre](../domaine/journal-des-modifications-mise-en-oeuvre.md)), consignerait un changement qui n'en est pas un. Les critères ne sont jamais interrogés en base.
  - **Alternative écartée (critères représentés dans le domaine)** : une seconde copie de l'arbre du contrat et de ses règles, que le domaine ne pourrait de toute façon pas valider sans le schéma, propre à l'API.
- **Variables par pointeur JSON** (RFC 6901) : chacune désigne, dans les critères, la constante qu'elle remplace, et cette constante **est** sa valeur par défaut, sans seconde donnée à tenir alignée. Leurs noms se distinguent sans casse ni accents, leur valeur étant demandée par leur nom au rejeu.
  - **Alternative écartée (variable écrite à la place de sa valeur dans l'arbre)** : le contrat de la recherche avancée devrait accepter une seconde sorte de valeur, que la recherche elle-même n'utilise pas.
- **Fiches désignées protégées par clé étrangère** ([Suppression des entités](../../fonctionnel/regles-metier/suppression-des-entites.md), « Toute fiche ») : seul le schéma de la recherche avancée dit de quelle entité est une valeur, l'API relève donc les fiches que désignent les critères, et le domaine les enregistre avec eux. Une table de désignations porte une **colonne générée stockée par entité désignable**, avec sa clé étrangère `RESTRICT` (PostgreSQL n'en accepte pas sur une colonne générée virtuelle), et une seule renseignée : la base refuse la suppression d'une fiche désignée, quelle que soit l'écriture qui la supprime (cascade, formulaire de la fiche porteuse, écriture concurrente), et aucune désignation ne peut viser une entité sans clé étrangère (une intention d'achat).
  - **Alternative écartée (entité et identifiant seuls, sans clé étrangère)** : la base ne refuserait plus rien, chaque écriture qui supprime une fiche hors du mécanisme de suppression devrait y penser, et une recherche pourrait désigner une fiche disparue.
  - **Alternative écartée (une clé étrangère par entité dans le domaine)** : la sorte de la fiche serait dispersée sur dix propriétés ; les colonnes générées gardent dans le domaine une désignation unique (entité, identifiant).
- **Rejeu** : la valeur des variables est donnée par leur nom, les autres gardant leur valeur par défaut, et la recherche avancée de l'entité est exécutée telle quelle (résultats, ordre, présentation). Le nom d'une recherche est unique sans casse ni accents, comme le libellé d'un genre ([Collation des colonnes texte](../persistance/collation-des-colonnes-texte.md)).
