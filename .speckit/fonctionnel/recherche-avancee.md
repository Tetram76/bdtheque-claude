# Recherche avancée

Recherche avancée de la consultation et recherches enregistrées.

---

La recherche avancée de la consultation recherche n'importe quelle entité sur **n'importe quelle combinaison de critères**, chacun portant sur **n'importe quelle donnée du modèle métier** : elle doit couvrir toutes les combinaisons de recherche (ex. « éditions dont l'album est paru tel mois ou telle année, dont le résumé ou les notes personnelles de l'album contiennent le mot ZZZ, et dont l'album a pour genre AAA, BBB ou CCC »).

L'interface de construction de la recherche est **ergonomiquement utilisable par un utilisateur sans aucune connaissance de SQL** ni d'un autre langage de requête : entités, données, liens, comparaisons et combinaisons y sont présentés dans les termes du métier, jamais dans ceux de la base.

## Entités recherchées

La recherche n'est pas limitée à certaines entités : elle porte sur toute entité dont la consultation présente les données (cf. `fonctionnel/regles-metier.md` § Périmètre de la consultation), c'est-à-dire les albums, éditions, séries, auteurs, éditeurs, collections éditeur, genres, univers, visuels d'édition, intentions d'achat et contributions. Le journal des modifications, consultable dans la seule administration (`modele-metier.md` § Entrée du journal des modifications), le paramétrage de l'application et les recherches enregistrées n'en font pas partie.

Les résultats sont présentés comme les entités de leur type le sont dans la consultation, et chacun se comporte comme la consultation le prévoit pour son entité (cf. `fonctionnel/index.md` § Structure de l'application) : il mène à la fiche d'un album, d'une série ou d'un auteur ; à la fiche de l'album d'une édition, d'un visuel ou d'une intention d'achat ; à la fiche de l'album ou de la série qui porte une contribution ; au site web d'un éditeur, s'il est renseigné ; à la liste d'un genre ou d'un univers. Un éditeur sans site web et une collection éditeur ne sont pas des liens.

## Données interrogées

- Toute donnée du modèle métier peut servir de critère, qu'elle soit **publique** (ex. titre, année de parution, genre), **de gestion** (ex. intention d'achat, date de création, date de dernière modification) ou **déduite** (ex. nombre d'éditions, clé de tri, initiale, appartenance à la collection, genres et univers affichés d'un album).
- Les données techniques d'optimisation, s'il en existe, en sont exclues.
- La recherche ne propose pas, dans un premier temps, de **regroupement** défini par l'utilisateur (somme, moyenne, minimum, maximum) ; les données déduites que l'application calcule elle-même (ex. nombre d'éditions) restent interrogeables, de même que le **nombre d'entités liées** qui remplissent des critères (cf. § Liens entre entités).
- Les règles de rattachement s'appliquent aux critères comme au reste de la consultation : un critère sur un univers couvre ses sous-univers (cf. `fonctionnel/regles-metier.md` § Hiérarchie des univers).

## Liens entre entités

- Un critère peut porter sur une donnée d'une **entité liée**, sur un ou plusieurs niveaux, en suivant les relations du modèle métier (`modele-metier.md` § Relations et cardinalités) : deux entités sans lien direct ne sont reliées que par les entités intermédiaires du modèle.
- Lorsque plusieurs chemins relient deux entités, l'utilisateur choisit celui que suit le critère, et l'interface **met en évidence** ce chemin, au moins lors du choix de la donnée interrogée. Ex. un visuel est relié à une collection éditeur par la collection de son édition, ou par l'une des collections de l'éditeur de son édition.
- Un critère qui suit un lien vers **plusieurs** entités (ex. genres d'un album, éditions d'un album) précise s'il doit être satisfait par **au moins une**, par **aucune** ou par **toutes** les entités liées, ou par un **nombre** d'entités liées, comparé comme tout nombre (ex. « éditions dont l'album a **plus d'une** contribution de dessinateur », soit les éditions qui ont plusieurs dessinateurs) : sans ce choix, certaines combinaisons ne seraient pas exprimables. Sans entité liée (ex. un album sans édition), « au moins une » et « toutes » ne sont pas satisfaits, « aucune » l'est : « toutes » exige au moins une entité liée. Sur un chemin de plusieurs niveaux, **chaque** lien vers plusieurs entités porte son propre choix (ex. « séries dont **tous** les albums ont **au moins une** édition chez Dargaud »).
- Ce choix peut porter sur un **groupe de critères** appliqué à chaque entité liée, combinés comme tout autre groupe (cf. § Combinaison des critères), pour que plusieurs conditions portent sur **la même** entité liée : « albums ayant au moins une édition (chez Dargaud ET parue en 2000) » exige qu'une même édition remplisse les deux conditions, là où « albums ayant au moins une édition chez Dargaud ET au moins une édition parue en 2000 » les admet remplies par deux éditions différentes.

## Comparaisons

Chaque critère compare une donnée à une valeur, ou à une autre donnée du même type (cf. ci-dessous), avec les méthodes de comparaison adaptées au type de la donnée :

| Type de donnée | Comparaisons |
| --- | --- |
| Texte | contient, ne contient pas, est égal à, est différent de, commence par |
| Nombre, date, montant | égal, différent, inférieur, inférieur ou égal, supérieur, supérieur ou égal, compris entre |
| Énuméré, booléen, fiche liée | est, n'est pas, est l'un de, n'est aucun de |
| Toute donnée optionnelle | est renseignée, n'est pas renseignée |

- Une comparaison n'est satisfaite que si les **données connues l'établissent**. Une donnée absente ne satisfait donc aucune comparaison, positive ou négative, hormis « n'est pas renseignée » : « état n'est pas Excellent » ne retient pas une édition dont l'état est absent. Le **NON** d'un critère (cf. § Combinaison des critères) retient au contraire tout ce que le critère n'établit pas : « NON (état est Excellent) » retient cette édition. Une comparaison négative et le NON de la comparaison positive ne sont donc pas équivalents : l'une exige que la donnée soit établie différente, l'autre seulement qu'elle ne soit pas établie égale.
- Un **texte** se compare par défaut sans tenir compte de la casse ni des accents : « contient epee » retient `L'Épée`. **Chaque critère** sur un texte peut, à l'image de la version de comparaison d'un montant, **respecter la casse et les accents**, pour toutes les comparaisons de texte : « contient Épée » ne retient alors ni `L'épée` ni `L'Epee`.
- Une **date** se compare à la précision choisie par l'utilisateur : jour, mois ou année (ex. « publiée tel mois », « publiée telle année »). Selon la règle ci-dessus, une date partielle ne satisfait un critère que si sa précision suffit à l'établir : une date connue à l'année seule ne satisfait ni « publiée en mars 2020 » ni « publiée en dehors de mars 2020 », mais « NON (publiée en mars 2020) » la retient.
- Un **montant** se compare dans la version que **chaque critère** choisit, si bien qu'une même recherche peut comparer un montant en euros du jour et un autre en euros courants ou dans sa devise d'origine (cf. `fonctionnel/valorisation.md` § Gestion des devises) : **euros du jour** par défaut, **euros courants**, ou **devise d'origine**. En euros du jour comme en euros courants, un montant non convertible ne satisfait pas le critère. Dans la devise d'origine, la valeur du critère porte sa devise, et seuls les montants dans cette devise peuvent le satisfaire : « prix supérieur à 100 USD » ne retient aucun prix en francs, des montants de devises différentes ne se comparant qu'une fois convertis.
- Une donnée peut aussi être comparée à une **autre donnée du même type**, de la même fiche ou d'une fiche liée (ex. « édition dont l'année d'édition est postérieure à sa date d'acquisition, ou antérieure à la date de première publication de l'album »), par les comparaisons de son type qui portent sur une seule valeur. Comme pour une valeur, la comparaison n'est satisfaite que si les deux données sont renseignées et que les données connues l'établissent ; deux dates se comparent à la précision choisie par l'utilisateur, chacune couvrant toute la période où elle est connue (une date connue à l'année couvre, au mois, ses douze mois).

## Combinaison des critères

Les critères se combinent librement par **ET**, **OU** et **NON**, en groupes imbriqués à tout niveau, pour exprimer toute combinaison (ex. « (paru tel mois OU telle année) ET (résumé OU notes personnelles contiennent ZZZ) ET genre est l'un de AAA, BBB, CCC »).

## Recherches enregistrées

- **Enregistrement** : l'administrateur peut enregistrer une recherche sous un **nom**, pour la rejouer ultérieurement.
- **Rejeu en consultation** : les recherches enregistrées sont proposées dans la consultation, dans un menu ou une liste ; les rejouer ne demande que les valeurs de leurs variables.
- **Variables** : à l'enregistrement, une ou plusieurs constantes des critères peuvent être remplacées par des **variables**. La valeur d'une variable est demandée lorsqu'on rejoue la recherche en consultation ; sa **valeur par défaut**, fixée par l'administrateur, est d'abord la constante qu'elle remplace.
- **Aucune intention d'achat désignée** : une intention d'achat n'est que l'intention d'acquérir une autre entité (un album ou une édition) ; une recherche enregistrée ne peut donc désigner aucune intention d'achat précise, ni par un critère ni par la valeur par défaut d'une variable. Elle peut en revanche rechercher les intentions d'achat, ou suivre le lien qui y mène (ex. « albums visés par une intention d'achat »). Une recherche ponctuelle, non enregistrée, peut désigner une intention d'achat.
- **Point de départ d'une nouvelle recherche** : en consultation, les critères d'une recherche enregistrée peuvent pré-charger le formulaire de construction de la recherche, comme modèle d'une nouvelle recherche. Aucun lien n'est conservé entre la recherche d'origine et la recherche complétée.
- **Gestion** : une page de l'administration gère les recherches enregistrées : suppression, renommage, modification des critères et des valeurs par défaut des variables.
