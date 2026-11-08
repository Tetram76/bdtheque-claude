---
type: Choix d'implémentation
title: 'Suppression des entités : mise en œuvre'
description: 'Impact et empreinte de suppression, verrouillage racine puis enfant, mécanisme commun, règles ON DELETE, fichiers des visuels.'
applies_to:
  - 'src/Bdtheque.Api/Deletion/**'
  - 'src/Bdtheque.Api/Endpoints/DeletionEndpoints.cs'
  - 'src/Bdtheque.Contracts/Deletion/**'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Suppression des entités : mise en œuvre

- **Règles appliquées par l'API avant la suppression** ([Suppression des entités](../../fonctionnel/regles-metier/suppression-des-entites.md)) : une fiche liée par une *Référence* la refuse (erreur métier). Ce contrôle est **indispensable** : sur une relation optionnelle dont les dépendants sont chargés, EF Core met leur clé étrangère à `null` au lieu d'échouer, quel que soit le `ON DELETE` (une série supprimée détacherait ses albums en silence).
- **Un impact de suppression qui porte tout ce que le message annonce** : les fiches concernées par type d'entité et par nature de lien, et les **albums qui sortent de la collection** parce que la suppression emporte leur dernière édition possédée, effet qu'aucun compte de liens ne révèle. Le client en construit la confirmation.
- **La suppression n'emporte jamais plus que ce qui a été confirmé** : la demande transmet une **empreinte** de l'impact affiché, calculée sur l'**identité** des fiches concernées (des comptes resteraient égaux si une association disparaissait pendant qu'une autre apparaissait). L'API recalcule impact et empreinte dans la transaction de suppression, après avoir **verrouillé la racine de l'agrégat puis l'entité** ; un écart est une erreur fonctionnelle qui porte le nouvel impact, à reconfirmer.
  - **Ordre racine → enfant**, celui de toute écriture sur un agrégat ([Concurrence d'accès](../persistance/concurrence-d-acces.md)) : un ordre unique empêche deux transactions de s'attendre mutuellement.
  - **Le verrou suffit, sans isolation sérialisable** : toute écriture qui créerait un lien vers l'entité pose un verrou de clé étrangère incompatible (PostgreSQL, *Explicit Locking*), attend la fin de la suppression, puis échoue ; toute écriture validée avant est vue par le recalcul.
  - La garantie porte sur ce que la suppression **ajoute** à l'impact confirmé, jamais sur ce qu'une autre écriture en **retire** : l'état final est alors un sous-ensemble de l'effet confirmé, sans rien d'inattendu. Verrouiller les lignes de jointure n'apporterait rien.
  - La version de l'entité ne suffit pas : ajouter un genre à un album modifie l'album, pas le genre.
- **Un mécanisme commun**, tiré des liens que déclare chaque ressource : les **recherches enregistrées qui désignent** la fiche ou ses compositions ([Recherches enregistrées : mise en œuvre](../recherche-avancee/recherches-enregistrees-mise-en-oeuvre.md)) y sont ajoutées par le mécanisme lui-même, sans qu'aucune ressource puisse les oublier.
- **Un enfant d'agrégat est supprimé par sa racine**, dont la version est vérifiée, puis marquée modifiée ; une suppression qui emporte plus que sa ligne (une intention emporte son édition) passe par l'opération de l'agrégat, dont l'effet est celui que l'impact annonce.
- **Le schéma reflète les mêmes règles** : `RESTRICT` pour chaque *Référence* (filet de sécurité, traduit en erreur métier), `CASCADE` pour chaque *Composition* et pour les jointures des *Associations*. Un test classe chaque clé étrangère dans une de ces natures, et échoue pour toute clé ajoutée sans classement, qui échapperait sinon aux règles de suppression.
- **Fichiers des visuels supprimés après la validation** de la transaction, d'après les visuels que l'impact annonce : un échec de la suppression laisse les fichiers intacts, un échec sur un fichier ne laisse qu'un orphelin, rattrapé par la réconciliation ([Visuels : stockage et traitement](../domaine/visuels-stockage-et-traitement.md)). Ce relevé appartient au mécanisme commun, pour qu'aucune suppression ne l'oublie.
