---
type: Choix d'implémentation
title: 'Visuels : stockage et traitement'
description: 'Volume privé à api, médias servis et relayés, téléversement validé et borné, cohérence fichiers/base, version d''affichage WebP, SkiaSharp.'
applies_to:
  - 'src/Bdtheque.Api/Visuals/**'
  - 'src/Bdtheque.Api/Endpoints/EditionVisualEndpoints.cs'
  - 'src/Bdtheque.Api/Endpoints/VisualMediaEndpoints.cs'
  - 'src/Bdtheque.Frontend/Api/VisualMediaRelay.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Visuels : stockage et traitement

## Volume privé à `api`

- Le volume des visuels ([Stockage des visuels](../../contraintes-techniques/architecture-deploiement/stockage-des-visuels.md)) n'est monté que par `api`, comme `db` : ses clients n'obtiennent les médias que par elle, sans connaître l'organisation des fichiers. C'est l'**isolation stricte** des tiers ([Déploiement](../../contraintes-techniques/architecture-deploiement/deploiement.md)) et la souveraineté des données que recommande Microsoft (*.NET Microservices Architecture for Containerized .NET Applications*, § Data sovereignty per microservice) : une évolution du stockage reste interne à `api`. La documentation d'ASP.NET Core (*Upload files*, § Security considerations) recommande en outre une zone dédiée aux fichiers téléversés, nommés par l'application.
  - **Alternative écartée (volume lu par `frontend`, en lecture seule)** : plus simple, un saut de moins pour un média pas encore en cache ; mais la présentation lirait elle-même des données persistées, contre l'isolation imposée, et les contrats devraient révéler l'organisation des fichiers. Le saut évité, que le cache rend rare, ne compense pas cet écart.
- **Un média est désigné par l'identifiant de son visuel**, jamais par un chemin : seul le média d'un visuel existant est servi, jamais un orphelin. Il est servi en flux, jamais chargé entier.
- **Cache HTTP d'un an, `immutable`** (RFC 8246), posé par `api`, qui connaît seule la règle qui le permet : les noms de fichiers ne sont jamais réutilisés et le média d'un visuel ne change jamais. Le navigateur ne le redemande pas, ce dont dépend l'affichage sur smartphone ([Compatibilité multi-supports](../../contraintes-techniques/architecture-deploiement/compatibilite-multi-supports.md)).
- **Relais par `frontend`**, seul interlocuteur du navigateur ([Architecture des tiers](../../contraintes-techniques/architecture-deploiement/architecture-des-tiers.md)), sous les mêmes chemins qu'`api`, en flux, avec le **transfert de YARP** (licence MIT) : un `HttpClient` mettrait par défaut la réponse entière en mémoire. Seules ces routes sont relayées, `frontend` n'étant pas un proxy ouvert ; la requête relayée perd les cookies du visiteur et reçoit le secret interne.
- **Écriture vérifiée au démarrage** : rien dans le déploiement ne garantit l'accès en écriture (`api` ne tourne pas en root, Docker crée en root un dossier hôte absent, un dossier partagé du NAS appartient à un utilisateur du NAS), et le garantir exigerait de tourner en root. `api` **refuse de démarrer** sans écriture, en désignant le dossier et l'utilisateur en cause, et avertit si le volume n'est pas monté depuis l'hôte : la mauvaise configuration apparaît au déploiement, non au premier téléversement.

## Téléversement

- **Validé en décodant l'image**, jamais sur le type annoncé ; un fichier refusé est une saisie à corriger, donc une erreur **métier** ([Erreurs métier, fonctionnelles et techniques](../ecritures/erreurs-metier-fonctionnelles-et-techniques.md)). Formats acceptés : ceux que tout navigateur affiche (JPEG, PNG, WebP, GIF), l'original étant servi tel quel ; l'extension suit le format décodé.
- **Dimensions bornées avant toute allocation** (80 millions de pixels ; un A3 numérisé à 600 dpi y tient) : la mémoire du décodage dépend des pixels, non du poids, et un PNG ou un GIF se décode en taille réelle. Une image trop grande arrêterait `api` faute de mémoire sur le NAS.
- **Poids borné pendant la réception** (paramétrable), le formulaire étant lu en flux par le point d'entrée.
  - **Alternative écartée (liaison du formulaire par le framework)** : elle recopierait toute la requête dans un fichier temporaire avant le contrôle, et un envoi de plusieurs gigaoctets remplirait le disque du NAS avant d'être refusé.
  - La limite de transport du serveur est ramenée à la taille d'un formulaire valide : elle borne un envoi sans longueur annoncée sans jamais devancer le refus métier, alors qu'atteinte la première, elle répondrait `413`, erreur technique.
- **Un téléversement à la fois**, les autres attendant leur tour sans être refusés : chacun mobilise quelque 400 Mo, et plusieurs scans déposés d'un coup épuiseraient la mémoire du NAS. Le tour est pris avant la lecture du corps : une requête en attente ne mobilise rien, et Kestrel ne contrôle le débit d'un corps que pendant sa lecture (code source de Kestrel, `TimeoutControl`), si bien qu'elle n'est jamais coupée pour son attente.
- **Réarrangement en une écriture** : la liste complète des visuels de l'édition, dans l'ordre voulu, sous la version de l'album ([Concurrence d'accès](../persistance/concurrence-d-acces.md)) : un glisser-déposer réordonne plusieurs visuels d'un coup.

## Cohérence entre fichiers et base

- Fichiers et base ne s'écrivent pas de façon atomique : toute validation et toute conversion ont lieu **en mémoire, avant** toute écriture ; les fichiers sont écrits juste avant l'enregistrement, et supprimés s'il échoue avant la validation de la transaction, seul cas où l'échec est certain. Un échec de la validation elle-même n'est pas compensé : la transaction a pu aboutir, et supprimer les fichiers priverait un visuel enregistré de ses médias.
- **Une réconciliation quotidienne**, et au démarrage, supprime tout fichier sans visuel en base qui date de plus d'une heure (délai qui écarte un téléversement en cours) : aucun chemin d'échec ne laisse durablement d'orphelin, qui ne coûterait de toute façon que de la place.
- **Un identifiant propre au média**, et non celui du visuel : connu avant la création du visuel, que le domaine construit avec sa référence. La réconciliation reconnaît à cet identifiant les fichiers d'un visuel.

## Version d'affichage

- `api` garde l'**original** et produit une **version d'affichage WebP** réduite (1200 pixels au plus sur le grand côté, jamais agrandie), servie par défaut : les scans originaux sont trop lourds sur smartphone ([Compatibilité multi-supports](../../contraintes-techniques/architecture-deploiement/compatibilite-multi-supports.md)).
  - **Alternative écartée (original seul)** : plusieurs mégaoctets par couverture dans une grille d'albums sur réseau mobile.
- Elle applique l'**orientation que l'original déclare** (EXIF), comme le font les navigateurs pour l'original, la version WebP ne conservant pas la balise ; aucune rotation n'est déduite des proportions (un scan de double page reste en paysage). Le décodage se fait à l'échelle réduite quand le format le permet, pour épargner la mémoire.
- **SkiaSharp** (licence MIT, .NET Foundation), avec ses bibliothèques natives sans dépendance système.
  - **Alternative écartée (ImageSharp)** : licence *Six Labors Split*, et clé de licence exigée à la compilation depuis la version 4.
