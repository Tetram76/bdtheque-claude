# Reprise des données

Application existante (client lourd) et reprise de ses données.

---

## Application existante

| Élément | Valeur |
| --- | --- |
| Langage | Delphi 10.3 Rio (10.3.3) |
| Base de données | Firebird 2.5 |
| Connexion à la base (production) | Firebird embarqué (*embedded*) |
| Type | Client lourd (desktop) |
| Dépôt des sources | <https://github.com/Tetram76/tetram> (dossier `bdtheque/`) |

## Reprise des données

- Des données existantes sont stockées dans la base Firebird 2.5 de l'application client lourd.
- Une **migration de données** depuis Firebird vers la base cible devra être possible lorsque la nouvelle application sera suffisamment mature.
- La conception du modèle de données cible doit tenir compte de cette migration future : préserver la sémantique des données existantes et ne pas rendre la migration inutilement complexe.
- La migration est une **opération unique** (*one-shot*) : aucun import successif ni partiel. Elle **fusionne** les données importées avec les données déjà présentes dans la base cible lorsqu'il y a correspondance (pas de doublons, mise à jour des éléments existants).
- La migration est **unidirectionnelle** : aucun retour en arrière, aucune synchronisation vers l'application client lourd.
- Elle ne donne lieu à aucun outil de migration maintenu : c'est une opération ponctuelle, préparée le moment venu.

### Base existante

Caractéristiques de la base Firebird de l'application existante, que la migration doit prendre en compte (moyens d'accès retenus : cf. `choix-implementation/reprise-donnees.md` § Accès à la base existante (Firebird)) :

| Élément | Valeur |
| --- | --- |
| Fichier | Copie de la base de production, fournie hors dépôt sous un nom non fixé (ex. `BD.GDB`), accompagnée de `BDT_UDF.dll` |
| Format | ODS 11.2 (Firebird 2.5), pages de 16 Ko |
| Tables | `ALBUMS`, `ALBUMS_UNIVERS`, `AUTEURS`, `AUTEURS_PARABD`, `AUTEURS_SERIES`, `COLLECTIONS`, `CONVERSIONS`, `COTES`, `COTES_PARABD`, `COUVERTURES`, `CRITERES`, `EDITEURS`, `EDITIONS`, `EMPRUNTEURS`, `GENRES`, `GENRESERIES`, `IMPORT_ASSOCIATIONS`, `LISTES`, `OPTIONS`, `OPTIONS_SCRIPTS`, `PARABD`, `PARABD_UNIVERS`, `PERSONNES`, `PHOTOS`, `SERIES`, `SERIES_UNIVERS`, `STATUT`, `SUPPRESSIONS`, `UNIVERS` |
| Texte | Jeu de caractères `UTF8` |

- **Collations propres à la base** : `UTF8_FR`, `UTF8_FR_CI` et `UTF8_FR_CI_AI`, dérivées de `UNICODE` avec `LOCALE=fr_FR`, portent l'attribut `COLL-VERSION=58.0.6.50`, celui d'**ICU 52**. L'application existante livre cet ICU 52 avec Firebird (dossier `bdtheque/delphi/trunk/deploy/` du dépôt des sources ; ses DLL 64 bits ont la taille de celles du build officiel ICU4C 52.1 Win64 msvc10), et son `intl/fbintl.conf` le déclare pour le module `builtin` (`icu_versions 5.2`). Avec l'ICU 3.0 des kits officiels Firebird 2.5.9, Firebird refuse toute requête sur une table qui utilise ces collations (`COLLATION … is not installed`).
- **Fonctions externes (UDF)** : la base déclare 20 UDF de la bibliothèque propre à l'application, `BDT_UDF.dll` (dépôt des sources : `bdtheque/delphi/trunk/src/BDT_UDF.DLL/`, sans binaire compilé ; la DLL est fournie avec la base). Certaines agissent sur le système de fichiers (`UDF_DELETEFILE`, `UDF_SAVEBLOBTOFILE`, `UDF_LOADBLOBFROMFILE`, `UDF_FINDFILEFIRST`…).
- **Vues et procédures stockées** : 20 vues (`VW_*`) et des procédures stockées qui portent une partie de la logique de l'application (listes par initiale, albums manquants, prévisions de sorties…). Certaines procédures construisent leur requête en concaténant un paramètre `FILTRE` ; d'autres enveloppent les UDF de fichiers (`DELETEFILE`, `SAVEBLOBTOFILE`, `LOADBLOBFROMFILE`, `DIRECTORYCONTENT`, `SEARCHFILENAME`).
- **Titres stockés sous forme de tri** (`ALBUMS.TITREALBUM`, `SERIES.TITRESERIE`) : l'article initial est reporté en suffixe entre crochets (`fils d'Asterix [Le]`, `étoile du désert [L']`), selon la même convention que la clé de tri de `fonctionnel/presentation.md` § Titres (séries et albums). La forme affichée est reconstituée par l'UDF `UDF_FORMATTITLE` (`Le fils d'Asterix`).
- **Date d'achat absente** (`EDITIONS.DATEACHAT`) : une édition sans date d'achat a été acquise **avant 2004**.
- **Initiale stockée** (`ALBUMS.INITIALETITREALBUM`, `CHAR(1)` en `UTF8`) : initiale **brute** du titre, non normalisée — casse et accents conservés (`É` et `é` sont deux valeurs distinctes), chaque chiffre est une valeur à part entière, `#` figure parmi les valeurs, et la colonne est vide (`NULL`) pour un album sans titre propre, que l'application range sous l'initiale de sa série (`coalesce(initialetitrealbum, initialetitreserie)`, procédure `INITIALES_ALBUMS`). Elle ne correspond donc pas aux entrées de navigation de `fonctionnel/presentation.md` § Entrées de la navigation par initiale.

### Données non reprises

| Données | Tables de la base existante | Motif |
| --- | --- | --- |
| Objets para-BD (avec leurs auteurs, univers, cotes et photos) | `PARABD`, `AUTEURS_PARABD`, `PARABD_UNIVERS`, `COTES_PARABD`, `PHOTOS` | Absents de la nouvelle application |
| Prêts (emprunteurs, suivi des prêts d'éditions) | `EMPRUNTEURS`, `STATUT`, colonnes `EDITIONS.PRETE` et `EDITIONS.STOCK` | Absents de la nouvelle application |
| Cotes des éditions (historique daté de leur valeur de marché) | `COTES`, colonnes `EDITIONS.ANNEECOTE` et `EDITIONS.PRIXCOTE` | Absentes de la nouvelle application |
| Taux de conversion saisis | `CONVERSIONS` | Taux fixes constants et taux variables issus de Frankfurter (`fonctionnel/valorisation.md` § Gestion des devises) |
| Listes de valeurs modifiables | `LISTES` | Énumérations fixes dans la nouvelle application (`modele-metier.md`) ; seules les valeurs portées par les fiches sont reprises, traduites vers ces énumérations |
| Paramétrage et critères de recherche du client lourd | `OPTIONS`, `OPTIONS_SCRIPTS`, `CRITERES` | Propres au client lourd |
| Visuel rattaché à un album sans édition | Lignes de `COUVERTURES` sans `ID_EDITION` | Un visuel appartient toujours à une édition (`modele-metier.md` § Visuel d'édition) |
| Correspondances d'import (libellé d'une source externe → fiche) | `IMPORT_ASSOCIATIONS` | Absentes de la nouvelle application |
| Journal des suppressions | `SUPPRESSIONS` | Les suppressions faites dans la base existante ne sont pas répercutées par la reprise |
| Sites web des séries, des auteurs et des univers | Colonnes `SERIES.SITEWEB`, `PERSONNES.SITEWEB`, `UNIVERS.SITEWEB` | Absents de la nouvelle application |
| Note d'une série | Colonne `SERIES.NOTATION` | Absente de la nouvelle application |
| Version originale | Colonnes `EDITIONS.VO`, `SERIES.VO` | Absente de la nouvelle application |
| Clés phonétiques des titres | Colonnes `SOUNDEX*` | Pas de recherche phonétique dans la nouvelle application |

### Correspondances particulières

| Données de la base existante | Reprise | Motif |
| --- | --- | --- |
| Éditions gratuites non offertes (`EDITIONS.GRATUIT = 1`, `EDITIONS.OFFERT = 0`) | Mode d'acquisition `Offerte`, gratuité conservée | Une édition achetée ne peut pas être gratuite (`modele-metier.md` § Édition) |
| Prix d'acquisition antérieurs à l'euro (`EDITIONS.PRIX`, toujours exprimé en euros dans la base existante, les francs y étant convertis à la saisie) | Reprise dans la devise réellement payée, reconvertie en francs (6,55957 FF = 1 €) si nécessaire, ou en anciens francs (655,957 anciens francs = 1 €) pour un prix antérieur au nouveau franc de 1960 :<br>- **devise certaine** : le prix est repris tel quel s'il a été payé en euros, reconverti en francs sinon ;<br>- **devise incertaine** : le montant en euros et son équivalent en francs sont comparés, et le plus vraisemblable comme prix affiché est retenu ;<br>- **doute persistant** : la devise est arbitrée au cas par cas par l'utilisateur. | Restituer le montant dans la devise réellement payée (`fonctionnel/valorisation.md` § Gestion des devises) |
