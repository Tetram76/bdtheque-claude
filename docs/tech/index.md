# Documentation technique

Comment BDthèque est réellement construite et fonctionne : architecture, mécanismes, références générées depuis le code. Cette documentation décrit l'existant, elle ne prescrit rien : exigences, contraintes et raisons des choix sont dans le [`.speckit/`](../../.speckit/index.md), qui prime ([Documentation technique](../../.speckit/gestion-projet/gouvernance/documentation-technique.md)).

Lire cette carte, puis la seule carte du domaine concerné, puis les seules pages utiles : jamais tout le dossier.

* [Architecture](architecture/index.md) - Conteneurs, projets de la solution et déploiement : pour découvrir le système ou situer un changement avant de lire le code.
* [Mécanismes](mecanismes/index.md) - Processus qui traversent plusieurs composants (authentification, appels internes, persistance, erreurs, frontend, recherche…) : pour comprendre ou modifier l'un d'eux.
* [Intégration continue et tests](integration-et-tests/index.md) - Pipeline du CI et ses conditions, projets de tests et leurs ressources, captures de référence, contrôles hebdomadaire et quotidien : pour comprendre un check ou un échec, écrire un test, ou toucher à un workflow.
* [Références générées](references/index.md) - Vues produites depuis les sources qui font autorité (dépendances des projets, schéma de la base, contrat HTTP) : pour un fait exact, sans interprétation.
* [Documentation technique](documentation/index.md) - Consulter, construire, vérifier et maintenir cette documentation ; ce qu'elle couvre et ce qui reste à documenter.
