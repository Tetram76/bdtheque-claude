---
type: Choix d'implémentation
title: 'Mesure d''audience : mise en œuvre'
description: 'Visites, utilisations et compteurs dans l''API : enregistrement, utilisations répétées, clôture des mois, conservation, indicateurs.'
applies_to:
  - 'src/Bdtheque.Api/Audience/**'
  - 'src/Bdtheque.Api/Endpoints/AudienceEndpoints.cs'
  - 'src/Bdtheque.Contracts/Audience/**'
  - 'src/Bdtheque.Domain/Entities/Audience*.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Mesure d'audience : mise en œuvre

- **Visites, utilisations et compteurs ne sont pas des fiches** ([Visite de l'interface web](../../modele-metier/visite-de-l-interface-web.md), [Utilisation d'une fonction](../../modele-metier/utilisation-d-une-fonction.md), [Compteur d'audience mensuel](../../modele-metier/compteur-d-audience-mensuel.md)) : ni journal des modifications, ni version, ni modification. Leurs dates sont leur date de création, posée par l'horloge du serveur, jamais par celle du navigateur. La fiche présentée est désignée sans clé étrangère, pour survivre à sa suppression.
- **Une troisième famille de routes, `/audience`**, à côté de `/admin` et `/catalog` ([Organisation de l'API](../api/organisation-de-l-api.md)) : le `frontend` y écrit pour tout utilisateur, ce que ni l'administration, réservée, ni la consultation, en lecture, ne recouvrent. Une visite s'ouvre avec sa première utilisation (elle en a toujours une) ; une visite inconnue, supprimée au terme de sa conservation, est une erreur fonctionnelle, sur laquelle le `frontend` en ouvre une autre. Délimitation de la visite, appareil, moteur et opposition relèvent du `frontend` ([Relevé par le frontend](releve-par-le-frontend.md)).
- **Utilisation répétée déterminée à l'enregistrement**, et stockée, jamais modifiée : la précédente utilisation de la visite est connue à cet instant, et les comptages restent de simples sommes. L'enregistrement verrouille la visite avant de lire sa dernière utilisation, pour que deux onglets d'une même visite soient sérialisés.
  - **Alternative écartée (repérage à la lecture, fonction de fenêtre)** : EF Core ne traduit pas les fonctions de fenêtre, et un SQL écrit à la main devrait être reproduit pour l'établissement des compteurs comme pour la lecture des mois ouverts.
- **Compteurs par mois et par compartiment**, uniques par un index `NULLS NOT DISTINCT` : la version du moteur peut être absente, et sans cette option deux compteurs sans version ne se heurteraient pas. Le mois d'une visite est celui de son début, en temps universel ([Langue et culture d'affichage](../../fonctionnel/presentation/langue-et-culture-d-affichage.md)). Seule l'application écrit les compteurs : une violation de leur unicité est une erreur de programmation, donc technique ([Erreurs métier, fonctionnelles et techniques](../ecritures/erreurs-metier-fonctionnelles-et-techniques.md)).
- **Conservation par une tâche de fond quotidienne** ([Mesure d'audience de l'interface web](../../fonctionnel/mesure-audience.md), Durées de conservation), qui clôt les mois puis supprime les visites publiques arrivées à leur terme, dans une même transaction :
  - un mois est **clos au terme de la conservation** de ses premières visites (25 mois) : sa plus récente visite a alors 24 mois, bien plus que les 13 mois du traceur qui la porte ; aucune utilisation ne peut plus s'y ajouter, et ses compteurs, établis sur toutes ses visites, sont définitifs ;
  - le dernier mois clos sépare les mois que portent les compteurs de ceux que porte encore le détail, sans autre donnée à tenir ;
  - deux exécutions simultanées sont sérialisées par un verrou consultatif ; un échec n'arrête pas l'`api`, l'exécution suivante rattrape les mois restés ouverts ;
  - les visites de l'administrateur sont conservées sans limite.
  - **Alternative écartée (clôture de chaque mois dès qu'il est révolu)** : une visite qui se prolonge au-delà de son mois y ajouterait encore des utilisations, et le mois ne serait définitif qu'après la durée de vie du traceur ; clore au terme de la conservation n'exige aucun autre délai, et laisse le détail, plus riche, servir la lecture le plus longtemps possible.
- **Indicateurs toujours recomposés à partir de compteurs** : enregistrés pour les mois clos, calculés sur le détail, par la même requête que la clôture, pour les mois ouverts. Une période donne ainsi les mêmes indicateurs avant et après la clôture de ses mois. Les fonctions jamais utilisées, que l'API ne connaît pas, sont ajoutées par le `frontend`, qui tient la liste des fonctions suivies.
