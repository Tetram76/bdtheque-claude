---
type: Choix d'implémentation
title: 'Validation de l''ISBN'
description: 'Contrôle non bloquant du chiffre de vérification de l''ISBN, exposé par l''API ; ISBN corrigé, acceptation enregistrée par l''ISBN accepté, erreurs relevées en mémoire.'
applies_to:
  - 'src/Bdtheque.Domain/Common/IsbnChecksumValidator.cs'
  - 'src/Bdtheque.Domain/Common/IsbnText.cs'
  - 'src/Bdtheque.Api/Endpoints/InvalidIsbnEndpoints.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-11T12:30:00Z }
---

# Validation de l'ISBN

- Le contrôle du chiffre de vérification (ISBN-10 et ISBN-13, [Édition](../../modele-metier/edition.md)) est une fonction du **domaine**, indépendante de l'entité Édition, qui ne s'en sert jamais pour refuser une valeur ([Validation de l'ISBN](../../fonctionnel/regles-metier/validation-de-l-isbn.md), contrôle non bloquant) : indépendante, elle contrôle aussi une saisie en cours, avant que l'édition existe.
- L'**API** en expose le résultat, avec chaque édition et par un point d'entrée de contrôle pendant la saisie : le frontend avertit l'utilisateur sans dépendre du domaine.
  - **Alternative écartée (contrôle refait dans le frontend)** : la même règle serait écrite deux fois.
- L'**ISBN corrigé** ([ISBN au chiffre de contrôle incohérent](../../fonctionnel/regles-metier/validation-de-l-isbn.md#isbn-au-chiffre-de-contrôle-incohérent)) est calculé par la même fonction du domaine, qui remplace le seul caractère de contrôle dans le texte saisi, séparateurs compris ; l'API le donne avec chaque ligne de la liste, et la correction, comme l'acceptation d'une erreur, est une écriture de l'édition, sous la version de son album, que le domaine refuse pour un ISBN sans erreur ou que rien ne corrige.
- **Acceptation enregistrée par l'ISBN accepté**, et non par un indicateur : l'édition dit elle-même pour quel ISBN elle vaut, et un autre ISBN, saisi sur sa fiche, y met fin, comme le veut la règle. Le domaine l'efface dès que l'ISBN change, si bien que l'ISBN accepté est toujours celui de l'édition.
  - **Alternative écartée (indicateur « erreur acceptée »)** : il survivrait à un ISBN modifié et toujours incohérent, qui doit redevenir une erreur à corriger, sauf à le remettre à zéro par chaque écriture de l'ISBN, sans rien pour dire laquelle l'a oublié.
- **Erreurs relevées en mémoire** : aucun SQL ne traduit le contrôle du chiffre de vérification. La liste, comme le nombre de l'accueil, lit les ISBN des éditions que retiennent ses filtres, quelques milliers de textes courts au plus, les contrôle par la fonction du domaine, puis lit sa page par les identifiants des erreurs, dans l'ordre de la liste des éditions.
  - **Alternative écartée (résultat du contrôle enregistré avec l'édition)** : une donnée dérivée de plus à tenir alignée sur l'ISBN et sur la règle, pour une lecture qui reste instantanée à ce volume.
- La **forme enregistrée** de l'ISBN (sans espaces autour, caractère de contrôle en majuscule) est aussi une fonction du domaine, que l'édition applique à l'enregistrement et que le point d'entrée de contrôle renvoie : la saisie reprend le caractère de contrôle en majuscule dès qu'il est tapé ; les espaces autour ne sont retirés qu'à l'enregistrement, pour ne pas gêner la frappe d'un séparateur.
