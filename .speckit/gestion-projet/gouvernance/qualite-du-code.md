---
type: Gouvernance
title: 'Qualité du code'
description: 'KISS, DRY, YAGNI, SOLID, bonnes pratiques, TDD, non-régression, commentaires, langue des fichiers techniques ; s''applique à tout code.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Qualité du code

- La **qualité du code est une préoccupation majeure** et permanente.
- Le projet est guidé par les principes **KISS, DRY, YAGNI et SOLID**. Toute décision (architecture, conception, implémentation, revue) doit s'y conformer :
  - **KISS** (*Keep It Simple, Stupid*) : privilégier la solution la plus simple qui répond au besoin réel, éviter la complexité non justifiée.
  - **DRY** (*Don't Repeat Yourself*) : éviter la duplication de logique ou de connaissance ; factoriser lorsque c'est pertinent, sans sur-factoriser prématurément.
  - **YAGNI** (*You Aren't Gonna Need It*) : ne pas implémenter de fonctionnalité, abstraction ou paramétrage anticipant un besoin futur non avéré.
  - **SOLID** : respecter les cinq principes de conception orientée objet (responsabilité unique, ouvert/fermé, substitution de Liskov, ségrégation des interfaces, inversion des dépendances) dans l'organisation du code.
- Le code doit suivre les **best practices communément admises** pour chaque technologie utilisée (conventions de nommage, patterns architecturaux, sécurité, performance, etc.). Ces best practices sont à vérifier via les sources officielles (cf. règle ci-dessus).
- **Méthodologie TDD (Test-Driven Development)** : toute implémentation de logique métier ou technique non triviale suit le cycle **Red → Green → Refactor** : écrire d'abord un test qui échoue pour le comportement visé (Red), écrire ensuite le code de production minimal qui le fait passer (Green), puis refactoriser le code — production et tests — une fois au vert (Refactor). Le test est **toujours écrit avant** le code qu'il vérifie, jamais après-coup pour documenter un comportement déjà implémenté.
- **Non-régression** : toute modification doit être accompagnée d'un moyen de vérifier qu'elle ne sera pas silencieusement annulée par une modification future. Le moyen de contrôle (test unitaire, test d'intégration, test de contrat, assertion, etc.) doit être **proportionné à la portée et au risque de la modification** : on n'écrit pas une suite de tests complète pour un changement trivial, mais toute logique métier ou technique non triviale doit être couverte — et développée selon le cycle TDD ci-dessus. La règle vaut pour tout livrable, ce que l'utilisateur voit et utilise compris (écrans, comportements, rendu) : la validation fonctionnelle de l'utilisateur ([Validation fonctionnelle](../revue-tickets/validation-fonctionnelle.md)) n'en tient jamais lieu. Elle accepte un comportement à un instant donné ; le moyen de contrôle le garantit dans la durée. Tout comportement validé est donc fixé par ce moyen dans la Pull Request qui le réalise.
- L'agent est **seul décisionnaire** sur l'architecture et l'implémentation : toute refactorisation jugée nécessaire (lisibilité, maintenabilité, testabilité, séparation des responsabilités, etc.) doit être faite sans attendre de validation.
- Le code doit être **propre et lisible** : l'utilisateur est développeur et lit le code produit.
- **Règle de commentaires** : les commentaires expliquent le **pourquoi** (intention, contrainte, décision de conception), jamais le **quoi** (ce que le code fait — le code se lit de lui-même).
- **Langue des fichiers techniques** : tout le contenu technique (code, noms de fonctions/classes/variables/constantes, commentaires, messages de log, noms de fichiers de configuration) est rédigé en **anglais**. Seule la documentation du projet (`.speckit/`, `README.md`, `README.dev.md`, etc.) est rédigée en **français**. Les messages destinés à l'utilisateur final sont produits par le système de traduction du frontend (cf. [Internationalisation](../../choix-implementation/socle/transverse/internationalisation.md)), pas codés en dur dans une langue donnée.
