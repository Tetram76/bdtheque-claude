---
type: Choix d'implémentation
title: 'Recherche avancée : mise en œuvre'
description: 'Arbre des critères, schéma, traduction en prédicat EF Core, comparaison entre deux données, exécution et erreurs.'
applies_to:
  - 'src/Bdtheque.Api/Search/**'
  - 'src/Bdtheque.Contracts/Search/**'
  - 'src/Bdtheque.Api/Endpoints/AdvancedSearchEndpoints.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Recherche avancée : mise en œuvre

- **Les critères sont un arbre** (groupe ET/OU, NON, critère, comparaison entre deux données, lien), en JSON polymorphe. Un **lien porte sa propre condition**, avec son quantificateur pour un lien multiple : un chemin est une imbrication de liens, et un groupe sous un lien porte sur la même entité liée ([Liens entre entités](../../fonctionnel/recherche-avancee/liens-entre-entites.md)).
  - **Alternative écartée (critère portant un chemin de liens)** : chaque lien multiple du chemin doit avoir son quantificateur, et un groupe doit pouvoir s'appliquer à une même entité liée ; un lien porteur d'une condition exprime les deux sans structure de plus.
  - **Valeurs écrites en texte**, quel que soit leur type : une seule forme, qu'une variable de recherche enregistrée remplace sans connaître son type. La précision d'une date est celle de sa valeur.
- **Un schéma déclaré une fois par entité** (données, liens) produit à la fois la description exposée aux clients et la traduction des critères, sans liste à tenir alignée. Ses **clés** sont un contrat que stockent les recherches enregistrées : jamais renommées ni réaffectées.
  - Un **lien suit la relation du modèle, jamais une clé technique**, et un lien inverse est l'inverse exact de son lien direct : les fiches atteintes par un chemin différent (ex. albums qui affichent un genre par leur série) le sont par ce chemin, que l'utilisateur choisit ([Liens entre entités](../../fonctionnel/recherche-avancee/liens-entre-entites.md)).
  - Les montants ne sont interrogés que sur leur présence, jusqu'à leur conversion en euros (Phase 4, [`suivi-implementation/`](../../suivi-implementation/index.md)).
- **Traduction en prédicat EF Core, exécuté par la base**, liens compris. Toute comparaison exige d'abord une donnée renseignée, et le NON retient ce que le critère n'établit pas ([Comparaisons](../../fonctionnel/recherche-avancee/comparaisons.md)). Les **valeurs sont des paramètres**, jamais écrites dans le SQL, celles de « est l'un de » un seul paramètre tableau, quel que soit le mode de traduction des collections du contexte : une même forme de critères reste une seule requête en cache.
  - **Texte** : par défaut sur la collation insensible à la casse et aux accents ([Collation des colonnes texte](../persistance/collation-des-colonnes-texte.md)) ; l'ISBN sans ses séparateurs, comme dans les listes.
  - **Dates comparées comme des périodes** (année, mois, jour). Une date et heure se compare par son jour dans le **fuseau horaire de la recherche**, celui du navigateur où le frontend la présente ([Langue et culture d'affichage](../../fonctionnel/presentation/langue-et-culture-d-affichage.md)), et non en temps universel, où elle est enregistrée. Le fuseau est un paramètre de la requête, jamais une partie des critères : une recherche enregistrée se rejoue depuis tout fuseau. Seuls les noms IANA sont admis, PostgreSQL ne connaissant pas les noms Windows que .NET accepte aussi.
  - **Univers** : la relation d'ancêtre est déroulée dans la requête sur la profondeur de la hiérarchie, lue sur le référentiel des univers dans le même instantané que les résultats ; un univers déplacé plus profond entre les deux échapperait sinon au prédicat ([Hiérarchie des univers](../../fonctionnel/regles-metier/hierarchie-des-univers.md)).
    - **Alternative écartée (vue récursive en base)** : du SQL hors du modèle EF, à ajouter à la main à chaque régénération de la migration unique ([Migrations EF Core](../persistance/migrations-ef-core.md)), pour une hiérarchie de quelques niveaux.
- **Comparaison entre deux données** ([Comparaisons](../../fonctionnel/recherche-avancee/comparaisons.md)) : un nœud de plus, circonscrit au schéma et à la traduction. L'autre donnée, de même type, est portée par la fiche ou atteinte par des **liens vers une seule fiche** ; sous un lien multiple, c'est son quantificateur qui compte les fiches liées. Les dates s'y comparent à la précision que choisit le critère, une date moins précise couvrant toutes les périodes qu'elle contient. Le nœud ne porte aucune valeur : les variables ne le concernent pas, et il ne désigne aucune fiche.
- **Exécution par `POST`**, les critères, un arbre, étant le corps de la requête ; les résultats sont les entrées des listes de la consultation de leur entité, dans leur ordre.
- **Erreurs** : une valeur laissée vide est une erreur métier, que l'utilisateur corrige ; des critères que le schéma ne décrit pas sont une erreur technique, le client les construisant d'après le schéma.
