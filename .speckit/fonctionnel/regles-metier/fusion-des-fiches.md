---
type: Règle métier
title: 'Fusion de deux fiches'
description: 'Fusion de deux fiches d''un même type, depuis la liste de leur type : garder le meilleur des deux fiches, chaque donnée retenue ou écartée, aucune fiche privilégiée, fiches qui les désignent reportées sur celle qui reste, ce qui en découle (listes, intentions, univers, confirmation, journal) ; à lire pour toute fusion.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-24T19:30:00Z }
---

# Fusion de deux fiches

La **fusion** réunit en une seule deux fiches d'un même type, saisies en double (ex. deux albums, ou deux éditeurs `Dargaud` et `dargaud`). Elle est proposée depuis la liste de leur type, dans l'administration ([Structure de l'application](../structure-de-l-application.md)).

## Principe : le meilleur des deux fiches

La fusion ne **remplace** pas une fiche par l'autre : elle **garde le meilleur des deux** (décision de l'utilisateur).

- **Chaque donnée** des deux fiches peut être **retenue**, au détriment de la donnée correspondante de l'autre fiche, ou **écartée**, au profit de celle de l'autre fiche (décision de l'utilisateur).
- **Aucune fiche n'est privilégiée** pour rester : ni celle depuis laquelle la fusion est lancée, ni l'autre. Le plus simple est de garder celle dont la fusion retient le plus de données (décision de l'utilisateur).
- **Les fiches qui désignent l'une des deux fiches désignent ensuite celle qui reste**, toutes et sans choix : elles ne sont pas des données de la fiche fusionnée, et les en détacher reviendrait à les supprimer (décision de l'utilisateur). Ex. les éditions et les séries de deux éditeurs passent à l'éditeur qui reste ; les albums de deux séries, à la série qui reste ; les contributions de deux auteurs, à l'auteur qui reste ; les albums et les séries de deux genres ou de deux univers, au genre ou à l'univers qui reste ; une recherche enregistrée qui désignait l'une des deux fiches désigne celle qui reste.

## Ce qui en découle

Ces règles appliquent le principe et les règles existantes du modèle, sans en ajouter :

- **Données d'une fiche** : une donnée à une seule valeur (titre, date, résumé…) prend la valeur de l'une ou de l'autre fiche ; une donnée de même valeur dans les deux fiches n'a rien à choisir. Chaque élément d'une liste propre à la fiche (genres et univers d'un album ou d'une série, contributions, visuels d'une édition) est retenu ou écarté un à un.
- **Doublons d'une liste** : un même élément présent dans les deux fiches n'est gardé qu'une fois (ex. une contribution d'un même auteur, dans un même rôle, sur un même album qu'ont deux auteurs fusionnés).
- **Intention d'acquisition** : les contraintes des intentions ([Intention d'acquisition](intention-d-acquisition.md)) valent pour la fiche qui reste. Une intention sur l'album n'est plus possible dès que l'album qui reste a une édition : elle est retirée. Une édition qui reste possédée ne peut pas être visée : son intention est retirée ; une édition qui reste non possédée garde une intention, une seule, et n'a pas de données d'acquisition ([Édition](../../modele-metier/edition.md)).
- **Univers** : l'univers qui reste ne peut pas avoir pour parent l'un de ses propres sous-univers ([Univers — relations](../../modele-metier/relations-et-cardinalites.md#univers--relations)), ni l'univers fusionné avec lui.
- **Confirmation et journal** : la fusion est définitive et se confirme, avec son impact par type de fiche, dans l'ordre des types de fiche ([Ordre des types de fiche](../presentation/ordre-des-types-de-fiche.md)), comme une suppression ([Suppression des entités](suppression-des-entites.md)). Le journal des modifications consigne ses écritures comme toutes les autres ([Entrée du journal des modifications](../../modele-metier/entree-du-journal-des-modifications.md)) : la modification de la fiche qui reste, la suppression de l'autre, et la modification de chaque fiche qui la désignait.
