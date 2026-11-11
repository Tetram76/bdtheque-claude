---
type: Choix d'implémentation
title: 'Erreurs métier, fonctionnelles et techniques'
description: 'Erreurs métier par code de règle, réponses ProblemDetails en trois catégories, traduction des violations de contraintes en base.'
applies_to:
  - 'src/Bdtheque.Api/Errors/**'
  - 'src/Bdtheque.Domain/Common/DomainRule*.cs'
  - 'src/Bdtheque.Contracts/Errors/**'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-11T11:16:20Z }
---

# Erreurs métier, fonctionnelles et techniques

- **Une violation de règle métier est une exception dédiée, porteuse d'un code de règle stable** ([Présentation des erreurs](../../fonctionnel/presentation-des-erreurs.md)) : le frontend en produit le texte par sa localisation, le message de l'exception ne servant qu'aux journaux. Un code publié n'est jamais renommé ni réaffecté (clé de localisation).
  - **Alternative écartée (exceptions .NET standard)** : levées aussi par le framework et par les erreurs de programmation, elles ne distinguent pas une erreur métier.
  - **Alternative écartée (type résultat)** : un résultat à propager à travers chaque setter et chaque appelant, pour un gain nul : une violation interrompt toujours l'opération, et l'API la traduit en une réponse unique.
- **Métier : ce qu'une saisie peut provoquer et que l'utilisateur corrige ; technique : ce qu'aucune saisie ne peut provoquer**, quelle que soit la couche qui le détecte (une image illisible que seule l'API vérifie est une erreur métier).
- **Réponses ProblemDetails (RFC 9457) dont le `type` est l'une des trois catégories, et aucune autre** : c'est tout ce dont les clients ont besoin pour la présentation distinctive exigée ; le code HTTP précise la nature de l'erreur dans sa catégorie (métier `422` avec le code de règle ; fonctionnelle `409` ou `404`, la demande ne pouvant aboutir en l'état des données ; technique `500`, sans détail interne). Seul le bilan de santé, que lit l'orchestrateur et jamais le frontend, garde son format.
  - **Deux points centraux, et deux seulement**, produisent ces réponses, aucun endpoint n'en construisant : le gestionnaire d'exceptions, et le service ProblemDetails pour toute réponse d'erreur émise sans corps par le framework (liaison de requête, route inconnue), classée technique, le frontend construisant les requêtes. Un `type` hors des trois catégories est remplacé par « technique ».
  - Le gestionnaire journalise lui-même les erreurs techniques : depuis .NET 10, le middleware ne journalise plus une exception déclarée traitée, et la réponse n'en révèle rien.
- **Violations de contraintes en base classées selon la nature de l'échec**, que distingue le code d'erreur PostgreSQL, et non selon le seul nom de la contrainte (une même clé étrangère échoue côté référençant à une écriture, côté référencé à une suppression), qu'elles soient levées à l'enregistrement ou à la validation de la transaction (contrainte unique différable, [Unicité vérifiée à la validation](../persistance/unicite-verifiee-a-la-validation.md)) :
  - **index unique** → erreur métier, par une correspondance index → code de règle, qui couvre **tous** les index uniques du modèle, contraintes uniques différables comprises (un test le vérifie dans les deux sens), hors les tables que seule l'application écrit, où une violation révèle une erreur de programmation. Une règle d'unicité ne peut pas être vérifiée par le domaine, qui ne voit pas les autres lignes ;
  - **texte trop long** → erreur métier : les longueurs maximales ne sont déclarées que dans le schéma, sans copie dans le domaine, et une seule traduction couvre tout champ texte, présent ou futur ;
  - **suppression bloquée par une référence** → refus de suppression avec l'impact recalculé ([Suppression des entités : mise en œuvre](suppression-des-entites-mise-en-oeuvre.md)) ; hors suppression, sur les fiches désignées par une recherche enregistrée, erreur métier que l'utilisateur résout en modifiant d'abord la recherche ([Recherches enregistrées : mise en œuvre](../recherche-avancee/recherches-enregistrees-mise-en-oeuvre.md)) ; ailleurs, un contrôle manquant, donc une erreur technique ;
  - **clé étrangère à une création ou une modification** → erreur fonctionnelle : la fiche référencée a été supprimée entre-temps.
- **Pas de validation déclarative des contrats** : le domaine est l'unique source des règles de saisie.
  - **Alternative écartée (validation native des Minimal API)** : elle répondrait en `400` sans code de règle, une erreur métier y serait indiscernable d'une erreur technique, et chaque règle serait écrite deux fois.
