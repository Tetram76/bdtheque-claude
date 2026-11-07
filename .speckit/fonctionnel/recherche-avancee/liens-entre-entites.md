---
type: Règle métier
title: 'Liens entre entités'
description: 'Critères sur les entités liées, choix du chemin, quantificateurs et groupes sur une même entité liée.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Liens entre entités

- Un critère peut porter sur une donnée d'une **entité liée**, sur un ou plusieurs niveaux, en suivant les relations du modèle métier ([Relations et cardinalités](../../modele-metier/relations-et-cardinalites.md)) : deux entités sans lien direct ne sont reliées que par les entités intermédiaires du modèle.
- Lorsque plusieurs chemins relient deux entités, l'utilisateur choisit celui que suit le critère, et l'interface **met en évidence** ce chemin, au moins lors du choix de la donnée interrogée. Ex. un visuel est relié à une collection éditeur par la collection de son édition, ou par l'une des collections de l'éditeur de son édition.
- Un critère qui suit un lien vers **plusieurs** entités (ex. genres d'un album, éditions d'un album) précise s'il doit être satisfait par **au moins une**, par **aucune** ou par **toutes** les entités liées, ou par un **nombre** d'entités liées, comparé comme tout nombre (ex. « éditions dont l'album a **plus d'une** contribution de dessinateur », soit les éditions qui ont plusieurs dessinateurs) : sans ce choix, certaines combinaisons ne seraient pas exprimables. Sans entité liée (ex. un album sans édition), « au moins une » et « toutes » ne sont pas satisfaits, « aucune » l'est : « toutes » exige au moins une entité liée. Sur un chemin de plusieurs niveaux, **chaque** lien vers plusieurs entités porte son propre choix (ex. « séries dont **tous** les albums ont **au moins une** édition chez Dargaud »).
- Ce choix peut porter sur un **groupe de critères** appliqué à chaque entité liée, combinés comme tout autre groupe (cf. [Combinaison des critères](combinaison-des-criteres.md)), pour que plusieurs conditions portent sur **la même** entité liée : « albums ayant au moins une édition (chez Dargaud ET parue en 2000) » exige qu'une même édition remplisse les deux conditions, là où « albums ayant au moins une édition chez Dargaud ET au moins une édition parue en 2000 » les admet remplies par deux éditions différentes.
