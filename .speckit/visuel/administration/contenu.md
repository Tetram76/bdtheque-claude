---
type: Référence visuelle
title: 'Contenu'
description: 'Contenu de la page d''accueil de l''administration.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-24T19:33:10Z }
---

# Contenu

La page met en avant les fonctions les plus utiles, de haut en bas :

- **Titre** « Administration », avec une phrase qui résume le rôle de l'espace.
- **Saisir à partir d'un ISBN** : un champ ISBN, dont la croix d'effacement vide le champ ([Contrôles](../liste-albums/controles.md)), et un bouton « Chercher dans les sources », qui crée ou complète à partir des sources externes ([Structure de l'application](../../fonctionnel/structure-de-l-application.md)) l'album, l'édition et ses visuels, la série, les auteurs, l'éditeur et la collection, relus avant enregistrement. Ce bloc est mis en relief. En dessous, « Ou créer directement : » propose la création d'un album, d'une série, d'un auteur ou d'un éditeur, par quatre boutons toujours alignés sur une seule ligne.
- **Les fiches** : une carte par type de fiche, dans l'ordre du menu, plan de l'administration ([En-tête et menu](en-tete-et-menu.md)) : séries, albums, éditions, auteurs, éditeurs, genres, univers (décision de l'utilisateur, #477). Chaque carte a son icône (pour les univers, une bulle de pensée : un nuage suivi de deux bulles, la plus grosse sous le nuage, légèrement décalée vers la droite, la plus petite au-delà ; jamais un globe, réservé aux liens qui font sortir de l'application, [Liens qui font sortir de l'application](../charte-graphique/liens-qui-font-sortir-de-l-application.md) ; décision de l'utilisateur), son nom en titre, son nombre, une précision (pour les albums, ceux qui ne sont que catalogués, « catalogués : 214 », comme les collections des éditeurs, « collections : 214 », au sens de ses [États d'un album](../../fonctionnel/regles-metier/appartenance-a-la-collection.md#états-dun-album)) et, sauf pour les éditions, qui se saisissent depuis leur album ([Principes communs](../listes/principes-communs.md), création), la création d'une nouvelle fiche. C'est le titre de la carte qui ouvre sa liste : il se lit comme un lien, sans perdre son rôle de titre, et la carte n'a aucun lien « Voir les … ».
- **Intentions d'acquisition** : à part des fiches, juste après elles, car leur liste ne se comporte pas comme les autres : une intention n'a pas de fiche propre, elle se pose depuis son album, et sa liste sert à confirmer les acquisitions ([Saisie selon l'entité](../listes/saisie-selon-l-entite.md)). Un bloc à elles, en pleine largeur, de mention « Acquisitions », qui est lui-même leur carte, sans carte à l'intérieur : sur une ligne, l'icône, le nombre, la phrase « acquisitions à confirmer, posées depuis leur album » et, à son extrémité, le lien « Confirmer les acquisitions » vers leur liste ; aucune création.
- **À vérifier** : des listes de fiches à corriger ou à compléter, chacune avec son nombre. Seule une incohérence à corriger (ex. ISBN au chiffre de contrôle incohérent) a la couleur d'alerte du thème. Une erreur acceptée n'est pas comptée ([Validation de l'ISBN](../../fonctionnel/regles-metier/validation-de-l-isbn.md#isbn-au-chiffre-de-contrôle-incohérent), décision de l'utilisateur, #613).
- **Dernières modifications** : les dernières fiches créées ou modifiées, avec leur type, la nature et la date de la modification, et un lien « Modifier ».
- **Paramétrage** : chaque réglage avec sa valeur et un lien « Modifier ».
- **Statistiques privées** : le bloc qui les réunit ; tant qu'aucune n'est définie, il l'indique.
