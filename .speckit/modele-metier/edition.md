---
type: Modèle métier
title: 'Édition'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Édition

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Année d'édition | entier (année) | non | Année de publication de cette édition. |
| ISBN | texte | non | Formats **ISBN-10** et **ISBN-13/EAN-13** tous deux supportés. La saisie doit permettre de détecter les erreurs de frappe (contrôle du chiffre de vérification). |
| Reliure | énuméré | non | `Brochée` / `Cartonnée`. Proposé à la saisie : `Cartonnée`. |
| Orientation | énuméré | non | `Portrait` / `Italienne`. Proposé à la saisie : `Portrait`. |
| Sens de lecture | énuméré | non | `Gauche à droite` / `Droite à gauche`. Proposé à la saisie : `Gauche à droite`. |
| Format | énuméré | non | `Poche` / `Moyen (A5)` / `Normal (A4)` / `Grand (> A4)` / `Spécial`. Proposé à la saisie : `Normal (A4)`. |
| Nombre de pages | entier | non | |
| Catégorie | énuméré | non | `Édition originale` / `Édition spéciale` / `Tirage de tête`. |
| Dédicacée | booléen | oui | `false` par défaut. |
| En couleur | booléen | oui | `true` par défaut. |
| État | énuméré | non | `Excellent (état neuf)` / `Très bon` / `Bon` / `Moyen` / `Mauvais` / `Très mauvais`. « Excellent » plutôt que « Neuf » : une édition qui n'a pas été achetée neuve peut être dans cet état. Proposé à la saisie : `Excellent (état neuf)`. |
| Mode d'acquisition | énuméré | conditionnel | `Achat` / `Offerte` / `Échange` / `Gagnée` / `Héritée`. Obligatoire si l'édition est possédée. Conditionne le libellé de la date et du montant en interface (voir [Libellés contextuels sur l'édition](/fonctionnel/presentation/libelles-contextuels-sur-l-edition.md)). |
| D'occasion | booléen | oui | `false` = neuve, `true` = occasion. |
| Date d'acquisition | date | non | Date à laquelle l'utilisateur a obtenu l'édition. |
| Date d'entrée dans la collection | date et heure | conditionnel | Renseignée automatiquement, jamais saisie, au moment où l'édition devient possédée (création d'une édition possédée, ou confirmation de l'achat d'une édition visée par une intention d'achat) ; absente tant que l'édition n'est pas possédée. Contrairement à la date d'acquisition, facultative, elle est toujours connue pour une édition possédée : elle sert à repérer les dernières entrées dans la collection. Pour une édition reprise de l'application existante, c'est la date de la reprise. |
| Nombre de mises à la une | calculé | — | Nombre de jours où l'édition a été mise à la une du dashboard, dont le tirage privilégie les éditions les moins souvent présentées (voir [Page d'accueil](/fonctionnel/page-accueil.md)). Déduit de l'historique des mises à la une ([Édition « À la une » : mise en œuvre](/choix-implementation/domaine/edition-a-la-une-mise-en-oeuvre.md)), non stocké sur l'édition : un tirage n'est jamais une écriture sur la fiche. |
| Prix d'acquisition | montant + devise (code **ISO 4217 alpha-3**, cf. [Représentation de la devise](/choix-implementation/domaine/representation-de-la-devise.md), ou **ancien franc**, cf. [Gestion des devises](/fonctionnel/valorisation/gestion-des-devises.md)) | non | Optionnel. `null` = aucun montant enregistré (prix inconnu ou non applicable). Pour une édition achetée, c'est le prix payé au moment de l'acquisition. Pour les modes sans transaction financière (ex. `Offerte`, `Héritée`), le champ peut accueillir une valeur marchande connue. Stocké dans sa devise ; converti en euro à l'usage, au taux de sa date de référence (cf. [Gestion des devises](/fonctionnel/valorisation/gestion-des-devises.md)). |
| Gratuite | booléen | oui | `false` par défaut. Indique que l'édition **n'a pas de valeur** : aucun montant n'est enregistré (ni prix payé, ni valeur marchande, ni valeur initiale) et aucune valeur n'est estimée. Interdit pour une édition achetée. Si `true`, le prix d'acquisition et la valeur initiale doivent être `null` (contrainte d'intégrité) ; ces champs sont désactivés et vidés en interface. |
| Valeur initiale | montant + devise (même représentation que le prix d'acquisition, ancien franc compris) | non | Prix de vente de l'édition à sa parution. Stocké dans sa devise ; converti en euro à l'usage, au taux de sa date de référence (cf. [Gestion des devises](/fonctionnel/valorisation/gestion-des-devises.md)). |
| Numérotation personnelle | texte | non | Référence libre saisie par l'utilisateur (ex. cote, numéro de rangement). |
| Valeur estimée | calculée | — | Estimation du prix d'acquisition d'une édition possédée, non gratuite, dont le prix d'acquisition n'est pas renseigné (voir [Estimation de la valeur des éditions](/fonctionnel/valorisation/estimation-de-la-valeur-des-editions.md)). Calculée dynamiquement, non stockée. |
| Notes personnelles | texte long | non | Annotations libres saisies par l'utilisateur. |

> **Valeurs par défaut des attributs énumérés** (État, Reliure, Orientation, Sens de lecture, Format) : ce sont uniquement des valeurs **proposées à la saisie**, qui pré-remplissent le formulaire de création. Elles ne sont **pas portées par la base** (aucune clause `DEFAULT` sur les colonnes correspondantes) ni appliquées à l'enregistrement : un attribut laissé vide est enregistré vide.

> **Contraintes d'intégrité :**
>
> - Si `Mode d'acquisition` est `null` (édition non possédée, ex. issue d'une intention d'achat), alors `Date d'acquisition`, `Prix d'acquisition` et `Valeur initiale` doivent également être `null` : une édition non possédée n'a pas de valeur.
> - Si `Gratuite` est `true`, alors `Prix d'acquisition` et `Valeur initiale` doivent être `null` : une édition gratuite n'a pas de valeur.
> - Si `Mode d'acquisition` est `Achat`, alors `Gratuite` doit être `false` : une édition achetée ne peut pas être gratuite.
> - Si `Prix d'acquisition` est renseigné, au moins une de ses dates de référence doit être connue : `Date d'acquisition`, `Année d'édition` ou `Date de première publication` de l'album.
> - Si `Valeur initiale` est renseignée, `Année d'édition` ou `Date de première publication` de l'album doit être connue.
