# Journal des changements du .speckit

## 2026-11-05

* **Choix** : libellés des albums et des éditions construits par le frontend, tirages d'une page fixés par les tests, applications de test propres à un écran qui dépend de toute la collection, contrôle local des captures dans l'image de Playwright (#284).
* **Règle** : le déplacement ou le renommage d'un concept ou d'une section met aussi à jour, à la fusion de sa Pull Request, les renvois des tickets ouverts qui le désignent, comme un nom que le `.speckit/` change (#282).
* **Règle** : la forme de `log.md` est celle d'OKF : entrées regroupées par date, les plus récentes en premier (#282).

## 2026-11-04

* **Restructuration** : le `.speckit/` est réécrit au format OKF, découpé en une carte (les `index.md`) et des concepts ; sa lecture est guidée par le chantier ; la forme d'un document dépend de sa nature, référence ou journal (#280).
* **Règle** : un contrôle local de non-régression ne s'exécute avant un push que si le push modifie ce qu'il vérifie ; les captures de référence, seulement pour un changement qui peut modifier le rendu (#280).
* **Socle allégé** : les règles qui ne valent qu'à un moment du travail (Pull Request, revue, merge, version, relevé des versions dû) sortent du socle ; la table des moments d'`AGENTS.md` les désigne et le hook du dépôt les rappelle (#280).
