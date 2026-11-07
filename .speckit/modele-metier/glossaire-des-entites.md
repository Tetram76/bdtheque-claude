---
type: Modèle métier
title: 'Glossaire des entités'
description: 'Définition de chaque entité du domaine ; point d''entrée du modèle.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Glossaire des entités

| Entité | Définition |
| --- | --- |
| **Album** | Œuvre de bande dessinée suivie dans le catalogue. |
| **Édition** | Manifestation publiée d'un album (format, publication, valeur, etc.). |
| **Série** | Regroupement d'albums dans une continuité éditoriale. |
| **Auteur / Artiste** | Personne ou duo crédité sur un album (scénario, dessin, couleur). |
| **Éditeur** | Entité qui publie des éditions. |
| **Collection éditeur** | Sous-ensemble optionnel rattaché à un éditeur, utilisé pour classer des éditions. |
| **Collection utilisateur** | Ensemble des éditions possédées par l'utilisateur. Ce n'est pas une entité distincte en base : la collection est définie implicitement par les éditions dont le `Mode d'acquisition` est renseigné. |
| **Genre** | Catégorie de contenu (aventure, SF, humour, etc.). |
| **Univers** | Cadre fictionnel auquel albums/séries peuvent être rattachés, avec hiérarchie possible. |
| **Visuel d'édition** | Média rattaché à une édition avec un type de visuel. |
| **Intention d'achat** | Souhait d'acquisition portant sur un album (toute édition) ou sur une édition spécifique. |
| **Recherche enregistrée** | Recherche avancée nommée par l'administrateur, rejouable en consultation (voir [Recherche avancée](../fonctionnel/recherche-avancee/recherche-avancee.md)). |
| **Paramétrage de l'application** | Réglages de l'application, fixés par l'administrateur dans le bloc « Paramétrage » de l'administration. Fiche unique. |
| **Jeton d'accès** | Jeton généré par l'administrateur, sans lequel l'outil pour agents IA n'est pas utilisable ; il détermine le niveau d'accès de l'agent (voir [Interface pour agents IA](../fonctionnel/agents-ia/interface-pour-agents-ia.md)). |
| **Session de l'outil pour agents IA** | Conversation d'un agent avec l'outil, ouverte par un jeton d'accès : elle regroupe ses utilisations, ses objectifs et ses retours écrits (voir [Suivi de l'utilisation](../fonctionnel/agents-ia/suivi-de-l-utilisation.md)). |
| **Objectif de session** | Objectif de la demande de l'utilisateur, donné par l'agent à l'ouverture de la session ou mis à jour ensuite. |
| **Utilisation de l'outil** | Trace d'un appel d'outil ou d'une lecture de ressource par un agent, au sein d'une session. |
| **Retour écrit** | Limite signalée par l'agent, sur une utilisation précise ou sur sa session. |
| **Visite de l'interface web** | Suite des utilisations de l'interface web par un même utilisateur (visiteur ou administrateur), au cours d'une même visite, sans rien qui l'identifie (voir [Mesure d'audience de l'interface web](../fonctionnel/mesure-audience.md)). |
| **Utilisation d'une fonction** | Trace de l'affichage d'un écran ou du déclenchement d'une action de l'interface web, au sein d'une visite. |
| **Compteur d'audience mensuel** | Indicateurs d'une fonction pour un mois, sans visite ni parcours, conservés au-delà de la suppression du détail des visites. |
| **Journal des modifications** | Trace de chaque écriture sur une fiche : création, modification, suppression ou reprise d'une ancienne donnée. |

---
