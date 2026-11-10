---
type: Choix d'implémentation
title: 'Outillage de développement'
description: 'Outillage .NET : packages centralisés, xunit, Testcontainers, bUnit, Playwright, couverture, commentaires XML, CI ; à lire pour tout test ou build.'
applies_to:
  - 'Directory.Packages.props'
  - 'Directory.Build.props'
  - 'tests/**'
  - '.github/workflows/ci.yml'
  - 'global.json'
  - 'dotnet-tools.json'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-10T17:33:23Z }
---

# Outillage de développement

## Packages et compilation

- **Versions des packages centralisées**, dépendances transitives comprises, à la racine de la solution ; restauration **sans fichiers de verrouillage**.
  - **Alternative écartée (fichiers de verrouillage, restauration verrouillée)** : le frontend porte un package implicite à la version du SDK qui restaure, si bien qu'une restauration verrouillée échoue avec tout autre SDK (`NU1004`, `dotnet/sdk` n° 48795) ; il faudrait imposer une version exacte du SDK à chaque poste et régénérer les fichiers à chaque correctif du SDK. La reproductibilité est déjà acquise par les versions exactes, nuget.org ne remplaçant jamais un package sous une même version.
- **Commentaires XML vérifiés par le compilateur, tout avertissement en erreur** : rien d'autre ne signalerait un commentaire devenu faux après une modification du code. Documenter chaque membre ou paramètre n'est pas exigé, les commentaires expliquant le pourquoi ([Qualité du code](../gestion-projet/gouvernance/qualite-du-code.md)) ; les fichiers de documentation ne sont pas publiés.
  - **Alternative écartée (liste choisie d'avertissements en erreur)** : elle laisserait passer ceux qu'elle oublie (ex. une balise `<param>` en double).
- **Version du SDK identique dans les Dockerfiles et les workflows**, vérifiée par un test ; `global.json` n'en fixe que le minimum, pour que les postes gardent le SDK de leur environnement.
  - **Alternative écartée (version lue dans `global.json` par le workflow)** : avec une montée automatique, la version changerait sans modification du dépôt ([Mises à jour des dépendances](../gestion-projet/depot/mises-a-jour-des-dependances.md)) ; avec une version exacte, `global.json` l'imposerait à chaque poste.

## Tests

- **xunit**, références communes déclarées une fois pour les seuls projets de test.
- **Persistance et API testées sur PostgreSQL réel** (Testcontainers), de la **même image** que la production ([Version de PostgreSQL](deploiement/version-de-postgresql.md)), schéma produit par les **migrations** ; chaque test a sa base, clonée d'une base modèle migrée une fois, dont il libère les connexions (un pool par base les épuiserait). Les tests de l'API démarrent sur une base vide, comme un premier déploiement. Docker est donc requis pour les tests.
  - **Alternative écartée (SQLite en mémoire)** : un autre moteur ne vérifie ni les migrations appliquées, ni la collation, ni la précision des `decimal`, ni les contraintes de PostgreSQL, précisément ce que ces tests doivent garantir.
- **Composants du frontend testés par bUnit** (MIT, .NET Foundation), en mémoire, sans navigateur ; un composant qui attend un délai le fait sur l'horloge de l'application, que ses tests remplacent par une horloge factice avancée par le test : aucune attente réelle.
  - **Évènement d'un composant interactif déclenché sur le répartiteur du rendu, son traitement attendu jusqu'à sa fin** : le test trouve l'élément et déclenche l'évènement sur le répartiteur, où le composant le traite aussitôt jusqu'à sa première attente (ex. la pause de la frappe, posée sur l'horloge factice) ; il avance ensuite l'horloge une fois, puis attend la tâche du traitement, que bUnit termine une fois fait le rendu qu'il cause. Toute attente est bornée, et échoue explicitement en nommant ce qui n'a pas abouti.
    - **Alternative écartée (boucle qui avance l'horloge et relit le rendu jusqu'à l'état attendu)** : la suite du traitement reprend sur le répartiteur, hors du fil du test ; la boucle, active, dispute les processeurs au travail qu'elle attend, et ne le voit aboutir qu'à travers une borne de temps réel (constaté, #344 : un échec du CI à la première recherche d'une exécution, la plus lente).
    - **Alternative écartée (`WaitForAssertion` de bUnit)** : vérifiée à chaque rendu, sans boucle, mais bornée par défaut à une seconde de temps réel, et sans savoir si le traitement est fini ; sa tâche le dit exactement.
- **Tests de bout en bout par Playwright pour .NET** (Apache 2.0), qui pilote Chromium sur l'**application complète** : `frontend` sur un vrai port (un navigateur ne joint pas un serveur en mémoire), en environnement `Production`, appelant `api` sur sa base. Ils vérifient ce que seul un navigateur exerce, dont chaque comportement des scripts du frontend.
  - **Alternative écartée (Selenium)** : attentes à écrire à la main, cause usuelle de tests instables.
  - **Alternative écartée (application en conteneurs `docker compose`)** : une construction des images à chaque exécution, et des tests qui ne pourraient plus régler l'horloge ni les données de l'application.
  - **Seul le navigateur sans interface** est installé (prescription de la documentation de Playwright), par les tests eux-mêmes s'il manque.
  - **Alternative écartée (cache des navigateurs dans le CI)** : la documentation de Playwright le déconseille, sa restauration prenant autant que le téléchargement, et les bibliothèques système ne se mettent pas en cache.
- **Pilotage du navigateur selon sa documentation** ([Qualité du code](../gestion-projet/gouvernance/qualite-du-code.md)) :
  - chaque test ferme les contextes qu'il ouvre : ouverts, ils accumuleraient connexions et circuits interactifs ;
  - une page est jugée prête sur **son propre état** (document complet, feuilles appliquées, polices disposées, dessin du thème achevé, puis deux images), jamais sur le silence du réseau, que la documentation déconseille, ni sur une attente fixe ; une feuille non chargée fait échouer le test, et un délai dépassé nomme les requêtes en échec et celles en cours ;
  - **seules les attentes que le pilote vérifie en un seul appel** sont permises (adresse atteinte, condition dans la page, navigation) : le client .NET vérifie un état puis s'abonne aux suivants, et perd celui qui arrive entre les deux ([`microsoft/playwright-dotnet` #3364](https://github.com/microsoft/playwright-dotnet/issues/3364), [#3399](https://github.com/microsoft/playwright-dotnet/issues/3399), constaté #298). Ses attentes en deux temps sont refusées par le compilateur (analyseur des API interdites), jusqu'à une version qui corrige le défaut.
    - **Alternative écartée (attente répétée par tranches courtes)** : elle retrouve l'état perdu, mais garde sous une relance une attente qui en perd.
    - **Alternative écartée (test du dépôt qui cherche ces noms dans les sources)** : il confondrait un appel avec sa mention dans un commentaire.
- **Composants partagés testés sur des pages propres aux tests**, avant qu'un écran les emploie, et fixés dans chaque thème par les captures ([Thèmes](socle/themes.md)) ; l'application ne route ces pages que dans les tests.
- **Règle d'un chantier d'écran** : chaque comportement de l'écran est fixé par un test dans sa PR, avant sa validation fonctionnelle : un test de composant pour ce qu'il présente et chacun de ses états, au moins un test de bout en bout par parcours de l'utilisateur. Un retour de validation qui change un comportement change son test. Les tests désignent les éléments par ce qui ne dépend pas du thème (rôle, lien, structure), jamais par l'apparence.
- **Un écran qui dépend de toute la collection** (ex. l'accueil) a sa propre application de test par état présenté : la base partagée reçoit les données des autres tests, qui changeraient son rendu selon l'ordre d'exécution.
- **Couverture collectée par l'unique exécution des tests du CI**, sur le code de l'application seul, la bibliothèque partagée des tests déclarée comme code de test ; elle informe, **sans seuil** : la non-régression se juge sur ce que vérifient les tests ([Qualité du code](../gestion-projet/gouvernance/qualite-du-code.md)), non sur un pourcentage.
  - Les **scripts du frontend** sont hors de la mesure ([Analyse de la qualité du code](../gestion-projet/depot/analyse-de-la-qualite-du-code.md)), non hors des tests : les tests de bout en bout fixent leurs comportements.
  - **Alternative écartée (mesure de leur couverture)** : SonarQube Cloud ne lit qu'un rapport LCOV, dont la conversion depuis Chromium exige un outil Node.js absent de la chaîne ; à reconsidérer quand les scripts ne se limiteront plus à quelques fichiers courts.

## CI

- **Contrôle de cohérence modèle ↔ migrations** : le CI échoue sur une modification du modèle commitée sans sa migration.
- **Tests selon la modification** ([Intégration continue (CI)](../gestion-projet/depot/integration-continue-ci.md)) : les fichiers modifiés sont ceux qui distinguent le commit de fusion que construit le CI de la tête de `main` qu'il fusionne, la PR telle qu'elle serait fusionnée ; un fichier déplacé compte à ses deux chemins. Sans source modifiée, seuls s'exécutent les tests du dépôt, désignés par leur trait, et l'analyse se fait sans couverture ; sans source du rendu modifiée, aucun test de bout en bout.
  - **Alternative écartée (action de filtrage par chemins)** : une dépendance de plus à épingler et à surveiller ([Suivi des versions et des vulnérabilités](../contraintes-techniques/suivi-des-versions-et-des-vulnerabilites.md)), pour une comparaison que `git` fait en une commande.
- **Interfaces créées par Docker sans adresse IPv6 de lien local** : Chromium abandonne ses requêtes dès qu'une adresse de la machine change (`net::ERR_NETWORK_CHANGED`), et, sur l'exécuteur, seules changent celles des interfaces des conteneurs, à leur démarrage et à leur suppression, que Testcontainers fait encore dix secondes après la fin d'un processus de test (relevé du CI, #294). Le réglage ne touche que les interfaces de Docker, dont le pont n'emploie que l'IPv4 (documentation du noyau, *IP Sysctl*). Un relevé des changements réseau fait échouer le job si une interface de Docker change d'adresse pendant les tests.
  - **Alternative écartée (tests de bout en bout tenus à l'écart des conteneurs par l'ordre des étapes)** : l'ordre déplace les changements d'adresse sans les supprimer (constaté, #294).
  - **Alternative écartée (IPv6 désactivé sur l'exécuteur)** : il modifie le réseau de toute la machine, quand seules les interfaces de Docker changent.
  - **Alternative écartée (navigateur en conteneur)** : les tests devraient joindre depuis ce conteneur l'application qu'ils hébergent, pour le même résultat.
- **Tests de bout en bout exécutés seuls, après les autres** : côte à côte, ils durent deux fois plus longtemps faute de processeurs (2 min 5 s au lieu d'une minute, constaté), et s'approchent des délais du navigateur, sans que l'ensemble finisse plus tôt.
- **Une seule exécution des tests par push** : l'analyse SonarQube Cloud est un job distinct qui suit celui des tests et reprend sa couverture ([Analyse de la qualité du code](../gestion-projet/depot/analyse-de-la-qualite-du-code.md)). Le check des tests dit seul si la non-régression est confirmée, et une analyse impossible n'échoue que son propre job. Le scanner désactivant les avertissements en erreur, sa compilation ne remplace pas celle, stricte, des tests.
  - **Alternative écartée (tests exécutés aussi par l'analyse)** : le double de temps d'exécuteur et de téléchargements de l'image PostgreSQL ([Intégration continue (CI)](../gestion-projet/depot/integration-continue-ci.md), quotas), et deux fois plus de chances qu'une instabilité fasse échouer un push.
  - **Alternative écartée (un seul job compilation, analyse et tests)** : une analyse indisponible ferait échouer le check de la non-régression, sans qu'on distingue l'un de l'autre.
- **CI lancé aussi à la demande**, pour l'analyse de `main` qui suit la publication d'une version ([Versions et releases](../gestion-projet/depot/versions-et-releases.md)) : l'analyse reprenant la couverture des tests, c'est le CI entier qui s'exécute.
- **Cache NuGet sous une clé tirée de tout ce qui décide des packages restaurés**, sans clé de repli : un cache partiel, complété puis réenregistré, garderait les packages de toutes les versions passées.
- **Exécution précédente d'une PR annulée** par une nouvelle, pour épargner les quotas ([Intégration continue (CI)](../gestion-projet/depot/integration-continue-ci.md)) ; une exécution sur `main` n'est jamais annulée.
