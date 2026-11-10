---
type: Choix d'implémentation
title: 'Édition « À la une » : mise en œuvre'
description: 'Historique des mises à la une, tirage du seul jour que donne le navigateur, un tirage par jour sans verrou, règles du tirage en fonction pure.'
applies_to:
  - 'src/Bdtheque.Domain/Common/SpotlightDraw.cs'
  - 'src/Bdtheque.Domain/Entities/EditionSpotlight.cs'
  - 'src/Bdtheque.Infrastructure/EditionSpotlightWrites.cs'
  - 'src/Bdtheque.Api/Endpoints/DashboardEndpoints.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-10T17:40:00Z }
---

# Édition « À la une » : mise en œuvre

- **Un historique des mises à la une** ([Page d'accueil](../../fonctionnel/page-accueil.md)) : une ligne par jour (le jour du visiteur, une seule édition pour tous les fuseaux), qui porte à la fois l'édition du jour, l'historique qui écarte les éditions récentes, et le nombre de mises à la une d'une édition, compté ([Édition](../../modele-metier/edition.md)). Il appartient à l'édition (composition) : une édition supprimée emporte ses lignes, et le tirage du jour est refait si elle était à la une. Ce n'est pas une fiche : son écriture ne modifie pas l'édition et n'entre pas au journal des modifications.
  - **Alternative écartée (compteur et date de dernière mise à la une sur l'édition)** : chaque tirage écrirait sur l'édition, écriture qu'il faudrait soustraire au journal des modifications et à la date de dernière modification ([Dates renseignées automatiquement : mise en œuvre](../persistance/dates-renseignees-automatiquement-mise-en-oeuvre.md), [Journal des modifications : mise en œuvre](journal-des-modifications-mise-en-oeuvre.md)) ; le compteur serait en outre une donnée redondante.
  - **Alternative écartée (tirage déterministe, graine dérivée de la date)** : la même édition toute la journée seulement si les données ne changent pas ; l'historique et les comptages devraient de toute façon être stockés.
- **Tirage du seul jour que donne le navigateur** : le jour du visiteur n'est connu qu'une fois que son navigateur a donné son fuseau ([Internationalisation](../socle/transverse/internationalisation.md), date jamais devinée). Sans lui (première page), le dashboard est demandé sans jour et présenté sans édition à la une (bloc sans donnée, [Page d'accueil](../../fonctionnel/page-accueil.md)), et le thème « La couverture du jour » garde ses propres couleurs ([Thèmes](../socle/themes.md)) : rien n'est tiré ni enregistré, jusqu'au nouveau rendu de la page, qui demande le jour du navigateur.
- **Un tirage par jour, sans verrou** : le jour est la clé de l'historique. Le premier affichage d'une journée, qu'il soit celui du dashboard ou des couleurs du jour ([Visuels : stockage et traitement](visuels-stockage-et-traitement.md)), tire une édition et l'insère sans écraser une ligne existante, puis relit la ligne du jour : de deux premiers affichages simultanés, la base ne retient qu'un tirage, que l'autre relit. La séquence reprend tant qu'elle ne relit rien (édition tirée, ou édition du jour, supprimée entre-temps). Sans édition possédée, aucun tirage n'est tenté : la réponse signale la collection vide, avant tout calcul ([Statistiques du dashboard](../../contraintes-techniques/architecture-deploiement/statistiques-du-dashboard.md)).
- **Règles du tirage en fonction pure du domaine**, qui reçoit les éditions possédées et le nombre aléatoire tiré par l'API : chaque règle se vérifie sur l'édition qu'elle désigne pour un tirage donné, sans test statistique.
- Une mise à la une est **récente** si elle date de moins de 10 jours **avant ou après** le jour tiré : un fuseau en avance a pu tirer les jours suivants avant que les autres tirent le leur.
