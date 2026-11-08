---
type: Choix d'implémentation
title: 'Accès à la base existante (Firebird)'
description: 'Accès à la base Firebird : kits officiels, ICU 52, UDF, copie de la base, pistes déjà explorées ; à lire pour la reprise des données.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Accès à la base existante (Firebird)

Seules la base, sous un nom non fixé, et `BDT_UDF.dll` sont fournies ; le reste vient des sources officielles.

- **Firebird 2.5.9 embarqué**, kit officiel ([`Firebird-2.5.9.27139-0_x64_embed.zip`](https://github.com/FirebirdSQL/firebird/releases/download/R2_5_9/Firebird-2.5.9.27139-0_x64_embed.zip)) : le moteur et le mode d'accès de l'application existante en production ([Application existante](../../contraintes-techniques/reprise-donnees/application-existante.md)), sans installation, service ni port. `isql` et `gstat`, absents de ce kit, sont repris du kit complet de la même release ([`Firebird-2.5.9.27139-0_x64.zip`](https://github.com/FirebirdSQL/firebird/releases/download/R2_5_9/Firebird-2.5.9.27139-0_x64.zip)).
  - **Alternative écartée (image Docker Firebird 2.5)** : aucune image officielle pour la 2.5, et l'image communautaire est plus lourde que le kit officiel.
  - **Alternative écartée (serveur du kit complet)** : fonctionne, mais lance un serveur à l'écoute sur un port, là où le mode embarqué, celui de la production, n'exige rien.
- **ICU 52, celle de l'application existante** ([Base existante](../../contraintes-techniques/reprise-donnees/reprise-des-donnees.md#base-existante)), build officiel ([`icu4c-52_1-Win64-msvc10.zip`](https://sourceforge.net/projects/icu/files/ICU4C/52.1/icu4c-52_1-Win64-msvc10.zip/download), qui exige le [redistribuable Visual C++ 2010 x64](https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x64.exe) s'il est absent du poste) : les collations d'origine se chargent telles quelles, la base n'est pas modifiée, et les requêtes suivent l'ordre français de la production.
  - **Alternative écartée (attributs spécifiques des collations effacés sur la copie, avec l'ICU 3.0 du kit)** : rend la base lisible, mais modifie ses métadonnées et remplace l'ordre français par l'ordre `UNICODE` générique.
- **Toujours sur une copie** de la base : Firebird écrit dans son fichier même pour une lecture.
- **UDF** : une UDF qui renvoie un `CSTRING(32767)` est interrogée seule dans sa requête, plusieurs dépassant la taille d'un message Firebird. Les UDF et procédures qui agissent sur le système de fichiers ne sont jamais exécutées, et un paramètre `FILTRE` ne reçoit que des conditions écrites par l'agent.
- **Sortie en UTF-8 dans un fichier**, relu en UTF-8 : les caractères accentués ne dépendent pas de l'encodage de la console.

## Pistes déjà explorées

À ne pas refaire si cette méthode doit être complétée (résultat constaté → enseignement) :

- **Kit complet seul** : `unavailable database` → son `fbclient.dll` n'est qu'un client réseau ; l'accès embarqué exige le kit `_embed`.
- **ICU 3.0 des kits** : `COLLATION UTF8_FR_CI_AI … is not installed`, même pour un `count(*)` → l'obstacle est la version d'ICU, pas les données.
- **Collation recréée avec `LOCALE` sous ICU 3.0** : `Invalid collation attributes` → l'ICU 3.0 des kits ne gère aucune locale ; aucun ajustement de collation ne remplace ICU 52.
- **Binaires d'ICU 52 sur GitHub** (`unicode-org/icu`) : sources seules → seule la distribution SourceForge fournit les binaires officiels.
- **Autre build d'ICU 52.1** (non officiel) : charge aussi les collations → leur version dépend des données d'ICU, communes à tous les builds 52.1, et non du compilateur.
- **Version ODS lue dans l'en-tête du fichier** : seul l'ODS majeur (11), qui ne distingue pas Firebird 2.0, 2.1 et 2.5 → `gstat -h` donne la version complète (11.2).
- **Classe de caractères dans `LIKE`** (`'%[[]%'`, syntaxe SQL Server) : aucune ligne → dans Firebird, `[` est littéral dans `LIKE`.
