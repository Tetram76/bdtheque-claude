---
type: Choix d'implémentation
title: 'Validation de l''ISBN'
description: 'Contrôle non bloquant du chiffre de vérification de l''ISBN, exposé par l''API.'
applies_to:
  - 'src/Bdtheque.Domain/Common/IsbnChecksumValidator.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Validation de l'ISBN

- Le contrôle du chiffre de vérification (ISBN-10 / ISBN-13, cf. [Édition](/modele-metier/edition.md)) est isolé dans `Bdtheque.Domain.Common.IsbnChecksumValidator`, utilisable indépendamment de l'entité `Édition`. Conformément à [Validation de l'ISBN](/fonctionnel/regles-metier/validation-de-l-isbn.md) (contrôle non bloquant), `Edition.SetIsbn` ne rejette jamais une valeur incorrecte. L'API expose le résultat de ce validateur dans le formulaire d'administration d'une édition et, en consultation, pour chaque édition que présente la fiche de son album (`IsbnChecksumValid`, absent sans ISBN ; le résumé d'une édition, qui ne sert qu'à construire son libellé, ne le porte pas), et par un point d'entrée de contrôle utilisable pendant la saisie (`GET /admin/editions/isbn-check?isbn=…`) : le frontend avertit l'utilisateur sans empêcher l'enregistrement, et sans référencer `Bdtheque.Domain`.
