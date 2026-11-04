---
type: Référence visuelle
title: 'Saisie selon l''entité'
description: 'Saisie d''une fiche selon l''entité : page dédiée, dans la ligne, panneau latéral.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T19:35:19Z }
---

# Saisie selon l'entité

La saisie d'une fiche s'ouvre selon le poids de l'entité :

| Entité | Saisie |
| --- | --- |
| Série, édition, auteur, éditeur | **Page dédiée**, comme l'album ; l'éditeur, qui porte ses collections, en est une. Série : [`saisie-serie/`](/visuel/saisie-serie/index.md) ; édition : [`saisie-edition/`](/visuel/saisie-edition/index.md) ; auteur : [`saisie-auteur/`](/visuel/saisie-auteur/index.md) ; éditeur : [`saisie-editeur/`](/visuel/saisie-editeur/index.md). |
| Genre | **Dans sa ligne** : « Modifier » change la ligne en champ, avec **Enregistrer** et **Annuler** ; un nouveau genre se saisit dans un cadre en tirets au-dessus de la liste, avec **Ajouter** et **Annuler** ; ce cadre est masqué par défaut, et le bouton **Nouveau genre** de l'en-tête l'ouvre. Un libellé déjà pris est refusé sur place, en couleur d'alerte, en rappelant que la casse et les accents ne distinguent pas deux genres ([Genre](/modele-metier/genre.md)). |
| Univers | **Panneau latéral**, qui glisse par-dessus la liste depuis la droite, la liste restant visible et grisée derrière : nom, univers parent (choisi en tapant son nom, présenté avec ses ancêtres, « Univers Hugo Pratt › Corto Maltese »), description, dates de la fiche, puis **Annuler** et **Enregistrer**. Un parent qui créerait un cycle est refusé sur place, avec sa raison ([Univers — relations](/modele-metier/relations-et-cardinalites.md#univers--relations)). |
| Intention d'achat | Pas de fiche propre : l'action principale est **Confirmer l'achat**, et le menu mène à la page des éditions et intentions d'achat de l'album, à la conversion vers une édition ou vers l'album (pour la seule édition de son album, cf. [Intention d'achat](/fonctionnel/regles-metier/intention-d-achat.md)), et au retrait de l'intention ([Intention d'achat](/fonctionnel/regles-metier/intention-d-achat.md)). |
