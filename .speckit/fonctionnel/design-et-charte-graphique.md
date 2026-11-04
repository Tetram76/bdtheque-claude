---
type: Règle métier
title: 'Design et charte graphique'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Design et charte graphique

L'interface doit offrir un rendu **soigné et personnel**, clairement au-dessus du rendu par défaut des frameworks UI. Un template de base (Bootstrap, Material out-of-the-box, etc.) n'est pas acceptable.

La charte graphique, la présentation de chaque écran et leurs maquettes de référence sont décrites dans le dossier [`visuel/`](/visuel) : [`charte-graphique.md`](/visuel/charte-graphique/index.md), puis un fichier par écran (ex. [`dashboard.md`](/visuel/dashboard/index.md)).

**Thème actif** : l'application propose plusieurs thèmes ([Thème de base](/visuel/charte-graphique/theme-de-base.md)). Le thème est **tiré au hasard** au début d'une **visite** (cf. [Mesure d'audience de l'interface web](/fonctionnel/mesure-audience.md), Visites), parmi les thèmes proposés, et conservé toute la durée de la visite, dans la consultation comme dans l'administration, que la visite soit relevée par la mesure d'audience ou non. Seule exception : une visite qui se prolonge au-delà de sa date calendaire (navigateur resté ouvert, notamment sur tablette ou smartphone) se poursuit, mais un nouveau thème est tiré pour chaque nouvelle date, dès la première page affichée à cette date ; la date est celle du fuseau horaire en cours du navigateur (cf. [Langue et culture d'affichage](/fonctionnel/presentation/langue-et-culture-d-affichage.md)).

Le dossier `visuel/` est une **référence visuelle et ergonomique** (apparence, mise en page, maquettes) : il ne définit **pas le contenu** des écrans, qui relève de ce fichier. Un écart entre le contenu d'une maquette et les règles fonctionnelles n'est pas une contradiction : les règles fonctionnelles s'appliquent, la maquette n'illustrant qu'un rendu possible.
