---
type: Modèle métier
title: 'Relations et cardinalités'
description: 'Relations et cardinalités de chaque entité, contraintes des intentions d''achat, du suivi de l''outil et des visites.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Relations et cardinalités

## Album — relations

| Relation | Cardinalité | Remarques |
| --- | --- | --- |
| Album → Édition | 0..n | Un album peut n'avoir aucune édition (non encore publié). |
| Album → Série | 0..1 | Un album peut n'appartenir à aucune série (album standalone). L'attribut `Hors-série` est indépendant : un album peut être hors-série tout en appartenant à une série. |
| Album → Contribution | 0..n | Un album peut avoir plusieurs contributions. Chaque contribution a un rôle et un artiste obligatoire. |
| Album → Genre | 0..n | Genres propres à l'album. Si l'album appartient à une série, les genres affichés sont l'union des genres de l'album et de ceux de la série. |
| Album → Univers | 0..n | Univers propres à l'album. Si l'album appartient à une série, les univers affichés sont l'union des univers de l'album et de ceux de la série. |

## Édition — relations

| Relation | Cardinalité | Remarques |
| --- | --- | --- |
| Édition → Éditeur | 1 | Une édition est toujours publiée par exactement un éditeur. |
| Édition → Collection éditeur | 0..1 | Optionnelle. Si renseignée, doit appartenir à l'éditeur de l'édition. |

## Série — relations

| Relation | Cardinalité | Remarques |
| --- | --- | --- |
| Série → Genre | 0..n | |
| Série → Univers | 0..n | |
| Série → Contribution | 0..n | Auteurs de la série, et template : recopiées sur un album rattaché à la série si l'album n'en a encore aucune. |
| Série → Éditeur | 0..1 | Éditeur de la série, et template pour les nouvelles éditions. |
| Série → Collection éditeur | 0..1 | Collection de la série, et template pour les nouvelles éditions. Si renseignée, l'éditeur de la série doit l'être aussi, et la collection doit lui appartenir. |

> Les contributions, l'éditeur et la collection éditeur d'une série sont **autant des données de la série que des templates** : ils décrivent la série (fiche, recherche, bibliographie) au même titre que ses autres attributs, et servent en outre de point de départ à la saisie de ses albums et de leurs éditions. Les autres valeurs template (reliure, orientation, sens de lecture, format, catégorie, état, couleur) ne sont que des templates.

## Univers — relations

| Relation | Cardinalité | Remarques |
| --- | --- | --- |
| Univers → Univers parent | 0..1 | Relation récursive permettant une hiérarchie d'univers. Acyclicité obligatoire : un univers ne peut pas être son propre ancêtre (auto-référence interdite, cycles interdits). |

## Intention d'achat — relations

| Relation | Cardinalité | Remarques |
| --- | --- | --- |
| Intention d'achat → Album | 0..1 | Renseignée si l'intention porte sur un album (toute édition acceptable). |
| Intention d'achat → Édition | 0..1 | Renseignée si l'intention porte sur une édition spécifique. |

> **Contraintes :**
>
> - Une intention d'achat doit cibler exactement l'un des deux : un Album ou une Édition, jamais les deux, jamais aucun.
> - Une cible ne fait l'objet que d'**une seule** intention d'achat : un album est visé par au plus une intention, une édition par au plus une intention.
> - Pour un même album, l'intention porte **soit** sur l'album, **soit** sur ses éditions, jamais les deux : un album visé par une intention ne peut pas avoir en même temps une de ses éditions visée par une autre intention, et réciproquement.
> - Un album visé par une intention portant sur lui-même n'a **aucune édition**, possédée ou non (voir [Intention d'achat](/fonctionnel/regles-metier/intention-d-achat.md)).
> - Une édition **possédée** (`Mode d'acquisition` renseigné) ne peut pas être visée par une intention d'achat.

## Jeton d'accès et suivi de l'outil — relations

| Relation | Cardinalité | Remarques |
| --- | --- | --- |
| Jeton d'accès → Session | 0..n | Sessions ouvertes par le jeton ; supprimées avec lui (voir [Suppression des entités](/fonctionnel/regles-metier/suppression-des-entites.md)). |
| Session → Session précédente | 0..1 | Session que la session prolonge (voir [Suivi de l'utilisation](/fonctionnel/agents-ia/suivi-de-l-utilisation.md)) ; ouverte avec le même jeton d'accès. Les sessions successives d'une conversation se relient ainsi de proche en proche. |
| Session → Objectif de session | 0..n | Objectifs successifs, dans l'ordre de leur date ; aucun si l'agent n'en a jamais donné. |
| Jeton d'accès → Utilisation de l'outil | 0..n | Utilisations faites avec le jeton ; supprimées avec lui. Une requête refusée faute de jeton d'accès valable n'est rattachée à aucun jeton et n'est pas relevée. |
| Utilisation de l'outil → Session | 0..1 | Session sous laquelle l'utilisation a été faite ; absente pour une utilisation refusée faute de jeton de session valable, rattachée à son seul jeton d'accès. |
| Session → Retour écrit | 0..n | Retours écrits faits sous la session. |
| Retour écrit → Utilisation de l'outil | 0..1 | Utilisation que le retour commente ; absente pour un retour qui ne porte sur aucune utilisation (ex. une fonction absente). |

> **Contraintes :**
>
> - L'utilisation qu'un retour écrit commente appartient à la même session que lui, et une utilisation rattachée à une session l'est au même jeton d'accès que cette session.
> - Sessions, objectifs, utilisations et retours écrits ne sont jamais modifiés après leur enregistrement ; ils ne disparaissent qu'avec le jeton d'accès qui porte la session. Ce ne sont pas des fiches : leur enregistrement n'est pas consigné au journal des modifications.

## Visite de l'interface web — relations

| Relation | Cardinalité | Remarques |
| --- | --- | --- |
| Visite → Utilisation d'une fonction | 1..n | Utilisations faites au cours de la visite, dans l'ordre de leur date : le parcours de la visite. |

> **Contraintes :** les visites et leurs utilisations ne sont jamais modifiées après leur enregistrement. Une visite d'un visiteur public est supprimée avec ses utilisations 25 mois après son début, une visite de l'administrateur est conservée sans limite (voir [Mesure d'audience de l'interface web](/fonctionnel/mesure-audience.md)). Ce ne sont pas des fiches : leur enregistrement n'est pas consigné au journal des modifications. Une visite ne porte aucune donnée qui identifie l'utilisateur ou son appareil, ou permette de le reconnaître d'une visite à l'autre : de l'appareil, elle ne retient que sa catégorie et le moteur de rendu de son navigateur, avec sa version.

---
