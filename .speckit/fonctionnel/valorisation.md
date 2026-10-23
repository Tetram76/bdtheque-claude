# Estimations et devises

Calcul des estimations, estimation de la valeur des éditions, coût de remplacement de la collection et gestion des devises.

---

## Calcul des estimations

Toute **estimation** produite par l'application (valeur estimée d'une édition, estimation de sortie d'un nouvel album) est **toujours calculée dynamiquement**, à partir de l'**état courant** de la base : elle n'est jamais stockée, ni figée à partir d'un état antérieur des données.

Objectif : tirer parti de l'enrichissement progressif de la base pour **affiner les estimations au fil du temps**, dans la mesure où les données ajoutées le permettent.

## Estimation de la valeur des éditions

- La **valeur estimée** d'une édition est l'estimation de son **prix d'acquisition** lorsqu'il n'est pas renseigné. Seules les éditions **possédées**, **non gratuites** et **sans prix d'acquisition** en ont une : une édition dont le prix d'acquisition est connu n'est jamais estimée.
- Elle sert à valoriser la collection : la **valeur totale estimée** est la somme des prix d'acquisition connus et des valeurs estimées.
- Elle suit la règle de [Calcul des estimations](#calcul-des-estimations) : elle est calculée à partir des données de la collection, et n'est pas stockée en base.
- Le modèle d'estimation retenu est un **Random Forest**.

## Coût de remplacement de la collection

Statistique **envisagée** : rien n'est statué, elle n'est pas à implémenter tant qu'elle n'est pas décidée (YAGNI).

- Le **coût de remplacement** de la collection est ce que coûterait aujourd'hui l'acquisition de toutes ses éditions : la valeur de chaque édition y est remplacée par son **prix actuel**.
- **Problème non résolu** : obtenir ces prix actuels. Le volume de la collection rend impraticable une recherche du prix de chaque édition, et certaines éditions ne sont plus en vente.
- **Piste envisagée** : établir des **groupes d'éditions** selon des critères **hors dates** (par exemple à la manière d'un Random Forest), récupérer les prix actuels de quelques éditions de chaque groupe, puis **extrapoler** ces prix aux autres éditions du même groupe.

## Gestion des devises

- L'application gère des **prix, montants et valeurs** (ex. valeur d'achat, valeur estimée d'un album, etc.).
- **Saisie et affichage des données de base** : peuvent être faits dans n'importe quelle devise. Une fiche affiche chaque montant **deux fois** : la valeur saisie, dans sa devise, et sa valeur en **euros du jour**.
- **Euros courants** : un montant exprimé en euros courants est **converti en euro** au taux de change de sa date de référence (cf. *Taux de change* ci-dessous), **sans correction de l'inflation**.
- **Euros du jour** : pour qu'une valorisation ait un sens quelle que soit l'époque du montant, un montant exprimé en euros du jour est d'abord exprimé en **euros courants**, puis **corrigé de l'inflation** entre sa date de référence et aujourd'hui. La correction utilise les coefficients annuels de **pouvoir d'achat de l'euro et du franc** publiés par l'**[INSEE](https://www.insee.fr/fr/information/2417794)** (source officielle et gratuite, disponible depuis 1901) : elle est **annuelle**, une date de référence plus précise étant corrigée selon son année.
- **Analyses et statistiques** : chaque montant est affiché **en parallèle** dans les deux versions, pour mettre en relation l'**investissement réalisé** (euros courants) et son **équivalent en pouvoir d'achat** à la date du jour (euros du jour). Un montant non convertible (cf. *Taux de change* ci-dessous) n'entre dans aucune des deux : elles portent toujours sur les mêmes données.
- **Comparaison de montants** : toute comparaison entre montants (classement, minimum, maximum, record…) se fait **en euros du jour**, seule version qui compare les époques entre elles, sauf cas particulier où l'on souhaite explicitement comparer les montants **d'origine** ou en **euros courants** (ex. un critère de la recherche avancée, cf. `fonctionnel/recherche-avancee.md` § Recherche avancée). Le résultat reste affiché dans les deux versions.
- **Agrégation multi-devises** : toute agrégation de données exprimées dans des devises différentes est convertie et consolidée en euro, dans ces deux versions.
- **Date de référence d'un montant** : première date connue, dans cet ordre :
  - **prix d'acquisition** : date d'acquisition, puis année d'édition, puis date de première publication de l'album ;
  - **valeur initiale** : année d'édition, puis date de première publication de l'album.

  Un montant ne peut pas être enregistré sans date de référence, **quelle que soit sa devise**.
- **Taux de change** : tout montant est **stocké dans sa devise** de saisie, jamais converti à l'enregistrement. Lorsqu'il doit être exprimé en euro, quel que soit l'usage (statistiques, valeur de la collection, estimation de valeur), il est converti au taux de change en vigueur à sa **date de référence** :
  - Certaines devises ont un taux **fixe et définitif** vis-à-vis de l'euro (ex. Franc français : 6,55957 FF = 1 €) → le taux est une constante, identique quelle que soit la date.
  - **Ancien franc** : proposé comme devise à la saisie, pour les prix français antérieurs au nouveau franc de 1960 ; il est converti à raison de 100 anciens francs pour 1 franc, puis comme le franc.
  - D'autres devises ont un taux **variable** (ex. Dollar américain) → le taux est celui de la date de référence, récupéré depuis l'API **[Frankfurter](https://www.frankfurter.app/)** (open source, gratuite, sans clé API). Lorsque la date de référence est partielle, le taux appliqué est le **taux moyen** de la période connue : celui du **mois** pour un mois et une année, celui de l'**année** pour une année seule.
  - **Montant non convertible** : un montant pour lequel aucun taux de change ou coefficient d'inflation n'est disponible à sa date de référence (date antérieure à l'historique de la devise ou à 1901) peut être saisi et reste affiché dans sa devise, mais il n'est pas pris en compte dans les calculs exprimés en euro.
