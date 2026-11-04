---
type: Choix d'implémentation
title: 'Représentation de la devise'
description: 'Devise en code ISO 4217, ancien franc sous le code QZF.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/EditionAcquisition.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Représentation de la devise

- **Devise d'un montant** (ex. `Édition.Prix d'acquisition`, cf. [`modele-metier/`](/modele-metier/index.md)) : stockée comme un **code ISO 4217 alpha-3** (`string`, 3 lettres majuscules), validée par le domaine sur sa seule **forme**, pas contre une liste fermée de devises.
  - **Alternative écartée (énumération C# figée)** : contredirait l'exigence « n'importe quelle devise » de [Gestion des devises](/fonctionnel/valorisation/gestion-des-devises.md), qui n'est pas limitée aux quelques exemples cités (Franc français, Dollar américain) dans ce même fichier.
- **Ancien franc** ([Gestion des devises](/fonctionnel/valorisation/gestion-des-devises.md)) : ISO 4217 ne lui attribue aucun code, et le code du franc (`FRF`) perdrait le rapport de 100 à 1. Il est stocké sous le code réservé **`QZF`**, de même forme que les codes ISO : son préfixe `QZ` appartient à la plage `QM`–`QZ` des codes que l'ISO 3166-1 réserve aux utilisateurs et n'attribuera jamais, dont dérivent les codes pays des devises ISO 4217 ; aucun code officiel ne peut donc lui être identique. La conversion traite `QZF` comme `FRF` divisé par 100.
  - **Alternative écartée (`FRF` accompagné d'un indicateur « ancien franc »)** : une seconde donnée attachée à chaque montant, pour une seule devise, là où un code distinct garde une représentation unique pour tous les montants.
