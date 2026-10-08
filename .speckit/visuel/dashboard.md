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
- **Tout juste arrivés !** : les dernières éditions entrées dans la collection, par leur couverture, avec le libellé de l'album, l'éditeur et la date d'entrée ; un lien mène à la liste des albums.
- **Ça vaut combien, tout ça ?** : valeur totale estimée et valeur totale connue, puis prix moyen, médian, minimum et maximum, chaque montant en euros courants et en euros du jour (`fonctionnel.md` § Gestion des devises) ; une mention indique les montants non convertibles exclus et les éditions gratuites non valorisées.
- **Tomes, intégrales ou hors-séries ?** : la répartition par type d'album est représentée par des **piles de livres** posées sur une étagère, une par type (un livre ≈ 100 albums), avec le nombre d'albums au-dessus et le pourcentage en dessous. Les piles ont l'air **empilées à la main** : en tendance, les livres s'élargissent vers la base pour que la pile tienne, mais leur largeur, leur décalage et leur inclinaison varient, et cette variation est **tirée au hasard à chaque affichage**, dans des limites qui gardent la pile plausible.
- **Plutôt quel genre ?** : les genres en étiquettes colorées, dont la taille suit le nombre d'albums ; un album peut relever de plusieurs genres.
- **Chez quels éditeurs ?** : les éditeurs en barres horizontales, sur les éditions possédées.

Chaque genre, éditeur ou couverture mène à la liste ou à la fiche correspondante.
