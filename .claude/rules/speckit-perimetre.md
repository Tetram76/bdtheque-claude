---
paths:
  - ".speckit/*.md"
---

# Périmètre des fichiers .speckit

Avant tout ajout ou modification, vérifier dans quel fichier l'information appartient.

Le périmètre de chaque fichier est défini dans `AGENTS.md` (vue d'ensemble) et en tête de chaque fichier ; seuls les exemples de placement ci-dessous complètent ces définitions.

## Exemples de placements

- Ordre de tri des albums dans une liste → `fonctionnel.md` (règle d'affichage)
- Mécanique de calcul d'une clé de tri (insensibilité à la casse, gestion des frontières de mot, etc.) → code + tests, pas `fonctionnel.md` (implémentation d'une règle déjà énoncée)
- Attribut `hors_serie` sur Album → `modele-metier.md` (attribut d'entité)
- Algorithme Random Forest pour l'estimation de valeur → `fonctionnel.md` uniquement (choix métier imposé par l'utilisateur, pas une contrainte technique) ; la bibliothèque qui l'implémente (ML.NET) et les versions de la stack → `choix-implementation.md`
- Persistance des enums en entier explicite plutôt qu'en chaîne → `choix-implementation.md` (alternative — la chaîne — explicitement envisagée puis écartée, avec argumentation détaillée)
- Convention de nommage des branches → `gestion-projet.md`
