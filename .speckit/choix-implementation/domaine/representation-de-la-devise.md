---
type: Choix d'implémentation
title: 'Représentation de la devise'
description: 'Devise en code ISO 4217, ancien franc sous le code QZF, décimales d''une devise tirées de l''ISO 4217 dans une table partagée par le domaine et le frontend, deux par défaut.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/EditionAcquisition.cs'
  - 'src/Bdtheque.Currencies/**'
  - 'src/Bdtheque.Frontend/wwwroot/amount-field.js'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-27T12:00:00Z }
---

# Représentation de la devise

- La devise d'un montant est un **code ISO 4217 alpha-3**, que le domaine valide sur sa seule **forme**, et non contre une liste fermée.
  - **Alternative écartée (énumération figée)** : contredirait « n'importe quelle devise » ([Gestion des devises](../../fonctionnel/valorisation/gestion-des-devises.md)), qui ne se limite pas aux exemples cités.
- L'**ancien franc** ([Gestion des devises](../../fonctionnel/valorisation/gestion-des-devises.md)), sans code ISO 4217, a le code **`QZF`**, de même forme : le préfixe `QZ` appartient à la plage que l'ISO 3166-1 réserve aux utilisateurs et n'attribuera jamais, si bien qu'aucun code officiel ne peut lui être identique. La conversion le traite comme `FRF` divisé par 100.
  - **Alternative écartée (`FRF`)** : perdrait le rapport de 100 à 1.
  - **Alternative écartée (`FRF` et un indicateur « ancien franc »)** : une seconde donnée sur chaque montant pour une seule devise, là où un code distinct garde une représentation unique.

## Décimales d'une devise

- **L'unité mineure de l'ISO 4217**, relevée dans la liste officielle que publie son agence de maintenance (SIX, *list one*), donne les décimales d'un montant ([Gestion des devises](../../fonctionnel/valorisation/gestion-des-devises.md), précision d'un montant) : seules les devises dont l'unité mineure n'est pas de deux décimales sont listées.
- **Deux décimales par défaut**, pour toute devise à laquelle l'ISO 4217 n'en donne pas : une devise retirée de la liste (le franc, le franc belge), une devise dont l'unité mineure est sans objet (l'or, les unités de compte), l'ancien franc (`QZF`), un code qu'elle ne connaît pas. Deux est l'unité mineure de la plupart des devises, et celle que [Gestion des devises](../../fonctionnel/valorisation/gestion-des-devises.md) donne au franc.
  - **Alternative écartée (aucune décimale, ou quatre, la précision du stockage, pour une devise inconnue)** : aucune refuserait des montants vraisemblables d'une devise retirée, en centimes ; quatre laisserait revenir les montants trop précis que la règle écarte.
- **Une table, et une seule, partagée par le domaine et le frontend** (`Bdtheque.Currencies`, [Organisation du code](../organisation-du-code.md)) : le domaine refuse un montant plus précis que sa devise, à toute écriture, et le frontend présente chaque montant avec ses décimales et ne laisse pas taper au-delà ; la table n'est écrite qu'une fois, si bien que la saisie et l'affichage ne peuvent diverger de la règle.
  - **Alternative écartée (table du domaine, décimales données par l'API avec chaque montant et à chaque devise choisie dans la saisie)** : la saisie interrogerait l'API à chaque changement de devise et l'affichage recevrait les décimales de chaque montant de chaque contrat, pour une donnée de référence qui ne change qu'avec une édition de l'ISO 4217 et ne dépend d'aucune donnée de l'application.
  - **Alternative écartée (décimales de la culture d'une région de la devise, `NumberFormatInfo.CurrencyDecimalDigits`)** : elles s'écartent de l'ISO 4217 pour certaines devises (aucune pour le dinar irakien, culture `ar-IQ`, quand l'ISO 4217 lui en donne trois), et aucune région n'a une devise retirée.
  - **Alternative écartée (bibliothèque de montants, ex. `NodaMoney`)** : toute une bibliothèque de types monétaires, d'arrondis et de sérialisation, dans le domaine et le frontend, pour une table d'une trentaine d'entrées.
- **Sans contrainte CHECK** : la règle ne s'écrirait en base qu'en y recopiant la table ; comme la forme du code, elle n'est vérifiée que par le domaine.
- **Frappe retenue par un script** ([Frontend](../socle/frontend.md), JavaScript) : seul le navigateur peut empêcher une touche ou un collage d'entrer dans le champ ; le champ lui donne les décimales de sa devise et le séparateur de la culture, que le script ne fait que comparer au texte qui en résulterait. Sans script, l'enregistrement refuse le montant, et le message sous le champ dit les décimales que la devise admet.
