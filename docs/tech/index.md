# Documentation technique

Cette documentation explique comment BDthèque est réellement construite et comment elle fonctionne : son architecture, les mécanismes qui traversent plusieurs de ses composants, son intégration continue et ses tests, ainsi que des références générées directement depuis le code. Elle décrit l'existant et ne prescrit rien. Les exigences, les contraintes et les raisons des choix se trouvent dans le [`.speckit/`](../../.speckit/index.md), qui prime sur elle en cas de doute ([Documentation technique](../../.speckit/gestion-projet/gouvernance/documentation-technique.md)).

Pour trouver une information, le plus efficace est de partir de cette carte, d'ouvrir la carte du domaine qui vous concerne, puis de ne lire que les pages utiles : la documentation n'est pas faite pour être lue d'un bout à l'autre.

* [Architecture](architecture/index.md) - Ce domaine présente les conteneurs de l'application, les projets .NET de la solution et leur déploiement ; c'est le point de départ pour découvrir le système ou situer un changement avant de lire le code.
* [Mécanismes](mecanismes/index.md) - Ce domaine explique les processus qui traversent plusieurs composants, comme l'authentification, la persistance, les erreurs, le rendu du frontend ou la recherche ; à lire pour comprendre ou modifier l'un d'eux.
* [Intégration continue et tests](integration-et-tests/index.md) - Ce domaine décrit le pipeline du CI, les projets de tests et leurs ressources, les captures de référence et les contrôles périodiques ; à lire pour comprendre un check ou un échec, écrire un test ou modifier un workflow.
* [Références générées](references/index.md) - Ce domaine rassemble des vues produites automatiquement depuis les sources qui font autorité, comme les dépendances entre projets, le schéma de la base et le contrat HTTP de l'API ; à consulter pour un fait exact, sans interprétation.
* [Documentation technique](documentation/index.md) - Ce domaine explique comment consulter, construire, vérifier et maintenir cette documentation, et indique ce qu'elle couvre et ce qui reste à documenter.
