---
paths:
  - ".speckit/*.md"
---

# Périmètre des fichiers .speckit

Avant tout ajout ou modification, vérifier dans quel fichier l'information appartient.

| Fichier | Contient | Ne contient PAS |
| --- | --- | --- |
| `modele-metier.md` | Entités, attributs, relations, cardinalités, contraintes d'intégrité | Règles d'affichage, comportement applicatif, choix techniques |
| `fonctionnel.md` | Règles métier applicatives, règles d'affichage/tri, cas d'usage, flux, UX, et tout élément technique explicitement imposé par l'utilisateur comme règle métier (ex. un algorithme retenu) | Détails d'implémentation laissés à la discrétion de l'agent : structure des données, choix techniques, mécanique de calcul |
| `contraintes-techniques.md` | Stack, frameworks, algorithmes, déploiement, sécurité — **imposés** (par l'utilisateur, des normes/lois, ou une réalité externe non négociable) | Tout choix technique fait par l'agent de sa propre initiative (→ `choix-implementation.md`) |
| `choix-implementation.md` | Choix techniques retenus par l'agent **de sa propre initiative**, délibérés entre options ou non (architecture, bibliothèque, algorithme non imposé, etc.) | Une contrainte imposée par l'utilisateur ou une réalité externe (→ `contraintes-techniques.md`) |
| `gestion-projet.md` | Branches, commits, CI/CD, outillage | Tout ce qui concerne l'application elle-même |

## Exemples de placements

- Ordre de tri des albums dans une liste → `fonctionnel.md` (règle d'affichage)
- Mécanique de calcul d'une clé de tri (insensibilité à la casse, gestion des frontières de mot, etc.) → code + tests, pas `fonctionnel.md` (implémentation d'une règle déjà énoncée)
- Attribut `hors_serie` sur Album → `modele-metier.md` (attribut d'entité)
- Algorithme Random Forest pour l'estimation de valeur → `fonctionnel.md` uniquement (choix métier imposé par l'utilisateur, pas une contrainte technique) ; la bibliothèque qui l'implémente (ML.NET) et les versions de la stack → `choix-implementation.md` - Persistance des enums en entier explicite plutôt qu'en chaîne → `choix-implementation.md` (alternative — la chaîne — explicitement envisagée puis écartée, avec argumentation détaillée)
- Convention de nommage des branches → `gestion-projet.md`
