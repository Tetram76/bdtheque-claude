# Charte graphique

Ce fichier décrit l'identité visuelle de l'application, c'est-à-dire ce que partagent tous ses thèmes : thème de base et ton général. Comme tout le dossier `visuel/`, il n'est lu que lorsque le chantier entrepris le nécessite (cf. `AGENTS.md` § Règles de consultation). L'exigence de rendu est posée par `fonctionnel.md` § Design et charte graphique ; la présentation de chaque écran est décrite dans son propre fichier de ce dossier, avec ses maquettes.

---

## Portée

**Chaque page** de l'application, quelle que soit sa partie (Consultation, Administration, aide contextuelle, `fonctionnel.md` § Structure de l'application), **suit l'identité visuelle du thème actif** : ses couleurs, sa typographie et ses éléments graphiques.

La charte ci-dessous est issue des maquettes du dashboard (cf. [`dashboard.md`](dashboard.md)), premier écran conçu, dont elle réunit ce que partagent toutes les alternatives : elle s'applique à la partie **Consultation**.

La partie **Administration** suit cette identité dans un sens plus sobre : un espace technique, qui privilégie l'efficacité et le fonctionnel sans être impersonnel, sans fantaisie ni désordre (cf. [`administration.md`](administration.md) § Ton).

## Thème de base

Le rendu repose sur un **thème de base neutre**, qui définit les **contrats** que tout thème respecte. Aucune des alternatives des maquettes (cf. [`dashboard.md`](dashboard.md)), pas même « Cases et bulles » (violine ou mûre), n'est retenue à ce stade, et aucune ne sert de base au rendu : un thème issu des maquettes ne fait que s'appuyer sur ce thème de base.

## Ton général

- **Joyeux et chaleureux**, ni sérieux ni sévère : l'application présente une collection de BD, avec un clin d'œil à l'univers de la BD.
- **Ludique.**
- **Non conventionnel, borderline mais pas excentrique.**
- **Sans pastiche de comics.**

## Apparence propre à chaque alternative

Couleurs, typographie et éléments graphiques ne sont pas communs : chaque alternative des maquettes a les siens, décrits avec elle dans [`dashboard.md`](dashboard.md) § Alternatives. Seule la règle suivante vaut pour toutes : le **champ de recherche** et le **bouton d'aide** sont toujours visibles dans l'en-tête.

## Maquettes

Toutes les maquettes sont réalisées dans **un seul canevas** *Design* de Claude, <https://claude.ai/artifact/57fW7eokzrDkJ1MwVPMZJA>, avec **une page par écran** de l'application. Ses sources sont conservées dans [`maquettes/`](maquettes/), un sous-dossier par écran ; [`maquettes/canvas.json`](maquettes/canvas.json) en est l'index : il donne les pages, et la disposition et le titre des planches. Les maquettes ne s'ouvrent qu'à travers ce canevas. Leurs données sont fictives. Chaque maquette est **déclinée dans le visuel de chaque thème** en lice, une planche par thème. Le fichier de chaque écran indique ses sources.
