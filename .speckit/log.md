# Journal des changements du .speckit

## 2026-11-07

* **Choix** : les tests ne sont exécutés qu'une fois par push, l'analyse SonarQube Cloud devenant un job du CI qui reprend leur couverture ; navigateur sans interface seul installé (`--only-shell`) ; chaque test ferme les contextes du navigateur qu'il ouvre ; une page est jugée prête sur son propre état (feuilles de style appliquées, polices, dessin du thème), jamais sur le silence du réseau ni sur une attente fixe ; captures comparées sur leurs tampons de pixels ; contrôle local des captures borné et lancé avec les options que Playwright recommande pour son image (#283, #285, #291).
* **Règle** : exigence de rigueur sur tout ce que produit l'agent : même exigence pour les tests, scripts et outillage que pour le code de l'application, pratiques recommandées et déconseillées de chaque outil lues dans sa documentation officielle avant de l'employer, toute ressource ouverte libérée par son propriétaire, son propre code relu d'abord face à une instabilité, relecture exigeante avant tout push ; toute commande, et ce qu'elle lance, est dimensionnée d'après les ressources réellement disponibles (parallélisme, processus, navigateurs, conteneurs) (#289).
* **Forme** : les renvois du `.speckit/` sont des chemins relatifs depuis le fichier qui les contient, que GitHub et les éditeurs savent suivre ; un lien qui commence par `/` est refusé par `SpeckitStructureTests` (#290).
* **Règle** : avant de lancer une commande, l'agent vérifie que les ressources qu'elle exige sont disponibles, estime sa durée et la borne (délai maximal, surveillance de la progression d'une commande longue, aucune attente sans borne) ; le délai maximal est la plus longue durée légitime majorée d'au plus la moitié, jamais un multiple arbitraire ; la surveillance de la revue Codex est bornée à 18 minutes et chacun de ses appels à GitHub à 1,3 seconde (#288).

## 2026-11-06

* **Choix** : compte administrateur par variables d'environnement, comparé en temps constant, tentatives de connexion limitées, pages de connexion et de déconnexion, retour à la page demandée ; nombre de fiches du menu de l'administration ; contrats de l'accueil de l'administration (`--admin-`, `--color-tile`) ; API de l'accueil de l'administration (décomptes, contrôles, dernières fiches écrites) ; compte et cas de l'environnement de validation ; erreurs présentées par leur cadre, leur icône et leur couleur, comme dans les maquettes, à l'arrondi d'une tuile (`--radius-tile`) ; lien vers l'administration au pied de l'accueil de la consultation ; captures de référence reprises écran par écran (`-Filter`) ; visite de la mesure d'audience retenue avec son genre, administrateur ou visiteur, une autre s'ouvrant quand l'authentification change (#287).

## 2026-11-05

* **Choix** : libellés des albums et des éditions construits par le frontend, tirages d'une page fixés par les tests, applications de test propres à un écran qui dépend de toute la collection, contrôle local des captures dans l'image de Playwright, contrats de la présentation d'un écran propre à chaque thème et règle du chantier d'un écran, thème choisi par l'adresse dans l'environnement de validation, nouveau rendu de toute page dont le contenu dépend du fuseau du navigateur (#284).
* **Règle** : le déplacement ou le renommage d'un concept ou d'une section met aussi à jour, à la fusion de sa Pull Request, les renvois des tickets ouverts qui le désignent, comme un nom que le `.speckit/` change (#282).
* **Règle** : la forme de `log.md` est celle d'OKF : entrées regroupées par date, les plus récentes en premier (#282).

## 2026-11-04

* **Restructuration** : le `.speckit/` est réécrit au format OKF, découpé en une carte (les `index.md`) et des concepts ; sa lecture est guidée par le chantier ; la forme d'un document dépend de sa nature, référence ou journal (#280).
* **Règle** : un contrôle local de non-régression ne s'exécute avant un push que si le push modifie ce qu'il vérifie ; les captures de référence, seulement pour un changement qui peut modifier le rendu (#280).
* **Socle allégé** : les règles qui ne valent qu'à un moment du travail (Pull Request, revue, merge, version, relevé des versions dû) sortent du socle ; la table des moments d'`AGENTS.md` les désigne et le hook du dépôt les rappelle (#280).
