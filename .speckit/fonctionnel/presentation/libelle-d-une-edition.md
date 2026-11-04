---
type: Règle métier
title: 'Libellé d''une édition'
description: 'Libellé d''une édition : éditeur, collection, année, ISBN.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Libellé d'une édition

Le libellé d'une édition est construit dynamiquement selon le modèle suivant :

```text
{Éditeur}[ ({Collection})][ [{Année d'édition}]][ - ISBN {ISBN formaté}]
```

- La collection n'apparaît que si elle est renseignée.
- L'année n'apparaît que si elle est connue.
- L'ISBN n'apparaît que s'il est connu, préfixé de `ISBN ` et séparé par ` - `.

**Exemples :**

- `Dargaud` *(aucune collection, année ni ISBN)*
- `Dargaud (Lucky Luke) [1978]`
- `Dargaud (Lucky Luke) [1978] - ISBN 978-2-205-01234-5`
- `Dargaud [1978] - ISBN 978-2-205-01234-5`
