---
type: Règle métier
title: 'Libellés contextuels sur l''édition'
description: 'Libellés de la date et du montant selon le mode d''acquisition, gratuité.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Libellés contextuels sur l'édition

Le libellé de la date et du montant d'acquisition s'adapte au mode d'acquisition :

| Mode | Libellé de la date | Libellé du montant |
| --- | --- | --- |
| `Achat` | Date d'achat | Prix d'achat |
| `Offerte` | Date d'acquisition | Valeur d'acquisition |
| `Échange` | Date d'acquisition | Valeur d'acquisition |
| `Gagnée` | Date d'acquisition | Valeur d'acquisition |
| `Héritée` | Date d'acquisition | Valeur d'acquisition |

Dans tous les cas, le champ montant est affiché et reste optionnel : même sans transaction financière, une édition peut avoir une valeur marchande connue. Si l'édition est marquée **Gratuite**, le champ montant est désactivé et vidé, de même que la valeur initiale. Une édition achetée (mode `Achat`) ne peut pas être marquée gratuite.
