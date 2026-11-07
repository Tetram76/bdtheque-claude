---
type: Référence visuelle
title: 'Apparence propre à chaque alternative'
description: 'Règles communes aux alternatives : recherche et aide visibles, menu horizontal figé qui défile.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Apparence propre à chaque alternative

Couleurs, typographie et éléments graphiques ne sont pas communs : chaque alternative des maquettes a les siens, décrits avec elle dans [Alternatives](../dashboard/index.md). Seules les règles suivantes valent pour toutes :

- le **champ de recherche** et le **bouton d'aide** sont toujours visibles dans l'en-tête, à une exception : l'écran d'accueil d'une collection vide n'a pas de champ de recherche, qui n'aurait presque rien à trouver (cf. [Collection vide](../dashboard/collection-vide.md)) ;
- le **menu horizontal** reste figé en haut de l'écran quand la page défile, avec l'en-tête qui le porte lorsque celui-ci est compact ; il ne passe jamais à la ligne : il défile horizontalement, et un dégradé marqué d'un chevron signale, de chaque côté, les entrées hors de vue. Il défile au doigt, à la molette de la souris, ou d'un clic sur un chevron, qui l'avance d'une largeur : sans écran tactile, sa barre de défilement étant masquée, rien d'autre ne le ferait défiler. Il en va de même de toute ligne d'entrées qui défile ainsi (ex. la navigation par initiale). À l'ouverture de la page, il est centré sur l'entrée active. Sur petit écran, la recherche passe sous le menu, sans défiler.
