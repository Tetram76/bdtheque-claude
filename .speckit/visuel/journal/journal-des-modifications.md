---
type: Référence visuelle
title: 'Journal des modifications'
description: 'Écran de consultation de tout le journal des modifications, dans l''administration : accès, recherche et filtres, entrées groupées par jour, changements d''une modification, menu d''une entrée, pagination, liste vide, smartphone.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-25T13:30:00Z }
---

# Journal des modifications

Ce dossier décrit l'écran de l'administration qui présente **tout le journal des modifications** ([Entrée du journal des modifications](../../modele-metier/entree-du-journal-des-modifications.md)), de la plus récente à la plus ancienne entrée. Le journal ne se lit que là, et en lecture seule : rien ne s'y crée, ne s'y modifie ni ne s'y supprime. L'écran suit le modèle des listes de l'administration ([`liste-albums/`](../liste-albums/index.md), [`listes/`](../listes/index.md)), dans le ton sobre de l'administration ([Ton](../administration/ton.md)) ; ce concept ne décrit que ce qui lui est propre (décisions de l'utilisateur, #657).

## Accès

- Le lien **« Tout le journal »** du bloc « Dernières modifications » de l'accueil de l'administration ([Contenu](../administration/contenu.md)).
- L'entrée **« Journal des modifications »** du menu de l'administration, dans le groupe « Réglages », après « Statistiques privées » ([En-tête et menu](../administration/en-tete-et-menu.md)).

## Contenu

De haut en bas :

- **En-tête et menu** de l'administration, l'entrée « Journal des modifications » étant l'entrée courante du menu.
- **Fil d'Ariane** : Administration, puis Journal des modifications.
- **Titre** « Journal des modifications », suivi du nombre total d'entrées ; aucun bouton de création.
- **Le journal**, dans un bloc unique, de mention « Journal », celle de son bloc sur l'accueil :
  - un **champ de recherche** sur le libellé des fiches, sans tenir compte des majuscules ni des accents, qui trouve aussi les fiches supprimées depuis, et dont la croix d'effacement ramène le journal sans recherche ([Contrôles](../liste-albums/controles.md)) ; une édition et une intention d'acquisition sont trouvées aussi par le libellé de leur album ;
  - le bouton **Filtres**, qui porte le nombre de filtres actifs, et le **panneau des filtres**, replié par défaut : type de fiche et opération, choisis dans une liste, et période, « Du » et « Au », deux dates, bornes comprises, un jour s'entendant dans le fuseau horaire du navigateur ([Langue et culture d'affichage](../../fonctionnel/presentation/langue-et-culture-d-affichage.md)) ;
  - les **filtres actifs**, en étiquettes « Nom : valeur » retirables une à une, et « Tout effacer », comme dans toute liste ([Contenu](../liste-albums/contenu.md)) ; le filtre sur une fiche donnée, posé par « Historique de cette fiche » (§ Menu d'une entrée), y figure de même (« Fiche : Album L'Iris blanc (Astérix - T. 40) ») ;
  - ni navigation par initiale, le journal étant chronologique ;
  - le **décompte** (« 12 408 entrées », « 27 entrées sur 12 408 ») et l'ordre, rappelé en clair : « De la plus récente à la plus ancienne », ordre unique ;
  - les **entrées**, groupées par jour (§ Entrées) ;
  - la **pagination** : 50 entrées par page, la position dans le journal (« Entrées 1 à 50 sur 12 408 »), les pages, précédente et suivante. Un jour coupé par une page reprend son titre en tête de la page suivante.

## Entrées

- **Par jour** : les entrées d'un même jour, dans le fuseau horaire du navigateur, sont réunies sous un titre qui le nomme : « Aujourd'hui, mercredi 25 novembre 2026 », « Hier, mardi 24 novembre 2026 », sinon la date entière, « Jeudi 8 octobre 2026 ».
- **Une ligne par entrée**, dense comme une table sur grand écran :
  - l'**heure** ;
  - le **libellé de la fiche**, tel que l'entrée l'a relevé, selon ses règles ([Libellé d'un album](../../fonctionnel/presentation/libelle-d-un-album.md), libellé complet ; [Libellé d'une édition](../../fonctionnel/presentation/libelle-d-une-edition.md), son ISBN d'un seul bloc), avec son **type de fiche** en petit dessous, pour une édition suivi de son album (« Édition de L'Iris blanc (Astérix - T. 40) ») ;
  - l'**opération**, en étiquette, distinguée au premier coup d'œil : « Création » pleine, « Modification » en contour, « Suppression » en contour de la couleur de suppression du thème, « Reprise » en tirets ;
  - le bouton « ⋯ » de son menu (§ Menu d'une entrée).
- **Changements d'une modification**, tous présentés, sous sa ligne, chacun sur une ligne : l'attribut, puis sa valeur avant, une flèche et sa valeur après (« Bien → Très bien ») ; un ajout, « + » et sa valeur (« + Humour ») ; un retrait, sa valeur barrée, suivie de « retiré » ; une valeur absente, le tiret d'une donnée absente ([Convivialité et efficacité](../charte-graphique/convivialite-et-efficacite.md)) ; un secret, « renouvelée, valeur non consignée », sans valeur avant ni après. Une valeur qui désigne une autre fiche la présente par son libellé.
- **Aucun texte figé** : type de fiche, opération, attribut, valeurs et libellés sont présentés dans la culture d'affichage en vigueur, toujours de la même façon pour une même entrée ([Entrée du journal des modifications](../../modele-metier/entree-du-journal-des-modifications.md)).
- **Fiche supprimée depuis** : rien ne la signale sous son libellé ; seul son menu le dit, qui ne mène plus à sa saisie ni à la consultation (décision de l'utilisateur, #657).

## Menu d'une entrée

Une entrée n'a pas d'action principale en bouton : toutes ses actions sont dans son menu « ⋯ », qui nomme ce qu'elles visent, la fiche, et non l'entrée du journal, qui ne se modifie jamais (décision de l'utilisateur, #657) :

- **Modifier la fiche**, pour une fiche qui existe encore : sa saisie, sous son parent pour une édition, une intention d'acquisition ou une collection, comme les dernières modifications de l'accueil ([Contenu](../administration/contenu.md)) ;
- **Historique de cette fiche** : le journal filtré sur cette seule fiche, absent du menu quand le journal l'est déjà ;
- **Voir dans la consultation**, pour un album, une série, un auteur ou un univers qui existe encore ([Structure de l'application](../../fonctionnel/structure-de-l-application.md)).

Un clic en dehors d'un menu ouvert le referme.

## Journal vide

Selon ce qui le restreint, comme toute liste ([Contenu](../liste-albums/contenu.md), liste vide) :

- une **recherche** sans résultat le dit, rappelle sur quoi porte la recherche (« La recherche porte sur le libellé des fiches, sans tenir compte des majuscules ni des accents. ») et propose « Effacer la recherche » ;
- sans texte cherché, sous des **filtres** : « Aucune entrée ne correspond à ces critères. », et « Effacer les filtres » ;
- **sans critère** : « Aucune modification n'est encore consignée. », sans autre action.

## Smartphone

Chaque entrée tient sur deux lignes, puis ses changements : le libellé, son type dessous, et le bouton « ⋯ » ; puis l'heure et l'opération ; puis les changements, sur toute la largeur, chaque attribut au-dessus de ses valeurs. Le panneau des filtres passe sur deux colonnes.

## Déclinaison par thème

Celle des listes de l'administration ([Déclinaison par thème](../listes/declinaison-par-theme.md)), la mention du bloc étant « Journal ».
