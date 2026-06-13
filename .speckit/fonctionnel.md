# Fonctionnel

Ce fichier décrit les fonctionnalités de l'application, ainsi que les éléments de design et la charte graphique.

---

## Contexte

L'application est la réécriture d'une application client lourd existante sous forme d'application web n-tiers. L'application existante est une **BDthèque** : un gestionnaire de collection de bandes dessinées.

## Règles métier

### Gestion des devises

- L'application gère des **prix, montants et valeurs** (ex. valeur d'achat, valeur estimée d'un album, etc.).
- **Saisie et affichage des données de base** : peuvent être faits dans n'importe quelle devise.
- **Analyses et statistiques** : toujours affichées en **euro (€)**.
- **Agrégation multi-devises** : toute agrégation de données exprimées dans des devises différentes est convertie et consolidée en euro.
- **Taux de change** :
  - Certaines devises ont un taux **fixe et définitif** vis-à-vis de l'euro (ex. Franc français : 6,55957 FF = 1 €) → le taux est une constante.
  - D'autres devises ont un taux **variable** (ex. Dollar américain) → le taux de change utilisé devra être configurable ou récupéré dynamiquement.

## Design et charte graphique

<!-- À compléter : charte graphique (couleurs, typographie, iconographie), principes UX, composants UI, maquettes, etc. -->

<!-- À compléter : cas d'usage, user stories, autres règles métier, flux applicatifs, etc. -->
