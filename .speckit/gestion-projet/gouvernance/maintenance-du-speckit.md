---
type: Gouvernance
title: 'Maintenance du .speckit'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Maintenance du .speckit

- Les fichiers `.speckit/` sont des **documents vivants** : l'agent peut les restructurer à tout moment (fusion, split, déplacement de sections, création de nouvelles sections) si cela améliore leur clarté ou leur cohérence.
- Toute restructuration est faite sans validation préalable, dans le même esprit d'autonomie qui régit les décisions techniques. Elle porte sur la **forme** uniquement : elle ne modifie, n'ajoute ni ne retire aucun contenu dont l'utilisateur est seul décisionnaire (cf. [Qui décide du contenu](/gestion-projet/gouvernance/gouvernance-documentaire.md#qui-décide-du-contenu) ci-dessus).
- **Taille d'un fichier** : un fichier du `.speckit/` ne dépasse pas **40 Ko**, pour que l'agent puisse le lire d'un seul tenant. Au-delà, il est découpé : le fichier devient un dossier du même nom, dont le fichier `index.md` expose le périmètre et liste les fichiers, chacun consacré à un thème.
- **Renvois** : un renvoi désigne une section par son titre (`§ Titre`). Sans fichier, il désigne une section du même fichier ; sinon, le fichier est désigné par son chemin depuis la racine du `.speckit/`, y compris au sein d'un même dossier (ex. [Suppression des entités](/fonctionnel/regles-metier/suppression-des-entites.md)), et un dossier entier par son nom (`fonctionnel/`). Tout déplacement ou renommage d'une section met à jour les renvois qui la désignent, dans le `.speckit/` comme dans le reste du dépôt (code, tests, configuration, skills).
