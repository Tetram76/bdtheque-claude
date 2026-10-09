# Prototype : thèmes de rendu

Prototype **exploratoire et jetable** : il valide un mécanisme de thèmes pour la partie Consultation, sur les maquettes du dashboard (`.speckit/visuel/dashboard.md`). Rien de ce qu'il contient n'est statué : le `.speckit/` ne le mentionne pas, et il ne fait partie ni de la solution (`Bdtheque.slnx`), ni du CI, ni des images Docker. Ses données sont fictives.

## Idée explorée

- Un même thème s'applique à tout le site.
- Il est tiré au hasard à l'ouverture d'une session de navigation, et conservé tant que la navigation continue.
- Les pages ne sont **jamais dupliquées** par thème.

## Lancer

```bash
dotnet run --project poc/theming --urls http://localhost:5056
```

Une barre en bas de page affiche le thème de la session et permet d'en forcer un (`?theme=<id>`) ou d'en tirer un nouveau (`?theme=new`). Ce paramètre n'existe que pour la démonstration.

## Mécanisme

- **Données brutes** (`Data/`) : un modèle par page, sans rien de visuel, avec les liens déjà calculés. Il porte le sens des données, pas seulement leur texte (type de chaque prix, « Autres » distinct des éditeurs et des genres) : un thème doit pouvoir citer « le prix moyen » ou écarter « Autres » d'un podium.
- **Emplacements typés** (`Theming/Slots.cs`) : chaque page expose un emplacement de layout ; le layout place les emplacements de ses blocs. Un bloc qui attend d'autres données que son emplacement ne compile pas.
- **Thèmes avec héritage** (`Theming/Theme.cs`, déclarés dans `Program.cs`) : le thème racine définit tous les emplacements, ce que vérifie le démarrage ; les autres ne déclarent que ce qu'ils remplacent, le reste est résolu sur leur parent. Les feuilles de style s'empilent de la racine au thème actif.
- **Rendu** (`Theming/ThemeSlot.razor`) : résout le bloc du thème actif pour un emplacement et l'affiche avec `DynamicComponent`.
- **Sélection** (`Theming/ThemeSelection.cs`) : un middleware tire le thème d'un visiteur qui n'en a pas et le garde dans un cookie de session (sans expiration, supprimé à la fermeture du navigateur).
- **Éléments partagés** (`Components/Shared/`) : montants en deux devises, recherche et aide, phrases de synthèse communes à plusieurs thèmes. Ils ne sont pas thémés, seulement habillés.

Une page tient en une ligne (`Components/Pages/Dashboard.razor`, `Albums.razor`) : elle charge ses données et confie l'affichage à son emplacement de layout.

## Thèmes

| Thème | Parent | Composants propres | Hérités, réhabillés en CSS |
| --- | --- | --- | --- |
| Cases et bulles, violine | — | tous (racine) | — |
| Cases et bulles, mûre | violine | aucun : 5 lignes de CSS | tout |
| La gazette | violine | en-tête, layout, étagère de dos | accueil, nouveautés, valeur, genres, éditeurs, page Albums |
| Le sommaire de magazine | violine | layout, couverture, nouveautés, valeur, répartition, podium | en-tête, genres, page Albums |
| Le festival en métro | violine | layout et stations, affiche, programme, pavillons, lignes, exposants | en-tête, valeur, page Albums |

La page Albums n'est définie que par le thème racine : chaque thème l'affiche par repli, avec son propre en-tête et sa propre feuille de style.

## Enseignements

- **Le coût d'un thème suit ce qu'il change** : de 5 lignes de CSS (mûre) à un composant par station (festival). Une métaphore qui se prolonge mal hors du dashboard se paiera aussi sur chaque nouvel écran, sauf à accepter le repli sur le thème racine.
- **Les classes du thème racine sont un contrat.** Ses règles de placement ne s'appliquent qu'à ses propres layouts (`.dashboard > .block`) : appliquées à tout bloc, elles cassaient chaque layout qui le plaçait ailleurs (dimension de base devenue une hauteur en colonne). Une évolution du thème racine peut casser un thème enfant sans que rien ne le signale : il faudrait des tests de rendu par capture d'écran, pour chaque thème.
- **Les noms de classes doivent être neutres** : `.bubble` titre une case dans le thème racine, mais n'est plus une bulle dans les autres thèmes (`.block-title` conviendrait mieux).
- **Les textes propres à un thème ne sont pas traités** : la Gazette titre encore « Ça vaut combien, tout ça ? » au lieu de « Le cours de la collection ». Il faudrait des clés de traduction par thème, avec le même repli que les blocs. Les accroches rédigées à partir des chiffres (magazine) sont des données éditoriales, dont les règles seraient à définir.
- **Le CSS seul a ses limites** : le magazine réutilise le bloc des genres de la racine, sans l'accroche de sa maquette ; l'obtenir demanderait un bloc propre.

## Hors du périmètre du prototype

Tests, hébergement des polices (chargées ici depuis Google Fonts), vérification des contrastes, maquettes autres que le dashboard, et les questions fonctionnelles de l'idée : durée de la session, choix du visiteur, partie Administration, thèmes à retenir.
