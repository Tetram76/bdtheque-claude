# Mesure d'audience de l'interface web

Mesure d'audience de l'interface web.

---

Comme celle de l'outil pour agents IA (cf. `fonctionnel/agents-ia.md` § Interface pour agents IA, Suivi de l'utilisation), l'utilisation de l'interface web est suivie, pour savoir quelles fonctions sont utilisées, lesquelles le sont moins, et lesquelles ne le sont pas du tout. Ce suivi est une **mesure d'audience** au sens de la CNIL : les visites, qui isolent chacune le parcours d'une personne, ne sont pas des données anonymes au sens du RGPD, même sans rien qui identifie le visiteur. La mesure d'audience **s'aligne sur les règles de la CNIL qui la dispensent du consentement des visiteurs** (lignes directrices et recommandation « cookies et autres traceurs ») :

- elle sert **exclusivement** à l'application et à son administrateur : ses données ne sont ni communiquées à un tiers, ni confiées à un service de mesure extérieur, ni combinées à un autre traitement ;
- elle ne suit pas un visiteur d'un site ou d'une application à l'autre ;
- elle ne produit que des **statistiques**, consultées dans l'administration ;
- les visiteurs en sont **informés** et peuvent **s'y opposer** (ci-dessous) ;
- ses traceurs et ses données respectent les **durées maximales** de la CNIL (ci-dessous).

- **Périmètre** : toute l'interface web, consultation, administration et aide contextuelle (cf. `fonctionnel/index.md` § Structure de l'application).
- **Fonctions suivies** : chaque **écran** affiché (ex. fiche d'un album, liste des séries, dashboard, formulaire d'une édition) et chaque **action** déclenchée (ex. recherche lancée, rejeu d'une recherche enregistrée, navigation par initiale, page suivante d'une liste, enregistrement, suppression, import depuis une source externe, ouverture de l'aide).
- **Fiche présentée et paramètres** : une même fonction utilisée plusieurs fois sur le même objet n'a pas la même signification que sur des objets différents. Chaque utilisation porte donc ce sur quoi elle porte :
  - un **écran** qui présente une fiche précise (ex. la fiche d'un album) désigne cette fiche : trois affichages successifs de la fiche d'un même album se distinguent de trois fiches d'album différentes ;
  - une **action** porte ses **paramètres** (ex. texte ou critères d'une recherche, variables du rejeu d'une recherche enregistrée, entrée de navigation, numéro de page, fiche enregistrée ou supprimée) : la même recherche relancée trois fois se distingue de trois recherches différentes. Les paramètres ne portent jamais de secret (mot de passe, valeur d'un jeton d'accès).
- **Visites** : une visite est une **session de navigation** : celle du navigateur, qui couvre tous ses onglets et fenêtres ; ouvrir un nouvel onglet ou une nouvelle fenêtre du même navigateur ne commence pas une nouvelle visite, ouvrir un autre navigateur, si. Les utilisations d'un même utilisateur au cours d'une même visite sont regroupées, pour que le **parcours** suivi pendant une visite reste lisible et qu'une fonction utilisée de nombreuses fois au cours d'une seule visite ne pèse pas comme une fonction utilisée au cours de nombreuses visites.
- **Minimisation** : la mesure d'audience est **la moins identifiante possible**. Une visite ne porte rien qui identifie l'utilisateur (ni adresse IP, ni identité, ni appareil précis), et rien ne permet de reconnaître un même visiteur d'une visite à l'autre.
- **Type d'appareil** : chaque visite indique seulement la **catégorie** d'appareil employée, **ordinateur**, **tablette** ou **smartphone** (les supports de `contraintes-techniques/architecture-deploiement.md` § Compatibilité multi-supports), pour distinguer les usages sur ordinateur des usages mobiles.
- **Moteur de rendu** : chaque visite indique aussi le **moteur de rendu** du navigateur employé (Blink, Gecko, WebKit ou autre) et sa **version**, et non le nom du navigateur : plusieurs navigateurs partagent un même moteur et affichent les pages de la même façon. Les indicateurs disent ainsi sur quels moteurs l'application est réellement utilisée, et révèlent une fonction délaissée sur un moteur en particulier.
- **Administrateur et visiteurs** : chaque visite indique si elle est celle de l'**administrateur** (session authentifiée) ou d'un **visiteur** public, y compris lorsque l'administrateur consulte la partie publique : l'usage public se lit sans être faussé par celui de l'administrateur, qui se lit aussi à part.
- **Indicateurs**, pour chaque fonction, distingués pour les visiteurs, pour l'administrateur et ensemble, et par type d'appareil et par moteur de rendu :
  - le **nombre d'utilisations** et le **nombre de visites** au cours desquelles elle a été utilisée ;
  - pour un écran qui présente une fiche, le **nombre de fiches différentes** présentées, et pour une action, le **nombre de paramètres différents** employés ; dans les deux cas, la part des utilisations qui **répètent** aussitôt, au cours de la même visite, l'utilisation précédente de la fonction sur la même fiche ou avec les mêmes paramètres ;
  - leur **fréquence** dans le temps, et la date de sa **dernière utilisation** ;
  - une fonction **jamais utilisée** figure dans le suivi, avec des indicateurs nuls.
- **Information et opposition** : depuis toute page de l'interface web, le visiteur est informé de la mesure d'audience (finalité, données relevées, durées de conservation) et peut **s'y opposer** ; son choix est retenu, et aucune de ses visites n'est alors relevée, jusqu'à ce qu'il revienne sur ce choix.
- **Durées de conservation** :
  - un traceur déposé chez le visiteur (ex. celui qui regroupe les utilisations d'une visite, ou qui retient son opposition) ne vit pas plus de **13 mois** ;
  - le détail des visites des visiteurs publics (parcours, fiches présentées, paramètres) est **supprimé 25 mois** après la visite ;
  - les **compteurs agrégés** par fonction et par mois, qui portent les indicateurs ci-dessus sans aucune visite ni parcours, sont conservés **sans limite**, pour suivre l'évolution de l'usage dans le temps ;
  - les visites de l'**administrateur**, utilisateur de sa propre application, sont conservées sans limite.
- La mesure d'audience est consultable dans l'**administration** seule. Les visites, leurs utilisations et les compteurs agrégés ne sont pas des fiches : ils ne sont jamais modifiés, ni consignés au journal des modifications (`modele-metier.md` § Entrée du journal des modifications).
