---
type: Règle métier
title: 'Présentation des erreurs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Présentation des erreurs

Toute erreur présentée à l'utilisateur doit lui permettre de distinguer **très facilement**, au premier coup d'œil, s'il s'agit :

- d'une **erreur métier** : l'action demandée enfreint une règle de gestion de l'application (ex. une donnée obligatoire manquante, une incohérence entre deux champs, une intention d'achat déjà existante pour cet album). L'utilisateur peut la résoudre lui-même en corrigeant sa saisie ou sa demande ;
- d'une **erreur fonctionnelle** : la demande n'enfreint aucune règle de gestion et l'application fonctionne normalement, mais la demande ne peut pas aboutir en l'état des données au moment où elle est traitée. Cas type : l'enregistrement d'une fiche **modifiée entre-temps** (depuis un autre onglet ou un autre appareil), refusé pour ne pas écraser silencieusement cette autre modification. L'utilisateur peut la résoudre lui-même en tenant compte du nouvel état des données. Cette catégorie regroupe les erreurs de même nature ;
- d'une **erreur technique** : l'application n'a pas pu traiter la demande pour une raison indépendante de la saisie de l'utilisateur (dysfonctionnement, service indisponible, etc.).

Cette distinction s'applique à toutes les parties de l'application (consultation, administration, aide contextuelle).


<!-- À compléter : cas d'usage, user stories, autres règles métier, flux applicatifs, etc. -->
