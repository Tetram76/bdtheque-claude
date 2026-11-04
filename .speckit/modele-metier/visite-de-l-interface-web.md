---
type: Modèle métier
title: 'Visite de l''interface web'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Visite de l'interface web

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Début | date et heure | oui | Moment de la première utilisation de la visite. |
| Administrateur | booléen | oui | `true` pour une visite de l'administrateur (session authentifiée), y compris lorsqu'il consulte la partie publique ; `false` pour un visiteur public. |
| Type d'appareil | énuméré | oui | `Ordinateur` / `Tablette` / `Smartphone`. Avec le moteur de rendu, seule caractéristique de l'appareil retenue : aucune donnée qui l'identifie. |
| Moteur de rendu | énuméré | oui | `Blink` / `Gecko` / `WebKit` / `Autre`. Moteur du navigateur employé, et non le navigateur lui-même. |
| Version du moteur | texte | non | Version du moteur de rendu, si le navigateur la transmet. |
