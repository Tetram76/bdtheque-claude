---
type: Règle métier
title: 'Gestion des devises'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Gestion des devises

- L'application gère des **prix, montants et valeurs** (ex. valeur d'achat, valeur estimée d'un album, etc.).
- **Saisie et affichage des données de base** : peuvent être faits dans n'importe quelle devise. Une fiche affiche chaque montant **deux fois** : la valeur saisie, dans sa devise, et sa valeur en **euros du jour**.
- **Euros courants** : un montant exprimé en euros courants est **converti en euro** au taux de change de sa date de référence (cf. *Taux de change* ci-dessous), **sans correction de l'inflation**.
- **Euros du jour** : pour qu'une valorisation ait un sens quelle que soit l'époque du montant, un montant exprimé en euros du jour est d'abord exprimé en **euros courants**, puis **corrigé de l'inflation** entre sa date de référence et aujourd'hui. La correction utilise les coefficients annuels de **pouvoir d'achat de l'euro et du franc** publiés par l'**[INSEE](https://www.insee.fr/fr/information/2417794)** (source officielle et gratuite, disponible depuis 1901) : elle est **annuelle**, une date de référence plus précise étant corrigée selon son année.
- **Analyses et statistiques** : chaque montant est affiché **en parallèle** dans les deux versions, pour mettre en relation l'**investissement réalisé** (euros courants) et son **équivalent en pouvoir d'achat** à la date du jour (euros du jour). Un montant non convertible (cf. *Taux de change* ci-dessous) n'entre dans aucune des deux : elles portent toujours sur les mêmes données.
- **Comparaison de montants** : toute comparaison entre montants (classement, minimum, maximum, record…) se fait **en euros du jour**, seule version qui compare les époques entre elles, sauf cas particulier où l'on souhaite explicitement comparer les montants **d'origine** ou en **euros courants** (ex. un critère de la recherche avancée, cf. [Recherche avancée](/fonctionnel/recherche-avancee/recherche-avancee.md)). Le résultat reste affiché dans les deux versions.
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
