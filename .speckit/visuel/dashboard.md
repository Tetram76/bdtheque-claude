# Dashboard

Ce fichier décrit la présentation du dashboard, la page d'accueil de la partie Consultation (`fonctionnel.md` § Page d'accueil), selon la charte de [`charte-graphique.md`](charte-graphique.md). Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation).

---

## Maquette de référence

Sources dans [`maquettes/dashboard/`](maquettes/dashboard/), canevas : <https://claude.ai/artifact/GVhoEBfShTiTCK9ZEPSUaf>.

| Fichier | Contenu |
| --- | --- |
| `Violine.dc.html` | Le dashboard en teinte violine. |
| `Mure.dc.html` | Le dashboard en teinte mûre. |
| `Main.dc.html` | Source commune aux deux variantes, qu'elles affichent chacune avec sa teinte ; son nom est imposé par le canevas, dont c'est le fichier d'entrée. |

## Présentation

De haut en bas :

- **Accueil** : une grande bulle porte une **phrase d'accueil tirée au hasard** à chaque affichage, dans une liste prédéfinie :
  - « Entrez donc, il y a de quoi lire ! »
  - « Installez-vous, la collection vous attend ! »
  - « Alors, on bouquine ? »
  - « Encore de la place sur les étagères ? Pas sûr… »
  - « Attention, ça déborde des étagères ! »

  À côté, les **compteurs** : albums, séries, éditions.
- **Tout juste arrivés !** : les dernières éditions entrées dans la collection (ordre : `fonctionnel.md` § Page d'accueil), par leur couverture, avec le libellé de l'album, l'éditeur et la date d'acquisition, omise lorsqu'elle n'est pas connue ; un lien mène à la liste des albums. Sur smartphone, ce lien passe sous la bulle de titre.
- **Ça vaut combien, tout ça ?** : valeur totale estimée et valeur totale connue, puis prix moyen, médian, minimum et maximum, chaque montant en euros courants et en euros du jour (`fonctionnel.md` § Gestion des devises) ; une mention indique les montants non convertibles exclus et les éditions gratuites non valorisées.
- **Tomes, intégrales ou hors-séries ?** : la répartition par type d'album est représentée par des **piles de livres** posées sur une étagère, une par catégorie — réguliers, intégrales, hors-séries, une intégrale hors-série comptant parmi les hors-séries (`fonctionnel.md` § Page d'accueil) — (un livre ≈ 100 albums), avec le nombre d'albums au-dessus et le pourcentage en dessous. Les piles ont l'air **empilées à la main** : en tendance, les livres s'élargissent vers la base pour que la pile tienne, mais leur largeur, leur décalage et leur inclinaison varient, et cette variation est **tirée au hasard à chaque affichage**, dans des limites qui gardent la pile plausible.
- **Plutôt quel genre ?** : les genres en étiquettes colorées, dont la taille suit le nombre d'albums ; un album peut relever de plusieurs genres.
- **Chez quels éditeurs ?** : les éditeurs en barres horizontales, sur les éditions possédées.

Chaque genre, éditeur ou couverture mène à la liste ou à la fiche correspondante.

## Alternatives

Deux autres présentations du dashboard restent en lice. Elles portent le même contenu, mais **s'écartent de la charte** de [`charte-graphique.md`](charte-graphique.md) : thème clair, sans cases ni bulles, avec leur propre typographie. En retenir une imposerait de revoir la charte.

Leurs sources sont dans [`maquettes/dashboard/`](maquettes/dashboard/), et les deux maquettes partagent un même canevas : <https://claude.ai/artifact/DSFTnXe5VY4J1nmZrDDngs>.

### La gazette

Le dashboard mis en page comme la une d'un journal. Source : `Gazette.dc.html`.

#### Apparence

- **Couleurs** : papier crème `#FBF3EA`, encre violine `#2B1030`, texte secondaire `#5E3F63`, filets `#D9C6CF` ; accent ambre `#FFB547` ; les montants en euros du jour sont en rouille `#9A3D12`.
- **Mise en page** : colonnes séparées de filets, comme dans un journal. Les titres de rubrique sont en serif gras (*Fraunces*). La seule touche de désordre est l'inclinaison de la couverture mise à la une.

#### Présentation

De haut en bas :

- **En-tête de journal** : un bandeau « Édition du jour · n° » suivi du nombre d'albums, avec la recherche et l'aide ; puis le titre « La Gazette de la BDthèque » ; enfin le menu, sous un filet, avec l'entrée active soulignée de l'accent.
- **La une**, sur deux tiers de la largeur :
  - La **phrase d'accueil** est le gros titre, en italique et entre guillemets, tirée au hasard dans la même liste.
  - Les **compteurs** (albums, séries, éditions) forment un bandeau de trois colonnes.
  - La **dernière édition entrée** dans la collection est à la une : sa couverture, sa date d'entrée, le titre, la série et l'éditeur de son album, et le résumé de l'album.
  - Sous « Également arrivés », les entrées suivantes sont listées avec leur date. Un lien mène à la liste des albums.
- **La colonne latérale** :
  - **Le cours de la collection** : la valeur totale estimée dans un encadré en couleurs inversées, avec son montant en euros du jour. Un tableau donne ensuite la valeur totale connue et les prix moyen, médian, minimum et maximum, en euros courants et en euros du jour. Une mention indique les montants non convertibles exclus et les éditions gratuites non valorisées.
  - **Le palmarès des éditeurs** : un classement numéroté des éditeurs, avec le nombre d'éditions possédées de chacun.
- **Bas de page**, sous un double filet :
  - **Tomes, intégrales ou hors-séries ?** : une phrase de synthèse en italique, puis une étagère de 100 dos de livres, chacun valant 1 % des albums : encre pour les réguliers, ambre pour les intégrales, rouille pour les hors-séries. Le nombre et le pourcentage de chaque catégorie sont soulignés de sa couleur.
  - **Plutôt quel genre ?** : les genres sur une ligne typographique. La taille de chaque mot suit le nombre d'albums, les mots alternent romain et italique, et le nombre d'albums suit chaque mot en petit.

Comme dans la présentation principale, chaque genre, éditeur ou album mène à la liste ou à la fiche correspondante.

### Le sommaire de magazine

Le dashboard présenté comme la couverture et le sommaire d'un magazine populaire. Chaque rubrique du sommaire annonce une statistique et mène à sa liste ou à sa page détaillée. Source : `Sommaire.dc.html`, en thème « papier ».

#### Apparence

- **Couleurs** : impression en trois encres vives et du noir, sur papier journal. Fond `#FAF7F0` ; encre noire `#111111`, texte secondaire `#2A2A2A`, texte atténué `#5C5C5C` ; rouge `#E63946`, qui sert aussi d'accent, bleu `#1D4ED8` et jaune `#FFD60A`. Un texte posé sur le rouge ou le bleu est blanc, un texte posé sur le jaune est noir.
- **Typographie** :
  - la police du nom, des titres d'une page et des numéros de page est *Anton*, une police haute et étroite, en majuscules ;
  - les titres de rubrique et le texte sont en *Libre Franklin*, en gras pour les titres.
- **Mise en page** : des filets noirs épais. La seule touche de désordre est l'inclinaison de la couverture et d'une étiquette posée sur elle.

#### Présentation

- **En-tête** : le menu, sur des majuscules en gras, avec l'entrée active sur fond d'accent. La recherche est un champ à bord noir, et le bouton d'aide est jaune. Un filet noir épais termine l'en-tête.
- **La couverture**, à gauche, au format d'une page de magazine, rouge, légèrement inclinée et portant une ombre noire décalée :
  - le nom de l'application en blanc ;
  - un bandeau entre deux filets : « N° », suivi du nombre d'albums, le mois et la mention « Gratuit » ;
  - la couverture du dernier album entré dans la collection, sur presque toute la hauteur ;
  - la **phrase d'accueil** en gros titre blanc, tirée au hasard dans la même liste ;
  - des étiquettes noires, qui donnent le nombre de séries et d'éditions, et une étiquette jaune inclinée, qui annonce le dernier album entré.
- **Au sommaire**, à droite : une rubrique par statistique, chacune avec un grand numéro de page coloré, son titre, une accroche et un petit visuel.
  - **Tout juste arrivés !** : une accroche qui cite les derniers albums entrés, et les vignettes de leurs couvertures.
  - **Ça vaut combien, tout ça ?** : la valeur totale estimée, avec son montant en euros du jour surligné en jaune, le prix moyen et le prix maximum. En petit suivent la valeur totale connue, les prix médian et minimum, et la mention des montants non convertibles exclus et des éditions gratuites non valorisées.
  - **Tomes, intégrales ou hors-séries ?** : une phrase de synthèse avec les trois nombres, puis une barre en trois couleurs dont la longueur de chaque part suit son pourcentage : jaune pour les réguliers, bleu pour les intégrales, rouge pour les hors-séries.
  - **Plutôt quel genre ?** : une accroche sur les genres en tête, puis les genres en majuscules. La taille de chaque mot suit le nombre d'albums, les couleurs alternent entre rouge, bleu et noir, et les mots alternent romain et italique.
  - **Chez quels éditeurs ?** : une accroche sur le classement, puis un podium des trois premiers éditeurs, avec le nom et le nombre d'éditions possédées de chacun. Les marches sont jaune pour le premier, bleu pour le deuxième, rouge pour le troisième.
