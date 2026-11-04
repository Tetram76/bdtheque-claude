---
type: Plan d'implémentation
title: 'Phase 4 — Statistiques, devises et estimation de valeur'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Phase 4 — Statistiques, devises et estimation de valeur

Dashboard public et statistiques (sur la collection uniquement) ; conversion des montants en euros courants (taux fixes, taux variables récupérés auprès de Frankfurter et mis en cache) et en euros du jour (coefficients d'inflation de l'INSEE, mis en cache) ; valeur estimée des éditions (Random Forest, ML.NET), recalculée à partir de l'état courant de la base ([Calcul des estimations](/fonctionnel/valorisation/calcul-des-estimations.md)).

Découpage et version à établir à l'ouverture de la phase. La qualité de l'estimation de valeur ne peut être évaluée que sur des données réelles (collection issue de l'application existante).

- **Note — recherche avancée** : les montants en euros courants et en euros du jour et la valeur estimée des éditions deviennent des critères de la recherche avancée ([Recherche avancée](/fonctionnel/recherche-avancee/recherche-avancee.md)), avec leurs comparaisons (version choisie par critère, montant non convertible), à une valeur comme à un autre montant.
- **Note — couverture d'un album** : une statistique qui présente un album par sa couverture (ex. doyen et benjamin) est une donnée de la collection, qui ne retient que les éditions possédées ([Couverture d'un album](/fonctionnel/regles-metier/couverture-d-un-album.md)) ; l'expression de la couverture ([Organisation de l'API](/choix-implementation/api/organisation-de-l-api.md), cartes d'album) ne couvre encore que la variante sans filtre.
