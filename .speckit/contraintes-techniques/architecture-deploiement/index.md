# Architecture, déploiement et exploitation

* [Architecture des tiers](architecture-des-tiers.md) - Socle de trois conteneurs (frontend, api, db), seul frontend exposé, seul api accède à db.
* [Indépendance de l'outil pour agents IA](independance-de-l-outil-pour-agents-ia.md) - Outil pour agents IA indépendant de l'API ; conteneur souhaité, exposition non souhaitée (non-contraintes).
* [Déploiement](deploiement.md) - Isolation des tiers, Docker Compose, images publiées sur Docker Hub (nightly, à la demande), optimisées, sans vulnérabilité corrigeable introduite.
* [Hébergement](hebergement.md) - NAS Synology, TLS par son reverse proxy, accès direct en HTTP en interne, plusieurs versions sur le NAS et données d'une pré-version.
* [Stockage des visuels](stockage-des-visuels.md) - Visuels sur un volume Docker, chemin de montage configurable.
* [Compatibilité multi-supports](compatibilite-multi-supports.md) - Application responsive, mobile-first : PC, tablette, smartphone ; s'applique à tout écran.
* [Authentification](authentification.md) - Compte administrateur unique pour l'administration, consultation publique.
* [Sources de données externes](sources-de-donnees-externes.md) - Données des sources externes (taux de change, inflation) mises en cache.
* [Statistiques du dashboard](statistiques-du-dashboard.md) - Seules les statistiques présentées par un affichage du dashboard sont calculées : le tirage précède le chargement.
