---
type: Contrainte technique
title: 'Authentification'
description: 'Compte administrateur unique pour l''administration, protégé par identifiant et mot de passe, et par un second facteur TOTP exigé sur un déploiement exposé en HTTPS ; consultation publique.'
applies_to:
  - 'src/Bdtheque.Frontend/Security/**'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-27T12:00:00Z }
---

# Authentification

- L'accès à la partie Administration est protégé par un **compte administrateur unique** (identifiant et mot de passe), sans gestion multi-utilisateurs.
- **Second facteur sur un déploiement exposé** (décision de l'utilisateur, #601) : sur un déploiement servi en HTTPS derrière le reverse proxy du NAS, et donc exposé sur Internet ([Hébergement](hebergement.md)), la connexion exige en plus de l'identifiant et du mot de passe un **code à usage unique fondé sur le temps** (TOTP, RFC 6238), donné par une application d'authentification de l'administrateur.
  - **Exigé, jamais facultatif** : un tel déploiement ne fonctionne pas sans second facteur, pour qu'un réglage oublié ne le laisse jamais protégé par le seul mot de passe.
  - **Déploiement interne en HTTP** (versions à valider ou à tester, sans accès depuis l'extérieur, [Hébergement](hebergement.md)) : l'identifiant et le mot de passe suffisent.
  - **Une même image** pour les deux déploiements : c'est la configuration du déploiement qui dit s'il est exposé, jamais une image à part.
  - Le calcul du code se fait par l'application elle-même, sans aucun service tiers.
  - **Enregistrement par un QR code** (décision de l'utilisateur, #601) : un script fourni avec l'application crée le secret du second facteur, à déclarer dans le réglage du déploiement, et l'affiche en QR code, que l'administrateur scanne avec son application d'authentification. L'application elle-même n'affiche jamais le secret.
  - **Alternative écartée (clé d'accès, *passkey* WebAuthn)** : le navigateur ne la propose que dans un contexte sécurisé (HTTPS, ou `localhost`), donc jamais sur le déploiement interne en HTTP.
  - **Alternative écartée (protection par le reverse proxy du NAS)** : l'image resterait protégée par le seul mot de passe.
  - **Alternative écartée pour l'instant (connexion par un compte Google, OpenID Connect)**, notée pour plus tard (décision de l'utilisateur) : le compte administrateur, toujours unique, serait lié à un compte Google à l'installation, et cette connexion s'ajouterait au second facteur comme une autre façon de se connecter, sur un déploiement HTTPS seulement. Google refuse en effet une adresse de retour en HTTP ou une adresse IP brute, `localhost` excepté. Elle exige aussi un projet Google Cloud et l'adresse de retour de chaque version déclarée chez Google, et elle fait dépendre l'accès d'un service tiers et de la sortie Internet du NAS. Une gestion de plusieurs comptes, avec un premier compte simple désactivé une fois un compte lié à Google créé, contredirait le compte administrateur unique.
- Pas d'authentification sur la partie Consultation (accès public).
