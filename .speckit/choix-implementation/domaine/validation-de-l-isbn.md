---
type: Choix d'implémentation
title: 'Validation de l''ISBN'
description: 'Contrôle non bloquant du chiffre de vérification de l''ISBN, exposé par l''API.'
applies_to:
  - 'src/Bdtheque.Domain/Common/IsbnChecksumValidator.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Validation de l'ISBN

- Le contrôle du chiffre de vérification (ISBN-10 et ISBN-13, [Édition](../../modele-metier/edition.md)) est une fonction du **domaine**, indépendante de l'entité Édition, qui ne s'en sert jamais pour refuser une valeur ([Validation de l'ISBN](../../fonctionnel/regles-metier/validation-de-l-isbn.md), contrôle non bloquant) : indépendante, elle contrôle aussi une saisie en cours, avant que l'édition existe.
- L'**API** en expose le résultat, avec chaque édition et par un point d'entrée de contrôle pendant la saisie : le frontend avertit l'utilisateur sans dépendre du domaine.
  - **Alternative écartée (contrôle refait dans le frontend)** : la même règle serait écrite deux fois.
