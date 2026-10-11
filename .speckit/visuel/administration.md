# Administration

Ce fichier décrit la présentation de la page d'accueil de la partie Administration (`fonctionnel.md` § Structure de l'application) : le point d'entrée de la saisie et de la modification des données, du paramétrage et des statistiques privées. Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation).

La page est déclinée selon chacun des thèmes en lice pour la partie Consultation (cf. [`dashboard.md`](dashboard.md) § Alternatives) : comme toute page, elle suit l'identité visuelle du thème actif (cf. [`charte-graphique.md`](charte-graphique.md) § Portée).

---

## Maquettes

Toutes les déclinaisons forment la page « Administration » du canevas des maquettes (cf. [`charte-graphique.md`](charte-graphique.md) § Maquettes). Leurs sources sont dans [`maquettes/administration/`](maquettes/administration/).

| Thème | Fichiers |
| --- | --- |
| Cases et bulles, violine | `EtabliMenu-Violine.dc.html` |
| Cases et bulles, mûre | `EtabliMenu-Mure.dc.html` |
| La gazette | `EtabliMenu-Gazette.dc.html` |
| Le sommaire de magazine | `EtabliMenu-Sommaire.dc.html` |
| Le festival en métro | `EtabliMenu-Festival.dc.html` |
| Le fanzine en risographie | `EtabliMenu-Riso.dc.html` |
| La vitrine de librairie | `EtabliMenu-Librairie.dc.html` |
| L'album de vignettes | `EtabliMenu-Vignettes.dc.html` |

`Main.dc.html` n'est pas une déclinaison : c'est la source commune, que chaque planche affiche avec son thème. Son nom est imposé par le canevas, dont c'est le fichier d'entrée.

## Ton

- Un espace **technique**, qui privilégie l'efficacité et le fonctionnel, sans être lugubre, strict ni impersonnel : l'identité visuelle du thème actif est conservée.
- Contrairement au dashboard, aucune recherche de ludique : chaque thème garde ses couleurs, sa typographie, ses formes et **une seule touche** qui lui est propre (cf. § Déclinaison par thème), sans ses mises en scène (aucune métaphore de page, aucun graphique décoratif).
- **Aucune inclinaison**, à l'exception du logo lorsque le thème l'incline.

## Contenu

La page met en avant les fonctions les plus utiles, de haut en bas :

- **Titre** « Administration », avec une phrase qui résume le rôle de l'espace.
- **Saisir à partir d'un ISBN** : un champ ISBN et un bouton « Chercher dans les sources », qui crée ou complète à partir des sources externes (`fonctionnel.md` § Structure de l'application) l'album, l'édition et ses visuels, la série, les auteurs, l'éditeur et la collection, relus avant enregistrement. Ce bloc est mis en relief. En dessous, « Ou créer directement : » propose la création d'un album, d'une série, d'un auteur ou d'un éditeur.
- **Les fiches** : une carte par type de fiche (albums, éditions, séries, intentions d'achat, auteurs, éditeurs, genres, univers), avec son icône, son nombre, une précision, le lien « Gérer la liste » et, sauf pour les éditions et les intentions d'achat, qui se saisissent depuis leur album, la création d'une nouvelle fiche.
- **À vérifier** : des listes de fiches à corriger ou à compléter, chacune avec son nombre. Seule une incohérence à corriger (ex. ISBN au chiffre de contrôle incohérent) a la couleur d'alerte du thème.
- **Dernières modifications** : les dernières fiches créées ou modifiées, avec leur type, la nature et la date de la modification, et un lien « Modifier ».
- **Paramétrage** : chaque réglage avec sa valeur et un lien « Modifier ».
- **Statistiques privées** : le bloc qui les réunit ; tant qu'aucune n'est définie, il l'indique.

## En-tête et menu

- **En-tête**, de gauche à droite : le logo, une étiquette « Administration », la recherche d'une fiche à modifier, le bouton d'aide, le lien « Aller à la consultation », marqué d'une flèche de sortie, puis le bouton « Menu », à l'extrémité droite.
- Le **menu** n'est pas permanent :
  - il est **fermé par défaut** et s'ouvre par le bouton « Menu » ;
  - il s'ouvre en **panneau flottant** sous le bouton, aligné à droite, **par-dessus** le contenu : il ne décale jamais le reste de la page ;
  - il se referme par sa croix, par le bouton « Menu », ou par **un clic en dehors** du panneau ;
  - il présente le plan de l'administration, par groupes (Accueil, Collection, Auteurs et éditeurs, Référentiels, Réglages), chaque entrée avec son nombre de fiches le cas échéant, l'entrée courante dans la couleur d'accent du thème ;
  - l'entrée « **Nouvelle édition** » donne un accès direct à l'action principale de l'administration : une édition se saisissant depuis son album, elle commence par le choix de cet album (ou par un ISBN, comme le bloc de saisie de la page) ;
  - « **Se déconnecter** » en est la dernière entrée, séparée des autres par un filet.
- Le panneau a toujours un fond opaque, une bordure et une ombre, y compris pour un thème dont les blocs n'en ont pas (gazette, festival).

## Déclinaison par thème

Couleurs et typographie de chaque thème : cf. [`dashboard.md`](dashboard.md) § Alternatives. Sa touche propre dans l'administration :

| Thème | Touche propre |
| --- | --- |
| Cases et bulles (violine, mûre) | Titres de bloc en bulle crème, sans queue ni inclinaison ; coins arrondis irréguliers des blocs. |
| La gazette | Double filet sous l'en-tête ; blocs ouverts par un filet d'encre, sans cadre ; petite mention en capitales au-dessus de chaque titre de bloc. |
| Le sommaire de magazine | Gros filets noirs, titres en *Anton* soulignés d'un filet épais ; ombre noire décalée sur le bloc de saisie, logo sur fond rouge. |
| Le festival en métro | Station (rond blanc cerclé de bleu nuit) devant chaque titre de bloc, précédé de « Station … » ; formes rondes, pastille « B » du logo. |
| Le fanzine en risographie | Cadres et séparations en pointillés ; ombre rose décalée des titres et du bloc de saisie. |
| La vitrine de librairie | En-tête vert bouteille filet de laiton ; titres de bloc en serif d'enseigne italique. |
| L'album de vignettes | En-tête bleu canard ; pastille « Page n · … » à cheval sur le bord de chaque bloc ; icônes en vignettes rondes à liseré blanc. |
