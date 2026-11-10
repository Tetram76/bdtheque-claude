---
type: Référence visuelle
title: 'Contenu'
description: 'Contenu de la page d''accueil de l''administration.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-10T21:30:00Z }
---

# Contenu

La page met en avant les fonctions les plus utiles, de haut en bas :

- **Titre** « Administration », avec une phrase qui résume le rôle de l'espace.
- **Saisir à partir d'un ISBN** : un champ ISBN et un bouton « Chercher dans les sources », qui crée ou complète à partir des sources externes ([Structure de l'application](../../fonctionnel/structure-de-l-application.md)) l'album, l'édition et ses visuels, la série, les auteurs, l'éditeur et la collection, relus avant enregistrement. Ce bloc est mis en relief. En dessous, « Ou créer directement : » propose la création d'un album, d'une série, d'un auteur ou d'un éditeur, par quatre boutons toujours alignés sur une seule ligne.
- **Les fiches** : une carte par type de fiche (albums, éditions, séries, auteurs, éditeurs, genres, univers), avec son icône, son nom en simple titre, son nombre, une précision, un seul lien vers sa liste, qui désigne les fiches (« Voir les albums », « Voir les éditions », « Voir les séries », « Voir les auteurs », « Voir les éditeurs », « Voir les genres », « Voir les univers ») et a l'apparence de tout lien de l'application, et, sauf pour les éditions, qui se saisissent depuis leur album, la création d'une nouvelle fiche.
- **Intentions d'achat** : à part des fiches, juste après elles, car leur liste ne se comporte pas comme les autres : une intention n'a pas de fiche propre, elle se pose depuis son album, et sa liste sert à confirmer les achats ([Saisie selon l'entité](../listes/saisie-selon-l-entite.md)). Un bloc à elles, en pleine largeur, de mention « Achats », qui ne contient qu'une carte, de même aspect que celles des fiches mais sans titre propre : son icône, son nombre, la phrase « achats à confirmer, posés depuis leur album » et le lien « Confirmer les achats » vers leur liste ; aucune création.
- **À vérifier** : des listes de fiches à corriger ou à compléter, chacune avec son nombre. Seule une incohérence à corriger (ex. ISBN au chiffre de contrôle incohérent) a la couleur d'alerte du thème.
- **Dernières modifications** : les dernières fiches créées ou modifiées, avec leur type, la nature et la date de la modification, et un lien « Modifier ».
- **Paramétrage** : chaque réglage avec sa valeur et un lien « Modifier ».
- **Statistiques privées** : le bloc qui les réunit ; tant qu'aucune n'est définie, il l'indique.
