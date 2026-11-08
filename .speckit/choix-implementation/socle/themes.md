---
type: Choix d'implémentation
title: 'Thèmes'
description: 'Contrats du thème de base, thèmes et scripts, thème de la visite, composants de présentation, contrastes, polices, captures de référence ; à lire pour tout écran.'
applies_to:
  - 'src/Bdtheque.Frontend/Theming/**'
  - 'src/Bdtheque.Frontend/wwwroot/**'
  - 'src/Bdtheque.Frontend/Components/**'
  - 'tests/Bdtheque.EndToEnd.Tests/**'
  - 'scripts/Test-Captures.ps1'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Thèmes

## Contrats et thèmes

- **Les contrats du thème de base sont des propriétés personnalisées CSS** ([Thème de base](../../visuel/charte-graphique/theme-de-base.md)), à valeur neutre, déclarées par sa feuille, chargée pour tout thème : couleurs, typographie, formes, espacements et tailles, points d'ancrage des touches propres à un thème, et présentation d'un écran quand les thèmes ne la partagent pas. Les espacements et tailles des contrôles sont des propriétés uniques, qui donnent à un bouton la même taille sur toutes les pages ([Convivialité et efficacité](../../visuel/charte-graphique/convivialite-et-efficacite.md)).
- **Un thème ne redéfinit que ces propriétés**, sous son sélecteur, et accroche ses touches aux points d'ancrage, jamais une règle propre à un composant ; les composants ne lisent que les contrats. Un besoin que les contrats ne couvrent pas les fait évoluer, dans le thème de base.
- **Chantier d'un écran** : avant de l'écrire, il lit la présentation de l'écran dans chaque thème proposé (`visuel/` et maquettes), relève ce qui y varie d'un thème à l'autre et en fait des contrats du thème de base ; l'écran n'a qu'un balisage et des styles qui ne lisent que ces contrats.
- **Un script de thème** dessine ce qu'une feuille ne peut pas (ex. bords tracés à la main, tirés au hasard) :
  - chaque page charge les scripts de **tous** les thèmes proposés, chacun n'agissant que sur une page de son thème, vérifié à chaque navigation : la navigation améliorée de Blazor garde les scripts de la page et n'exécute jamais ceux qu'elle insère, alors que le thème peut changer d'une page à l'autre ;
  - il n'insère **aucun nœud**, la page appartenant au rendu de Blazor (un nœud inséré dans un composant interactif fausserait ses mises à jour) : un dessin est une image SVG posée en propriété personnalisée, que la feuille peint dans un pseudo-élément ;
  - ses tirages restent ceux de la page tant qu'elle est affichée, et emploient le générateur du navigateur, que les captures remplacent par une suite fixe.
- **Le bloc est positionné sans créer de contexte d'empilement**, pour qu'un thème dessine sur ses bords dans ses pseudo-éléments : un contexte d'empilement ferait passer sous le bloc suivant la liste d'un champ qu'il contient.
- **Inclinaison réservée aux couvertures mises en avant** : une vignette, contenu d'une liste, reste droite.

## Thème de la visite

- **Thème de la requête porté par un attribut de la racine de la page**, que fixe un sélecteur : le thème de la visite dans l'application, un thème choisi dans l'environnement de validation et dans les tests.
- **Visite délimitée par un cookie de session**, que le navigateur partage entre ses onglets et oublie avec sa session ; c'est la délimitation unique de la visite, que reprend la mesure d'audience ([Relevé par le frontend](../mesure-audience/releve-par-le-frontend.md)). Il n'identifie personne et aucun script ne le lit. Une session qui ne finit jamais (navigateur qui la restaure, smartphone resté ouvert) ne garde pas sa visite au-delà de **13 mois**, durée maximale d'un traceur ([Mesure d'audience de l'interface web](../../fonctionnel/mesure-audience.md)).
  - **Alternative écartée (cookie persistant)** : il survivrait à la fermeture du navigateur ; une visite ne serait plus une session de navigation.
  - **Alternative écartée (`sessionStorage`)** : propre à chaque onglet, il ferait d'un nouvel onglet une nouvelle visite, et le serveur, qui rend la page, ne le lit pas.
- **Thème tiré par le serveur**, uniformément, au début d'une visite ([Design et charte graphique](../../fonctionnel/design-et-charte-graphique.md)), puis de nouveau à la première page d'une nouvelle date du navigateur ([Internationalisation](transverse/internationalisation.md)) ou si le thème n'est plus proposé. La première page, qui ne connaît pas encore le fuseau du navigateur, tire sans date et prend celle de la page suivante, sans nouveau tirage : le thème ne change pas en pleine première journée. Le cookie est écrit avant le début de la réponse, comme le prévoit ASP.NET Core (*IHttpContextAccessor/HttpContext in ASP.NET Core Blazor apps*) ; une navigation améliorée reçoit l'attribut et la feuille de son thème, Blazor synchronisant les attributs de `<html>` (source de Blazor, `DomSync.ts`).
  - **Alternative écartée (tirage dans le navigateur)** : la page arriverait dans un thème, puis changerait d'apparence une fois le script exécuté.

## Composants, accessibilité et polices

- **Composants de présentation partagés, construits sur les seuls contrats** (bloc, bouton, étiquette, couverture, pile de couvertures…). Une couverture n'est **jamais rognée** : l'image garde ses proportions dans la place d'une couverture, et le cadre que lui donne un thème est porté par l'image, qu'il épouse quel que soit son format.
- **Icônes dessinées pour l'application**, en SVG de la couleur du texte : aucune police d'icônes ni bibliothèque à servir ou à licencier.
- **Contrastes vérifiés par un test** dans chaque thème (WCAG 2.2, 1.4.3 et 1.4.11) : 4,5:1 pour un texte, 3:1 pour la bordure d'un contrôle. Un filet décoratif n'y est pas soumis : la bordure des contrôles est donc un contrat distinct.
- **Focus en contour de la couleur du texte, détaché par une bande de la couleur de la surface** : il contraste avec l'élément, coloré ou non, comme avec cette surface ([Contrôles](../../visuel/liste-albums/controles.md)). Le texte indicatif est du texte atténué, sans opacité.
- **Polices servies par l'application**, en WOFF2, sous licence libre ([Licences](../../contraintes-techniques/licences.md)), contrôlées par leur empreinte d'intégrité, sans service extérieur à l'exécution ([Mesure d'audience de l'interface web](../../fonctionnel/mesure-audience.md)). Une police de plusieurs thèmes est déclarée une fois, une police déclarée n'étant téléchargée que par une page qui l'emploie ; la page précharge la police du texte de son thème.
  - **Alternative écartée (polices du système)** : un rendu qui change d'un appareil à l'autre, et des captures qui dépendraient des polices installées.
- **Mouvement réduit traité par chaque composant animé**, qui présente alors un état fixe ; SonarQube Cloud signale une animation qui l'omet.
  - **Alternative écartée (règle globale du thème de base)** : elle ne l'emporte sur les styles isolés qu'avec `!important`, et ne donne pas à chaque composant l'état fixe qui lui convient.

## Captures de référence

- **Chaque écran réalisé est capturé dans chaque thème, aux largeurs des maquettes** ([Maquettes](../../visuel/charte-graphique/maquettes.md)), une fois la page prête, jugée sur son propre état ([Outillage de développement .NET](../outillage.md)), et **comparé pixel par pixel** à sa référence, conservée dans le dépôt. Un écran réalisé s'ajoute aux captures, un thème ajouté reçoit celles de tous les écrans.
  - **Alternative écartée (comparaison tolérante, seuil de pixels)** : elle laisserait passer un changement réel de même ampleur (une couleur, un pixel de décalage), et aucun seuil n'est juste pour toutes les pages.
- **Références mises à jour seulement après la validation de l'utilisateur** : la validation fonctionnelle est le seul juge du rendu ([Validation fonctionnelle](../../gestion-projet/revue-tickets/validation-fonctionnelle.md)), et chaque correction lui est soumise sans attendre de captures. Jusque-là, les captures d'une PR en brouillon diffèrent de leurs références, sans que ce soit à corriger ; les références mises à jour fixent ensuite le rendu validé.
- **Captures comparées sous Linux seulement**, dans le CI et, ailleurs, dans le conteneur officiel de Playwright pour .NET : le rendu des polices diffère d'un système à l'autre.
  - **Alternative écartée (références propres à chaque système)** : chaque changement de rendu les demanderait toutes, sans garantie qu'elles montrent le même rendu.
- **Tuiles redessinées en entier** par le navigateur des captures (commutateur de Blink `--disable-partial-raster`) : une tuile redessinée en partie n'arrondit pas l'anticrénelage comme une tuile dessinée d'un coup, et la même page donnerait deux captures selon le moment de son premier rendu. La capture ne dépend ainsi que de l'état de la page.
  - **Alternative écartée (attendre une page stable)** : la course porte sur le premier rendu, pendant le chargement ; une capture prise trois secondes après différait encore une fois sur cinq.
  - **Alternative écartée (tolérance d'une unité par composante)** : elle masquerait un rendu instable au lieu de le signaler, alors que sa cause se corrige ; elle ne se justifierait que face à une instabilité du moteur qu'aucun réglage ne corrige.
- **Toute la matrice à chaque contrôle**, écrans, thèmes et largeurs : une capture coûte surtout le rendu de sa page. Au-delà de 5 minutes de tests de bout en bout dans le CI, les captures sont réparties par thème entre plusieurs jobs.
  - **Alternative écartée (empreinte de rendu : ne reprendre que les captures dont l'empreinte a changé)** : réalisée et mesurée ; elle n'épargne que la prise et la comparaison, le rendu restant à faire, et ajoute une empreinte à tenir et des échecs sans changement de pixels. Conservée dans `poc/empreinte-de-rendu/`, à reconsidérer si la prise et la comparaison redevenaient la part dominante.
