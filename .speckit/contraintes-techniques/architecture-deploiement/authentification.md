---
type: Contrainte technique
title: 'Authentification'
description: 'Compte administrateur unique pour l''administration, consultation publique.'
applies_to:
  - 'src/Bdtheque.Frontend/Security/**'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Authentification

- L'accès à la partie Administration est protégé par un **compte administrateur unique** (login + mot de passe).
- Pas d'authentification sur la partie Consultation (accès public).
