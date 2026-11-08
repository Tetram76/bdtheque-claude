---
type: Choix d'implémentation
title: 'Internationalisation'
description: 'Culture de la requête, textes en ressources, formatage, fuseau horaire du navigateur, ICU requis.'
applies_to:
  - 'src/Bdtheque.Frontend/Formatting/**'
  - 'src/Bdtheque.Frontend/wwwroot/browser-time-zone.js'
  - 'Directory.Build.props'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Internationalisation

- **Textes en ressources de localisation d'ASP.NET Core**, un fichier par composant à côté de lui, un fichier commun pour les mises en page ; aucun texte utilisateur dans le code. Les ressources sans culture sont en français, seule culture proposée.
- **Culture de la requête fixée par l'application**, sans fournisseur de culture : la culture d'affichage est un paramètre de l'application ([Langue et culture d'affichage](../../../fonctionnel/presentation/langue-et-culture-d-affichage.md)), jamais la langue que demande le navigateur. Le paramétrage n'en proposant qu'une, le frontend la fixe sans le lire, jusqu'à ce qu'une autre soit proposée. Elle décide du formatage (nombres, montants, dates, dates partielles à leur précision) et du tri ; un montant dans une autre devise que celle de la culture est suivi de son symbole, à défaut de son code.
- **Fuseau horaire du navigateur** ([Langue et culture d'affichage](../../../fonctionnel/presentation/langue-et-culture-d-affichage.md)) : aucune requête ne le porte, un script de chaque page le garde donc dans un **cookie**, que toute requête apporte au serveur, si bien qu'une page rendue en statique le connaît. Le script le vérifie à chaque page et à chaque retour sur l'onglet, pour suivre un changement de fuseau.
  - Sans cookie (première page) ou avec un fuseau inconnu, le serveur présente en **temps universel**. Une page dont le contenu dépend du fuseau, et dont la requête n'a pas transmis le fuseau courant, est **rendue de nouveau** une fois le cookie à jour ; jamais si le navigateur refuse le cookie (ce serait sans fin), ni pour un fuseau inconnu que la requête a bien transmis. Le fuseau transmis, et non celui que le serveur a résolu, décide seul si la page est à jour.
  - La date du jour du navigateur, et le fuseau des recherches avancées ([Recherche avancée : mise en œuvre](../../recherche-avancee/recherche-avancee-mise-en-oeuvre.md)), viennent de ce cookie.
  - Le cookie vit un an, sous les 13 mois d'un traceur ([Mesure d'audience de l'interface web](../../../fonctionnel/mesure-audience.md)), et n'identifie personne : nécessaire à la présentation demandée, ce n'est pas une mesure d'audience.
  - **Alternative écartée (dates réécrites dans le navigateur par un script)** : le formatage du navigateur peut différer de celui de la culture de l'application, et le serveur ignorerait toujours le fuseau pour une saisie, une recherche ou la date du jour.
  - **Alternative écartée (fuseau par défaut de l'application, ex. `Europe/Paris`, sur une première page)** : faux, sans que rien ne le signale, pour tout visiteur d'un autre fuseau ; le temps universel, comme le stockage, est corrigé par le nouveau rendu.
- **ICU requis** : le mode *globalization-invariant* de .NET, incompatible avec le tri et le formatage culturels, est désactivé, et les images embarquent ICU ([Images de base des conteneurs](../../deploiement/images-de-base-des-conteneurs.md)).
