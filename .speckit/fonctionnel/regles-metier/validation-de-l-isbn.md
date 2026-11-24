---
type: Règle métier
title: 'Validation de l''ISBN'
description: 'ISBN-10 et 13, chiffre de contrôle vérifié sans bloquer l''enregistrement, ISBN corrigé que suppose l''application, erreur acceptée pour un ISBN.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-24T19:33:10Z }
---

# Validation de l'ISBN

Les formats **ISBN-10** et **ISBN-13/EAN-13** sont tous deux supportés. La saisie d'un ISBN vérifie le chiffre de contrôle correspondant afin de détecter les erreurs de frappe. Cette vérification est **non bloquante** : l'utilisateur est averti en cas d'incohérence mais peut enregistrer la valeur telle quelle (certains éditeurs ont publié des albums avec un ISBN erroné).

Le caractère de contrôle « X » d'un ISBN-10 s'écrit en **majuscule** : saisi en minuscule, il est mis en majuscule dès la saisie, et l'ISBN est enregistré ainsi.

## ISBN au chiffre de contrôle incohérent

Les éditions dont l'ISBN a un chiffre de contrôle incohérent, possédées ou visées par une intention d'acquisition, se retrouvent dans une liste de l'administration, à laquelle mène le bloc « À vérifier » de son accueil ([Contenu](../../visuel/administration/contenu.md), [Liste par liste](../../visuel/listes/liste-par-liste.md)) (décisions de l'utilisateur, #613).

- **ISBN corrigé** : l'ISBN que l'application suppose, fait des chiffres saisis, dont seul le chiffre de contrôle est recalculé, dans le même format (ISBN-10 ou ISBN-13) ; ex. `978-2-505-00865-3` a pour ISBN corrigé `978-2-505-00865-1`. Un ISBN que ce calcul ne peut corriger (une longueur qui n'est ni celle d'un ISBN-10 ni celle d'un ISBN-13, un caractère qui n'a pas sa place, un ISBN-13 sans préfixe `978` ou `979`) n'a pas d'ISBN corrigé.
- **Corriger** : remplace l'ISBN saisi par l'ISBN corrigé.
- **Accepter l'erreur** : l'édition, ou l'intention qui la vise, n'est plus présentée comme une erreur, l'ISBN étant tenu pour juste tel qu'il est (ex. imprimé ainsi par l'éditeur). Une erreur acceptée n'est plus comptée par l'accueil de l'administration, ni présentée par défaut par la liste, dont un filtre la montre.
  - L'acceptation vaut pour l'**ISBN accepté** : un ISBN modifié ensuite qui reste incohérent est de nouveau une erreur à corriger.
  - **Ne plus accepter l'erreur** la rend de nouveau à corriger.
