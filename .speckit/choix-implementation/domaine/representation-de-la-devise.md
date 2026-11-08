---
type: Choix d'implémentation
title: 'Représentation de la devise'
description: 'Devise en code ISO 4217, ancien franc sous le code QZF.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/EditionAcquisition.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Représentation de la devise

- La devise d'un montant est un **code ISO 4217 alpha-3**, que le domaine valide sur sa seule **forme**, et non contre une liste fermée.
  - **Alternative écartée (énumération figée)** : contredirait « n'importe quelle devise » ([Gestion des devises](../../fonctionnel/valorisation/gestion-des-devises.md)), qui ne se limite pas aux exemples cités.
- L'**ancien franc** ([Gestion des devises](../../fonctionnel/valorisation/gestion-des-devises.md)), sans code ISO 4217, a le code **`QZF`**, de même forme : le préfixe `QZ` appartient à la plage que l'ISO 3166-1 réserve aux utilisateurs et n'attribuera jamais, si bien qu'aucun code officiel ne peut lui être identique. La conversion le traite comme `FRF` divisé par 100.
  - **Alternative écartée (`FRF`)** : perdrait le rapport de 100 à 1.
  - **Alternative écartée (`FRF` et un indicateur « ancien franc »)** : une seconde donnée sur chaque montant pour une seule devise, là où un code distinct garde une représentation unique.
