---
type: Choix d'implémentation
title: 'Validation de l''ISBN'
description: 'Contrôle non bloquant du chiffre de vérification de l''ISBN, exposé par l''API.'
applies_to:
  - 'src/Bdtheque.Domain/Common/IsbnChecksumValidator.cs'
  - 'src/Bdtheque.Domain/Common/IsbnText.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-11T12:30:00Z }
---

# Validation de l'ISBN

- Le contrôle du chiffre de vérification (ISBN-10 et ISBN-13, [Édition](../../modele-metier/edition.md)) est une fonction du **domaine**, indépendante de l'entité Édition, qui ne s'en sert jamais pour refuser une valeur ([Validation de l'ISBN](../../fonctionnel/regles-metier/validation-de-l-isbn.md), contrôle non bloquant) : indépendante, elle contrôle aussi une saisie en cours, avant que l'édition existe.
- L'**API** en expose le résultat, avec chaque édition et par un point d'entrée de contrôle pendant la saisie : le frontend avertit l'utilisateur sans dépendre du domaine.
  - **Alternative écartée (contrôle refait dans le frontend)** : la même règle serait écrite deux fois.
- La **forme enregistrée** de l'ISBN (sans espaces autour, caractère de contrôle en majuscule) est aussi une fonction du domaine, que l'édition applique à l'enregistrement et que le point d'entrée de contrôle renvoie : la saisie reprend le caractère de contrôle en majuscule dès qu'il est tapé ; les espaces autour ne sont retirés qu'à l'enregistrement, pour ne pas gêner la frappe d'un séparateur.
