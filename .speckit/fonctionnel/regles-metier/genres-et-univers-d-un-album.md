---
type: Règle métier
title: 'Genres et univers d''un album'
description: 'Genres et univers affichés d''un album : union avec ceux de sa série.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Genres et univers d'un album

Les genres et univers affichés pour un album dépendent de son rattachement à une série. La même règle s'applique aux deux :

- **Album dans une série** : les valeurs affichées sont l'**union sans doublons** de celles de l'album et de celles de la série.
- **Album sans série** : seules les valeurs propres à l'album sont affichées.

Les genres et univers sont optionnels sur l'album comme sur la série.
