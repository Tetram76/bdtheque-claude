---
type: Règle métier
title: 'Recherches enregistrées'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Recherches enregistrées

- **Enregistrement** : l'administrateur peut enregistrer une recherche sous un **nom**, pour la rejouer ultérieurement.
- **Rejeu en consultation** : les recherches enregistrées sont proposées dans la consultation, dans un menu ou une liste ; les rejouer ne demande que les valeurs de leurs variables.
- **Variables** : à l'enregistrement, une ou plusieurs constantes des critères peuvent être remplacées par des **variables**. La valeur d'une variable est demandée lorsqu'on rejoue la recherche en consultation ; sa **valeur par défaut**, fixée par l'administrateur, est d'abord la constante qu'elle remplace.
- **Aucune intention d'achat désignée** : une intention d'achat n'est que l'intention d'acquérir une autre entité (un album ou une édition) ; une recherche enregistrée ne peut donc désigner aucune intention d'achat précise, ni par un critère ni par la valeur par défaut d'une variable. Elle peut en revanche rechercher les intentions d'achat, ou suivre le lien qui y mène (ex. « albums visés par une intention d'achat »). Une recherche ponctuelle, non enregistrée, peut désigner une intention d'achat.
- **Point de départ d'une nouvelle recherche** : en consultation, les critères d'une recherche enregistrée peuvent pré-charger le formulaire de construction de la recherche, comme modèle d'une nouvelle recherche. Aucun lien n'est conservé entre la recherche d'origine et la recherche complétée.
- **Gestion** : une page de l'administration gère les recherches enregistrées : suppression, renommage, modification des critères et des valeurs par défaut des variables.
