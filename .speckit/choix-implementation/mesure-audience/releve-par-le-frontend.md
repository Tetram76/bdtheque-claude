---
type: Choix d'implémentation
title: 'Relevé par le frontend'
description: 'Relevé par le frontend : cookie de visite, écrans et actions, fonctions suivies, appareil et moteur, information et opposition ; à lire pour tout écran.'
applies_to:
  - 'src/Bdtheque.Frontend/Audience/**'
  - 'src/Bdtheque.Frontend/Visits/**'
  - 'src/Bdtheque.Frontend/wwwroot/touch-screen.js'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-09T12:00:00Z }
---

# Relevé par le frontend

- **La visite de la mesure est portée par le cookie de visite du thème** ([Thèmes : mise en œuvre](../socle/themes.md), visite), à côté de son début et de son thème : une visite qui commence n'en a pas, la première utilisation relevée en ouvre une. Le cookie est écrit une seule fois par réponse, par un service de la requête que thème et mesure alimentent, et seulement s'il a changé.
  - **Alternative écartée (cookie propre à la mesure)** : un second traceur pour la même session de navigation, dont la délimitation devrait rester alignée sur celle du thème, délimitation unique de la visite.
- **Une visite par genre d'utilisateur** : la visite enregistrée dit si elle est celle de l'administrateur ou d'un visiteur ; une utilisation relevée sous l'autre genre (connexion, déconnexion, fin de session) en ouvre une nouvelle.
- **La mesure ne fait jamais échouer une page** : une utilisation que l'`api` n'enregistre pas est journalisée en avertissement. Elle sert l'administrateur, jamais au détriment de l'utilisateur.
- **Un écran est relevé par la requête `GET` qui le rend**, par un composant placé dans la page : ni par le rendu interactif qui suit un prérendu, sans requête, ni par un formulaire soumis, qui relève son action. Chaque requête relève ainsi un seul affichage, navigation améliorée comprise.
- **Écran relevé dans une requête seulement**, la visite étant retenue par le cookie de la réponse ; un écran relevé hors requête est une erreur technique. **Une action d'une page interactive** (ex. l'enregistrement d'une fiche de saisie) est relevée dans la visite où le prérendu de la page a relevé l'écran, portée dans l'état persistant de son circuit, comme le fuseau horaire ([Internationalisation](../socle/transverse/internationalisation.md)). Le circuit n'ouvre jamais de visite, faute de réponse pour la retenir : sans visite (opposition, écran non enregistré) ou avec une visite qui n'existe plus, l'action n'est pas relevée.
  - **Alternative écartée (action relevée par une requête du navigateur)** : une requête de plus par action, pour retenir une visite que le circuit connaît déjà.
- **Actions d'une liste déduites de son adresse** : rendue en statique, une liste reçoit chaque action comme une requête `GET`, qui relève l'action avant l'écran, d'après la façon dont chaque contrôle pose ses critères. La suppression confirmée est relevée une fois faite. Une liste sans filtres n'a pas de fonction de filtre : suivie, elle figurerait parmi les fonctions jamais utilisées sans pouvoir l'être. Les actions d'une fiche se déduisent de même de son adresse ([Frontend](../socle/frontend.md), fiches de la consultation) : un visuel montré dans la visionneuse, que tout lien d'un visuel nomme avec son édition.
- **Fonctions suivies sous une clé stable** ([Mesure d'audience de l'interface web](../../fonctionnel/mesure-audience.md), Indicateurs), jamais renommée ni réaffectée, ses utilisations étant conservées et comptées sous elle ; chaque chantier d'écran y ajoute les siennes. Les pages d'erreur et « pas encore implémenté » ne sont pas des fonctions, et ne sont pas relevées.
- **Type d'appareil et moteur de rendu lus dans l'agent utilisateur**, la page étant rendue par le serveur :
  - smartphone si l'agent porte `Mobi` (recommandation de MDN, *Browser detection using the user agent*), tablette pour un iPad ou un Android sans `Mobi`, ordinateur sinon. Safari sur iPad se présente comme un Mac, que seul l'écran tactile distingue : un script le retient dans un cookie, si bien qu'une première page d'iPad compte pour un ordinateur, les suivantes pour une tablette ;
  - moteur : WebKit pour tout navigateur d'iOS, Gecko, Blink, WebKit sur Mac ; version majeure (et mineure pour WebKit, dont les versions mineures de Safari apportent des fonctionnalités) : assez pour dire sur quelles versions l'application est utilisée, sans multiplier les compartiments des compteurs.
  - **Alternative écartée (*User-Agent Client Hints*)** : ni Safari ni Firefox ne les transmettent.
  - **Alternative écartée (détection entière par un script)** : la première page, rendue avant tout script, ne la connaîtrait pas, et chaque relevé attendrait le navigateur.
- **Opposition retenue par un cookie d'un an**, sous les 13 mois d'un traceur ([Mesure d'audience de l'interface web](../../fonctionnel/mesure-audience.md), Information et opposition), jamais prolongé, la durée d'un traceur se comptant depuis son dépôt ; s'opposer efface toute trace de la mesure dans le navigateur, la visite se poursuivant pour son thème seul. Le formulaire de choix renvoie vers sa page, pour qu'un rechargement ne le soumette pas de nouveau. L'opposition n'est pas relevée, le visiteur refusant le relevé ; son retrait l'est.
