---
type: Référence visuelle
title: 'Écran d''accueil'
description: 'Écran de la première requête d''un navigateur, avant la page demandée : identité commune, pile d''albums réaliste animée, nom et état annoncé.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-12T19:51:08Z }
---

# Écran d'accueil

L'écran d'accueil s'affiche à la première requête d'un navigateur, à la place de la page demandée, le temps d'apprendre ce que seul le navigateur connaît (mise en œuvre : [Internationalisation](../../choix-implementation/socle/transverse/internationalisation.md)). La page le remplace dès que possible, le plus souvent au bout d'une fraction de seconde : il n'a aucune durée minimale.

- **Identité commune** : le thème de la visite n'étant pas encore tiré, l'écran ne suit aucun thème : il porte les couleurs du thème de base ([Thème de base](../charte-graphique/theme-de-base.md)) pour le fond, le texte et l'accent, et la police du système, sans police ni fichier à télécharger (décision de l'utilisateur).
- **Une pile d'albums**, au centre de l'écran (décision de l'utilisateur, parmi une bulle et un strip de cases) : quatre albums cartonnés en volume, vus de trois quarts par-dessus, avec leur couverture, la tranche des pages entre les deux plats et la tranche de côté dans l'ombre. Ils sont décalés les uns des autres, si bien que les couvertures du dessous dépassent ; celui du dessus montre sa couverture entière : un titre et une scène (ciel, colline, soleil, un personnage qui parle dans une bulle).
- **Rendu réaliste**, dessiné en SVG et CSS (décision de l'utilisateur, plutôt qu'une photographie intégrée à la page) : grain d'impression sur les couvertures et le papier, tranche des pages finement lignée et légèrement irrégulière, couvertures éclairées avec un reflet doux, ombre de chaque album sur celui du dessous et de la pile sur la table.
- **Animation** : les albums tombent l'un après l'autre sur la pile, d'une chute qui accélère, rebondissent un peu et s'y posent ; la pile reste ensuite telle quelle tant que la page n'est pas là. En **mouvement réduit**, la pile est fixe.
- **Sous la pile**, le nom de l'application, « BD » à l'encre et « thèque » dans la couleur d'accent, puis « Ouverture de la collection… » en texte atténué : l'état de l'écran, annoncé aux lecteurs d'écran, le dessin leur étant masqué.
