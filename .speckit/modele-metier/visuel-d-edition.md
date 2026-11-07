---
type: Modèle métier
title: 'Visuel d''édition'
description: 'Type, fichier et ordre d''un visuel d''édition.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/EditionVisual.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/EditionVisualConfiguration.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Visuel d'édition

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Type de visuel | énuméré | oui | Couverture / Dédicace / Page de garde / Planche / 4e de couverture. `Couverture` proposé par défaut à la saisie (formulaire pré-rempli), non porté par la base (aucune clause `DEFAULT`). |
| Fichier / URL | texte | oui | Référence au média stocké. Donnée technique, interne à l'application : elle n'est ni présentée, ni interrogeable par la recherche avancée ([Données interrogées](../fonctionnel/recherche-avancee/donnees-interrogees.md)), ni consignée au journal des modifications. |
| Ordre d'affichage | entier | oui | Rang au sein des visuels du même type, ajustable manuellement par l'utilisateur. |
