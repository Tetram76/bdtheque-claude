---
type: Règle métier
title: 'Fusion de deux fiches'
description: 'Fusion de deux fiches d''un même type, depuis la liste de leur type : garder le meilleur des deux fiches, chaque donnée retenue ou écartée, aucune fiche privilégiée, fiches qui les désignent reportées sur celle qui reste, doublons créés par la fusion fusionnés avec elle, fiches pressenties proposées d''office au choix de l''autre fiche (forme commune des textes, ressemblances, données comparées par type, ordre, mesure d''audience), ce qui en découle (listes, intentions, univers, confirmation et ses suppressions d''information mises en évidence, journal) ; à lire pour toute fusion.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-27T12:16:22Z }
---

# Fusion de deux fiches

La **fusion** réunit en une seule deux fiches d'un même type, saisies en double (ex. deux albums, ou deux éditeurs `Dargaud` et `dargaud`). Elle est proposée depuis la liste de leur type, dans l'administration ([Structure de l'application](../structure-de-l-application.md)).

## Principe : le meilleur des deux fiches

La fusion ne **remplace** pas une fiche par l'autre : elle **garde le meilleur des deux** (décision de l'utilisateur).

- **Chaque donnée** des deux fiches peut être **retenue**, au détriment de la donnée correspondante de l'autre fiche, ou **écartée**, au profit de celle de l'autre fiche (décision de l'utilisateur).
- **Aucune fiche n'est privilégiée** pour rester : ni celle depuis laquelle la fusion est lancée, ni l'autre. Le plus simple est de garder celle dont la fusion retient le plus de données (décision de l'utilisateur).
- **Les fiches qui désignent l'une des deux fiches désignent ensuite celle qui reste**, toutes et sans choix : elles ne sont pas des données de la fiche fusionnée, et les en détacher reviendrait à les supprimer (décision de l'utilisateur). Ex. les éditions et les séries de deux éditeurs passent à l'éditeur qui reste ; les albums de deux séries, à la série qui reste ; les contributions de deux auteurs, à l'auteur qui reste ; les albums et les séries de deux genres ou de deux univers, au genre ou à l'univers qui reste ; une recherche enregistrée qui désignait l'une des deux fiches désigne celle qui reste.

- **Doublons créés par la fusion, fusionnés avec elle** : deux fiches que la fusion rendrait homonymes là où leur nom doit être unique sont fusionnées dans la même opération, selon le même principe, ce que l'on garde de chacune se choisissant au même moment (décision de l'utilisateur). Ex. deux éditeurs fusionnés qui ont chacun une collection « Aire Libre » ([Collection éditeur](../../modele-metier/collection-editeur.md)) ; deux univers fusionnés qui ont chacun un sous-univers « Clone Wars » ([Univers](../../modele-metier/univers.md)), à tous les niveaux de leur hiérarchie.

## Fiches pressenties

Le choix de l'autre fiche propose d'office les fiches **pressenties**, celles qui ressemblent à la fiche de départ (exigence de l'utilisateur, #655). Pressentir n'est pas tomber juste à chaque fois : la fusion sert surtout à corriger une double saisie, le plus souvent les mêmes identifiants avec une différence de frappe, mais ce n'est pas sa seule raison (décision de l'utilisateur). Une fiche pressentie est proposée, jamais imposée : la recherche de l'autre fiche reste disponible.

- **Textes ramenés à une forme commune** avant d'être comparés (décision de l'utilisateur) : sans majuscules ni accents, les variantes typographiques d'un caractère confondues comme dans la recherche par texte, sans ponctuation ni espaces en trop, sans l'article initial que la clé de tri met en suffixe (« Les Tuniques bleues », « Tuniques bleues (Les) », [Tri et navigation par initiale](../presentation/tri-et-navigation-par-initiale.md)), un nombre valant le même nombre dans toutes ses notations, en chiffres arabes, en chiffres romains ou en toutes lettres (« Astérix 2 », « Astérix II », « Astérix deux »).
- **Ressemblances** (décision de l'utilisateur) : une fiche est pressentie si ses textes, ainsi ramenés, sont **identiques** à ceux de la fiche de départ (« Dargaud », « DARGAUD. »), ou n'en diffèrent que de quelques caractères, une **distance de Levenshtein courte** proportionnée à leur longueur : au plus 1 caractère jusqu'à 6 caractères, 2 jusqu'à 12, 3 au-delà (« Franquin », « Franqin »).
  - **Inclusion, là où elle est pertinente** (décision de l'utilisateur) : un texte contenu dans l'autre, en mots entiers, rend aussi la fiche pressentie pour les types où une double saisie ajoute souvent des mots à un même identifiant : auteurs (« Gotlib », « Marcel Gotlib »), éditeurs (« Dupuis », « Éditions Dupuis »), séries (« Blake et Mortimer », « Les aventures de Blake et Mortimer ») et albums (« Le Lotus bleu », « Tintin – Le Lotus bleu »). Elle ne vaut ni pour les genres ni pour les univers, dont un nom plus long désigne d'ordinaire une autre fiche, plus précise (« Humour noir », « Star Wars : Clone Wars »).
- **Données comparées, selon le type** (décision de l'utilisateur) : une fiche est pressentie dès qu'une de ses données ressemble à une donnée de la fiche de départ.
  - **Auteurs** : chacun de leurs identifiants, pseudonyme, nom, prénom et nom dans les deux ordres, face à chacun de ceux de l'autre : le pseudonyme de l'un peut être le nom de l'autre.
  - **Éditeurs** : le nom ; **genres** : le libellé ; **univers** : le nom, quel que soit leur parent ; **séries** : le titre.
  - **Albums** : le libellé, série et titre (« Tintin – Le Lotus bleu », « Le Lotus bleu » de la série « Tintin ») ; et, quels que soient leurs titres, deux albums d'une même série et d'un même tome.
  - **Éditions** : un même ISBN, l'ISBN-10 et l'ISBN-13 d'un même livre étant équivalents ; ou un même album et un même éditeur, deux tirages réels pouvant alors être pressentis.
- **Ordre** : de la plus ressemblante à la moins ressemblante, textes identiques, puis distance croissante, puis inclusion.
- **Mesure d'audience** : la fusion poursuivie avec une fiche pressentie, plutôt qu'avec un résultat de la recherche, est une action suivie de la liste de son type ([Mesure d'audience de l'interface web](../mesure-audience.md)), pour dire si les fiches pressenties servent (décision de l'utilisateur, #655).

## Ce qui en découle

Ces règles appliquent le principe et les règles existantes du modèle, sans en ajouter :

- **Données d'une fiche** : une donnée à une seule valeur (titre, date, résumé…) prend la valeur de l'une ou de l'autre fiche ; une donnée de même valeur dans les deux fiches n'a rien à choisir. Chaque élément d'une liste propre à la fiche (genres et univers d'un album ou d'une série, contributions, visuels d'une édition) est retenu ou écarté un à un.
- **Doublons d'une liste** : un même élément présent dans les deux fiches n'est gardé qu'une fois (ex. une contribution d'un même auteur, dans un même rôle, sur un même album qu'ont deux auteurs fusionnés).
- **Intention d'acquisition** : les contraintes des intentions ([Intention d'acquisition](intention-d-acquisition.md)) valent pour la fiche qui reste. Une intention sur l'album n'est plus possible dès que l'album qui reste a une édition : elle est retirée. Une édition qui reste possédée ne peut pas être visée : son intention est retirée ; une édition qui reste non possédée garde une intention, une seule, et n'a pas de données d'acquisition ([Édition](../../modele-metier/edition.md)).
- **Éditeur et collection d'une même fiche** : la collection d'une édition, ou la collection template d'une série, appartient à son éditeur ([Relations et cardinalités](../../modele-metier/relations-et-cardinalites.md)) : l'éditeur et la collection d'une fiche fusionnée se gardent ensemble, de la même fiche.
- **Univers** : l'univers qui reste ne peut pas avoir pour parent l'un de ses propres sous-univers ([Univers — relations](../../modele-metier/relations-et-cardinalites.md#univers--relations)), ni l'univers fusionné avec lui.
- **Confirmation** : la fusion est définitive et se confirme, avec son impact par type de fiche, dans l'ordre des types de fiche ([Ordre des types de fiche](../presentation/ordre-des-types-de-fiche.md)), comme une suppression ([Suppression des entités](suppression-des-entites.md)).
  - **Suppressions d'information mises en évidence** : la confirmation met en évidence les suppressions d'information que fait la fusion, pour qu'elles ne puissent être confondues avec les modifications (décision de l'utilisateur, #646) : toute fiche qu'elle supprime en plus de l'autre fiche (ex. l'intention d'acquisition sur l'album entier qu'elle retire), et tout élément d'une liste écarté (un genre, un univers, une contribution que portait une seule des deux fiches). Une valeur écartée, que l'écran vient de faire choisir, n'y figure pas (décision de l'utilisateur, #646).
- **Journal** : le journal des modifications consigne ses écritures comme toutes les autres ([Entrée du journal des modifications](../../modele-metier/entree-du-journal-des-modifications.md)) : la modification de la fiche qui reste, la suppression de l'autre, et la modification de chaque fiche qui la désignait.
