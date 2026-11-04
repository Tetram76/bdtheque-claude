---
type: Choix d'implémentation
title: 'Accès à la base existante (Firebird)'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Accès à la base existante (Firebird)

- **Archives à récupérer** (sources officielles ; seules la base et `BDT_UDF.dll` sont fournies) :

  | Archive | Source | Usage |
  | --- | --- | --- |
  | `Firebird-2.5.9.27139-0_x64_embed.zip` | <https://github.com/FirebirdSQL/firebird/releases/download/R2_5_9/Firebird-2.5.9.27139-0_x64_embed.zip> | Moteur Firebird embarqué |
  | `Firebird-2.5.9.27139-0_x64.zip` | <https://github.com/FirebirdSQL/firebird/releases/download/R2_5_9/Firebird-2.5.9.27139-0_x64.zip> | `isql.exe` et `gstat.exe` uniquement |
  | `icu4c-52_1-Win64-msvc10.zip` | <https://sourceforge.net/projects/icu/files/ICU4C/52.1/icu4c-52_1-Win64-msvc10.zip/download> | ICU 52 des collations d'origine |
  | `vcredist_x64.exe` (Visual C++ 2010 SP1 x64, 10.0.40219.325) | <https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x64.exe> | Runtime d'ICU 52, à récupérer et installer uniquement s'il est absent du poste |

- **Firebird 2.5.9 embarqué**, kit officiel `Firebird-2.5.9.27139-0_x64_embed.zip` (releases GitHub `FirebirdSQL/firebird`, tag `R2_5_9`) : même moteur et même mode d'accès que l'application existante en production ([Application existante](/contraintes-techniques/reprise-donnees/application-existante.md)), sans installation, sans service ni port réseau. Le kit embarqué ne contient ni `isql` ni `gstat` : ils sont repris du kit complet de la même release (`Firebird-2.5.9.27139-0_x64.zip`) et copiés dans le dossier du kit embarqué, où `fbembed.dll` est dupliquée en `fbclient.dll` (bibliothèque que charge `isql`). La variable d'environnement `FIREBIRD` désigne ce dossier, et la base est ouverte par son chemin local, sans nom d'hôte.
- **ICU 52, comme l'application existante** ([Base existante](/contraintes-techniques/reprise-donnees/reprise-des-donnees.md#base-existante)) : les DLL `icudt52.dll`, `icuin52.dll` et `icuuc52.dll` du dossier `bin64` du build officiel `icu4c-52_1-Win64-msvc10.zip` (distribution officielle d'ICU sur SourceForge, `ICU4C/52.1` ; les releases GitHub d'ICU ne fournissent pas de binaires Windows pour cette version) sont copiées dans le dossier du kit, et `intl/fbintl.conf` y déclare `icu_versions 5.2` pour le module `builtin`, comme celui de l'application. Les collations d'origine se chargent alors telles quelles : la base n'est pas modifiée et les requêtes suivent l'ordre français de la production. Ce build requiert le redistribuable Visual C++ 2010 x64 complet : `icuin52.dll` importe `msvcp100.dll` et `msvcr100.dll`, `icuuc52.dll` importe `msvcr100.dll`.
- **Seules la base et `BDT_UDF.dll` sont fournies**, la base sous un nom non fixé : kits Firebird et ICU sont téléchargés depuis leurs sources officielles ci-dessus, et la base est ouverte par son chemin, quels que soient son nom et son extension.
- **UDF** : `BDT_UDF.dll` (64 bits, comme le kit) est copiée dans le dossier `udf` du kit, emplacement autorisé par défaut (`UdfAccess = Restrict UDF`). Chaque UDF renvoyant un `CSTRING(32767)` est interrogée seule dans sa requête : plusieurs dans une même ligne dépassent la taille maximale d'un message Firebird (`block size exceeds implementation restriction`). Les UDF et procédures qui agissent sur le système de fichiers ne sont jamais exécutées, et un paramètre `FILTRE` ne reçoit que des conditions écrites par l'agent.
- **Toujours sur une copie** de la base fournie, jamais sur le fichier lui-même : Firebird écrit dans le fichier de la base même pour une simple lecture.
- **Sortie** : `isql -ch UTF8`, résultat écrit dans un fichier (`-o`) relu en UTF-8, pour restituer les caractères accentués sans dépendre de l'encodage de la console. L'en-tête de la base (version ODS, taille de page) se lit avec `gstat -h`.
  - **Alternative écartée (image Docker Firebird 2.5, ex. `jacobalberty/firebird:2.5-ss`)** : Firebird ne publie aucune image officielle pour la 2.5, et l'image communautaire est plus lourde que le kit officiel.
  - **Alternative écartée (serveur `fbserver.exe -a` du kit complet)** : fonctionne, mais lance un serveur à l'écoute sur le port 3050, là où le mode embarqué, celui de la production, n'exige rien.
  - **Alternative écartée (effacement des attributs spécifiques des collations sur la copie, avec l'ICU 3.0 du kit)** : rend la base lisible, mais modifie ses métadonnées et remplace l'ordre français de la production par l'ordre `UNICODE` générique.
- **Pistes déjà explorées**, à ne pas refaire si cette méthode doit être complétée (résultat constaté → indication) :
  - **Kit complet seul** (`Firebird-2.5.9.27139-0_x64.zip`, sans le kit embarqué) : `unavailable database` → ce kit ne contient pas `fbembed.dll`, son `fbclient.dll` n'est qu'un client réseau ; l'accès embarqué exige le kit `_embed`.
  - **ICU 3.0 des kits** : `COLLATION UTF8_FR_CI_AI for CHARACTER SET UTF8 is not installed` sur toute requête touchant une table qui utilise ces collations, même un `count(*)` → l'obstacle est la version d'ICU, pas les données.
  - **Recréer une collation avec `LOCALE` sous ICU 3.0** (`fr_FR` puis `fr`, sur une base vierge) : `Invalid collation attributes`, alors que la même collation sans `LOCALE` est acceptée → l'ICU 3.0 des kits ne gère aucune locale ; aucun ajustement de collation ne remplace ICU 52.
  - **Binaires Windows d'ICU 52 sur GitHub** (`unicode-org/icu`) : pas de tag `release-52-1`, et `release-52-2` ne fournit que les sources → seule la distribution SourceForge (`ICU4C/52.1`) fournit les binaires officiels.
  - **Autres builds d'ICU 52.1** : un build non officiel compilé avec Visual Studio 2010 (dossier `bin64`) charge aussi les collations d'origine → la version des collations dépend des données ICU, communes à tous les builds 52.1, et non du compilateur.
  - **Version ODS lue directement dans l'en-tête du fichier** : seul l'ODS majeur (11) est lu, qui ne distingue pas Firebird 2.0, 2.1 et 2.5 → `gstat -h` donne la version complète (11.2).
  - **Plusieurs UDF `CSTRING(32767)` dans une même ligne** : `block size exceeds implementation restriction` → une UDF par requête.
  - **Classe de caractères dans `LIKE`** (`'%[[]%'`, syntaxe SQL Server) : aucune ligne → dans Firebird, `[` est littéral dans `LIKE` (`'%[%'`).
