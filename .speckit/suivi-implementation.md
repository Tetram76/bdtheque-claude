# Suivi d'implémentation

Ce fichier décrit le plan d'implémentation de l'application, découpé en phases puis en chantiers — pas l'historique des décisions qui ont conduit à ce découpage.

Chaque ligne correspond à un chantier, c'est-à-dire à un ticket GitHub (et à une Pull Request) : elle en indique le **numéro** et le **titre**. Le contenu détaillé du chantier est porté par le ticket. L'**état** d'un chantier est celui de son ticket, signalé par la mise en forme de son numéro, selon la règle unique de `gestion-projet/revue-tickets.md` § « Suivi par tickets » (Statut).

Chaque phase, ou partie de phase, est livrée par une version (`gestion-projet/depot.md` § Versions et releases), indiquée sous son titre. Un ajustement réalisé après la consolidation de sa phase est livré par la version en cours lors de sa réalisation : celle du jalon de son ticket.

---

## Phase 0 — Architecture

**Version** : `v0.0.1`, avec la mise en place du dépôt et du `.speckit/`.

Mise en place de la structure porteuse de l'application, indépendamment du modèle métier :

- Solution .NET découpée en 5 projets (`Bdtheque.Domain`, `Bdtheque.Contracts`, `Bdtheque.Infrastructure`, `Bdtheque.Api`, `Bdtheque.Frontend`).
- Architecture 3 conteneurs Docker (`frontend` / `api` / `db`), réseaux isolés, `docker-compose.yml`.
- Authentification cookie côté `frontend` (BFF) ; middleware de clé interne côté `api`.
- `DbContext` EF Core opérationnel (connexion PostgreSQL), sans entité métier.
- Endpoints `/health` sur `frontend` et `api` ; OpenAPI/Scalar en développement.
- Pipeline CI GitHub Actions (restauration, build, tests).
- Premiers tests d'intégration (health check, middleware de clé interne).

---

## Phase 1 — Modèle de domaine

**Version** : `v0.0.2`.

Implémentation des entités du modèle métier (`modele-metier.md`) dans `Bdtheque.Domain` et `Bdtheque.Infrastructure` (configurations EF Core + migrations), séquencée selon les dépendances entre entités. Le `DbContext` reçoit progressivement ses `DbSet`.

| Ticket | Titre |
| --- | --- |
| ~~#60~~ | Ajout des référentiels de base du domaine |
| ~~#61~~ | Ajout de l'entité Série |
| ~~#62~~ | Ajout de l'entité Album |
| ~~#63~~ | Ajout de l'entité Contribution |
| ~~#64~~ | Ajout de l'entité Édition |
| ~~#65~~ | Ajout de l'entité Visuel d'édition |
| ~~#66~~ | Ajout de l'entité Intention d'achat |

### Consolidation avant la Phase 2

Corrections issues de la revue complète de la Phase 1, à réaliser avant d'ouvrir la Phase 2.

| Ticket | Titre |
| --- | --- |
| ~~#67~~ | Tests de persistance sur PostgreSQL réel |
| ~~#68~~ | Passage à PostgreSQL 18 et regroupement des migrations |
| ~~#69~~ | Tri linguistique des colonnes texte en base |
| ~~#70~~ | Distinction des erreurs métier et techniques dans le domaine |
| ~~#71~~ | Encapsulation des collections de navigation |
| ~~#72~~ | Correction de la documentation relevée par la revue de la Phase 1 |

### Ajustement issu de la reprise des données

Répercussion dans le modèle de domaine des ajustements du `.speckit/` issus de la confrontation avec la base existante (ticket global #101), à réaliser avant les chantiers restants de la Phase 2.

| Ticket | Titre |
| --- | --- |
| ~~#102~~ | Ajustement du modèle de domaine issu de la reprise des données |

---

## Phase 2 — Contracts et API

**Version** : `v0.0.3`, avec l'ajustement issu de la reprise des données (#102), réalisé pendant la phase.

Exposition du modèle de domaine via l'API : DTOs dans `Bdtheque.Contracts`, endpoints Minimal API dans `Bdtheque.Api` — administration (`/admin` : saisie, suppression selon `fonctionnel/regles-metier.md` § Suppression des entités) et consultation (`/catalog` : listes, navigation par initiale, recherche, fiches). Choix techniques : `choix-implementation/api.md` § Organisation de l'API et sections suivantes.

Hors périmètre (phases dédiées ci-dessous) : statistiques, conversion de devises, estimations.

| Ticket | Titre |
| --- | --- |
| ~~#73~~ | Socle des endpoints et des réponses d'erreur |
| ~~#74~~ | Entrée de navigation par initiale et clé de tri des auteurs |
| ~~#75~~ | Alignement des suppressions en base sur les règles de suppression |
| ~~#76~~ | Administration des genres et des univers |
| ~~#77~~ | Administration des éditeurs et de leurs collections |
| ~~#78~~ | Administration des auteurs |
| ~~#79~~ | Administration des séries |
| ~~#103~~ | Ajustement de l'API des séries issu de la reprise des données |
| ~~#80~~ | Administration des albums |
| ~~#81~~ | Administration des éditions |
| ~~#82~~ | Téléversement et gestion des visuels d'édition |
| ~~#83~~ | Intentions d'achat |
| ~~#84~~ | Listes et recherche de la consultation |
| ~~#85~~ | Fiches détaillées de la consultation |

### Consolidation avant la Phase 3

Corrections issues de la revue complète de la Phase 2 (ticket parent #121), à réaliser avant d'ouvrir la Phase 3.

| Ticket | Titre |
| --- | --- |
| ~~#122~~ | Filtres des séries par auteur, éditeur et collection sur les données de la série |
| ~~#123~~ | Remplacement des contributions porté par le domaine |
| ~~#124~~ | Retrait des opérations du domaine supplantées par les opérations atomiques |
| ~~#125~~ | Borne de la mémoire des traitements simultanés de visuels |
| ~~#126~~ | Maîtrise des versions de collation ICU de la base |
| ~~#127~~ | Correction et validation de la documentation du code de la Phase 2 |
| ~~#134~~ | Optimisation des images Docker pour la production |
| ~~#136~~ | Journalisation de production et chargement des agrégats par EF Core |

### Ajustement issu de la maquette du dashboard

Répercussion dans le modèle de domaine et dans l'API de la date d'entrée dans la collection, ajoutée au `.speckit/` avec la maquette du dashboard. Les phases 1 et 2 étant validées, un seul ticket couvre les deux.

| Ticket | Titre |
| --- | --- |
| ~~#141~~ | Date d'entrée dans la collection dans le modèle de domaine et l'API |

### Ajustement issu de la maquette de l'administration

Répercussion dans le modèle de domaine et dans l'API du journal des modifications, ajouté au `.speckit/` avec la maquette de l'administration pour son bloc « Dernières modifications ». Les phases 1 et 2 étant validées, un seul ticket couvre les deux.

| Ticket | Titre |
| --- | --- |
| ~~#148~~ | Journal des modifications dans le modèle de domaine et l'API |

### Ajustement issu des maquettes des fiches de série et d'auteur

Répercussion dans l'API des données que les cartes d'album des fiches de série et d'auteur affichent (couverture, appréciation, intention d'achat, nombre d'albums d'une série), après que l'utilisateur a statué quelle édition fournit la couverture d'un album. La phase 2 étant validée, un seul ticket la couvre.

| Ticket | Titre |
| --- | --- |
| ~~#156~~ | Données des cartes d'album dans les fiches de série et d'auteur |

### Ajustement issu des fiches de consultation

Répercussion dans l'API de la règle qui réserve les fiches de consultation aux albums, aux séries et aux auteurs : éditions présentées par la fiche de leur album ; éditeurs et collections éditeur sans fiche ; genres et univers menant à la liste de leurs séries et albums présentée comme la bibliographie d'un auteur. La phase 2 étant validée, un seul ticket la couvre.

| Ticket | Titre |
| --- | --- |
| ~~#157~~ | Fiches de consultation réservées aux albums, séries et auteurs |

### Ajustement issu de la définition de la recherche avancée

Répercussion dans le modèle de domaine et dans l'API de la recherche avancée (`fonctionnel/recherche-avancee.md` § Recherche avancée) : recherche de toute entité consultable sur toute combinaison de critères portant sur les données du modèle métier, et recherches enregistrées, avec leurs variables. Les phases 1 et 2 étant validées, un seul ticket couvre les deux (ticket parent #162), découpé en sous-tickets, le chantier étant trop vaste pour une seule Pull Request. Les critères sur des données que calculent des phases ultérieures (montants en euros, valeur estimée : phase 4 ; tomes manquants, sortie estimée : phase 5) en sont exclus, et rejoignent la recherche avancée avec ces phases.

| Ticket | Titre |
| --- | --- |
| ~~#168~~ | Moteur de la recherche avancée en consultation |
| ~~#169~~ | Recherches enregistrées : administration et rejeu en consultation |
| ~~#170~~ | Blocage de la suppression des fiches désignées par une recherche enregistrée |
| ~~#171~~ | Comparaison entre deux données dans la recherche avancée |

### Ajustement issu des maquettes des listes et saisies de l'administration

Répercussion dans le modèle de domaine et dans l'API de la règle qui réserve une intention d'achat sur l'album à un album sans édition, ajoutée au `.speckit/` avec la page des éditions et intentions d'achat d'un album (`fonctionnel/regles-metier.md` § Intention d'achat). Les phases 1 et 2 étant validées, un seul ticket couvre les deux.

| Ticket | Titre |
| --- | --- |
| ~~#185~~ | Intention d'achat sur un album réservée à un album sans édition |

### Ajustement issu de la mesure d'audience de l'interface web

Répercussion dans le modèle de domaine et dans l'API de la mesure d'audience de l'interface web (`fonctionnel/mesure-audience.md` § Mesure d'audience de l'interface web) : visites, utilisations des fonctions et compteurs d'audience mensuels, leur conservation et la lecture de leurs indicateurs. Les phases 1 et 2 étant validées, un seul ticket couvre les deux ; la part de la Phase 3, pas encore découpée, y est notée.

| Ticket | Titre |
| --- | --- |
| ~~#186~~ | Mesure d'audience de l'interface web dans le modèle de domaine et l'API |

### Ajustement issu de l'exploitation des technologies retenues

Revue de l'existant au regard de la contrainte d'exploitation au mieux des technologies retenues (`contraintes-techniques/index.md` § Exploitation des technologies retenues), pour en identifier les chantiers d'amélioration, chacun avec son ticket. Les phases 0 à 2 étant validées, un seul ticket les couvre.

| Ticket | Titre |
| --- | --- |
| ~~#189~~ | Revue de l'existant au regard de l'exploitation des technologies retenues |

Chantiers d'amélioration identifiés par cette revue, chacun réalisé par sa propre Pull Request.

| Ticket | Titre |
| --- | --- |
| ~~#219~~ | Horloge de l'application pour les dates renseignées automatiquement |
| ~~#220~~ | Pipeline CI et outillage .NET : SDK, restauration verrouillée, cache NuGet, couverture et projets de test |
| ~~#221~~ | Paramétrage des valeurs de la recherche avancée par EF Core |

### Ajustement issu de la sécurisation SSL/TLS par le reverse proxy du NAS

Répercussion dans le frontend de la sécurisation SSL/TLS assurée par le reverse proxy du NAS Synology (`contraintes-techniques/architecture-deploiement.md` § Hébergement) : une même image derrière le reverse proxy et en déploiement interne en HTTP, adresse publique déclarée par le déploiement derrière le reverse proxy. La configuration du frontend relevant de la Phase 0, validée, un seul ticket la couvre ; il précède la connexion de l'administration (#199), qui en dépend.

| Ticket | Titre |
| --- | --- |
| #249 | Frontend derrière le reverse proxy du NAS Synology, qui assure la sécurisation SSL/TLS |

## Phase 3 — Frontend Blazor

Pages de consultation (public) et d'administration (authentifié), composants partagés, aide contextuelle. Le rendu repose sur un thème de base neutre, qui définit les contrats des thèmes ; aucune alternative des maquettes n'en sert de base (`visuel/charte-graphique.md` § Thème de base).

La phase est réalisée en plusieurs parties, découpées l'une après l'autre ; seule la partie 1 l'est à ce stade.

- **Pages non implémentées** : tant qu'une page n'est pas implémentée, tout lien qui y mène affiche une page « Pas encore implémenté » (#238). Le chantier qui réalise une page remplace par le vrai lien tous les liens qui y mènent.
- **Validation fonctionnelle** : les chantiers qui réalisent ce que l'utilisateur voit et utilise sont validés par lui avant fusion (`gestion-projet/revue-tickets.md` § Validation fonctionnelle), avec l'environnement de validation (#214), réalisé avant le premier d'entre eux ; chaque ticket précise s'il y est soumis.
- **Thèmes** : la partie 1 réalise le thème de base et deux thèmes, « Cases et bulles » (violine) et « La gazette », retenus pour s'opposer sur presque tout (sombre et clair, blocs encadrés et ouverts, titre en bulle et mention au-dessus du titre, désordre ponctuel et sobriété), pour que les contrats du thème de base couvrent ces extrêmes. Un thème ajouté par une partie ultérieure est décliné sur tous les écrans déjà réalisés.

### Partie 1 — Éléments de base

**Version** : `v0.1.0`, avec les ajustements des phases 0 à 2 qui suivent la consolidation de la Phase 2.

Socle du frontend, environnement de validation fonctionnelle, thème de base et deux thèmes, et les écrans qui ont une maquette : dashboard (collection vide comprise), accès à l'administration, listes de l'administration, fiches de saisie et fiches de consultation. Le chantier #161 (accès du frontend au volume des visuels) est tranché avant tout écran qui affiche un visuel ; #185 et #186 précèdent les écrans qui en dépendent. Le socle du frontend (ticket parent #191) est réalisé par trois sous-tickets, #238 à #240.

| Ticket | Titre |
| --- | --- |
| ~~#161~~ | Accès du frontend au volume des visuels |
| ~~#238~~ | Structure du frontend, page « Pas encore implémenté » et outillage de non-régression du frontend |
| ~~#239~~ | Appels à l'API, présentation des erreurs et localisation du frontend |
| ~~#240~~ | Fuseau horaire du navigateur et alignement de la recherche avancée |
| ~~#214~~ | Environnement de validation fonctionnelle |
| ~~#192~~ | Données du dashboard dans l'API avant la Phase 4 |
| #193 | Paramétrage de l'application dans le modèle de domaine et l'API |
| #194 | Thème de base neutre, contrats des thèmes et thème de la visite |
| #195 | Thème « Cases et bulles » (violine) |
| #196 | Thème « La gazette » |
| #197 | Mesure d'audience dans l'interface web : relevé, visites, information et opposition |
| #198 | Dashboard et écran d'accueil d'une collection vide |
| #199 | Connexion et page d'accueil de l'administration |
| #200 | Liste des albums de l'administration |
| #201 | Listes des séries, des auteurs et des éditeurs de l'administration |
| #202 | Listes des éditions et des intentions d'achat de l'administration |
| #203 | Genres et univers : listes et saisie de l'administration |
| #204 | Page des éditions et intentions d'achat d'un album |
| #205 | Saisie d'un album |
| #206 | Saisie d'une série |
| #207 | Saisie d'un auteur |
| #208 | Saisie d'un éditeur et de ses collections |
| #209 | Saisie d'une édition, de ses visuels et d'une intention d'achat |
| #210 | Fiche d'un album dans la consultation |
| #211 | Fiche d'une série dans la consultation |
| #212 | Fiche d'un auteur dans la consultation |

### Parties suivantes

Contenu, nombre et version à établir au découpage de chacune.

- **Note — contraste des thèmes** : le texte courant du thème « fanzine en risographie » (bleu `#0078BF` sur papier `#F4EFE4`, `visuel/dashboard.md`) n'atteint que 4,1:1, sous les 4,5:1 attendus pour du texte courant. La couleur relevant de l'identité du thème, sa correction est soumise à l'utilisateur lors du découpage de la partie qui le réalise, avec la vérification des contrastes des thèmes qu'elle ajoute.
- **Note — style des contrôles** : dans chaque thème, le texte indicatif (*placeholder*) des champs garde un contraste d'au moins 4,5:1, et l'indicateur de focus clavier contraste à la fois avec le contrôle et avec la surface qui l'entoure, y compris sur un bouton à fond coloré. Les maquettes de l'administration (`visuel/maquettes/administration/`) ne respectent pas ces deux points (texte indicatif atténué par une opacité, contour de focus dans la couleur du texte du contrôle). Le style des contrôles relève des contrats du thème de base (#194), que tout thème ajouté respecte.
- **Note — paramétrage de l'application** : le modèle de domaine et l'API du paramétrage sont réalisés par la partie 1 (#193), dont la page d'accueil de l'administration présente les valeurs ; l'écran qui le modifie reste à réaliser.
- **Note — mesure d'audience de l'interface web** : la partie 1 réalise le relevé, les visites, l'information des visiteurs et leur opposition (#197), et chaque écran ajoute ses fonctions à la liste des fonctions suivies ; l'écran de l'administration qui présente les indicateurs reste à réaliser.
- **Note — recherche avancée** : les écrans de la recherche avancée (`fonctionnel/recherche-avancee.md` § Recherche avancée) sont intégrés à cette phase : formulaire de construction des critères, utilisable sans aucune connaissance de SQL et qui met en évidence le chemin suivi entre entités, y compris vers l'autre donnée d'une comparaison entre deux données (`choix-implementation/recherche-avancee.md` § Recherche avancée : mise en œuvre), rejeu des recherches enregistrées avec la saisie de leurs variables, et page d'administration des recherches enregistrées.

## Phase 4 — Statistiques, devises et estimation de valeur

Dashboard public et statistiques (sur la collection uniquement) ; conversion des montants en euros courants (taux fixes, taux variables récupérés auprès de Frankfurter et mis en cache) et en euros du jour (coefficients d'inflation de l'INSEE, mis en cache) ; valeur estimée des éditions (Random Forest, ML.NET), recalculée à partir de l'état courant de la base (`fonctionnel/valorisation.md` § Calcul des estimations).

Découpage et version à établir à l'ouverture de la phase. La qualité de l'estimation de valeur ne peut être évaluée que sur des données réelles (collection issue de l'application existante).

- **Note — recherche avancée** : les montants en euros courants et en euros du jour et la valeur estimée des éditions deviennent des critères de la recherche avancée (`fonctionnel/recherche-avancee.md` § Recherche avancée), avec leurs comparaisons (version choisie par critère, montant non convertible), à une valeur comme à un autre montant.
- **Note — couverture d'un album** : une statistique qui présente un album par sa couverture (ex. doyen et benjamin) est une donnée de la collection, qui ne retient que les éditions possédées (`fonctionnel/regles-metier.md` § Couverture d'un album) ; l'expression de la couverture (`choix-implementation/api.md` § Organisation de l'API, cartes d'album) ne couvre encore que la variante sans filtre.

## Phase 5 — Fonctionnalités de second plan

Identification des albums manquants d'une série ; estimation de sortie d'un nouvel album (`fonctionnel/second-plan.md` § Fonctionnalités de second plan). Les statistiques du dashboard qui en dépendent (tomes manquants, prochaines sorties estimées, `fonctionnel/page-accueil.md` § Page d'accueil) n'entrent dans son tirage qu'une fois ces fonctionnalités réalisées : le dashboard de la Phase 4 les en exclut.

Découpage et version à établir à l'ouverture de la phase.

- **Note — recherche avancée** : les données que calculent ces fonctionnalités (tomes manquants d'une série, sortie estimée du prochain tome) deviennent des critères de la recherche avancée (`fonctionnel/recherche-avancee.md` § Recherche avancée).

## Phase 6 — Outil pour agents IA

Serveur MCP exposant l'aide à la saisie, la recherche/consultation (recherche avancée comprise) et les analyses et statistiques ; accès par jeton, avec la gestion des jetons dans l'administration ; suivi de l'utilisation par jeton et par outil ou ressource (`fonctionnel/agents-ia.md` § Interface pour agents IA ; `choix-implementation/agents-ia.md` § Outil pour agents IA). Dépend des API de consultation (Phase 2), des statistiques (Phase 4) et de l'administration du frontend (Phase 3), qui accueille la gestion des jetons et le suivi de l'utilisation.

Découpage et version à établir à l'ouverture de la phase, une fois le périmètre de l'aide à la saisie précisé.

- **Note — modèle de domaine et journal des modifications** : la phase ajoute au modèle de domaine et à l'API les entités du suivi de l'outil (`modele-metier.md` : jeton d'accès, session, objectif de session, utilisation de l'outil, retour écrit), sans modifier les entités existantes, et étend le journal des modifications au jeton d'accès (type de fiche, correspondance de ses attributs, valeur consignée sans contenu pour son réarmement, `choix-implementation/domaine.md` § Journal des modifications : mise en œuvre).
- **Note — image du conteneur `mcp`** : le conteneur dédié `mcp` (`choix-implementation/agents-ia.md` § Outil pour agents IA) doit avoir son image, comme `api` et `frontend` : image de base (`choix-implementation/deploiement.md` § Images de base des conteneurs), image publiée `tetram76/bdtheque-mcp`, intégrée à la paire d'images construites toutes avant d'en publier aucune, à l'analyse des vulnérabilités et au job `docker-build` du CI (`choix-implementation/deploiement.md` § Publication des images Docker ; `gestion-projet/depot.md` § Intégration continue (CI), Ruleset compris). Sans elle, le déploiement sans les sources ne peut pas démarrer `mcp`.
