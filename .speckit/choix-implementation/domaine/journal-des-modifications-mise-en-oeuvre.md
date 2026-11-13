---
type: Choix d'implémentation
title: 'Journal des modifications : mise en œuvre'
description: 'Capture des écritures à l''enregistrement, correspondance des attributs, libellés et valeurs en données, stockage JSON, API ; à lire pour toute fiche ou attribut ajouté.'
applies_to:
  - 'src/Bdtheque.Infrastructure/ChangeLog*.cs'
  - 'src/Bdtheque.Domain/Entities/ChangeLog*.cs'
  - 'src/Bdtheque.Domain/Entities/RecordLabel.cs'
  - 'src/Bdtheque.Api/Endpoints/ChangeLogEndpoints.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-13T10:00:00Z }
---

# Journal des modifications : mise en œuvre

- **Entrées capturées à l'enregistrement** ([Entrée du journal des modifications](../../modele-metier/entree-du-journal-des-modifications.md)), point de passage unique de toute écriture, comme les dates ([Dates renseignées automatiquement : mise en œuvre](../persistance/dates-renseignees-automatiquement-mise-en-oeuvre.md)), d'après ce que le suivi des modifications s'apprête à écrire, dans la même transaction : aucune opération du domaine ni aucun endpoint n'y pense, et la reprise des données les produira de même. Une fiche ajoutée au modèle y est ajoutée avec son libellé et la correspondance de ses attributs.
  - **Alternative écartée (entrées produites par chaque endpoint)** : autant d'appels à ne pas oublier que d'écritures, sans rien pour signaler un oubli.
  - Une **modification est déduite des valeurs**, jamais de l'état de l'entité : la racine d'un agrégat étant marquée modifiée à toute écriture ([Concurrence d'accès](../persistance/concurrence-d-acces.md)), une écriture qui ne change rien de la fiche ne produit pas d'entrée.
  - Un **élément porté** (genre, univers, contribution, visuel) ajouté, retiré ou réarrangé est un changement de la fiche qui le porte ; son retrait par la suppression de l'élément associé, fait par la base, n'en est pas un : l'entrée de suppression de l'élément en rend compte.
  - Les **fiches supprimées avec une autre** reçoivent chacune leur entrée : elles doivent être chargées, pour que leur suppression passe par le suivi plutôt que par la cascade de la base, hors de vue du journal ; un enregistrement qui supprimerait par cascade une fiche non chargée est refusé.
  - **Enregistrement asynchrone seulement** : les libellés lisent en base les fiches que l'enregistrement ne suit pas.
- **Chaque propriété d'une fiche forme un attribut, ou est écartée délibérément** (valeurs dérivées ou renseignées automatiquement, rattachements qui ne changent jamais) ; un test échoue pour toute propriété ajoutée sans être classée. Un attribut n'est consigné que si sa valeur change. L'empreinte d'un jeton d'accès, secret, forme un attribut consigné **sans valeur** : son renouvellement apparaît, sans rien révéler.
- **Des données, jamais des textes** ([Langue et culture d'affichage](../../fonctionnel/presentation/langue-et-culture-d-affichage.md)) : le libellé est enregistré par ses composantes, chaque valeur par son type, et le frontend construit le message dans la culture d'affichage. Le libellé est relevé sur la fiche telle que l'écriture la laisse, ou telle qu'elle était pour une suppression. La référence du média d'un visuel, donnée technique ([Visuel d'édition](../../modele-metier/visuel-d-edition.md)), n'en fait pas partie.
- **Stockage en `jsonb`, sans clé étrangère vers la fiche**, qui peut disparaître. Le journal n'étant jamais purgé, sa forme JSON est **figée par des tests** (discriminants et noms de propriétés jamais renommés : un renommage rendrait illisibles les entrées déjà écrites), les membres d'énumération étant enregistrés par leur valeur, qui garde son sens ([Conventions de persistance (EF Core)](../persistance/conventions-de-persistance-ef-core.md)). `jsonb` réordonnant les clés, la lecture accepte le discriminant à toute position : une entrée relue est toujours l'entrée écrite.
- **Dernières fiches écrites** ([Contenu](../../visuel/administration/contenu.md), Dernières modifications) : chaque fiche une fois, par sa dernière entrée, sans les fiches supprimées (un identifiant n'est jamais redonné). Les fiches sont lues d'abord, par regroupement, puis leurs entrées : EF Core ne traduit la première entrée d'un groupe que comme dernier opérateur d'une requête (documentation d'EF Core, *Complex query operators*).
- **Parent lu avec l'entrée, jamais enregistré** : une édition et une intention d'achat, adressées par leur album, une collection, saisie avec son éditeur, n'ont de page que sous leur parent ([Contenu](../../visuel/administration/contenu.md), Dernières modifications) ; l'API le lit avec les entrées, pour les fiches qui existent encore, et la fiche supprimée depuis n'en a pas, ni de page où mener.
  - **Alternative écartée (parent enregistré dans l'entrée)** : un attribut de plus dans le modèle de l'entrée, que les entrées déjà écrites n'ont pas, pour une donnée qui dit où est la fiche, et non ce qu'elle était, ce que l'entrée consigne.
