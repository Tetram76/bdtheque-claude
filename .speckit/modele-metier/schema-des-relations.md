---
type: Modèle métier
title: 'Schéma des relations'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Schéma des relations

```mermaid
erDiagram
    SERIE |o--o{ ALBUM : "contient"
    ALBUM ||--o{ EDITION : "publiée en"
    ALBUM |o--o{ CONTRIBUTION : "créé par"
    AUTEUR ||--o{ CONTRIBUTION : "contribue"
    SERIE |o--o{ CONTRIBUTION : "template"
    SERIE }o--|o EDITEUR : "template éditeur"
    SERIE }o--|o COLLECTION_EDITEUR : "template collection"
    ALBUM }o--o{ GENRE : "catégorisé"
    ALBUM }o--o{ UNIVERS : "rattaché à"
    SERIE }o--o{ GENRE : "catégorisé"
    SERIE }o--o{ UNIVERS : "rattaché à"
    UNIVERS |o--o{ UNIVERS : "contient"
    EDITION }o--|| EDITEUR : "publiée par"
    EDITION }o--|o COLLECTION_EDITEUR : "classée dans"
    EDITEUR ||--o{ COLLECTION_EDITEUR : "propose"
    EDITION ||--o{ VISUEL_EDITION : "illustrée par"
    INTENTION_ACHAT }o--|o ALBUM : "cible (album)"
    INTENTION_ACHAT }o--|o EDITION : "cible (édition)"
```

> La **Collection utilisateur** n'est pas une entité en base. Elle est définie par le filtre `Mode d'acquisition IS NOT NULL` sur les éditions.
>
> **Contrainte (Contribution) :** Une contribution appartient à exactement l'un des deux : un Album ou une Série. Les deux références ne peuvent pas être nulles simultanément, ni renseignées toutes les deux.

---
