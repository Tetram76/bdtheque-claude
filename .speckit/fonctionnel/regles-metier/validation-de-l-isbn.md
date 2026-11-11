---
type: Règle métier
title: 'Validation de l''ISBN'
description: 'ISBN-10 et 13, chiffre de contrôle vérifié sans bloquer l''enregistrement.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-11T12:30:00Z }
---

# Validation de l'ISBN

Les formats **ISBN-10** et **ISBN-13/EAN-13** sont tous deux supportés. La saisie d'un ISBN vérifie le chiffre de contrôle correspondant afin de détecter les erreurs de frappe. Cette vérification est **non bloquante** : l'utilisateur est averti en cas d'incohérence mais peut enregistrer la valeur telle quelle (certains éditeurs ont publié des albums avec un ISBN erroné).

Le caractère de contrôle « X » d'un ISBN-10 s'écrit en **majuscule** : saisi en minuscule, il est mis en majuscule dès la saisie, et l'ISBN est enregistré ainsi.
