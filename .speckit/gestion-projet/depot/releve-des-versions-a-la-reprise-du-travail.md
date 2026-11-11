---
type: Gouvernance
title: 'Relevé des versions à la reprise du travail'
description: 'À chaque reprise du travail : quand le relevé des versions est dû et où sa trace se lit ; s''il est dû, lire Mises à jour des dépendances.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-11T13:30:02Z }
---

# Relevé des versions à la reprise du travail

Le relevé des versions, que décrit [Mises à jour des dépendances](mises-a-jour-des-dependances.md), est vérifié à chaque reprise du travail :

- **Fréquence : un relevé par contrôle de sécurité quotidien.** Entre deux exécutions du contrôle quotidien ([Mises à jour des dépendances](mises-a-jour-des-dependances.md)), rien de ce qui décide d'une montée ne change : les vulnérabilités connues sont celles du contrôle, et une version tout juste publiée attend 7 jours ([Mises à jour des dépendances](mises-a-jour-des-dependances.md)). Plusieurs chantiers lancés le même jour ne refont donc ni le relevé ni sa revue. Chaque relevé laisse une **trace** dans la **description** du ticket des vulnérabilités connues (étiquettes `security-audit` et `harness`), que l'agent modifie : une section « Relevé des versions du `<date>` », ajoutée après la liste des vulnérabilités, qui donne sa conclusion (rien à appliquer, ou la Pull Request de la correction ou de la montée). Le contrôle quotidien réécrit cette description en entier à chaque exécution, si bien que la section disparaît avec lui : à la reprise du travail, le relevé n'est dû que si la description du ticket ne porte pas cette section. Sinon, la reprise passe directement au chantier, la conclusion du relevé restant valable jusqu'au contrôle suivant (une Pull Request qu'il a ouverte se poursuit selon ses propres règles).
  - **Alternative écartée (commentaire du ticket à chaque relevé)** : les commentaires s'accumuleraient sans fin sur un ticket qui reste ouvert tant qu'une vulnérabilité est connue, alors que seule compte la conclusion du dernier relevé.
  - **Alternative écartée (trace dans un fichier du dépôt)** : elle exigerait un commit sur `main` à chaque relevé, ce qu'interdit [Règle absolue : interdiction de pousser directement sur `main`](regle-absolue-interdiction-de-pousser-directement-sur-main.md).
  - **Alternative écartée (relevé exécuté par le contrôle quotidien lui-même)** : le script n'est que la moitié du travail ; décider, pour chaque vulnérabilité et chaque version, de ce qui est applicable reste la revue de l'agent, qu'il faudrait tracer de toute façon.
