# Persistance

* [Conventions de persistance (EF Core)](conventions-de-persistance-ef-core.md) - Énumérations en entier à valeurs explicites et leur registre, chargement des collections en requêtes séparées.
* [Identifiants](identifiants.md) - Identifiants Guid v7 créés par le domaine.
* [Cohérence entre tables](coherence-entre-tables.md) - Règles multi-lignes garanties par le domaine seul, verrou consultatif de la hiérarchie des univers.
* [Collation des colonnes texte](collation-des-colonnes-texte.md) - Collation ICU française, unicité insensible à la casse et aux accents, versions des collations vérifiées au démarrage.
* [Migrations EF Core](migrations-ef-core.md) - Migration unique avant le premier déploiement, append-only ensuite.
* [Concurrence d'accès](concurrence-d-acces.md) - Concurrence optimiste par xmin sur les racines d'agrégat, verrou et marquage de la racine à toute écriture.
* [Unicité vérifiée à la validation](unicite-verifiee-a-la-validation.md) - Contrainte unique différable, vérifiée à la validation de la transaction, pour une unicité que l'enregistrement d'un agrégat peut permuter (noms des collections d'un éditeur) ; revue des autres unicités des enfants d'agrégat.
* [Dates renseignées automatiquement : mise en œuvre](dates-renseignees-automatiquement-mise-en-oeuvre.md) - Dates de création, de modification et d'entrée dans la collection posées par le contexte, horloge de l'application, horloge des tests.
* [Collections de navigation du domaine](collections-de-navigation-du-domaine.md) - Collections en lecture seule, enfants créés par leur parent, AutoInclude des collections de l'album.
