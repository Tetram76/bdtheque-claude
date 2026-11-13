---
type: Gouvernance
title: 'Relevé des versions à la reprise du travail'
description: 'À chaque reprise du travail : quand le relevé des versions est dû et où sa trace se lit ; s''il est dû, lire Mises à jour des dépendances.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-13T18:30:00Z }
---

# Relevé des versions à la reprise du travail

Le relevé des versions, que décrit [Mises à jour des dépendances](mises-a-jour-des-dependances.md), est vérifié à chaque reprise du travail :

- **Fréquence : un relevé par contrôle de sécurité quotidien.** Entre deux exécutions du contrôle quotidien ([Mises à jour des dépendances](mises-a-jour-des-dependances.md)), rien de ce qui décide d'une montée ne change : les vulnérabilités connues sont celles du contrôle, et une version tout juste publiée attend 7 jours ([Mises à jour des dépendances](mises-a-jour-des-dependances.md)). Plusieurs chantiers lancés le même jour ne refont donc ni le relevé ni sa revue. Chaque relevé laisse sa **trace** dans la **description** du ticket des vulnérabilités connues (étiquettes `security-audit` et `harness`) : un état, pas un historique ([Issues](../revue-tickets/issues.md#tickets-de-suivi-des-harnais)), traitement acquis par la fusion de la Pull Request de la correction. À la reprise du travail, le relevé n'est dû que si la description ne porte pas la ligne `Dernière prise en compte` ([Issues](../revue-tickets/issues.md#tickets-de-suivi-des-harnais), traitement dû), que l'agent ajoute à chaque relevé. Sinon, la reprise passe directement au chantier, le relevé restant valable jusqu'au contrôle suivant (une Pull Request qu'il a ouverte se poursuit selon ses propres règles).
  - **Alternative écartée (commentaire du ticket à chaque relevé)** : les commentaires s'accumuleraient sans fin sur un ticket qui reste ouvert tant qu'une vulnérabilité est connue, alors que seule compte la date du dernier relevé.
  - **Alternative écartée (trace dans un fichier du dépôt)** : elle exigerait un commit sur `main` à chaque relevé, ce qu'interdit [Règle absolue : interdiction de pousser directement sur `main`](regle-absolue-interdiction-de-pousser-directement-sur-main.md).
  - **Alternative écartée (relevé exécuté par le contrôle quotidien lui-même)** : le script n'est que la moitié du travail ; décider, pour chaque vulnérabilité et chaque version, de ce qui est applicable reste la revue de l'agent, qu'il faudrait tracer de toute façon.
