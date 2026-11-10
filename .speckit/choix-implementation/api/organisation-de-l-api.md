---
type: Choix d'implémentation
title: 'Organisation de l''API'
description: 'Minimal API, familles /admin, /catalog et /audience, DTO, modification, fiches de consultation, dashboard, listes de l''administration, pagination, contrats ; à lire pour tout endpoint.'
applies_to:
  - 'src/Bdtheque.Api/Endpoints/**'
  - 'src/Bdtheque.Contracts/**'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-09T22:34:59Z }
---

# Organisation de l'API

## Style

- **Minimal API**, approche que recommande Microsoft pour les nouveaux projets (documentation ASP.NET Core, *APIs overview*) : routes déclarées directement, moins de code d'infrastructure, moindre surcoût.
  - **Alternative écartée (contrôleurs MVC)** : ils n'apportent que des extensions sans usage ici (liaison personnalisée, validation déclarative, écartée par [Erreurs métier, fonctionnelles et techniques](../ecritures/erreurs-metier-fonctionnelles-et-techniques.md), OData).
- **Handlers sur le contexte EF Core directement**, qui joue déjà le rôle de dépôt et d'unité de travail ; un groupe de routes par ressource, mapping écrit à la main.
  - **Alternative écartée (MediatR, AutoMapper)** : passés sous double licence RPL 1.5 / commerciale en 2025, contraire aux [Licences](../../contraintes-techniques/licences.md) ; une indirection de plus pour des clients internes qui partagent les contrats.
  - **Alternative écartée (couche repository)** : une abstraction sans second fournisseur de persistance à masquer.

## Familles de routes

- **`/admin` pour la saisie, `/catalog` pour la consultation**, chacune avec ses propres DTO : une vue de lecture d'un côté, les champs saisissables de l'autre. `api` ne connaît pas l'utilisateur ([Authentification : mise en œuvre](../socle/transverse/authentification-mise-en-oeuvre.md)) : chaque client réserve `/admin` à l'administrateur authentifié. `/audience` reçoit la mesure d'audience, écrite pour tout utilisateur ([Mesure d'audience : mise en œuvre](../mesure-audience/mesure-d-audience-mise-en-oeuvre.md)).
- **Appartenance à la collection calculée par une expression unique**, réutilisée par toute requête, jamais recodée.

## Écritures

- **Modification par `PUT` de l'état complet du formulaire** ; les champs interdépendants sont appliqués par des **méthodes atomiques du domaine**, plutôt que par des setters dont l'API devrait connaître l'ordre valide ([Valeur d'une édition : mise en œuvre](../domaine/valeur-d-une-edition-mise-en-oeuvre.md)).
  - **Alternative écartée (`PATCH`)** : les formulaires envoient toujours la fiche entière ; un correctif partiel multiplierait les combinaisons à valider, sans cas d'usage.
- **Un même contenu pour la création et la modification** d'une fiche, associations et contributions comprises, remplacées en bloc, seule la différence étant écrite. Une référence à une fiche supprimée entre-temps est une erreur fonctionnelle.
- **Fiches ajoutées sur place créées avec la fiche saisie** : le contenu décrit les auteurs, genres et univers que le formulaire a ajoutés, chacun sous une clé qu'il cite à la place d'un identifiant ; ils sont créés par le domaine dans la transaction de l'enregistrement, avec lui ou pas du tout.
- **La lecture d'une fiche pour sa saisie nomme les fiches qu'elle cite** (série, genres, univers avec leurs ancêtres, auteurs) et porte ses dates : le formulaire les présente sans autre requête. Un univers ajouté se lit avec ses ancêtres (`/admin/universes/{id}/hierarchy`), la clé de tri d'un titre se demande au domaine (`/admin/sort-keys/title`).
- **Reprise des auteurs de la série calculée par le domaine, à la demande du formulaire** ([Initialisation des contributions depuis la série](../../fonctionnel/regles-metier/initialisation-des-contributions-depuis-la-serie.md)) : `POST /admin/series/{id}/album-contributions` reçoit les contributions de l'album, s'il est en création, et la série choisie précédemment, et dit si la série les remplace, avec, nommées, celles qu'elle lui donne. L'enregistrement de l'album ne recopie rien.
  - **Alternative écartée (reprise à l'enregistrement)** : l'utilisateur ne verrait les auteurs recopiés qu'après avoir enregistré, et des auteurs retirés ensuite reviendraient au prochain rattachement.
- **Éditions et intentions d'achat adressées par leur album**, sous sa version : ce sont des enfants de son agrégat ([Intention d'achat : agrégat porté par l'album](../domaine/intention-d-achat-agregat-porte-par-l-album.md)).
  - **Une création d'édition est toujours une édition possédée** ; seule une intention crée une édition non possédée ([Appartenance à la collection](../../fonctionnel/regles-metier/appartenance-a-la-collection.md)).
  - **La confirmation d'achat reçoit le formulaire entier de l'édition** : les caractéristiques de l'exemplaire, absentes de la saisie d'une intention, se saisissent à l'achat ([Réalisation d'une intention](../../fonctionnel/regles-metier/intention-d-achat.md#réalisation-dune-intention)).
  - **La conversion d'une intention vers l'album passe par le mécanisme de suppression**, avec l'empreinte de l'impact de suppression de l'intention : elle supprime l'édition visée et ses visuels, effet identique ([Suppression des entités : mise en œuvre](../ecritures/suppression-des-entites-mise-en-oeuvre.md)).
- **Toute modification du parent d'un univers charge le référentiel des univers en entier**, table de petite taille : la chaîne d'ancêtres est complète en mémoire, sans requête récursive.

## Consultation

- **Fiches réservées aux albums, aux séries et aux auteurs** ([Structure de l'application](../../fonctionnel/structure-de-l-application.md)). Une fiche porte tout ce qui est connu de l'entité, les entités sans fiche propre qu'elle présente, et les entrées qui mènent aux fiches liées : c'est la navigation inter-entités.
  - Elle porte en entier les fiches liées en **nombre borné** ; celles en **nombre non borné** se lisent par les listes filtrées, paginées ([Recherche](recherche.md)).
  - **Alternative écartée (fiche portant toutes ses fiches liées)** : des centaines de fiches liées non paginées (ex. les éditions d'un éditeur), que les listes filtrées paginent déjà.
  - Les lectures assemblées l'une dans l'autre (bibliographie, éditions et univers d'une fiche, œuvres d'un genre ou d'un univers) partagent un **même instantané**, pour qu'une écriture concurrente ne laisse jamais une entrée sans sa série, son album ou son univers.
- **Bibliographie d'un auteur et œuvres d'un genre ou d'un univers : une liste unique**, séries et albums sans série rangés ensemble par clé de tri, calculée par la base, construites par le même mécanisme et paginées pour les œuvres.
  - **Alternative écartée (listes séparées des albums et des séries)** : ni les séries avec leurs albums, ni les deux ensemble par clé de tri ; le frontend devrait tout reconstituer en lisant toutes les pages des deux listes.
- **Cartes d'album et aperçu des séries** (pile des couvertures des trois premiers tomes réguliers, nombre d'albums) calculés par l'API seule, en une requête sur les albums des séries présentées : une série compte au plus quelques dizaines d'albums.
- **Couverture d'un album par une expression unique** ([Couverture d'un album](../../fonctionnel/regles-metier/couverture-d-un-album.md)), deux éditions de même année départagées par leur identifiant, ordonné dans le temps : la première créée. Toute couverture est présentée avec ses couleurs ([Visuels : stockage et traitement](../domaine/visuels-stockage-et-traitement.md)).
- **Ordre des albums dans une série** ([Ordre des albums dans une série](../../fonctionnel/regles-metier/ordre-des-albums-dans-une-serie.md)) complété par la clé de tri et l'identifiant pour un ordre total ; un tome et une date ne se comparent pas, si bien qu'un album sans tome ne s'intercale pas entre deux tomes.
- **Dashboard sans contenu pour une collection vide**, constaté avant tout calcul ([Statistiques du dashboard](../../contraintes-techniques/architecture-deploiement/statistiques-du-dashboard.md)) ; il reçoit la date du navigateur ([Internationalisation](../socle/transverse/internationalisation.md)) et le nombre de dernières entrées que présente le thème. Les blocs encore non calculables (Phase 4) sont signalés comme tels, sans donnée.
- **Couleurs du jour en dehors du dashboard**, celles de la couverture de l'édition à la une, que toute page du thème « La couverture du jour » demande ; le tirage du jour est commun aux deux ([Édition « À la une » : mise en œuvre](../domaine/edition-a-la-une-mise-en-oeuvre.md)).
- **Contrôles de l'accueil de l'administration** : ceux seuls que définit la spécification, l'ISBN étant vérifié en mémoire par le validateur du domaine, qu'aucun SQL ne traduit ([Validation de l'ISBN](../domaine/validation-de-l-isbn.md)).
- **Listes de l'administration** : recherche, navigation et filtres partagés avec la consultation ; l'**état dans la collection** est une expression unique que la projection et le filtre partagent ; la réponse **nomme les fiches de ses filtres**, pour que l'écran les présente sans autre requête.
  - **Nombres d'albums et de séries d'un genre ou d'un univers comptés par les filtres des listes** : ceux que la liste des albums, ou des séries, filtrée sur lui trouve, sous-univers compris, par les mêmes expressions.
  - **Liste des univers dans l'ordre de la hiérarchie, ordonnée et paginée en mémoire** sur le référentiel chargé en entier, frères triés par la base selon sa collation ; seules les lignes de la page sont ensuite comptées, en une requête. Chaque ligne porte ses ancêtres : sa place dans la hiérarchie, et ce qui la nomme quand la page ne montre pas son parent.
    - **Alternative écartée (requête récursive, tri par chemin)** : un SQL hors du modèle pour une table de quelques dizaines de lignes, que l'API charge déjà en entier pour tout changement de parent.
  - **Conversion d'une intention d'achat proposée d'après la liste elle-même** : chaque intention de la liste de l'administration dit si elle se convertit vers l'autre cible, ce que l'API calcule par la même règle que le domaine ([Intention d'achat](../../fonctionnel/regles-metier/intention-d-achat.md) : toujours pour une intention sur l'album, pour une intention sur une édition seulement si c'est la seule de l'album). Le frontend n'offre ainsi que la conversion que le domaine acceptera, sans y écrire la règle ([Frontend](../socle/frontend.md), aucune règle de saisie).
    - **Alternative écartée (conversion toujours offerte, refusée à l'enregistrement)** : l'utilisateur ne l'apprendrait qu'après l'avoir demandée, pour un refus que la ligne savait déjà.
- **Page des éditions et intentions d'achat d'un album en une lecture**, dans un même instantané ([`visuel/editions-album/`](../../visuel/editions-album/index.md)) : l'album en bref, tel que le présente la liste des albums, avec la version que renvoie toute suppression de ses éditions ou de ses intentions ; ses éditions possédées, dans l'ordre de sa fiche ; ses intentions, chacune disant si elle se convertit, par la règle de la liste des intentions.
  - **Alternative écartée (page assemblée par le frontend depuis la fiche de consultation de l'album et les intentions de son agrégat)** : la fiche de consultation ne porte ni la version de l'album ni la conversion d'une intention, et la page demanderait plusieurs lectures sans instantané commun.

## Pagination et contrats

- **Pagination par décalage**, sur des tris adossés aux clés indexées, complétés par l'identifiant pour un ordre total (sans quoi un élément pourrait figurer sur deux pages, ou sur aucune) ; taille bornée, une page hors bornes étant une erreur technique, le frontend construisant la pagination.
  - **Alternative écartée (pagination par clé)** : plus performante sur de très gros volumes, mais elle complique l'accès direct à une page et le total, sans gain pour quelques milliers d'albums.
- **Énumérations dupliquées dans les contrats**, leur parité vérifiée par un test : ni le frontend ni l'outil pour agents IA ne référencent le domaine. Elles sont sérialisées **en chaîne** (lisibilité de la documentation OpenAPI), par un convertisseur déclaré sur chaque énumération : le contrat porte lui-même son format.
  - **Alternative écartée (convertisseur global de l'API)** : chaque client devrait déclarer le même, autant de configurations à tenir alignées que de clients.
- **Tests d'intégration sur PostgreSQL réel** ([Outillage de développement .NET](../outillage.md)), une API et sa base par classe de tests, chaque test créant ses données.
