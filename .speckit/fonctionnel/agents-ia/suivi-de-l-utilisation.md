---
type: Règle métier
title: 'Suivi de l''utilisation'
description: 'Suivi par jeton et par session, objectif, indice d''efficacité et ses signaux, retour écrit de l''agent.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Suivi de l'utilisation

- L'utilisation de l'outil est **suivie par jeton** : pour chaque fonctionnalité de l'outil (chaque point d'entrée qu'il propose aux agents), le **nombre** d'appels et leur **fréquence** dans le temps.
- **Sessions** : deux sortes de jetons coexistent. Le **jeton d'accès** authentifie l'agent ; il est géré manuellement dans l'administration (cf. [Accès par jeton](/fonctionnel/agents-ia/acces-par-jeton.md)). Le **jeton de session** est géré automatiquement : l'agent l'obtient d'un outil dédié, en présentant son jeton d'accès, avant toute autre utilisation de l'outil ; c'est cet outil qui l'informe de la **durée de validité** du jeton de session qu'il lui remet (cf. ci-dessous). Chacune de ses utilisations désigne ensuite ce jeton ; une utilisation sans jeton de session valable est refusée. Une session regroupe les utilisations d'une même conversation de l'agent, et peut porter l'**objectif** de la demande de l'utilisateur, si l'agent le précise (facultatif) : le suivi présente ainsi le bilan de chaque session au regard de la demande qu'elle sert. L'objectif pouvant évoluer ou se préciser au cours d'une conversation, un outil facultatif permet à l'agent de le **mettre à jour** : chaque objectif est **conservé et daté** dans la session, si bien que le suivi rapporte chaque utilisation à l'objectif alors en vigueur.
- **Validité d'un jeton de session** : un jeton de session a une **durée de validité** limitée, qui lui est propre : elle ne dépend pas de celle du jeton d'accès, les deux étant vérifiés à chaque utilisation, et le premier qui n'est plus valable interdit l'accès. Seul l'outil qui crée le jeton de session en informe l'agent ; les autres outils ne la rappellent pas, et refusent seulement l'appel, avec sa raison, une fois le jeton de session ou le jeton d'accès expiré (cf. [Accès par jeton](/fonctionnel/agents-ia/acces-par-jeton.md), Refus expliqué). L'agent ouvre alors une nouvelle session. Pour tracer les **prolongations**, l'outil qui crée un jeton de session accepte, facultativement, la **session précédente** que la nouvelle prolonge (session expirée ou invalidée, de la même conversation) : le suivi relie ainsi les sessions successives d'une même conversation. La session précédente doit avoir été ouverte avec le même jeton d'accès ; une session précédente inconnue ou ouverte avec un autre jeton fait refuser l'ouverture, avec sa raison. Il est **admis**, sans être exigé, qu'un **redémarrage** de l'outil invalide les jetons de session en cours : l'agent, dont la session est alors refusée, en ouvre une nouvelle, et l'historique des sessions invalidées reste dans le suivi.
- **Objectif** : repérer les points d'entrée à améliorer, et **comment** ou **en quoi** les améliorer, ainsi que les **nouveaux points d'entrée** nécessaires (ex. un besoin que l'agent ne satisfait qu'en enchaînant plusieurs appels). Le suivi ne se limite donc pas à compter les appels : il doit, idéalement, aboutir à un **indice d'efficacité** de chaque point d'entrée.
- **Indice d'efficacité** d'un point d'entrée : la part de ses utilisations **productives**, c'est-à-dire réussies et dont l'agent n'abandonne pas aussitôt le résultat pour une nouvelle tentative sur le même point d'entrée, dans la même session (ex. une recherche sans résultat aussitôt reformulée). Consulter la page suivante d'un résultat n'est pas une nouvelle tentative. Il est accompagné de signaux qui disent **quoi** améliorer :

  | Signal | Indique |
  | --- | --- |
  | Erreurs dues à des paramètres mal construits | Description ou exemples du point d'entrée à revoir |
  | Nouvelle tentative après une erreur, un résultat vide ou un résultat que l'agent abandonne aussitôt | Message d'erreur, comportement de la recherche ou pertinence des résultats à revoir |
  | Enchaînement de pages | Taille des pages ou filtres manquants |
  | Réponses volumineuses | Besoin d'une forme de réponse plus concise |
  | Mêmes enchaînements de points d'entrée dans de nombreuses sessions | **Nouveau point d'entrée** qui les regroupe |
  | Durée des utilisations | Performance |

  Sa pertinence sera jugée à l'usage, et sa définition ajustée en conséquence.
- **Retour écrit de l'agent** : l'agent peut, s'il le juge utile, signaler par écrit une limite rencontrée (ex. un filtre ou une donnée qui lui a manqué, une réponse inadaptée). Ce retour est facultatif et n'est pas une note : il complète le suivi, rattaché à l'utilisation précise qu'il concerne, ou à sa session s'il ne porte sur aucune utilisation (ex. une fonction absente), sans entrer dans l'indice d'efficacité.
- Le suivi, retours écrits compris, est consultable dans l'administration. L'historique d'un jeton est conservé tant que le jeton existe, archivé compris, et supprimé avec lui (cf. [Accès par jeton](/fonctionnel/agents-ia/acces-par-jeton.md), Archivage).

Les règles d'accès sont celles de l'application (cf. [Règles d'accès](/fonctionnel/regles-d-acces.md)) : l'outil n'a pas de règles d'accès aux données qui lui soient propres, son jeton ne faisant que déterminer le niveau d'accès de l'agent, et les fonctionnalités équivalentes de l'interface web ont les mêmes restrictions.

<!-- À préciser : périmètre exact de l'aide à la saisie, cas d'usage détaillés, analyses et statistiques attendues. -->
