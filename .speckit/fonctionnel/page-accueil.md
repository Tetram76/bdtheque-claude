# Page d'accueil

Dashboard public de la page d'accueil, écran d'accueil d'une collection vide et statistiques tirées au hasard.

---

La page d'accueil est un **dashboard public** présentant les statistiques principales de la collection. Elle n'est pas personnalisée (pas d'authentification sur la partie consultation). Certaines statistiques pourront être présentées sous forme de **graphiques** lorsque c'est pertinent.

Sa présentation est décrite dans [`visuel/dashboard.md`](../visuel/dashboard.md). Étant public, il n'affiche que des statistiques de niveau **public** (cf. `fonctionnel/index.md` § Règles d'accès) : il est considéré comme **purement public** dans un premier temps, sans variante pour une session authentifiée. Afficher aussi des données privées dans une session authentifiée est une ouverture **envisagée**, si une raison particulière le justifiait, mais **rien n'est statué** : elle n'est pas à implémenter tant qu'elle n'est pas décidée (YAGNI).

**Collection vide** : sans édition possédée, la page d'accueil ne présente pas le dashboard, qui n'aurait rien à montrer, mais un **écran d'accueil**. Celui-ci annonce que le dashboard s'affichera dès la première édition entrée, propose de parcourir la base documentaire (albums, séries, auteurs et recherche avancée, cf. `fonctionnel/regles-metier.md` § Périmètre de la consultation) et mène à l'administration pour commencer la collection. Sa présentation est décrite dans [`visuel/dashboard.md`](../visuel/dashboard.md) § Collection vide.

**Composition du dashboard** :

- Il présente **toujours** les quatre blocs suivants, qui ne font pas partie du tirage :
  - **À la une** : une **édition possédée** mise en avant, à la manière d'une citation du jour. Elle est tirée au hasard **une fois par jour** et reste la même pour tous les affichages de la journée.
    - **Jour et fuseaux horaires** : le jour est celui du visiteur, dans le fuseau horaire de son navigateur (cf. `fonctionnel/presentation.md` § Langue et culture d'affichage). L'édition du jour est attachée à la **date** du jour, et non à un instant : tous les fuseaux présentent la même édition pendant leurs 24 heures de cette date. Le premier affichage, dans quelque fuseau que ce soit, qui ne trouve pas d'édition pour sa date la tire pour tous les fuseaux, qui la présentent à leur tour dès qu'ils entrent dans cette date.
    - **Présentation** : sa couverture, le libellé de son album (cf. `fonctionnel/presentation.md` § Libellé d'un album), le sien (cf. `fonctionnel/presentation.md` § Libellé d'une édition) et le résumé de l'album s'il en a un, que la représentation retenue (le thème) peut abréger. L'album mène à sa fiche.
    - **Répétitions** : le tirage **évite les répétitions rapprochées sans les interdire**. Une édition mise à la une n'y revient pas avant **10 jours** : la période se compte en jours de calendrier, et non en tirages, qu'il y ait eu ou non un tirage chaque jour (une journée sans affichage n'en a pas). Seule exception : toutes les éditions possédées sont déjà passées à la une pendant cette période.
    - **Couverture** : le tirage **privilégie les éditions avec couverture**. Parmi les éditions que la règle des 10 jours n'écarte pas, il ne retient que celles qui ont une couverture, et ne retient les éditions sans couverture que si aucune n'en a. Une édition présentée sans couverture reçoit le visuel générique (cf. `fonctionnel/regles-metier.md` § Visuel par défaut).
    - **Équité** : chaque édition garde le **nombre de fois** où elle a été mise à la une (`modele-metier.md` § Édition), et le tirage **privilégie les éditions les moins souvent mises à la une**, sans exclure les autres : une édition déjà présentée garde une chance d'être tirée, moindre. Parmi les éditions que retiennent la règle des 10 jours puis la préférence pour la couverture, chacune a une chance proportionnelle à **1 / (1 + n)²**, où *n* est son nombre de mises à la une : une édition jamais présentée a 4 fois plus de chances d'être tirée qu'une édition présentée une fois, et 9 fois plus qu'une édition présentée deux fois. Une édition entrée récemment dans la collection, jamais mise à la une, est ainsi favorisée.
    - **Changements en cours de journée** : si l'édition du jour est supprimée, une autre est tirée. Toute autre modification la laisse à la une jusqu'au lendemain : la préférence pour la couverture ne joue qu'au tirage, et une édition du jour qui perd ses couvertures est présentée avec le visuel générique.
    - **Collection vide** : sans édition possédée, aucun tirage n'est enregistré pour ce jour, la page d'accueil présentant l'écran d'accueil (ci-dessus).
  - **Compteurs globaux** : nombre total d'albums, de séries et d'éditions.
  - **Dernières entrées** : les dernières éditions entrées dans la collection, de la plus récente à la plus ancienne selon leur date d'acquisition, ou à défaut leur date d'entrée dans la collection (`modele-metier.md` § Édition).
  - **Indicateurs de valeur** (exprimés en €, en euros courants et en euros du jour, cf. `fonctionnel/valorisation.md` § Gestion des devises) : prix moyen, médian, min/max, valeur totale connue et valeur totale estimée. Les prix minimum et maximum sont ceux des éditions dont le prix d'acquisition est **connu** (jamais une valeur estimée).
- Chacun de ses **autres blocs** présente une statistique **tirée au hasard** parmi les statistiques ci-dessous. Le tirage a lieu **à chaque affichage**. Le nombre de ces blocs dépend de la représentation retenue, et sera fixé avec elle. La représentation (le thème) ne fait que **fournir ce paramétrage** : le tirage est fait **en amont**, selon les règles ci-dessous, et le thème présente les statistiques tirées sans choisir lui-même lesquelles afficher. Il choisit en revanche **l'ordre** dans lequel il les présente.
- **Catégories** : ce sont les rubriques de la liste ci-dessous. Les statistiques **utiles** (toutes celles qui ne sont pas ludiques) forment quatre catégories : répartitions clés (dont chaque répartition est une statistique distincte), suivi des séries, dépenses et valeur, composition de la collection. Les statistiques **ludiques** forment une seule catégorie pour le tirage, elle-même divisée en **catégories ludiques** (ses rubriques).
- **Règles du tirage** :
  - un bloc présente une statistique **ludique**, les autres une statistique **utile**, toutes de **catégories différentes** ;
  - si la représentation demande **plus de blocs qu'il n'y a de catégories**, chaque catégorie, ludique comprise, est présente **au moins une fois**, et les blocs en surplus sont tirés parmi toutes les statistiques qui ne sont pas encore présentées, quelles que soient leur catégorie et leur nature (utile ou ludique) ;
  - lorsque plusieurs statistiques ludiques sont présentées, elles appartiennent chacune à une **catégorie ludique différente**, selon la même règle : au moins une de chaque catégorie ludique si elles sont plus nombreuses que ces catégories.
- Exemples, pour cinq catégories :

  | Blocs tirés | Résultat |
  | --- | --- |
  | 3 | 1 ludique, 2 utiles de deux catégories différentes, toutes deux marquées *(graphique)* |
  | 5 | 1 ludique, 1 utile de chacune des quatre catégories |
  | 6 | les 5 ci-dessus, plus 1 statistique quelconque non encore présentée : utile de n'importe quelle catégorie, ou ludique d'une autre catégorie ludique que la première |
- Le dashboard est **graphique** : au moins **75 %** des statistiques **tirées au hasard** **peuvent** être représentées graphiquement, c'est-à-dire sont marquées *(graphique)* dans la liste ci-dessous ; les quatre blocs toujours présents et la statistique ludique **obligatoire** (aucune statistique ludique n'étant marquée *(graphique)*) ont leur propre présentation et n'entrent pas dans ce compte ; les statistiques ludiques tirées en surplus y entrent. L'objectif est de **privilégier le visuel** : le dashboard ne doit pas devenir un pavé de texte, sauf choix du thème. Le tirage écarte toute combinaison qui n'atteint pas ce seuil. Le seuil porte sur le tirage, non sur le rendu : la représentation retenue (le thème) reste libre de formuler en texte une statistique marquée *(graphique)*.
- **Bloc sans donnée** : tout bloc, fixe ou tiré au hasard, peut n'avoir aucune donnée à présenter (ex. aucune édition dédicacée, aucun prix connu). Aucune donnée n'est alors fournie pour lui, et la représentation retenue (le thème) décide de ce que montre le bloc. Le tirage ne tient pas compte des données : une statistique sans donnée peut être tirée.

Les statistiques que le dashboard tire au hasard sont listées ci-dessous, sous réserve de ce niveau d'accès (liste non exhaustive). Celles qui sont marquées *(graphique)* **peuvent** être présentées sous forme de graphique (la forme indiquée n'est qu'une suggestion) : le choix final revient à la représentation retenue (le thème), qui peut aussi bien les formuler en texte, comme le ferait une présentation de type magazine :

- **Répartitions clés** : par type d'album, par genre, par éditeur *(graphique, chacune)*. La répartition par type d'album distingue trois catégories exclusives, réguliers, intégrales et hors-séries, où le hors-série l'emporte sur le type : une intégrale hors-série compte parmi les hors-séries, comme dans l'ordre des albums d'une série (cf. `fonctionnel/regles-metier.md` § Ordre des albums dans une série).
- **Suivi des séries** :
  - les séries **complètes** et **à compléter** : nombre et part des séries de la collection marquées complètes *(graphique)* ;
  - les **tomes manquants** : leur nombre total, et les séries qui en comptent le plus (cf. `fonctionnel/second-plan.md` § Identification des albums manquants dans une série) *(graphique : le classement des séries)* ;
  - la répartition des séries de la collection par **statut** (en cours, terminée, abandonnée, non renseigné) *(graphique)* ;
  - les **prochaines sorties estimées** : quelques séries, celles dont la sortie estimée du prochain tome est la plus proche (cf. `fonctionnel/second-plan.md` § Estimation de sortie d'un nouvel album) ; leur nombre dépend de la représentation retenue, et sera fixé avec elle. La liste complète est réservée à un écran dédié *(graphique : une frise chronologique est envisagée)*.
- **Dépenses et valeur** :
  - les **dépenses par année** : total des prix d'acquisition des éditions achetées (mode `Achat`), par année d'acquisition ; les éditions achetées sans date d'acquisition forment une tranche à part. Sur le dashboard, les deux versions des montants (cf. `fonctionnel/valorisation.md` § Gestion des devises) forment **deux statistiques distinctes**, les dépenses par année en euros courants et les dépenses par année en euros du jour, chacune avec son propre graphique *(graphique)* ;
  - la part de la valeur totale estimée qui est **connue** (prix d'acquisition) et celle qui est **estimée** (valeurs estimées) *(graphique)* ;
  - la répartition des éditions par **mode d'acquisition** *(graphique)* ;
  - la répartition des éditions de la collection entre **neuves** et **d'occasion**, quel que soit leur mode d'acquisition *(graphique, par exemple en camembert)*.
- **Composition de la collection** :
  - les albums par **décennie** de première publication *(graphique)* ;
  - l'**âge moyen d'un album au moment de son achat** : l'écart moyen entre la date de première publication de l'album et la date d'acquisition de l'édition, sur les éditions achetées qui ont les deux. L'écart se calcule **à l'année** (année d'acquisition moins année de première publication), quelle que soit la précision des dates, et la moyenne s'exprime en années ;
  - les **auteurs les plus présents**, par rôle (scénariste, dessinateur, coloriste) *(graphique)* ;
  - la répartition des **auteurs** de la collection **par rôle** : le nombre d'auteurs distincts crédités comme scénariste, comme dessinateur et comme coloriste ; un auteur qui tient plusieurs rôles compte dans chacun *(graphique)* ;
  - la répartition des albums par **univers**, chaque univers comptant les albums de ses sous-univers (cf. `fonctionnel/regles-metier.md` § Hiérarchie des univers) *(graphique, par exemple hiérarchique, qui montre l'imbrication des univers)* ;
  - la répartition des éditions par **catégorie** (originale, spéciale, tirage de tête) *(graphique)* ;
  - la répartition des éditions par **état** *(graphique)* ;
  - le nombre d'albums possédés en **plusieurs éditions**.
- **Statistiques ludiques**, propres au dashboard (aucune autre page ne les présente) :
  - **Records et curiosités** :
    - le **doyen** et le **benjamin** : les albums dont la date de première publication est la plus ancienne et la plus récente ;
    - le **plus cher** et le **moins cher** : les éditions qui portent les prix maximum et minimum des indicateurs de valeur, désignées nommément ;
    - le **plus gros pavé** : l'édition qui compte le plus de pages ;
    - l'**univers le plus vaste** : l'univers qui compte le plus d'albums, sous-univers compris (cf. `fonctionnel/regles-metier.md` § Hiérarchie des univers) ;
    - la **plus longue série** : la série dont la séquence théorique de tomes (cf. `fonctionnel/regles-metier.md` § Séquence théorique de tomes d'une série) est la plus longue, et la **série la plus fournie** : celle dont la collection compte le plus d'albums ;
    - les **auteurs les plus prolifiques** : l'auteur crédité sur le plus d'albums, tous rôles confondus et pour chaque rôle (scénariste, dessinateur, coloriste) ; le thème choisit la ou les versions qu'il présente, à partir des mêmes données ;
    - le **duo** scénariste-dessinateur (deux auteurs distincts) crédité ensemble sur le plus d'albums ;
    - les **hommes-orchestres** : le nombre d'albums dont un même auteur est à la fois scénariste et dessinateur ;
    - les **éditions dédicacées** : leur nombre.
  - **Machine à remonter le temps** :
    - **payé en francs** : le nombre d'éditions achetées (mode `Achat`) dont le prix d'acquisition est en francs ou en anciens francs, et la plus ancienne d'entre elles selon la date de référence de ce prix (cf. `fonctionnel/valorisation.md` § Gestion des devises), avec ce prix en euros du jour ;
    - **il y a N ans ce mois-ci** : les albums parus le mois en cours d'une année passée ; seuls les albums dont la date de première publication comporte le mois y figurent ;
    - le **mois le plus dépensier** : le mois dont les prix d'acquisition des éditions achetées (mode `Achat`) totalisent le montant le plus élevé ; le **record d'achats en une journée** : la date d'acquisition qui compte le plus d'éditions achetées.
  - **La collection en volume** : le **nombre total de pages**, et le **temps de lecture** qu'il représente, à raison d'**une minute par page**. Seules les éditions dont le nombre de pages est renseigné y comptent.
  - **Jeux sur les données** : le **titre le plus long** et le **titre le plus court**, parmi les albums qui ont un titre propre.

  Ces statistiques portent, comme toutes les autres, sur la collection seule (cf. `fonctionnel/regles-metier.md` § Périmètre de la consultation) : albums de la collection, éditions possédées, séries et auteurs de ces albums.
