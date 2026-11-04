---
type: Règle métier
title: 'Couverture d''un album'
description: 'Couverture d''un album : première couverture de sa plus ancienne édition illustrée, selon le contexte d''affichage.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Couverture d'un album

Un album n'a pas de couverture propre : sa couverture est celle de l'une de ses éditions (un visuel appartient toujours à une édition, [Visuel d'édition](/modele-metier/visuel-d-edition.md)). Toute représentation d'un album par sa couverture (ex. cartes d'album, piles de couvertures d'une série) retient, parmi les éditions qui ont un visuel de type **Couverture**, la plus ancienne selon son **année d'édition**, les éditions sans année d'édition venant en dernier, et de cette édition sa **première couverture** dans l'ordre de ses visuels (cf. [Ordre des visuels d'une édition](/fonctionnel/regles-metier/ordre-des-visuels-d-une-edition.md)).

Les éditions prises en compte dépendent du contexte d'affichage :

- **donnée de la collection** (ex. statistiques, consultation limitée à la collection, cf. [Périmètre de la consultation](/fonctionnel/regles-metier/perimetre-de-la-consultation.md)) : seules les éditions **possédées** ;
- **ailleurs** (ex. fiches de série et d'auteur) : toutes les éditions de l'album, possédées ou visées par une intention d'achat.

Un album dont aucune des éditions prises en compte n'a de couverture (y compris un album sans édition) est représenté par le visuel générique (cf. [Visuel par défaut](/fonctionnel/regles-metier/visuel-par-defaut.md)).
