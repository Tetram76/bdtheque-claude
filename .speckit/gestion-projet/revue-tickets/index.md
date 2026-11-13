# Revue et tickets

* [Règle de merge : non-régression, porte de qualité et revue Codex obligatoires](regle-de-merge-non-regression-porte-de-qualite-et-revue-codex-obligatoires.md) - Conditions de fusion d'une PR : non-régression, porte de qualité, approbation de Codex, validation fonctionnelle s'il y a lieu ; contrôles locaux avant push.
* [Validation fonctionnelle](validation-fonctionnelle.md) - Quand un chantier est validé par l'utilisateur, avec quel environnement, à quel moment et comment la demander.
* [Verrouillage des Pull Requests fusionnées](verrouillage-des-pull-requests-fusionnees.md) - Verrouillage « Resolved » des commentaires d'une PR fusionnée.
* [Revue de code](revue-de-code.md) - Règles générales d'application des retours des agents de revue : contre-vérification, application ou rejet, robustesse à l'épreuve de l'utilisateur.
* [Analyse SonarQube Cloud](analyse-sonarqube-cloud.md) - Traitement de l'analyse SonarQube Cloud de chaque PR : moment, problèmes, porte de qualité, marquage et son commentaire, remonté à Sonar au cas par cas, dispense, indisponibilité.
* [Analyse des vulnérabilités des images Docker](analyse-des-vulnerabilites-des-images-docker.md) - Analyse Docker Scout des images à chaque PR et avant publication ; traitement d'une vulnérabilité relevée.
* [Revue Codex (bloquante)](revue-codex-bloquante.md) - Revue Codex, condition du merge : ouverture de la PR, budget épuisé, statut et traitement des retours, cycle et surveillance.
* [Issues](issues.md) - Usage des tickets GitHub : chantiers du plan, bugs et fonctionnalités hors plan, type de tout ticket (Bug, Feature, Task), tickets de suivi des harnais et leurs étiquettes (celle du harnais, `harness`) ; recherche d'un ticket existant avant toute création.
* [Suivi par tickets](suivi-par-tickets.md) - Un ticket par chantier, correspondance ticket/PR, demande déplacée entre tickets soumise à l'utilisateur, découpage, consolidation, clôture et verrouillage, statut dans le suivi ; contenu d'un ticket.
* [Lien entre Pull Requests et Issues](lien-entre-pull-requests-et-issues.md) - Référencement du ticket dans la PR, mot-clé de fermeture, tickets associés.
* [Contrôle de cohérence à la reprise du travail](controle-de-coherence-a-la-reprise-du-travail.md) - À chaque reprise du travail : quand le contrôle de cohérence fonctionnelle est dû, qui le vérifie et où sa trace se lit ; s'il est dû, lire Contrôle de cohérence fonctionnelle.
* [Contrôle de cohérence fonctionnelle](controle-de-coherence-fonctionnelle.md) - Contrôle hebdomadaire de la cohérence de toute l'application, au sein des familles d'écrans et de points d'entrée de même nature et pour chaque règle transversale : sources confrontées, classement des écarts, suites, trace ; à lire quand le contrôle est dû ou demandé.
