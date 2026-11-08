---
type: Référence visuelle
title: 'La couverture du jour'
description: 'Alternative « La couverture du jour » : apparence, présentation, collection vide.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T20:00:00Z }
---

# La couverture du jour

Le dashboard présenté comme une application de lecture d'aujourd'hui, où les albums sont au premier plan : la couverture de l'édition à la une donne ses couleurs à toute la page, et les albums sont des objets en volume. C'est l'alternative résolument moderne : elle n'imite aucun objet imprimé ou physique, et n'est pour autant ni psychédélique (aucun effet criard ou irisé), ni futuriste. Source : `couverture.html`.

## Apparence

- **Couleurs** : celles de la **couverture de l'édition à la une**, qui changent donc chaque jour avec elle. De cette couverture sont tirées trois teintes : sa couleur **dominante**, une version **profonde** de celle-ci et une couleur d'**accent**.
  - Le haut de la page (en-tête, accueil, dernières arrivées) est une **scène** de la couleur profonde, éclairée d'un halo de la couleur dominante derrière l'album à la une et d'un reflet de l'accent dans le coin opposé. Le texte y est blanc.
  - Le reste de la page est d'un fond clair légèrement teinté de la couleur dominante ; les blocs y sont blancs, à peine teintés, avec une ombre douce de la couleur profonde.
  - Les graphiques (valeur, types d'album, éditeurs) prennent les couleurs de la couverture du jour. Les genres gardent chacun la leur, pour rester distincts.
  - **Lisibilité** : quelle que soit la couverture, même pâle (jaune, par exemple), le texte reste lisible :
    - sur la scène, le halo reste derrière l'album à la une et garde une part de la couleur profonde ;
    - sur la couleur d'accent (bouton d'aide, pastilles, bouton de l'administration), le texte est blanc ou presque noir, celui des deux qui atteint un contraste de 4,5:1 ; un accent de ton moyen, qu'aucun des deux n'atteint, est assombri jusqu'à ce que le blanc l'atteigne.
  - **Collection vide** : sans édition à la une, la page prend les couleurs propres au thème, bleu nuit `#1E2A44`, bleu `#3D5A80` et corail `#FF6B57`.
- **Typographie** : une seule police, *Inter Tight*, une grotesque contemporaine, très grasse et au crénage resserré pour les titres et les grands chiffres ; les chiffres sont tabulaires.
- **Albums en volume** : l'album à la une et les dernières éditions entrées sont des livres en volume, un peu tournés, qui montrent leur dos (de la couleur profonde de leur couverture) et posent une ombre au sol. Au survol, un album se tourne face au lecteur et se soulève.
- **Formes** : de grands coins arrondis, des pastilles et des pilules ; les éléments en relief (compteurs, pilules des genres, barres des éditeurs) ont un léger modelé. Aucune inclinaison de la page, aucune texture.
- **Mouvement** : l'album à la une flotte doucement ; à l'ouverture de la page, les dernières éditions entrées arrivent l'une après l'autre. Ces mouvements, comme ceux du survol, sont omis quand le lecteur demande moins d'animations.

## Présentation

- **En-tête**, sur la scène : le nom de l'application, précédé d'un petit album de la couleur d'accent ; le menu, l'entrée active en pilule blanche ; la recherche, champ arrondi translucide ; le bouton d'aide, disque de la couleur d'accent.
- **Accueil**, sur la scène, à gauche de l'album à la une :
  - la **phrase d'accueil**, tirée au hasard dans la liste commune, en très grand titre ;
  - les **compteurs** (albums, séries, éditions) en pastilles translucides ;
  - sous un filet, l'**édition à la une** : une pastille « À la une aujourd'hui » de la couleur d'accent, le libellé de son album en titre, le libellé de l'édition, le résumé de l'album et « Voir l'album → ».
- **L'album à la une**, à droite, en grand et en volume ; sur smartphone, il passe au-dessus du titre.
- **Tout juste arrivés !**, sur la scène : les dernières éditions entrées en rangée d'albums en volume, six sur grand écran, trois par ligne sur smartphone, avec sous chacune le libellé de l'album, l'éditeur et la date. Un lien mène à la liste des albums.
- Les blocs suivants **remontent sur la scène**, de sorte que la page se lit d'un seul tenant :
  - **Ça vaut combien, tout ça ?** : la valeur totale estimée en grands chiffres sur un encadré de la couleur profonde, et la valeur totale connue sur un encadré teinté, chacune avec son montant en euros du jour ; les prix moyen, médian, minimum et maximum en petits encadrés, en euros courants puis en euros du jour ; la mention des montants non convertibles exclus et des éditions gratuites non valorisées ;
  - **Tomes, intégrales ou hors-séries ?** : trois **piles d'albums** vues de côté, posées sur une étagère, dont la hauteur suit la part de chaque catégorie : couleur profonde pour les réguliers, accent pour les intégrales, dominante pour les hors-séries ; sous chacune, le pourcentage et le nombre ;
  - **Plutôt quel genre ?** : une pilule par genre, dans la teinte du genre, dont la taille suit le nombre d'albums ; chacune porte la petite couverture en volume d'un album du genre (sauf « Autres »), puis le nom et le nombre d'albums ;
  - **Chez quels éditeurs ?** : une barre modelée par éditeur, dans une piste en creux, dont la longueur suit le nombre d'éditions possédées ; le premier éditeur de la couleur d'accent, les autres de la couleur profonde.
- **Pied de page** : la mention des montants convertis en euro et le lien vers l'administration.

## Collection vide

- **En-tête** : sans le champ de recherche ; sur smartphone, le bouton d'aide reste sur la ligne du menu.
- **Sur la scène** : une pastille « Collection à démarrer », le titre en très grand, puis les textes. À droite, ou au-dessus sur smartphone, **la place du premier album** : un album en pointillés, tourné comme l'album à la une, marqué « + Premier album », avec son ombre au sol.
- **On jette un œil quand même ?**, bloc blanc remonté sur la scène : les liens Albums, Séries et Auteurs, puis Recherche avancée seule sur la ligne suivante, en pilules teintées.
- **C'est votre collection ?**, à côté, ou dessous sur smartphone : un bloc de la couleur profonde, éclairci, avec un bouton de la couleur d'accent « Ouvrir l'administration → » et la mention de la connexion.
