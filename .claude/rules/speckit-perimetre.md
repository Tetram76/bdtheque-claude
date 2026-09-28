---
paths:
  - ".speckit/*.md"
---

# Périmètre des fichiers .speckit

Avant tout ajout ou modification, vérifier dans quel fichier l'information appartient.

| Fichier | Contient | Ne contient PAS |
| --- | --- | --- |
| `modele-metier.md` | Entités, attributs, relations, cardinalités, contraintes d'intégrité | Règles d'affichage, comportement applicatif, choix techniques |
| `fonctionnel.md` | Règles métier applicatives, règles d'affichage/tri, cas d'usage, flux, UX | Toute considération technique : structure des données, choix techniques, détails d'implémentation (algorithme, mécanique de calcul) |
| `contraintes-techniques.md` | Stack, frameworks, algorithmes, déploiement, sécurité — y compris les choix techniques faits par l'agent, par défaut | Fonctionnel, gestion de projet |
| `choix-implementation.md` | **Uniquement** les choix techniques ayant fait l'objet d'une **délibération explicite entre options** (alternative envisagée puis écartée, avec argumentation) — cas rare, pas un inventaire général | Tout choix technique sans délibération écrite explicite (→ reste dans `contraintes-techniques.md`) |
| `gestion-projet.md` | Branches, commits, CI/CD, outillage | Tout ce qui concerne l'application elle-même |

## Exemples de placements

- Ordre de tri des albums dans une liste → `fonctionnel.md` (règle d'affichage)
- Mécanique de calcul d'une clé de tri (insensibilité à la casse, gestion des frontières de mot, etc.) → code + tests, pas `fonctionnel.md` (implémentation d'une règle déjà énoncée)
- Attribut `hors_serie` sur Album → `modele-metier.md` (attribut d'entité)
- Algorithme Random Forest pour l'estimation de valeur → `contraintes-techniques.md` (choix technique, sans délibération écrite entre plusieurs algorithmes)
- Persistance des enums en chaîne plutôt qu'en entier → `choix-implementation.md` (alternative explicitement envisagée puis écartée, avec argumentation détaillée)
- Convention de nommage des branches → `gestion-projet.md`
