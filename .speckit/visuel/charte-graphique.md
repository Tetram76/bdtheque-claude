# Charte graphique

Ce fichier décrit l'identité visuelle de l'application, c'est-à-dire ce que partagent tous ses thèmes : thème de base et ton général. Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation). L'exigence de rendu est posée par `fonctionnel.md` § Design et charte graphique ; la présentation de chaque écran est décrite dans son propre fichier de ce dossier, avec ses maquettes.

---

## Portée

**Chaque page** de l'application, quelle que soit sa partie (Consultation, Administration, aide contextuelle, `fonctionnel.md` § Structure de l'application), **suit l'identité visuelle du thème actif** : ses couleurs, sa typographie et ses éléments graphiques.

La charte ci-dessous est issue des maquettes du dashboard (cf. [`dashboard.md`](dashboard.md)), premier écran conçu, dont elle réunit ce que partagent toutes les alternatives : elle s'applique à la partie **Consultation**.

La partie **Administration** suit cette identité dans un sens plus sobre : un espace technique, qui privilégie l'efficacité et le fonctionnel sans être impersonnel, sans fantaisie ni désordre (cf. [`administration.md`](administration.md) § Ton).

## Thème de base

Le rendu repose sur un **thème de base neutre**, qui définit les **contrats** que tout thème respecte. Aucune des alternatives des maquettes (cf. [`dashboard.md`](dashboard.md)), pas même « Cases et bulles » (violine ou mûre), n'est retenue à ce stade, et aucune ne sert de base au rendu : un thème issu des maquettes ne fait que s'appuyer sur ce thème de base.

## Ton général

- **Joyeux et chaleureux**, ni sérieux ni sévère : l'application présente une collection de BD, avec un clin d'œil à l'univers de la BD.
- **Ludique.**
- **Non conventionnel, borderline mais pas excentrique.**
- **Sans pastiche de comics.**

## Convivialité et efficacité

L'application n'est pas un jeu : elle reste une **application bureautique**. Quel que soit le thème, son ton (§ Ton général) s'exprime dans le respect de contraintes de **convivialité** et d'**efficacité** :

- **Densité** : une page donne d'un coup d'œil le plus d'information utile possible. Tailles de texte, d'étiquettes, de boutons et espacements restent ceux d'une application, non ceux d'une affiche ou d'une vitrine.
- **Listes et grilles** : leurs éléments sont compacts. Dans une liste, une couverture est une vignette, jamais un visuel en pleine largeur ; sur smartphone, une grille de couvertures en aligne au moins trois par ligne, et aucune liste ne présente un seul élément par écran.
- **Smartphone** : marges et espacements se resserrent sur petit écran, pour que le contenu, et non l'habillage, occupe l'écran.

## Apparence propre à chaque alternative

Couleurs, typographie et éléments graphiques ne sont pas communs : chaque alternative des maquettes a les siens, décrits avec elle dans [`dashboard.md`](dashboard.md) § Alternatives. Seules les règles suivantes valent pour toutes :

- le **champ de recherche** et le **bouton d'aide** sont toujours visibles dans l'en-tête ;
- le **menu horizontal** reste figé en haut de l'écran quand la page défile, avec l'en-tête qui le porte lorsque celui-ci est compact ; il ne passe jamais à la ligne : il défile horizontalement, et un dégradé marqué d'un chevron signale, de chaque côté, les entrées hors de vue. À l'ouverture de la page, il est centré sur l'entrée active. Sur petit écran, la recherche passe sous le menu, sans défiler.

## Maquettes

Les maquettes sont des **pages HTML autonomes**, conservées dans [`maquettes/`](maquettes/) : elles s'ouvrent dans un navigateur, depuis le dépôt, sans autre ressource externe que leurs polices (Google Fonts). Leurs données sont fictives.

- [`maquettes/index.html`](maquettes/index.html) les liste, en deux sections : **Consultation** et **Administration**.
- Chaque écran a **son dossier**, dont la page `index.html` est une **visionneuse** : elle présente ses versions **côte à côte**, une par thème en lice, à la largeur d'un écran de bureau (1440 px) ou d'un smartphone (414 px, largeur en pixels CSS d'un Pixel 10 XL), avec leurs réglages (exemple présenté, menu ouvert…). Chaque version peut aussi être agrandie, seule en pleine largeur : elle défile alors dans la hauteur de la fenêtre, ce qui montre ce qui reste figé au défilement.
- Chaque maquette est ainsi **déclinée dans le visuel de chaque thème** en lice. Le fichier de chaque écran indique ses sources.
- Le moteur d'affichage des maquettes et la visionneuse sont communs à tous les écrans ([`maquettes/shared/`](maquettes/shared/)).

### Gestion des maquettes

- **Source de vérité : le dépôt.** Les maquettes sont conçues et modifiées dans [`maquettes/`](maquettes/), et nulle part ailleurs.
- **Publication systématique sur claude.ai**, pour en faciliter la visualisation et permettre à l'utilisateur de **contrôler chaque modification** : le dossier `maquettes/` est publié tel quel comme artifact claude.ai, <https://claude.ai/artifact/ADvU1T6KZEjKQdPFCRfSAD>, toujours à cette même adresse. Chaque modification des maquettes y est republiée **dès qu'elle est faite**, avant tout commit, Pull Request ou fusion, et jamais seulement une fois fusionnée. Cette publication n'est qu'une copie de consultation : elle n'est jamais modifiée directement, et ne fait jamais foi face au dépôt.
