---
type: Règle métier
title: 'Libellé d''une édition'
description: 'Libellé d''une édition : éditeur, collection, année, ISBN ; éditeur du libellé menant à son site dans la consultation.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-19T16:30:00Z }
---

# Libellé d'une édition

Le libellé d'une édition est construit dynamiquement selon le modèle suivant :

```text
{Éditeur}[ ({Collection})][ [{Année d'édition}]][ - ISBN {ISBN formaté}]
```

- La collection n'apparaît que si elle est renseignée.
- L'année n'apparaît que si elle est connue.
- L'ISBN n'apparaît que s'il est connu, préfixé de `ISBN ` et séparé par ` - `.
- L'ISBN est formaté avec ses tirets, placés selon les plages de l'Agence internationale de l'ISBN ; faute de pouvoir le formater (plages indisponibles ou illisibles, ISBN qu'aucune plage ne place), il est présenté tel qu'enregistré, jamais omis. Une indisponibilité des plages est signalée dans le journal de l'application.
- Un ISBN est d'un seul bloc : où qu'il soit affiché, dans un libellé avec sa mention `ISBN ` comme seul (détail d'une édition), il n'est jamais coupé en fin de ligne, ni de sa mention, ni à l'un de ses tirets.
- **Éditeur du libellé, lien vers son site** : dans la consultation, partout où le libellé d'une édition n'est pas lui-même un lien (édition à la une du dashboard ; sur la fiche d'un album, édition visée par une intention d'acquisition, légende de la visionneuse), le nom de son éditeur mène à son site web lorsqu'il est connu, signalé comme un lien qui fait sortir de l'application ([Liens qui font sortir de l'application](../../visuel/charte-graphique/liens-qui-font-sortir-de-l-application.md)), comme tout éditeur ([Structure de l'application](../structure-de-l-application.md)) ; le reste du libellé reste en texte. La référence est ce que voit l'utilisateur, plusieurs informations sur une même ligne, dont l'éditeur, et non la façon dont le libellé est construit :

  > si le nom complet de l'édition n'est pas un lien lui même, je ne vois aucune raison valable pour que le nom de l'éditeur ne soit pas un lien […] la référence c'est ce que voit l'utilisateur, pas comment on le construit ! (décision de l'utilisateur, #467)

  **Sauf là où la ligne « Éditeur » qui suit le libellé mène déjà au site** : le titre de chaque édition, sur la fiche d'un album, reste en texte, le détail de l'édition donnant son éditeur juste en dessous ([Contenu](../../visuel/fiche-album/contenu.md)) :

  > c'est un rare cas où l'editeur n'a pas besoin d'être présenté en lien même si le nom de l'édition ne l'est pas non plus : on a déjà la ligne editeur plus bas avec le lien (décision de l'utilisateur, #467)

  L'administration n'est pas concernée : l'entrée d'une ligne de ses listes n'est jamais un lien ([Principes communs](../../visuel/listes/principes-communs.md)).

**Exemples :**

- `Dargaud` *(aucune collection, année ni ISBN)*
- `Dargaud (Lucky Luke) [1978]`
- `Dargaud (Lucky Luke) [1978] - ISBN 978-2-205-01234-5`
- `Dargaud [1978] - ISBN 978-2-205-01234-5`
