---
type: Règle métier
title: 'Présentation des erreurs'
description: 'Erreurs métier, fonctionnelles et techniques distinguées au premier coup d''œil ; titre d''une erreur, qui nomme la situation rencontrée, le même partout pour une même situation.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-21T12:00:00Z }
---

# Présentation des erreurs

Toute erreur présentée à l'utilisateur doit lui permettre de distinguer **très facilement**, au premier coup d'œil, s'il s'agit :

- d'une **erreur métier** : l'action demandée enfreint une règle de gestion de l'application (ex. une donnée obligatoire manquante, une incohérence entre deux champs, une intention d'acquisition déjà existante pour cet album). L'utilisateur peut la résoudre lui-même en corrigeant sa saisie ou sa demande ;
- d'une **erreur fonctionnelle** : la demande n'enfreint aucune règle de gestion et l'application fonctionne normalement, mais la demande ne peut pas aboutir en l'état des données au moment où elle est traitée. Cas type : l'enregistrement d'une fiche **modifiée entre-temps** (depuis un autre onglet ou un autre appareil), refusé pour ne pas écraser silencieusement cette autre modification. L'utilisateur peut la résoudre lui-même en tenant compte du nouvel état des données. Cette catégorie regroupe les erreurs de même nature ;
- d'une **erreur technique** : l'application n'a pas pu traiter la demande pour une raison indépendante de la saisie de l'utilisateur (dysfonctionnement, service indisponible, etc.).

Cette distinction s'applique à toutes les parties de l'application (consultation, administration, aide contextuelle).

## Titre d'une erreur

Le titre d'une erreur nomme la **situation rencontrée**, en accord avec son message : un titre est un titre, et non le nom générique de sa catégorie, que son icône, sa couleur et son cadre disent déjà. Une même situation porte le **même titre partout** où elle survient, quel que soit l'écran (décision de l'utilisateur, #472) :

| Situation | Titre |
| --- | --- |
| La saisie d'une fiche enfreint une règle de gestion (son enregistrement) | Saisie à corriger |
| L'identifiant ou le mot de passe de la connexion est incorrect | Connexion refusée |
| Une action sans saisie est refusée par une règle de gestion (ex. une intention d'acquisition) | Action refusée |
| La fiche a été modifiée entre-temps | Fiche modifiée entre-temps |
| Une fiche a été supprimée entre-temps (celle que l'on enregistre, une fiche qu'elle cite, celle que l'on consulte) | Fiche supprimée entre-temps |
| Ce qu'emporte une suppression a changé depuis que son dialogue l'a présenté | Conséquences modifiées entre-temps |
| Trop de tentatives de connexion ont échoué | Connexion suspendue |
| La page demandée n'existe pas | Page introuvable |
| Erreur technique | Problème technique |

Une erreur technique porte ainsi partout le titre **« Problème technique »**, qu'elle survienne à l'enregistrement d'une fiche ou ailleurs (liste, fiche de la consultation, bandeau, contrôle) : l'utilisateur la reconnaît d'un écran à l'autre (décision de l'utilisateur, #411).


<!-- À compléter : cas d'usage, user stories, autres règles métier, flux applicatifs, etc. -->
