---
type: Choix d'implémentation
title: 'Frontend'
description: 'Blazor rendu côté serveur, statique par défaut, CSS sans framework, parties, chemins, appels à l''API, contrôles, en-têtes et navigation, listes de l''administration, fiches de la consultation, libellés, tirages, erreurs ; à lire pour tout écran.'
applies_to:
  - 'src/Bdtheque.Frontend/**'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-09T09:45:00Z }
---

# Frontend

## Rendu et styles

- **Blazor, rendu côté serveur**, dans `frontend` : seule famille compatible avec l'architecture imposée, où le navigateur ne dialogue qu'avec `frontend` et `api` n'est jamais exposée ; même langage et mêmes contrats que l'API, sans chaîne d'outillage JavaScript.
  - **Alternative écartée (Blazor WebAssembly, SPA JavaScript)** : le navigateur appellerait l'API, qu'il faudrait exposer ou relayer appel par appel ; un second langage pour une SPA ; un téléchargement initial lourd sur smartphone pour WebAssembly.
  - **Alternative écartée (Razor Pages / MVC)** : compatible, mais sans composants réutilisables ni interactivité ponctuelle sans JavaScript.
- **Rendu statique par défaut**, l'interactivité réservée aux composants qui en ont besoin : la consultation, en lecture et sur smartphone ([Compatibilité multi-supports](../../contraintes-techniques/architecture-deploiement/compatibilite-multi-supports.md)), n'a pas besoin d'une connexion SignalR par visiteur, coûteuse en mémoire sur le NAS et coupée à chaque changement de réseau mobile ; et ASP.NET Core réserve au rendu statique les pages qui écrivent des cookies (connexion, [Authentification : mise en œuvre](transverse/authentification-mise-en-oeuvre.md)).
  - **Alternative écartée (interactivité globale)** : une connexion permanente par visiteur pour des pages surtout statiques, et une exclusion à gérer pour chaque page à cookie.
- **Aucun framework CSS** : CSS natif, propriétés personnalisées qui portent les contrats du thème de base ([Thème de base](../../visuel/charte-graphique/theme-de-base.md)), isolation CSS par composant, mise en page *mobile-first*.
  - **Alternative écartée (Bootstrap)** : son apparence, que l'exigence de rendu exclut ([Design et charte graphique](../../fonctionnel/design-et-charte-graphique.md)), devrait être neutralisée règle par règle par chaque thème, devenu une surcharge de Bootstrap plutôt qu'une variante du thème de base.
  - **Alternative écartée (MudBlazor ou toute bibliothèque de composants)** : une apparence propre tout aussi présente, et des composants qui exigent l'interactivité.

## Structure

- **Pages de l'administration réservées en bloc** par leur dossier à l'administrateur authentifié ([Authentification](../../contraintes-techniques/architecture-deploiement/authentification.md)) : une page ajoutée ne peut pas l'oublier.
- **Chemins des pages déclarés une fois**, pour tout lien et toute route. Une page pas encore réalisée a le chemin de la page « Pas encore implémenté » ([`suivi-implementation/`](../../suivi-implementation/index.md), Phase 3) : le chantier qui la réalise lui donne le sien, et tous les liens suivent.
- **Un client unique de l'API**, qui ajoute le secret interne. Tout appel qui n'aboutit pas lève une exception porteuse de ce que la présentation de l'erreur exige, et rien d'interne ; seule une réponse ProblemDetails d'une des trois catégories est prise pour ce qu'elle dit, tout autre échec est technique ([Erreurs métier, fonctionnelles et techniques](../ecritures/erreurs-metier-fonctionnelles-et-techniques.md)).
  - **Alternative écartée (type résultat)** : comme pour le domaine, chaque appelant le propagerait, alors qu'un échec interrompt toujours l'action et n'appelle qu'une présentation.

## Contrôles

- **Apparence des contrôles posée sur les éléments HTML eux-mêmes**, si bien qu'un contrôle natif et un contrôle de formulaire Blazor sont identiques. Un champ est **dimensionné à ce qu'il contient**, jamais étiré sur toute la largeur ([Contenu](../../visuel/saisie-album/contenu.md)).
- **Choix d'une fiche en tapant son nom** (*combobox* de l'ARIA), dont les suggestions viennent de la recherche des listes ([Recherche](../api/recherche.md)) :
  - chaque changement du texte reçoit un **numéro**, avant toute attente, et seule la recherche du dernier écrit les suggestions : le circuit peut traiter la frappe suivante dès la première attente d'un gestionnaire, et une réponse antérieure, quel que soit son moment, est ignorée ;
  - la recherche attend une **pause de la frappe de 500 ms**, valeur courante d'un anti-rebond : un mot tapé d'un trait n'interroge le serveur qu'une fois ;
  - le texte tapé n'est jamais une valeur : quitter le champ sans choisir rend la fiche choisie. Un script retient l'action des touches de la liste ouverte, Blazor ne sachant retenir que toutes les touches ou aucune.
  - **Alternative écartée (chaque frappe annule la recherche précédente)** : pendant l'attente de l'annulation, le circuit traitait la frappe suivante, dont la réponse tardive remplaçait les suggestions du texte affiché (constaté : 7 saisies sur 180 ; aucune avec le numéro). Annuler n'épargne qu'une requête déjà partie, que la pause rend rare.
  - **Alternative écartée (aucune pause)** : une requête par lettre, pour des réponses presque toutes ignorées.
- **Menus en *popover* du navigateur**, ancrés à leur bouton par le positionnement par ancre de CSS : fermeture sur un clic en dehors ou Échap, premier plan, sans interactivité ni script, dans une page statique (*Baseline* de web.dev : Safari 18.3, Firefox 147).
  - **Alternative écartée (menu interactif, ou `<details>` refermé par un script)** : une connexion ou un script pour ce que le navigateur offre.
- **Dialogues `<dialog>` modaux**, ouverts par un script dès leur insertion (seul un script ouvre un dialogue en modal, au-dessus d'une page inerte), rendus quand l'adresse les demande ; la fermeture, choix sûr, a le focus.
  - **Alternative écartée (dialogue non modal rendu par le serveur)** : la page resterait accessible au clavier dessous, et Échap ne le fermerait pas.
- **Barre d'enregistrement collée au bas de l'écran** (`sticky`), qui ne masque jamais ce qui suit le formulaire.

## En-têtes et navigation

- **La recherche de l'en-tête est une section Blazor**, qu'une page peut vider (écran d'une collection vide, [Collection vide](../../visuel/dashboard/collection-vide.md)) : la dernière section rendue l'emporte (documentation d'ASP.NET Core, *Blazor sections*).
  - **Alternative écartée (paramètre de la mise en page)** : une page ne passe rien à sa mise en page, rendue avant elle, et ne sait qu'à l'exécution si la collection est vide.
- **Nombres de fiches du menu de l'administration lus une fois par requête** ; sans eux, le menu mène partout de même.
- **Lignes défilantes** (menu de la consultation, navigation par initiale), sans retour à la ligne : un script ne montre un chevron que s'il reste des entrées hors de vue, d'après les largeurs qu'il observe, et centre la ligne sur son entrée courante, de nouveau à la **fin du premier chargement de polices**, qui change la largeur des entrées, jamais sur la promesse des polices, parfois déjà résolue avant que la police du texte soit demandée (CSS Font Loading). Les chevrons sont réservés à la souris, le clavier atteignant chaque entrée.
- **Pagination de largeur à peu près constante** : toutes les pages jusqu'à sept, sinon les extrémités et le voisinage de la page courante.
- **Entrée du menu courante sur les pages de sa section** avant que sa propre page existe (la fiche d'un album sous « Albums », dont la liste reste à réaliser) : l'entrée garde le lien « Pas encore implémenté » et connaît le chemin que sa page aura.

## Listes de l'administration

- **Critères, panneau des filtres et dialogues portés par l'adresse**, en rendu statique ([`visuel/liste-albums/`](../../visuel/liste-albums/index.md), [`visuel/listes/`](../../visuel/listes/index.md)) : chaque lien et chaque formulaire mène à la liste avec d'autres critères, et une liste se partage ou se recharge telle quelle. Ce que les listes partagent est écrit une fois ; chacune n'ajoute que ses filtres, ses chemins, ses fonctions suivies et ses textes.
- **Disposition des lignes selon la largeur de la liste elle-même** (requête de conteneur), et non de l'écran, que les marges du thème rendent trompeuse. Chaque page écrit la disposition de ses colonnes, propres à elle.
  - **Alternative écartée (styles communs des lignes appliqués par `::deep`)** : leur spécificité dépasserait celle des règles de la page, que chaque page devrait forcer.
- **Recherche et filtres en formulaire `GET`**, dont le premier bouton, que le navigateur soumet sur Entrée, est la recherche ; replier ou déplier les filtres soumet aussi le formulaire, sans perdre un texte tapé.
- **Suppression confirmée par un formulaire `POST`** qui envoie version et empreinte de l'impact affiché ([Suppression des entités : mise en œuvre](../ecritures/suppression-des-entites-mise-en-oeuvre.md)), puis redirige vers la liste ; un refus ou un impact changé est présenté dans le même dialogue.
- **Site extérieur ouvert dans un nouvel onglet** (site web d'un éditeur, depuis sa ligne ou son menu), sans accès à la page d'origine ni adresse de provenance (`noopener noreferrer`) : la liste reste telle qu'elle était, avec ses critères.
  - **Alternative écartée (même onglet)** : l'administrateur quitterait l'application pour un site qui n'en fait pas partie, et n'y reviendrait que par l'historique du navigateur.

## Fiches de la consultation

- **Repli des auteurs d'un rôle par une divulgation du navigateur** (`<details>`, [`visuel/fiche-album/`](../../visuel/fiche-album/index.md)) : les noms repliés, placés avant elle, ne se montrent qu'une fois elle ouverte (`:has()`), si bien que « Réduire » suit le dernier nom. Ni script ni requête pour montrer des noms déjà reçus ; l'ouverture n'est pas une action suivie, comme celle du panneau des filtres d'une liste.
  - **Alternative écartée (rôles dépliés portés par l'adresse, comme les critères d'une liste)** : une requête et un nouveau rendu de la fiche pour des noms déjà présents dans la page.

## Présentation

- **Valeurs des énumérations nommées en un seul lieu** (rôle d'une contribution, appréciation…), dans la culture d'affichage, pour toute page qui les présente ou les saisit.
- **Libellés des albums et des éditions construits à la présentation**, d'après leurs composantes et le format du paramétrage ([Libellé d'un album](../../fonctionnel/presentation/libelle-d-un-album.md), [Libellé d'une édition](../../fonctionnel/presentation/libelle-d-une-edition.md)). L'ISBN y est tel qu'enregistré : sa mise en forme par tirets dépend de plages d'attribution que l'application ne tient pas.
- **Tirages d'une page par le générateur aléatoire du conteneur de services**, que les tests de bout en bout remplacent par un tirage constant, pour qu'une page soit la même à chaque capture.
- **Un composant unique présente toute erreur** ([Présentation des erreurs](../../fonctionnel/presentation-des-erreurs.md)), avec sa catégorie, qui lui donne cadre, icône et couleur pour la distinguer au premier coup d'œil, à l'arrondi d'une tuile, jamais d'un contrôle, qu'un thème arrondit en pilule. Le texte d'une erreur métier est celui de son code de règle ; une erreur technique n'a qu'un texte générique. Le frontend ne référençant pas le domaine, un test confronte ses textes aux codes de règle.
