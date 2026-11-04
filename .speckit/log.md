# Journal des changements du .speckit

## 2026-11-04

* **Restructuration** : le `.speckit/` est réécrit au format OKF, découpé en une carte (les `index.md`) et des concepts ; sa lecture est guidée par le chantier ; la forme d'un document dépend de sa nature, référence ou journal (#280).
* **Règle** : un contrôle local de non-régression ne s'exécute avant un push que si le push modifie ce qu'il vérifie ; les captures de référence, seulement pour un changement qui peut modifier le rendu (#280).
