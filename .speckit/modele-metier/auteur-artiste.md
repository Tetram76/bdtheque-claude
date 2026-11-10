---
type: Modèle métier
title: 'Auteur / Artiste'
description: 'Attributs d''un auteur, au moins un nom ou un pseudonyme.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/Author.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/AuthorConfiguration.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-10T15:32:35Z }
---

# Auteur / Artiste

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Nom | texte | non | |
| Prénom | texte | non | |
| Pseudonyme | texte | non | |
| Biographie | texte long | non | |
| Nationalité | texte | non | |

> **Contraintes :** Au moins un des champs Nom ou Pseudonyme doit être renseigné. Un auteur peut représenter un duo ou collectif (ex. deux auteurs créditant sous un nom commun).
>
> Le **nom affiché** d'un auteur, son identifiant principal ([Artistes](../fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes) : le pseudonyme, sinon le prénom et le nom), est **unique sans tenir compte de la casse ni des accents** : deux fiches ne peuvent pas porter le même, `Moebius`, `Mœbius` et `MOËBIUS` désignant le même auteur. Des homonymes de même nom affiché ne sont donc pas saisissables comme deux auteurs ; deux fiches d'un même auteur saisies en double se fusionnent ([Structure de l'application](../fonctionnel/structure-de-l-application.md)).
