---
type: Choix d'implémentation
title: 'Outil pour agents IA'
description: 'Outil pour agents IA (Phase 6) : MCP, conteneur mcp, outils, jetons d''accès et de session, relevé de l''utilisation, archivage et réarmement.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Outil pour agents IA

## Protocole et hébergement

- **MCP** (*Model Context Protocol*), standard ouvert des agents IA courants, par le **SDK C# officiel** (Apache 2.0, [Licences](../contraintes-techniques/licences.md)), transport Streamable HTTP.
  - **Alternative écartée (API REST décrite par OpenAPI)** : exploitable par un agent, mais sans découverte standardisée des outils ni de leurs paramètres.
- **Un projet et un conteneur `mcp` dédiés** ([Indépendance de l'outil pour agents IA](../contraintes-techniques/architecture-deploiement/independance-de-l-outil-pour-agents-ia.md)), client de l'API par HTTP et par les contrats, comme le frontend, sans logique métier dupliquée ([Organisation du code](organisation-du-code.md)). `api` reste non exposée.
  - **Alternative écartée (outil hébergé par `frontend`)** : aucun conteneur de plus, mais l'outil serait lié au déploiement de `frontend`.
- **`mcp` n'est pas exposé** : seul `frontend` l'est ([Architecture des tiers](../contraintes-techniques/architecture-deploiement/architecture-des-tiers.md)), et un agent externe atteint `mcp` par un relais de `frontend`. Son exposition directe ne serait envisagée que si ce relais s'avérait trop coûteux, avec l'accord de l'utilisateur sur la contrainte d'exposition.

## Outils

- **Des outils, et des ressources MCP quand elles sont utiles**, jamais seul accès à une donnée dont l'agent a besoin de lui-même : une ressource est pilotée par l'application hôte, qui n'est pas tenue de la fournir, un outil par le modèle, qui l'appelle de lui-même (spécification MCP, *Resources* et *Tools*, § User Interaction Model).
- **Un outil par fonction, l'entité en paramètre** : la recherche avancée est un outil unique, avec un outil qui décrit le schéma d'une entité ([Recherche avancée : mise en œuvre](recherche-avancee/recherche-avancee-mise-en-oeuvre.md)). Les recommandations d'Anthropic (*Writing effective tools for agents*) veulent peu d'outils, d'objets clairs et distincts, des outils qui se recouvrent détournant l'agent des stratégies efficaces. Les autres outils sont établis au découpage de la Phase 6 ([`suivi-implementation/`](../suivi-implementation/index.md)), selon la même règle.
  - **Alternative écartée (un outil de recherche par entité)** : des descriptions plus courtes, mais onze outils quasi identiques à payer en contexte et à départager.
  - **Alternative écartée (schéma exposé comme ressource)** : seul un outil garantit à l'agent le schéma au moment où il construit ses critères, quel que soit le client.

## Jetons d'accès

- **Valeur de 256 bits aléatoires, dont seule l'empreinte SHA-256 est stockée** ([Accès par jeton](../fonctionnel/agents-ia/acces-par-jeton.md)) : à cette entropie, aucune force brute ne retrouve la valeur, un hachage lent, fait pour les mots de passe, n'apporterait rien, et l'empreinte se recherche par un index unique. La valeur n'est renvoyée qu'à sa génération ou à son réarmement ; le journal des modifications ne consigne ni elle ni son empreinte.
- **En-tête `Authorization: Bearer`** sur chaque requête, jamais dans l'URL (spécification MCP, *Authorization*, § Access Token Usage). Le reste de cette spécification (OAuth 2.1, découverte), optionnel, n'est pas mis en œuvre : le jeton est généré dans l'administration, comme l'exige le fonctionnel, et déclaré comme en-tête dans le client, ce que les clients courants prennent en charge.
- **Refus expliqué selon la couche du jeton refusé** :
  - jeton d'accès refusé : `401` à toute requête, avant tout traitement MCP, avec la raison et l'action qui permet de reprendre (RFC 6750) : c'est à cette frontière HTTP qu'un client attend, et présente, un échec d'authentification ; aucun défi n'annonce une découverte OAuth, qui n'existe pas ;
  - jeton de session refusé : erreur d'exécution d'outil, que la spécification MCP destine au modèle pour qu'il corrige de lui-même (*Tools*, § Error Handling).
  - **Alternative écartée (erreur d'outil pour un jeton d'accès refusé)** : un défaut d'authentification passerait pour l'échec ordinaire d'un outil, et la requête atteindrait la couche MCP sans être authentifiée.
- **Vérifié par `api` à chaque requête**, seule à accéder à la base : suppression, archivage ou expiration s'appliquent dès la requête suivante. `api` ne connaissant pas l'utilisateur ([Organisation de l'API](api/organisation-de-l-api.md)), c'est `mcp` qui réserve l'administration et les statistiques privées aux jetons qui y ont accès, comme `frontend` les réserve à l'administrateur. `mcp` n'appelle jamais l'administration des jetons, réservée à `frontend`.
  - **Alternative écartée (liste des jetons en cache dans `mcp`)** : une révocation n'agirait qu'au renouvellement du cache, pour économiser un appel interne négligeable à l'échelle d'un utilisateur.
- **Archivage déduit à la lecture** (archivé explicitement, ou expiration passée) : un jeton expiré est refusé dès l'instant de son expiration, et l'archivage automatique n'écrit rien. L'expiration n'est écrite que par la génération et le réarmement, qui donnent une nouvelle valeur : un jeton archivé n'est rendu valable qu'avec une nouvelle valeur, faute de quoi repousser l'expiration d'un jeton expiré le rendrait valable avec l'ancienne.
  - **Alternative écartée (tâche de fond qui archive les jetons expirés)** : un jeton resterait accepté jusqu'au passage suivant, sauf à vérifier aussi l'expiration, ce qui revient à la déduire.
  - Le **réarmement** remplace l'empreinte et l'expiration sur la ligne du jeton : ses relevés restent communs à ses valeurs successives.

## Sessions et suivi de l'utilisation

- **Une session applicative, ouverte par un outil** ([Suivi de l'utilisation](../fonctionnel/agents-ia/suivi-de-l-utilisation.md)), dont l'identifiant est exigé par tout autre outil : MCP n'a plus de session de protocole depuis sa révision du 28 juillet 2026, et l'annonce de cette révision propose, pour porter un état d'un appel à l'autre, un identifiant créé par un outil et repassé par l'agent, ici rendu obligatoire. Les appels d'une conversation sont ainsi regroupés exactement, même pour deux conversations simultanées sur un jeton, et un oubli de l'agent apparaît dans le suivi, refusé et relevé, au lieu de le fausser. L'identifiant n'est pas un secret, le jeton d'accès authentifiant seul.
  - **Alternative écartée (sessions déduites du jeton, du client et de la proximité des appels)** : deux conversations simultanées se mêleraient, le découpage dépendrait d'un délai arbitraire, et aucun objectif ne relierait une conversation à sa demande.
- **Expiration fixe, 24 heures après l'ouverture** (paramétrable), annoncée à l'ouverture, indépendante de celle du jeton d'accès, chaque requête vérifiant les deux : l'agent sait d'emblée jusqu'à quand sa session vaut, et une conversation dépasse rarement une journée.
- **Sessions valables tenues en mémoire par `mcp`**, historique enregistré par `api` : un redémarrage de `mcp` invalide les sessions en cours, ce que le fonctionnel admet, et l'agent en rouvre une. La session précédente qu'une nouvelle prolonge est donc contrôlée sur l'historique.
  - **Alternative écartée (sessions vérifiées en base)** : un appel à `api` de plus par requête, pour survivre à un seul redémarrage de `mcp`, que l'agent surmonte en rouvrant une session.
- **Objectifs datés, jamais réécrits** : un objectif peut être donné à l'ouverture, puis mis à jour par un outil facultatif ; un appel relève du plus récent avant lui. L'historique garde la trace d'une demande qui se précise ou change de sujet, et le bilan se lit objectif par objectif.
- **Chaque appel relevé avec ses paramètres et le message d'erreur renvoyé** : ils disent **en quoi** un outil est mal utilisé, ce que les seuls comptes ne révèlent pas, pour un volume modeste. Le jeton, transmis par l'en-tête, n'y figure jamais ; l'identité du client, que MCP recommande sans l'exiger, est relevée quand la requête la porte.
- **Nouvelle tentative** : un nouvel appel du même outil, sans autre appel entre les deux, quelle qu'ait été l'issue du précédent ; la page suivante d'un résultat n'en est pas une. Rien dans un appel ne distingue une reformulation d'une nouvelle demande : l'indice d'efficacité s'appuie sur les paramètres relevés, et sa définition sera ajustée à l'usage.
- **Retour écrit en texte libre**, par un outil facultatif, sur l'utilisation qu'il désigne par l'identifiant que porte chaque réponse, ou sur sa session : les recommandations d'Anthropic réservent l'auto-évaluation de l'agent aux évaluations, et une note, rare et peu fiable, ferait double emploi avec les mesures du relevé, là où un texte dit ce qu'aucune mesure ne révèle (une fonction absente). Les appels de cet outil et de celui des objectifs n'entrent pas dans l'indice d'efficacité.
- **Historique composé dans le jeton** : sessions, relevés et retours sont supprimés avec lui, et conservés tant qu'il existe, archivé compris ; l'impact de suppression du jeton les annonce ([Suppression des entités : mise en œuvre](ecritures/suppression-des-entites-mise-en-oeuvre.md)).
