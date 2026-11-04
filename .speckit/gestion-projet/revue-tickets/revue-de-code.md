---
type: Gouvernance
title: 'Revue de code'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Revue de code

Des agents de revue de code (ex. Bugbot, outils d'analyse statique comme SonarQube Cloud, [Analyse de la qualité du code](/gestion-projet/depot/analyse-de-la-qualite-du-code.md)) peuvent intervenir sur les Pull Requests. Règles générales d'application de leurs retours :

- Les retours ne sont **pas une source de vérité** : ils sont systématiquement soumis à contre-vérification.
- Un retour est **appliqué** s'il est pertinent et que le gain justifie le coût de la modification.
- Un retour est **rejeté** s'il est jugé non pertinent, incorrect, ou si son coût (complexité, temps, lisibilité dégradée) est disproportionné par rapport au bénéfice obtenu.
- La décision d'accepter ou de rejeter un retour suit la règle générale de « Prise de décision » ci-dessus : un retour **technique** relève de l'agent ; un retour **fonctionnel ou métier** relève de l'agent si le `.speckit` existant permet déjà de trancher, ou d'une décision explicite de l'utilisateur (un point à la fois) s'il s'agit d'un véritable changement d'exigence ou d'une ambiguïté.
- L'objectif de robustesse est une application **à l'épreuve de l'utilisateur** (parcours réels, usages légitimes, erreurs utilisateur plausibles), pas un code **blindé** contre tout scénario théorique ou détourné. Le code est écrit pour le contexte de cette application — il n'a pas vocation à devenir une librairie publique ni à être réutilisé hors projet.
- Les scénarios invoqués par une revue (cas limites, abus, chemins d'exécution) doivent être **pertinents, valides et possibles** dans ce contexte applicatif ; un scénario hypothétique, irréaliste ou hors périmètre ne justifie pas une complexification du code.
