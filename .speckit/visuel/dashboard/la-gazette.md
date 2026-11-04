---
type: Référence visuelle
title: 'La gazette'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# La gazette

Le dashboard mis en page comme la une d'un journal. Source : `gazette.html`.

## Apparence

- **Couleurs** : papier crème `#FBF3EA`, encre violine `#2B1030`, texte secondaire `#5E3F63`, filets `#D9C6CF` ; accent ambre `#FFB547` ; les montants en euros du jour sont en rouille `#9A3D12`.
- **Mise en page** : colonnes séparées de filets, comme dans un journal. Les titres de rubrique sont en serif gras (*Fraunces*). La seule touche de désordre est l'inclinaison de la couverture mise à la une.

## Présentation

De haut en bas :

- **En-tête de journal** : un bandeau « Édition du jour · n° » suivi du nombre d'albums, avec la recherche et l'aide ; puis le titre « La Gazette de la BDthèque » ; enfin le menu, sous un filet, avec l'entrée active soulignée de l'accent.
- **La une**, sur deux tiers de la largeur :
  - La **phrase d'accueil** est le gros titre, en italique et entre guillemets, tirée au hasard dans la liste commune.
  - Les **compteurs** (albums, séries, éditions) forment un bandeau de trois colonnes.
  - L'édition **à la une** ([Page d'accueil](/fonctionnel/page-accueil.md)) : sa couverture, le libellé de son album, le sien et le résumé de l'album.
  - Sous « Tout juste arrivés ! », les dernières éditions entrées, la plus récente comprise, sont listées avec leur date. Un lien mène à la liste des albums.
- **La colonne latérale** :
  - **Le cours de la collection** : la valeur totale estimée dans un encadré en couleurs inversées, avec son montant en euros du jour. Un tableau donne ensuite la valeur totale connue et les prix moyen, médian, minimum et maximum, en euros courants et en euros du jour. Une mention indique les montants non convertibles exclus et les éditions gratuites non valorisées.
  - **Le palmarès des éditeurs** : un classement numéroté des éditeurs, avec le nombre d'éditions possédées de chacun.
- **Bas de page**, sous un double filet :
  - **Tomes, intégrales ou hors-séries ?** : une phrase de synthèse en italique, puis une étagère de dos de livres, un dos pour 1 % des albums : encre pour les réguliers, ambre pour les intégrales, rouille pour les hors-séries. Le nombre et le pourcentage de chaque catégorie sont soulignés de sa couleur. Chaque catégorie a autant de dos que son pourcentage arrondi, si bien que l'étagère peut compter un dos de plus ou de moins que 100, mais jamais un nombre de dos qui contredise le pourcentage affiché.
  - **Plutôt quel genre ?** : les genres sur une ligne typographique. La taille de chaque mot suit le nombre d'albums, les mots alternent romain et italique, et le nombre d'albums suit chaque mot en petit.

## Collection vide

- **En-tête de journal** : le bandeau devient « Édition du jour · Numéro zéro », sans le champ de recherche ; le titre et le menu restent.
- **La une**, sur deux tiers de la largeur :
  - « Les étagères attendent leur premier album ! » est le gros titre, en italique et entre guillemets ;
  - à gauche, un emplacement de photo réservé, barré en croix et marqué « Photo à venir », dans un cadre ambre incliné comme la couverture à la une ;
  - à droite, « La collection ne compte encore aucune édition. » en chapô, puis le texte, ouvert par une lettrine, et sous un filet la mention de la base consultable ;
  - une étagère vide clôt le texte : les dos des livres à venir y sont tracés en pointillés.
- **La colonne latérale** :
  - **On jette un œil quand même ?** : les liens « Albums → », « Séries → » et « Auteurs → » en rouille sur une ligne, puis « Recherche avancée → » ;
  - **C'est votre collection ?** : un encadré en couleurs inversées, avec un bouton ambre « Ouvrir l'administration → ».
- **Smartphone** : la colonne latérale passe sous la une, séparée par un double filet ; l'emplacement de photo, réduit, reste à gauche du texte.
