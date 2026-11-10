---
type: Référence visuelle
title: 'Saisie selon l''entité'
description: 'Saisie d''une fiche selon l''entité : page dédiée, dans la ligne, panneau latéral.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-10T15:32:35Z }
---

# Saisie selon l'entité

La saisie d'une fiche s'ouvre selon le poids de l'entité :

| Entité | Saisie |
| --- | --- |
| Série, édition, éditeur | **Page dédiée**, comme l'album ; l'éditeur, qui porte ses collections, en est une. Série : [`saisie-serie/`](../saisie-serie/index.md) ; édition : [`saisie-edition/`](../saisie-edition/index.md) ; éditeur : [`saisie-editeur/`](../saisie-editeur/index.md). |
| Genre | **Dans sa ligne** : « Modifier » change la ligne en champ, avec **Enregistrer** et **Annuler** ; un nouveau genre se saisit dans le même dialogue que le genre créé sur place depuis la saisie d'un album ([Contenu](../saisie-album/contenu.md)), ouvert par le bouton **Nouveau genre** de l'en-tête, avec **Annuler** et **Ajouter le genre**. Un libellé déjà pris est refusé sur place, en couleur d'alerte, en rappelant que la casse et les accents ne distinguent pas deux genres ([Genre](../../modele-metier/genre.md)). |
| Univers | **Panneau latéral**, qui glisse par-dessus la liste depuis la droite, la liste restant visible et grisée derrière : en modification, les dates de création et de modification de la fiche en tête, au-dessus du premier champ ; nom, univers parent (choisi en tapant son nom, présenté avec ses ancêtres, « Univers Hugo Pratt › Corto Maltese »), description, puis **Annuler** et **Enregistrer**. Un parent qui créerait un cycle est refusé sur place, avec sa raison ([Univers — relations](../../modele-metier/relations-et-cardinalites.md#univers--relations)). |
| Auteur | **Panneau latéral**, comme l'univers : en modification, les dates de la fiche en tête ; prénom et nom, pseudonyme, nationalité, biographie, puis **Annuler** et **Enregistrer**. Un nom affiché déjà porté par un autre auteur est refusé sur place ([Auteur / Artiste](../../modele-metier/auteur-artiste.md)) ([`saisie-auteur/`](../saisie-auteur/index.md)). |
| Intention d'achat | Pas de fiche propre : l'action principale est **Confirmer l'achat**, et le menu mène à la page des éditions et intentions d'achat de l'album, à la conversion vers une édition ou vers l'album (pour la seule édition de son album, cf. [Intention d'achat](../../fonctionnel/regles-metier/intention-d-achat.md)), et au retrait de l'intention ([Intention d'achat](../../fonctionnel/regles-metier/intention-d-achat.md)). |
