# Fonctionnalités de second plan

Fonctionnalités de second plan : identification des albums manquants et estimation de sortie d'un nouvel album.

---

Ces fonctionnalités sont prévues dans une phase ultérieure.

## Identification des albums manquants dans une série

Accessible en **mode consultation** (public). Permet de savoir quels albums d'une série ne sont pas encore présents dans la collection.

Un tome est **manquant** s'il est absent de la séquence théorique de tomes de la série (voir [Séquence théorique de tomes d'une série](regles-metier.md#séquence-théorique-de-tomes-dune-série)).

Les intégrales **couvrent** la plage `tome de début → tome de fin` de la séquence (chaque tome de la plage est considéré présent).

**Exclusions systématiques :**

- les séries avec l'attribut **Exclure des manquants**

**Affichage des manquants :**

Les tomes manquants consécutifs sont regroupés et affichés sous forme d'intervalle (ex. `T. 3 à 5` plutôt que `T. 3`, `T. 4`, `T. 5`).

**Options utilisateur :**

- **Exclure les intégrales** : les intégrales ne couvrent plus leur plage (pour posséder chaque tome en album individuel).
- **Exclure les intentions d'achat** : les intentions ne comblent plus les trous — affiche tous les manquants réels, intentions incluses.

## Estimation de sortie d'un nouvel album

Accessible en **mode consultation** (public). Permet d'estimer la date de sortie du prochain tome d'une série, sur la base du rythme de parution observé.
On parle d'**estimation** et non de prévision : le facteur humain rend toute prédiction précise impossible, et l'objectif est uniquement de donner un ordre de grandeur.

Comme toute estimation, elle suit la règle de [Calcul des estimations](valorisation.md#calcul-des-estimations).

Règles de calcul :

1. L'estimation se fait **série par série**. Elle n'est calculée que pour les séries dont le statut est `En cours` — les séries `Terminée` (tous les albums prévus ont été publiés) et `Abandonnée` ne font l'objet d'aucune estimation — et qui n'ont pas l'attribut **Exclure des estimations de sortie**.
2. Elle se base sur les **dates de première publication** des albums réguliers de la série (les intégrales et hors-série sont exclus du calcul).
3. Il faut **au moins 2 albums réguliers** avec une date de première publication **passée** pour pouvoir produire une estimation. Les albums dont la date de première publication est strictement dans le futur sont exclus du calcul du rythme. La règle s'adapte à la granularité de la date saisie :

- **Mois + année** : le mois en cours est considéré comme passé. Seuls les mois strictement postérieurs au mois courant sont exclus.
- **Année seule** : l'année en cours est considérée comme passée. Seules les années strictement postérieures à l'année courante sont exclues.

4. Le délai attendu avant le prochain tome est déduit à partir des **délais entre les parutions précédentes**, en tenant compte des éventuels **trous dans la séquence de tomes** (un tome manquant dans la collection ne doit pas fausser le calcul du rythme).
5. Les **évolutions de rythme** (accélération ou décélération des parutions) doivent être prises en compte dans l'estimation.
