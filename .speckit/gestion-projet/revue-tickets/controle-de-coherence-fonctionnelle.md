---
type: Gouvernance
title: 'Contrôle de cohérence fonctionnelle'
description: 'Contrôle hebdomadaire de la cohérence de toute l''application, au sein des familles d''écrans et de points d''entrée de même nature et pour chaque règle transversale : sources confrontées, classement des écarts, suites, trace ; à lire quand le contrôle est dû ou demandé.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-11T12:40:23Z }
---

# Contrôle de cohérence fonctionnelle

Chaque Pull Request vérifie qu'un écran ou un point d'entrée est conforme à **son** concept du `.speckit/`. Aucune ne vérifie que des éléments de **même nature** se comportent de la même façon, ni qu'une même règle se présente partout de même : une divergence naît du code, ou de deux concepts rédigés à des moments différents, chacun conforme à lui-même. Le contrôle de cohérence fonctionnelle, mené chaque semaine par l'agent (exigence de l'utilisateur), la cherche dans **toute l'application** : consultation, administration, aide contextuelle, API et outil pour agents IA.

- **Alternative écartée (workflow planifié du CI)** : la confrontation demande la lecture et le jugement de l'agent, et un écart de classe B la décision de l'utilisateur, ce qu'aucune exécution sans eux ne fournit.
- **Alternative écartée (agent planifié, sans l'utilisateur)** : il ne pourrait soumettre ses questions un point à la fois, et demanderait des identifiants de plus pour un travail que la session qui reprend le travail fait avec le `.speckit/` chargé.

## Ce qui est confronté

- **Au sein d'une famille** : les éléments de même nature, comparés entre eux sur chaque axe qui les concerne. Exemples de familles : fiches de la consultation ; listes d'un genre ou d'un univers, présentées comme la bibliographie d'un auteur ; recherche simple et recherche avancée ; fiches de saisie en page, panneaux latéraux, saisie dans la ligne ([Saisie selon l'entité](../../visuel/listes/saisie-selon-l-entite.md)) ; listes et dialogues de l'administration ; points d'entrée de l'API et de l'outil pour agents IA de même nature (lecture, écriture sous version, suppression avec impact). Exemples d'axes : modification concurrente (réponse, actions offertes) ; erreur métier (champ en cause, message, focus) ; mentions obligatoire ou facultatif ; unicité sans tenir compte de la casse ni des accents ; texte trop long ; création sur place ; enregistrement, annulation et retour ; dates de la fiche ; suppression et son impact.
- **Transversalement** : une même donnée ou une même règle partout où elle apparaît, quelle que soit la partie. Exemples : libellé d'un album ou d'une édition et ISBN présenté ; ordres de présentation ; appartenance à la collection ; données absentes ; présentation des erreurs ([Présentation des erreurs](../../fonctionnel/presentation-des-erreurs.md)) ; règles d'accès, communes à l'interface web et à l'outil pour agents IA ; mesure d'audience ; déclinaison par thème ; aide contextuelle au regard de ce que fait l'écran.

Familles et axes ne sont pas une liste close : un écran ou un point d'entrée nouveau rejoint la famille de sa nature, et un élément appartient à autant de familles que d'axes le concernent.

Pour chaque famille et chaque axe transversal, trois sources sont confrontées : le `.speckit/` (`fonctionnel/`, `visuel/`, `modele-metier/`), le code, et les tests qui fixent le comportement.

## Classement des écarts

- **A — le code s'écarte du `.speckit/`** : un défaut, dont la correction est un ticket ([Issues](issues.md), cause de fond) ; sa Pull Request est soumise à la validation fonctionnelle s'il change ce que l'utilisateur voit ([Validation fonctionnelle](validation-fonctionnelle.md)).
- **B — des éléments de même nature divergent sans que le `.speckit/` dise pourquoi** : un point fonctionnel, soumis à la décision de l'utilisateur, **un point à la fois** ([Prise de décision](../gouvernance/prise-de-decision.md)). La décision est aussitôt consignée, dans ses termes, dans un ticket dont la Pull Request l'écrit d'abord dans le concept concerné, puis la réalise.
- **C — l'écart est justifié** : le `.speckit/` en donne la raison. Rien n'est fait.

Le contrôle lui-même ne modifie ni le `.speckit/` ni le code, et n'a pas de Pull Request : chacune de ses suites est un ticket, réalisé par sa propre Pull Request. Il se termine quand sa conclusion est écrite dans la description de son ticket (§ Trace), ce qui libère sa place dans un lot de chantiers en parallèle ([Chantiers en parallèle](../gouvernance/chantiers-en-parallele.md)).

Une raison qui manque ou qui faillit fait d'un écart un B, jamais un C : une question de trop coûte une réponse à l'utilisateur, un écart réel classé C reste dans l'application sans que personne le voie.

- **Décrit n'est pas justifié** : un concept qui décrit le comportement d'un élément sans dire pourquoi il diffère de sa famille ne justifie rien.
- **Raison jugée** : la raison doit viser exactement cet écart, et non un cas voisin ou une généralité, et les faits sur lesquels elle repose doivent tenir encore. Une raison douteuse, imprécise ou périmée fait de l'écart un B, soumis à l'utilisateur en citant le passage.
- **Aucun C silencieux** : chaque C figure dans la trace avec le passage cité et ce qui, selon l'agent, le fait couvrir l'écart ; un C que l'utilisateur conteste devient un B.
- **Revu au changement** : un C est revu dès que change le concept qui porte sa raison, ou l'un des éléments en cause ; il n'est pas signalé de nouveau tant que rien ne change.

## Cohérence tenue dans la durée

Une règle commune confirmée (A corrigé, B tranché) est mise en œuvre **une seule fois**, par un composant ou un mécanisme partagé de tous les éléments concernés ([Frontend](../../choix-implementation/socle/frontend.md), composants communs ; [Erreurs métier, fonctionnelles et techniques](../../choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md), points centraux) : écrite une fois, elle ne peut plus diverger. Les tests propres à chaque élément fixent son usage de ce mécanisme ([Outillage de développement](../../choix-implementation/outillage.md), règle d'un chantier d'écran). Une règle qui ne se partage pas est fixée par les tests de chaque élément, et c'est le contrôle suivant qui détecte une divergence.

- **Alternative écartée (test paramétré sur toute une famille, énumérée depuis les routes)** : les familles se recouvrent, un élément en ayant plusieurs ; un tel test devient un cadre de test, avec un adaptateur par élément, les écarts C en conditions, et une énumération qui casse dès qu'un élément nouveau n'a pas encore le sien (KISS, [Qualité du code](../gouvernance/qualite-du-code.md)).

## Moment et proportion

- **Dû** chaque semaine calendaire (du lundi au dimanche, ISO 8601), à la reprise du travail ([Contrôle de cohérence à la reprise du travail](controle-de-coherence-a-la-reprise-du-travail.md)), ou à la demande de l'utilisateur, qui vaut le contrôle de la semaine.
- **Proportion** ([Budget IA](../gouvernance/budget-ia.md)) : le contrôle porte sur ce qui a changé depuis le commit de `main` du contrôle précédent, dans le code comme dans le `.speckit/`. Chaque famille dont un élément a changé est revue entière ; chaque axe transversal dont une occurrence a changé est revu partout où il apparaît. Le premier contrôle, sans commit précédent, porte sur toute l'application. Sans aucun changement, il se réduit à le constater dans la trace.

## Trace

Le ticket « Cohérence fonctionnelle » (étiquettes `functional-consistency` et `harness`, [Issues](issues.md#tickets-de-suivi-des-harnais)), sans jalon, que ne réalise aucune Pull Request ([Versions et releases](../depot/versions-et-releases.md)), porte dans sa **description** l'état du contrôle, que l'agent réécrit :

- « Contrôle en cours », avec sa date, écrit **avant** le contrôle : une autre session qui reprend le travail la même semaine le voit, et ne le refait pas ;
- à sa fin, le **dernier contrôle** : sa date et le commit de `main` sur lequel il porte, puis chaque écart relevé avec sa classe, les éléments en cause, et sa suite (ticket d'un A, question posée, décision et ticket d'un B, passage cité et raison d'un C), ou l'absence d'écart, constatée explicitement.

Un écart B en attente de la décision de l'utilisateur reste dans la description jusqu'à elle. Le ticket reste ouvert.
