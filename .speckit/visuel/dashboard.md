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

Cinq autres présentations du dashboard restent en lice. Elles portent le même contenu, mais **s'écartent de la charte** de [`charte-graphique.md`](charte-graphique.md) : thème clair, sans cases ni bulles, avec leur propre typographie. En retenir une imposerait de revoir la charte.

Leurs sources sont dans [`maquettes/dashboard/`](maquettes/dashboard/), et les cinq maquettes partagent un même canevas : <https://claude.ai/artifact/DSFTnXe5VY4J1nmZrDDngs>.

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

### Le plan de métro

Le dashboard présenté comme un plan de métro. Source : `Metro.dc.html`.

#### Apparence

- **Couleurs** : fond blanc `#FFFFFF`, texte bleu nuit `#14213D`, texte atténué `#5A6478`, fond des encadrés `#F2F4F7`. Le bleu émaillé `#0A2C6E` est celui des panneaux, de l'en-tête et du pied de page.
- **Lignes** : chacune a sa couleur et une couleur de texte qui reste lisible sur sa pastille.
  - Texte bleu nuit : jaune `#FFCD00`, bleu ciel `#6EC4E8`, orange `#F28E42` et gris `#9B9B9B`.
  - Texte blanc : rouge `#E3051C`, vert `#00A88F`, prune `#A0006E` et brun `#8D653D`.
- **Typographie** : *Barlow*, une police de signalétique, en gras pour les titres ; *Barlow Condensed* pour les mentions en capitales et le tableau des départs.
- **Mise en page** : aucune inclinaison ; tout est droit, comme sur un plan.

#### Présentation

Une ligne jaune, la « Ligne Collection », descend le long de la page. Chaque rubrique est une de ses stations : un rond blanc cerclé de bleu nuit, et une pastille jaune marquée « C » aux deux terminus.

- **En-tête**, sur fond bleu émaillé : le nom de l'application avec une pastille « B », le menu, avec l'entrée active sur fond jaune, la recherche et le bouton d'aide, jaune.
- **Terminus Accueil** :
  - la **phrase d'accueil**, tirée au hasard dans la même liste, sur un panneau bleu émaillé à double filet blanc ;
  - les **compteurs**, en pastilles de correspondance : une pastille de couleur avec une lettre (A, S, E), le nombre et son libellé.
- **Tout juste arrivés !** : un **tableau des départs** à fond noir et texte ambre, avec la date d'entrée, le libellé de l'album en blanc et l'éditeur de chacune des dernières éditions entrées. Un lien mène à la liste des albums.
- **Ça vaut combien, tout ça ?** :
  - la valeur totale estimée et la valeur totale connue, sur deux **tickets à bande magnétique**, chacune en euros courants et en euros du jour ;
  - les prix moyen, médian, minimum et maximum, dans des encadrés marqués d'un trait de couleur ;
  - la mention des montants non convertibles exclus et des éditions gratuites non valorisées.
- **Tomes, intégrales ou hors-séries ?** : une ligne par type, dont la longueur suit son pourcentage. Chaque ligne se termine par un terminus, suivi du nombre d'albums et du pourcentage.
- **Plutôt quel genre ?** : chaque genre est une **ligne numérotée**, avec sa pastille de couleur et son nom. Sa longueur suit le nombre d'albums, avec une station tous les 50 albums.
- **Chez quels éditeurs ?** : les éditeurs sont les **stations d'une ligne rouge horizontale**. La taille de chaque station suit le nombre d'éditions possédées, son nom est écrit en biais au-dessus, et le nombre d'éditions est indiqué en dessous.

Chaque genre, éditeur ou album mène à la liste ou à la fiche correspondante.

### Le fanzine en risographie

Le dashboard présenté comme un fanzine imprimé en risographie : peu d'encres, qui se mélangent là où elles se chevauchent, avec un léger décalage d'impression. Source : `Riso.dc.html`.

#### Apparence

- **Couleurs** : papier écru `#F4EFE4` et trois encres.
  - Le bleu `#0078BF` est la couleur du texte. Le rose fluo `#FF48B0` sert d'accent, et le jaune `#FFE800` sert de troisième couleur.
  - Les aplats qui se chevauchent se mélangent, comme deux encres superposées.
  - Les gros titres portent une ombre décalée de l'autre encre, qui imite le décalage d'impression.
- **Typographie** : *Rubik Mono One*, une police large en capitales, pour le nom, les titres et les grands chiffres ; *Space Grotesk* pour le texte.
- **Mise en page** : un collage. Bandes de papier et couvertures sont légèrement inclinées, les couvertures sont tenues par un morceau de scotch, et les traits de séparation sont en pointillés.

#### Présentation

- **En-tête** : le menu, avec l'entrée active sur fond rose ; la recherche dans un cadre en pointillés ; le bouton d'aide dans un disque bleu.
- **Accueil** :
  - le nom « BDTHÈQUE » en très grand, en bleu, avec son ombre rose décalée ;
  - la mention « Zine n° », suivie du nombre d'albums, et « Prix libre » ;
  - la **phrase d'accueil**, tirée au hasard dans la même liste, sur une bande rose légèrement inclinée ;
  - les **compteurs**, en trois disques qui se chevauchent : bleu pour les albums, rose pour les séries, jaune pour les éditions.
- **Tout juste arrivés !** : les couvertures des dernières éditions entrées, imprimées en trame bleue avec une ombre rose décalée, tenues par un morceau de scotch jaune et légèrement inclinées. Sous chacune, le libellé de l'album, l'éditeur et la date. Un lien mène à la liste des albums.
- **Ça vaut combien, tout ça ?** :
  - la valeur totale estimée en très grands chiffres roses, ombrés de bleu, puis son montant en euros du jour ;
  - la valeur totale connue, avec son montant en euros du jour ;
  - les prix moyen, médian, minimum et maximum, séparés par des pointillés roses, en euros courants et en euros du jour ;
  - la mention des montants non convertibles exclus et des éditions gratuites non valorisées.
- **Tomes, intégrales ou hors-séries ?** : trois disques qui se chevauchent, dont la surface suit le nombre d'albums : bleu pour les réguliers, rose pour les intégrales, jaune pour les hors-séries. Les nombres et les pourcentages sont écrits en dessous.
- **Plutôt quel genre ?** : les genres sur des bandes de papier découpées et inclinées, alternativement bleues, roses et jaunes. La taille de chaque bande suit le nombre d'albums, qui est écrit à côté du nom.
- **Chez quels éditeurs ?** : des barres tramées, alternativement bleues et roses, dont la longueur suit le nombre d'éditions possédées.

Chaque genre, éditeur ou album mène à la liste ou à la fiche correspondante.

### La vitrine de librairie

Le dashboard présenté comme une librairie de bandes dessinées : la façade, puis l'intérieur de la boutique. Source : `Librairie.dc.html`.

#### Apparence

- **Couleurs** : une seule palette, celle de la boutique.
  - Les surfaces : vert bouteille `#1E3B2F` (plus sombre, `#16291F` et `#12241B`, pour la barre de navigation et les huisseries), laiton `#B8924A` (plus clair, `#D8B972`, pour les lettres de l'enseigne), noyer `#5B3A24` (plus sombre, `#3F2818`, pour le fond des étagères).
  - Les fonds : crème `#F5EEDF`, plus clair `#FBF7EE` pour la vitrine et les cartons.
  - Le texte est brun `#2A2420`, atténué `#8A7A66`. La terre cuite `#B5523B` sert d'accent. L'ardoise est vert-noir `#263129`, avec une craie `#EFEDE4` et une craie jaune `#E8CF8F` pour les montants en euros du jour.
- **Typographie** :
  - *DM Serif Display*, une serif d'enseigne, pour le nom, la phrase d'accueil et les titres de rubrique, en italique ;
  - *Caveat*, une écriture manuscrite, seulement pour l'ardoise et la date écrite sur les cartons du libraire ;
  - *Karla* pour le texte.
- **Livres** : leurs dos reprennent les couleurs de la boutique, vert, laiton, terre cuite, crème et brun. Leur hauteur et leur couleur sont **tirées au hasard à chaque affichage**, et deux livres voisins n'ont presque jamais la même hauteur.

#### Présentation

- **Barre de navigation**, vert sombre : le menu, avec l'entrée active soulignée de laiton, la recherche et le bouton d'aide, cerclés de laiton.
- **La façade** :
  - l'enseigne « BDthèque » en lettres dorées, entre deux filets, sous la mention « Librairie · Bandes dessinées » ;
  - un store rayé vert et crème, à bord festonné ;
  - la vitrine, où la **phrase d'accueil**, tirée au hasard dans la même liste, est peinte en lettres dorées italiques ;
  - la porte, où pend une pancarte « Ouvert » qui donne les **compteurs** : albums, séries, éditions.
- **Tout juste arrivés !** : les couvertures des dernières éditions entrées, posées sur une tablette en noyer. Sous chacune, un carton de libraire donne la date d'entrée, écrite à la main, le libellé de l'album et l'éditeur. Un lien mène à la liste des albums.
- **Ça vaut combien, tout ça ?**, sur une **ardoise** encadrée de noyer, écrite à la craie :
  - la valeur totale estimée et son montant en euros du jour, puis la valeur totale connue et le sien ;
  - les prix moyen, médian, minimum et maximum, en euros courants et en euros du jour ;
  - en petit, la mention des montants non convertibles exclus et des éditions gratuites non valorisées.
- **Tomes, intégrales ou hors-séries ?** : trois casiers d'une bibliothèque en noyer, remplis de livres, dont la largeur suit le nombre d'albums. Sous chaque casier, une étiquette donne le nombre et le pourcentage.
- **Plutôt quel genre ?** : une plaque de rayon par genre, vert bouteille encadré de laiton, avec le nom du genre et une barre dont la longueur suit le nombre d'albums.
- **Chez quels éditeurs ?** : une étagère par éditeur, garnie de livres, un dos pour environ 25 éditions possédées, avec un porte-étiquette qui donne le nom de l'éditeur et son nombre d'éditions.

Chaque genre, éditeur ou album mène à la liste ou à la fiche correspondante.
