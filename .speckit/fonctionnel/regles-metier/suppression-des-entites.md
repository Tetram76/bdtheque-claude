---
type: Règle métier
title: 'Suppression des entités'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Suppression des entités

Toute suppression est soumise à la **confirmation** de l'utilisateur. Son effet dépend de la nature de chaque lien qui unit l'entité supprimée au reste des données :

- **Référence** — une autre fiche désigne l'entité par l'un de ses attributs : la suppression est **refusée** tant que la référence existe (erreur métier indiquant l'impact, cf. ci-dessous).
- **Association** — simple lien sans donnée propre : la suppression est **autorisée** ; les associations disparaissent avec l'entité.
- **Composition** — élément qui fait partie de l'entité : il est **supprimé avec elle**.

La suppression n'aboutit que si aucun lien de type **Référence** ne la bloque.

**Impact de la suppression** : le message de confirmation — comme l'erreur d'une suppression refusée — indique l'impact de la suppression, c'est-à-dire le **nombre de fiches impactées, distingué par type d'entité** (ex. « utilisé par 12 albums et 3 séries », « supprime 2 éditions et 5 visuels ») : fiches qui perdent leur association avec l'entité supprimée, fiches supprimées avec elle, ou fiches qui bloquent la suppression.

| Entité supprimée | Lien | Type | Effet |
| --- | --- | --- | --- |
| Genre | Genres d'albums et de séries | Association | Le genre est retiré de ces albums et séries. |
| Univers | Univers d'albums et de séries | Association | L'univers est retiré de ces albums et séries. |
| Univers | Univers parent de sous-univers | Référence | Refusée tant que l'univers a des sous-univers. |
| Auteur | Contributions sur des albums ou des séries | Référence | Refusée tant que l'auteur est crédité. |
| Éditeur | Éditeur d'éditions | Référence | Refusée. |
| Éditeur | Éditeur template d'une série | Référence | Refusée. |
| Éditeur | Ses collections éditeur | Composition | Supprimées avec lui. |
| Collection éditeur | Collection d'éditions | Référence | Refusée. |
| Collection éditeur | Collection template d'une série | Référence | Refusée. |
| Série | Albums rattachés | Référence | Refusée tant que la série contient des albums. |
| Série | Ses contributions template | Composition | Supprimées avec elle. |
| Album | Ses éditions (avec leurs visuels et intentions d'achat), ses contributions, son intention d'achat | Composition | Supprimés avec lui. |
| Édition | Ses visuels, son intention d'achat | Composition | Supprimés avec elle. Si c'était la dernière édition possédée de l'album, la confirmation signale que l'album sort de la collection. |
| Intention d'achat portant sur une édition | L'édition visée (non possédée), avec ses visuels | Composition | Supprimée avec elle : une édition non possédée n'existe que par son intention (cf. [Intention d'achat](/fonctionnel/regles-metier/intention-d-achat.md)). |
| Toute fiche | Désignée par un critère ou par la valeur par défaut d'une variable d'une recherche enregistrée (cf. [Recherche avancée](/fonctionnel/recherche-avancee/recherche-avancee.md)) | Référence | Refusée tant qu'une recherche enregistrée la désigne, elle ou une fiche supprimée avec elle. |
| Jeton d'accès | Son historique d'utilisation et ses retours écrits (cf. [Suivi de l'utilisation](/fonctionnel/agents-ia/suivi-de-l-utilisation.md)) | Composition | Supprimés avec lui. Pour conserver cet historique, le jeton est archivé plutôt que supprimé (cf. [Accès par jeton](/fonctionnel/agents-ia/acces-par-jeton.md)). |
| Visuel d'édition, Contribution, Intention d'achat portant sur l'album, Recherche enregistrée | — | — | Hors la ligne « Toute fiche », suppression sans autre effet (le média d'un visuel est supprimé avec lui). |
