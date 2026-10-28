# Revue et tickets

Règle de merge, validation fonctionnelle, verrouillage, revue de code et revue Codex, tickets et liens entre Pull Requests et tickets.

---

## Règle de merge : non-régression, porte de qualité et revue Codex obligatoires

> **Une Pull Request ne peut être fusionnée que si la non-régression est confirmée, que la porte de qualité de SonarQube Cloud est réussie ET que Codex l'a approuvée**, et, pour une Pull Request soumise à validation fonctionnelle, **que l'utilisateur l'a validée**.

- Tout merge sur `main` est conditionné à la **réussite des checks de non-régression** (pipeline CI), à la **réussite de la porte de qualité** de SonarQube Cloud sur le commit de tête (§ « Analyse SonarQube Cloud ») **et** à l'**approbation de la revue Codex** (voir « Revue de code » ci-dessous), ainsi qu'à la **validation fonctionnelle de l'utilisateur** lorsque la Pull Request y est soumise (§ « Validation fonctionnelle »).
- Les contrôles de non-régression **doivent être exécutés localement avant tout push** sur la branche de PR qui modifie un élément qu'ils vérifient (code, projets et configuration de la solution, workflow CI, fichiers Docker) — pour détecter les régressions au plus tôt et ne pas attendre le CI distant. Un push qui ne modifie que de la documentation (`.speckit/`, fichiers Markdown, skills) en est dispensé : aucun de ces contrôles ne la vérifie, et le CI reste le filet de sécurité.
- Le CI (GitHub Actions) constitue le filet de sécurité final et le verrou technique sur le merge.
- La réussite de la CI et celle de la porte de qualité sont **imposées techniquement** par le Ruleset GitHub (required status checks, `gestion-projet/depot.md` § Configuration du repository GitHub) : une PR dont un check requis n'a pas réussi ne peut pas être fusionnée.
- L'approbation de Codex n'est imposée par aucun mécanisme GitHub : elle reste une contrainte de processus, que l'agent vérifie avant tout merge.
- L'**analyse SonarQube Cloud** du commit de tête est **traitée** avant le merge (§ « Analyse SonarQube Cloud ») : chacun de ses nouveaux problèmes est corrigé, différé ou rejeté, et sa porte de qualité est réussie.

## Validation fonctionnelle

Un chantier dont l'utilisateur est le décisionnaire, c'est-à-dire qui réalise ce qu'il voit et utilise de l'application (écrans, comportements, contenu présenté : `fonctionnel/`, `visuel/`, cf. `gestion-projet/gouvernance.md` § « Qui décide du contenu »), est **validé fonctionnellement par l'utilisateur** avant sa fusion. Son ticket le précise (« Validation fonctionnelle : requise »). Un chantier purement technique n'y est pas soumis : sa validation reste la non-régression et la revue Codex. Pour un chantier qui y est soumis, elle s'ajoute à la non-régression sans la remplacer : l'utilisateur valide ce que la Pull Request change, et les contrôles de non-régression garantissent ce qui a été validé auparavant (`gestion-projet/gouvernance.md` § Qualité du code).

- **Environnement de validation** : l'agent fournit à l'utilisateur de quoi lancer la version de la Pull Request sur son poste, avec un service Docker (Docker Desktop, Rancher Desktop), en **une commande** et sans autre manipulation que celles que la plateforme impose, avec des données de validation qui couvrent les cas de l'écran (mise en œuvre : `choix-implementation/deploiement.md` § Environnement de validation fonctionnelle).
- **Moment** : la validation a lieu sur la Pull Request **en brouillon**, une fois son contenu complet, **avant** son passage en « prête pour la revue » : les corrections qu'elle entraîne ne coûtent ainsi aucune revue Codex (`gestion-projet/gouvernance.md` § « Budget IA »). L'agent l'annonce à l'utilisateur avec ce qu'il y a à valider et, à chaque demande, **toutes les lignes de commande**, complètes et prêtes à copier, dans l'ordre où l'utilisateur les exécute : mise à jour de son dépôt, lancement de la version à valider (numéro de la Pull Request compris, et option de l'écran à valider s'il en a une, ex. collection vide), arrêt de l'environnement. Une demande ne renvoie jamais à une commande donnée auparavant ni à la documentation.
- **Retours** : chaque retour de l'utilisateur est traité dans la Pull Request, puis soumis de nouveau à sa validation. Un retour qui change une exigence suit `gestion-projet/gouvernance.md` § « Prise de décision ».
- **Validation** : seule une validation **explicite** de l'utilisateur vaut validation ; elle porte sur le commit de tête qu'il a validé. Une modification ultérieure de ce qu'il a validé (ex. correction d'un retour de Codex qui change un comportement visible) lui est soumise de nouveau.

## Verrouillage des Pull Requests fusionnées

Une fois une Pull Request fusionnée, ses commentaires sont **verrouillés** avec la raison **« Resolved »** (`lock_reason: resolved` de l'API GitHub, qui traite une PR comme un ticket), la PR étant résolue. Le verrouillage vient **après** la dernière contribution à la PR. Il s'applique à toute PR fusionnée, qu'elle soit associée ou non à un ticket ; le verrouillage du ticket qu'elle ferme suit sa propre règle (cf. § « Suivi par tickets »).

## Revue de code

Des agents de revue de code (ex. Bugbot, outils d'analyse statique comme SonarQube Cloud, `gestion-projet/depot.md` § Analyse de la qualité du code) peuvent intervenir sur les Pull Requests. Règles générales d'application de leurs retours :

- Les retours ne sont **pas une source de vérité** : ils sont systématiquement soumis à contre-vérification.
- Un retour est **appliqué** s'il est pertinent et que le gain justifie le coût de la modification.
- Un retour est **rejeté** s'il est jugé non pertinent, incorrect, ou si son coût (complexité, temps, lisibilité dégradée) est disproportionné par rapport au bénéfice obtenu.
- La décision d'accepter ou de rejeter un retour suit la règle générale de « Prise de décision » ci-dessus : un retour **technique** relève de l'agent ; un retour **fonctionnel ou métier** relève de l'agent si le `.speckit` existant permet déjà de trancher, ou d'une décision explicite de l'utilisateur (un point à la fois) s'il s'agit d'un véritable changement d'exigence ou d'une ambiguïté.
- L'objectif de robustesse est une application **à l'épreuve de l'utilisateur** (parcours réels, usages légitimes, erreurs utilisateur plausibles), pas un code **blindé** contre tout scénario théorique ou détourné. Le code est écrit pour le contexte de cette application — il n'a pas vocation à devenir une librairie publique ni à être réutilisé hors projet.
- Les scénarios invoqués par une revue (cas limites, abus, chemins d'exécution) doivent être **pertinents, valides et possibles** dans ce contexte applicatif ; un scénario hypothétique, irréaliste ou hors périmètre ne justifie pas une complexification du code.

### Analyse SonarQube Cloud

L'analyse SonarQube Cloud (`gestion-projet/depot.md` § Analyse de la qualité du code) fait partie du cycle de réalisation d'une Pull Request. Elle a lieu sur chaque commit poussé, brouillon compris, en parallèle du CI, et ne coûte donc ni revue Codex ni attente propre tant que la PR est en brouillon : c'est là qu'elle est traitée.

- **Moment** : avant le passage de la PR en « prête pour la revue » (§ Ouverture de la PR), l'agent attend l'analyse du commit de tête et la traite. Après ce passage, chaque push (correction d'un retour de Codex) produit une nouvelle analyse, traitée de même avant le merge ; une correction qu'elle appelle rejoint, autant que possible, le push suivant d'une correction de Codex, pour ne pas déclencher une revue de plus.
- **Contenu** : les **nouveaux problèmes** de la PR (bugs, vulnérabilités, *code smells*), ses **points sensibles de sécurité** (*security hotspots*) à revoir, et sa porte de qualité. Chaque problème est traité comme un retour de revue (§ « Traiter un retour » : contre-vérification, cause racine, confrontation à l'objectif de la PR, correction à la racine dans un commit dédié, report ou rejet justifié) ; une règle d'analyse inadaptée au projet est un rejet, justifié de même.
- **Porte de qualité** : condition du merge (§ Règle de merge), elle est réussie avant le passage en « prête pour la revue » comme avant le merge. Ses conditions portent sur le nouveau code : un problème corrigé, reporté ou rejeté (et marqué comme ci-dessous) n'y compte plus ; une couverture insuffisante se corrige par les tests qui manquent (`gestion-projet/gouvernance.md` § Qualité du code), ou, pour un fichier que les tests .NET ne peuvent pas couvrir, par son exclusion de la couverture, justifiée dans `gestion-projet/depot.md` § Analyse de la qualité du code.
- **Trace** : les problèmes différés ou rejetés sont listés, avec leur justification, dans un **commentaire de la PR**, mis à jour à chaque analyse traitée. L'agent les marque lui-même dans SonarQube Cloud, acte de gestion du dépôt (`gestion-projet/gouvernance.md` § Prise de décision), chacun avec un commentaire qui renvoie à sa justification : *faux positif* pour un problème dont la prémisse est fausse ; *accepté* pour une règle inadaptée au cas, comme pour un problème pertinent dont la correction est différée (état que SonarQube Cloud réserve à un problème reconnu mais non corrigé dans l'immédiat), le commentaire nommant alors le ticket ou la note du plan qui porte sa correction. L'agent passe par l'API de SonarQube Cloud, avec le jeton du poste (`gestion-projet/depot.md` § Repository).
- **Dispense** : une PR dont aucun push ne modifie de fichier analysé (documentation seule : `.speckit/`, fichiers Markdown, skills ; prototypes de `poc/`) n'a aucun problème à traiter ; sa porte de qualité, sans nouveau code, est réussie d'office.
- **Analyse indisponible** (service en panne, PR venant d'un fork, analyse en échec) : sans porte de qualité, la PR ne peut pas être fusionnée. L'agent cherche la cause d'une analyse en échec et la corrige ; une panne du service suspend la fusion jusqu'à son retour, ce que l'agent signale à l'utilisateur. Une PR venant d'un fork, sans accès au jeton de l'analyse, ne peut pas être fusionnée en l'état, comme pour l'analyse des vulnérabilités des images (`gestion-projet/depot.md` § Intégration continue (CI)).

### Analyse des vulnérabilités des images Docker

Les images Docker de l'application sont analysées par **Docker Scout** lors de la revue de chaque Pull Request, par le CI (check requis, cf. `gestion-projet/depot.md` § « Intégration continue (CI) »), et de nouveau avant toute publication. L'analyse échoue dès qu'une image ajoute une vulnérabilité **corrigeable** par rapport à l'image publiée, qu'elle ne doit ni introduire ni voir publiée (`contraintes-techniques/architecture-deploiement.md` § Déploiement).

Une vulnérabilité ainsi relevée est **traitée dans la mesure du possible** : son correctif est appliqué (version corrigée de l'image de base ou de la dépendance concernée). Si ce correctif ne peut pas être appliqué (ex. il impose une montée de version incompatible avec l'application), la Pull Request reste bloquée : la vulnérabilité est évaluée dans le contexte réel de l'application (impact, probabilité de survenance) et soumise à la décision de l'utilisateur.

### Revue Codex (bloquante)

Une revue **Codex** se déclenche automatiquement à la création d'une Pull Request hors brouillon (*draft*), à son passage de brouillon à « prête pour la revue », et à chaque commit poussé sur une PR qui n'est pas en brouillon. À la différence des autres agents de revue, son approbation est une **condition bloquante du merge** : une PR ne peut être fusionnée que si Codex a donné sa **validation finale** pour le commit de tête, signe qu'une revue n'a rien trouvé à corriger. Elle prend l'une de deux formes :

- une réaction 👍 sur la **description de la PR**, qui ne désigne aucun commit : seul le déroulé de la revue la rattache au commit de tête (cf. § Cycle) ;
- un **commentaire de conclusion** de Codex qui déclare n'avoir trouvé aucun problème majeur (« Didn't find any major issues ») et désigne explicitement le **commit de tête** comme commit revu (« Reviewed commit »). Un tel commentaire qui désigne un commit antérieur ne vaut pas validation du commit de tête.

#### Ouverture de la PR

Codex revoit ce qui est poussé, que le travail soit achevé ou non : une PR ouverte trop tôt lui fait revoir un contenu incomplet, et consomme sa revue en pure perte (cf. `gestion-projet/gouvernance.md` § Budget IA). Une PR est donc toujours **créée en brouillon**, et n'est **passée de brouillon à « prête pour la revue »** qu'une fois le **contenu de sa branche complet**, les checks du CI réussis, et l'analyse SonarQube Cloud de son commit de tête traitée (§ Analyse SonarQube Cloud) : ce qu'ils relèvent est ainsi corrigé avant la revue de Codex, sans lui en coûter une de plus. Tant que ce contenu est en cours d'élaboration ou de discussion, il reste local, ou sur la PR en brouillon. Seule une PR limitée à `poc/`, hors de la revue Codex (aucun fichier analysé non plus), peut être créée directement « prête pour la revue ».

**Un travail en cours, c'est une PR en brouillon.** Le contenu d'une branche est celui du **chantier entier**, tel que l'utilisateur l'a demandé, et non celui de l'étape qui vient d'aboutir. Dans un chantier mené en plusieurs étapes (ex. les maquettes de plusieurs écrans, revues une à une par l'utilisateur), la validation d'une étape n'achève pas le chantier : la PR reste en brouillon jusqu'à la fin de sa dernière étape. Une PR créée pendant le chantier, pour publier ou sauvegarder un état intermédiaire, est toujours créée en brouillon. En cas de doute sur l'achèvement du contenu, la PR est en brouillon.

Le passage d'un brouillon en « prête pour la revue » déclenche une revue Codex, comme la création d'une PR hors brouillon : il ne s'accompagne donc d'aucune demande de revue. L'agent vérifie seulement que Codex s'est saisi de la PR (§ Cycle, revue non démarrée) ; à défaut, et à défaut seulement, il demande la revue par un commentaire `@codex review` (signaux à observer : skill `codex-review-loop`).

#### Budget de Codex épuisé

Codex signale l'épuisement de son budget par un commentaire sur une PR (« limite d'usage atteinte »), qu'aucune revue ne suit. Toute revue déclenchée dans les **5 heures** qui suivent ce signalement est refusée et **réarme ce délai** de 5 heures, quelle que soit la PR qui la déclenche, le budget étant commun à toutes. Tant que le dernier signalement de Codex sur une PR ouverte, ou fermée depuis moins de 5 heures, date de moins de 5 heures, l'agent ne déclenche donc **jamais** de revue Codex, intentionnellement ou non :

- aucune PR créée hors brouillon, ni passée de brouillon à « prête pour la revue » : une PR à ouvrir l'est en brouillon ;
- aucun commit poussé sur une PR qui n'est pas en brouillon ;
- aucun commentaire qui sollicite Codex (`@codex review`, ou toute autre mention `@codex`).

Avant chacun de ces actes, l'agent vérifie la date du dernier signalement de Codex sur ces PR : fermer ou fusionner la PR qui porte le signalement ne lève pas le délai. Cette règle prévaut sur toute autre qui prévoit de solliciter Codex (passage en « prête pour la revue », relance par `@codex review`). Une PR en brouillon étant hors du périmètre de Codex, le travail s'y poursuit normalement (création, push).

La PR sur laquelle Codex a signalé son budget épuisé est en outre **suspendue** : l'agent le signale à l'utilisateur, n'arme plus d'observateur et ne relance pas Codex. Sa revue ne reprend que sur demande de l'utilisateur, une fois le délai écoulé.

#### Statut des retours de Codex

Codex ne connaît **ni le contexte du projet** (`.speckit/`, architecture, conventions, choix déjà statués), **ni l'objectif de la PR** : il se contente de relire les diffs. L'agent qui traite les retours est le **seul à connaître ces contextes**, donc le seul à pouvoir décider ; et, comme pour toute décision technique (cf. `gestion-projet/gouvernance.md` § Prise de décision), il en est le **seul décisionnaire**. Il est donc primordial de **confronter chaque retour** au contexte du projet et à l'objectif de la PR. Dans tous les cas, les retours de Codex ne sont que des **indications**, à intégrer à sa réflexion — jamais des consignes à suivre à la lettre.

#### Traiter un retour

1. **Contre-vérifier la pertinence**, toujours et sans exception : au regard du contenu du `.speckit/` et des bonnes pratiques applicables, faisabilité du scénario dans le contexte réel de l'application, critère « à l'épreuve de l'utilisateur » (cf. règles générales ci-dessus). Un retour non vérifié n'est ni appliqué, ni différé, ni rejeté.
2. **Établir la cause racine** d'un retour jugé pertinent : le problème est analysé dans sa globalité, au-delà du passage relevé. La cause peut se trouver hors du diff, et hors de l'objectif de la PR.
3. **Confronter à l'objectif de la PR**, impérativement, le **point dans son ensemble** — ce qu'il relève, sa cause et sa correction —, et non le seul passage relevé : Codex ne connaissant pas cet objectif (§ « Statut des retours de Codex »), un retour pertinent n'est pas pour autant à corriger dans la PR.
   - **Point qui relève, dans son ensemble, de l'objectif de la PR** : il est corrigé dans la PR (étape 4).
   - **Point qui sort de l'objectif de la PR**, par ce qu'il relève, par sa cause ou par sa correction, y compris lorsque le symptôme se trouve dans le diff : sa correction peut être **différée**, sans qu'aucun correctif de surface ne compense dans la PR la cause laissée en place. Elle n'est jamais perdue pour autant, et sa trace dépend de la phase du plan d'implémentation dont relève le point :
     - **phase déjà terminée** : la correction prend la forme d'un **ticket** (§ « Issues ») ;
     - **phase déjà découpée** : elle est reportée dans le ticket ouvert du chantier dont elle relève, ou, à défaut, prend la forme d'un **nouveau ticket** ;
     - **phase pas encore découpée** : aucun ticket ; une **note** est ajoutée sur cette phase dans [`suivi-implementation.md`](../suivi-implementation.md), pour que le point soit intégré au plan lors de son découpage.
4. **Corriger à la racine** un retour validé dont la correction n'est pas différée : corriger la cause établie, jamais un correctif de surface (« sparadrap ») qui ne traite que le symptôme relevé. Un **commit dédié** par retour appliqué.
5. **Répondre à chaque retour**, toujours, qu'il soit appliqué (avec le commit correspondant), différé (avec le ticket ou la note du plan qui porte sa correction) ou rejeté (avec la justification du rejet).
6. **Apprécier chaque commentaire** de Codex, toujours, par une réaction : 👍 s'il était utile, 👎 sinon (Codex le demande dans chacun de ses commentaires). Un retour pertinent est utile, même lorsque sa correction est différée.
7. **Résoudre** chaque conversation de revue une fois tous ses retours traités.

#### Cycle

Le traitement est répété **tant que Codex émet des retours**, jusqu'à sa validation finale pour le commit de tête (👍 sur la description de la PR, ou commentaire de conclusion sans problème majeur qui désigne ce commit, cf. ci-dessus). Le merge n'intervient qu'ensuite, en complément de la réussite du CI.

- Après le push de commits, Codex relance une revue (avec un léger délai) ; l'agent attend son résultat avant de poursuivre.
- **Surveillance active obligatoire** : l'agent **surveille lui-même** la PR tant que la validation de Codex n'est pas acquise. Aucune notification ne remonte lorsque Codex la valide : ni la réaction 👍 sur la description de la PR ni le commentaire de conclusion ne sont signalés par le suivi de PR de l'application, et Codex ne poste pas toujours ce commentaire. L'agent ne peut donc ni attendre une notification ni se fier à la seule présence d'un commentaire : il interroge l'état réel de la PR (réactions, revues, commentaires) jusqu'à la validation ou un nouveau retour.
  - **Un observateur vivant par PR en attente** : la surveillance est un processus actif (tâche de fond qui interroge l'état réel de la PR), jamais une intention. L'agent ne déclare pas « je surveille » sans qu'un observateur tourne, et ne termine pas un tour tant qu'une PR en attente n'en a pas.
  - **Fin de l'observateur** : un observateur ne s'arrête que sur un état terminal propre à sa PR — nouveau retour de Codex (revue, commentaire), PR fusionnée ou fermée, validation finale du commit de tête (👍 de Codex postérieur au commit de tête, ou commentaire de conclusion sans problème majeur qui le désigne) **une fois les checks de la CI présents et tous terminés** (son résultat conditionnant aussi le merge ; aucun check enregistré n'est pas une CI terminée), changement du commit de tête, fin d'une revue de Codex lancée avant le déclencheur (l'observateur continue pendant qu'elle se déroule ; son verdict porte sur un commit antérieur, et son 👍 ne vaut jamais approbation du commit de tête), impossibilité durable d'interroger GitHub, ou délai dépassé : revue non démarrée **2 minutes** après son déclencheur, ou démarrée sans verdict **10 minutes** après son début (ci-dessous). Il ne s'arrête jamais sur le signal d'une autre PR : une PR par observateur. Un échec ponctuel d'interrogation n'est jamais pris pour un état de la PR : il est retenté. Tout autre commentaire de tête de Codex (ex. « limite d'usage atteinte », qu'aucune revue ne suit ; tableau de suivi des revues) rend la main à l'agent pour lecture : il ne vaut **jamais validation**, que seuls le 👍 et le commentaire de conclusion sans problème majeur qui désigne le commit de tête constituent.
  - **À chaque fin d'observateur**, l'agent traite l'état constaté, puis **ré-arme** un observateur pour chaque PR toujours en attente avant de rendre la main.
  - **Revue non démarrée** : une revue commence lorsque Codex pose 👀 sur la description de la PR. Faute de 👀 **2 minutes** après la dernière action qui devait la déclencher (création ou passage en « prête pour la revue » de la PR, push, demande de revue), la revue n'a pas démarré. L'agent la demande alors (`@codex review`, dans le respect de § « Budget de Codex épuisé ») et ré-arme l'observateur sur cette demande.
  - **Codex bloqué** : une revue démarrée dont le verdict (validation finale ou retour) n'est pas arrivé **10 minutes** après son début (son 👀) est celle d'un Codex **bloqué** : rien n'en sortira. L'agent demande alors une nouvelle revue du commit de tête (`@codex review`, dans le respect de § « Budget de Codex épuisé ») et ré-arme l'observateur sur cette demande.
  - Si la revue demandée pour l'un de ces deux motifs ne démarre pas ou reste à son tour bloquée, la PR est suspendue (ci-dessous) plutôt que de payer une demande de plus.
  - **PR suspendue** : une revue toujours non démarrée ou bloquée après une nouvelle demande de revue, ou une impossibilité durable d'interroger GitHub, **suspend** la PR, comme un budget de Codex épuisé (§ « Budget de Codex épuisé ») : rien n'y avancera sans intervention, et un nouvel observateur ne ferait qu'attendre en vain. L'agent le signale à l'utilisateur et n'arme plus d'observateur ; une PR suspendue n'est pas une PR en attente.
  - **Pas de push pendant une revue** : le 👍 de Codex ne désigne aucun commit ; seul le déroulé de la revue (Codex retire son 👍 et pose 👀 au début, conclut par un 👍 ou une revue) le rattache au commit de tête. L'agent ne pousse donc jamais pendant une revue en cours (👀 présent) : un push fait pendant une revue n'en déclenche pas de nouvelle, et le 👍 qui conclut la revue en cours passerait à tort pour l'approbation du nouveau commit. Si cela arrive malgré tout, l'agent attend la fin de cette revue, puis demande celle du commit de tête (`@codex review`).
  - **Budget de Codex épuisé** : un commentaire de Codex signalant que sa limite d'usage est atteinte **suspend** la PR, et interdit pendant 5 heures tout acte qui déclencherait une revue, sur quelque PR que ce soit (§ « Budget de Codex épuisé »).
- Si **tous les retours sont rejetés ou différés sans ajouter de commit**, Codex ne relance pas la revue : l'agent la **force** par un commentaire `@codex review` sur la PR.
- **Faux positif sur la signature des commits** : Codex peut signaler des commits non signés. Si la vérification (statut « Verified » côté GitHub) établit un faux positif, il se traite comme tout retour rejeté (réponse justifiée, appréciation 👎, résolution), et Codex est alors considéré comme n'ayant **plus rien d'autre à remonter**. L'agent relance la revue (`@codex review`) jusqu'à **3 fois** ; si Codex persiste sur ce seul faux positif, **sans autre commentaire**, la PR est considérée comme **validée par Codex**, en l'absence de validation finale.

La procédure opérationnelle détaillée (commandes `gh`, requêtes GraphQL de résolution de conversation, etc.) est décrite dans le skill `.claude/skills/codex-review-loop/`.

## Issues

Les Issues GitHub (« tickets ») servent à :

- tracer **chaque chantier** du plan d'implémentation (cf. § « Suivi par tickets » ci-dessous) ;
- tracer ponctuellement des **bugs** à corriger ou des **fonctionnalités** à implémenter dans le futur, hors plan d'implémentation.

## Suivi par tickets

Chaque chantier listé dans [`suivi-implementation.md`](../suivi-implementation.md) **doit** faire l'objet d'un ticket GitHub.

- **Correspondance** : un ticket correspond à une seule Pull Request ; une Pull Request correspond à zéro ou un ticket (jamais plusieurs), son **ticket de référence**, qu'elle réalise **entièrement**. Lorsque plusieurs tickets correspondent au chantier d'une PR, l'un d'eux est désigné comme ticket de référence, et les autres lui sont **associés** par les relations de GitHub (doublon, ticket lié…) :
  - la PR n'étant pas celle d'un ticket associé, elle peut ne le réaliser que **partiellement** sans enfreindre la correspondance « un ticket, une PR » ;
  - un ticket associé entièrement réalisé par la PR est fermé à sa fusion (comme doublon s'il en est un), avec son commentaire de clôture, puis verrouillé ;
  - un ticket associé partiellement réalisé reste ouvert, avec un commentaire qui indique ce que la PR en a réalisé et ce qui reste à faire : il est complété par une PR ultérieure, dont il est le ticket de référence.

  Un ticket parent, découpé en sous-tickets (ci-dessous), n'est réalisé par aucune PR en propre : chacun de ses sous-tickets l'est par la sienne. « Ticket » désigne toute Issue GitHub : la règle vaut pour les chantiers du plan comme pour les bugs et fonctionnalités tracés hors plan.
- **Chantiers hors suivi** : un chantier mené hors du plan d'implémentation peut faire l'objet d'un ticket, sans que ce soit obligatoire. Seuls les chantiers listés dans le suivi **doivent** avoir un ticket, à l'exception des chantiers de consolidation (ci-dessous).
- **Création** : le ticket d'un chantier du suivi est créé au moment du **découpage de la phase** en chantiers ; ses sous-tickets, s'il est découpé, le sont au démarrage de son chantier (ci-dessous).
- **Règles transverses au découpage d'une phase** : au découpage d'une phase, l'agent établit comment les règles transverses du `.speckit/` s'appliquent à ce qu'elle produit — non-régression et TDD (`gestion-projet/gouvernance.md` § Qualité du code), exploitation des technologies retenues (`contraintes-techniques/index.md` § Exploitation des technologies retenues), analyse des vulnérabilités (§ Analyse des vulnérabilités des images Docker)… —, en particulier à une nature de livrable qu'aucune phase précédente n'a produite (interface, outil pour agents IA, modèle d'estimation…). Les moyens qu'elles exigent (outillage, jobs du CI) sont portés par un chantier du découpage, ou par un élément d'un ticket, placé avant les chantiers qui en dépendent : un découpage qui les omet est incomplet.
- **Découpage au démarrage d'un chantier** : au démarrage du chantier d'un ticket, avant toute implémentation, l'agent estime si le ticket peut être réalisé **en une seule PR**. S'il l'estime trop gros pour cela, il **découpe le ticket** avant de commencer, la correspondance « un ticket, une PR » restant la règle :
  - le ticket d'origine devient un **ticket parent**, qu'aucune PR ne ferme : il est découpé en **sous-tickets**, rattachés par la relation de sous-ticket de GitHub, chacun réalisé entièrement par sa propre PR et rédigé selon § « Contenu d'un ticket ». GitHub affiche les sous-tickets d'un ticket parent, avec leur titre et leur état : sa description ne les liste pas ;
  - si le ticket parent figure dans le suivi, celui-ci remplace sa ligne par celles de ses sous-tickets, et mentionne le ticket parent dans le texte qui les introduit ; cette mise à jour est portée par la PR du premier sous-ticket. Un ticket absent du suivi (chantier hors suivi, consolidation) n'y fait pas entrer ses sous-tickets ;
  - le ticket parent est fermé une fois tous ses sous-tickets fermés, avec un commentaire de clôture qui récapitule, sous-ticket par sous-ticket, la PR et sa réalisation, puis verrouillé comme tout ticket résolu.
- **Ajustement du speckit avant la première release du socle** : tant que le socle de l'application (`db`, `api` et `frontend`) n'a pas eu sa première release, un ticket qui répercute dans l'application un ajustement du `.speckit/` couvre au plus **une phase non validée**, quel que soit son état (en réalisation, à découper…) ; une phase est **validée** lorsque tous ses chantiers sont terminés et qu'elle a été **consolidée** : les chantiers identifiés par la revue globale de la phase, menée à son terme, ont tous été traités (cf. § Consolidation d'une phase ci-dessous). Un ajustement qui ne touche que des phases validées fait l'objet d'un **ticket unique**, quel que soit leur nombre. Un ajustement qui touche à la fois des phases validées et des phases non validées fait l'objet d'un **ticket global**, découpé en **sous-tickets** : un pour l'ensemble des phases validées, et un par phase non validée **déjà découpée**. Aucun sous-ticket n'est créé pour une phase pas encore découpée : le contenu qui la concerne figure dans le `.speckit/`, et il est pris en compte lors de son découpage. Après cette première release du socle, cette règle ne s'applique plus : un ticket est traité dans sa globalité, quelle que soit sa portée (une nouvelle fonctionnalité touchant la base, l'infrastructure et le frontend fait l'objet d'une seule PR).
- **Consolidation d'une phase** : lors de la consolidation d'une phase, un ticket est créé pour **chaque chantier identifié**, sans que le suivi ait besoin d'être mis à jour : ces chantiers ont un ticket obligatoire même s'ils ne sont pas listés dans le suivi.
- **Pull Request** : la PR est associée à son ticket et le **ferme automatiquement au merge** (mot-clé `Closes #<numéro>` dans la description, cf. § « Lien entre Pull Requests et Issues »). Le ticket est donc clôturé quand la PR est fusionnée.
- **Commentaire de clôture** : à la fusion de la PR, un commentaire est ajouté au ticket pour indiquer **comment l'implémentation a répondu au ticket**. Il est **détaillé** et suit le contenu du ticket :
  - **Réalisation** : la PR (numéro, commit squashé), puis, **élément par élément du ticket**, ce qui a été réalisé (comportements, règles appliquées, éléments ajoutés) ;
  - **Décisions et écarts** : les décisions prises pendant l'implémentation et les écarts au regard du ticket ; l'absence d'écart est signalée explicitement ;
  - **Références** : les sections du `.speckit/` concernées et la PR.
  - Les **tests** ne sont mentionnés que lorsqu'un cas particulier important le justifie : refactorisation, ajout d'un mécanisme générique de test, ou **cas d'usage volontairement écarté des tests** (le commentaire en indique alors la **raison**). Jamais en simple liste de ce qui a été testé.
- **Verrouillage du ticket** : une fois le commentaire de clôture ajouté, les commentaires du ticket sont **verrouillés** avec la raison **« Resolved »** (`lock_reason: resolved` de l'API GitHub), le ticket étant résolu. Le verrouillage vient **après** le commentaire de clôture, dernière contribution du ticket. Il s'applique à tout ticket fermé par le merge de sa PR, qu'il appartienne ou non au plan d'implémentation.
- **Traçabilité jusqu'au ticket** : le commit final de la PR doit permettre de remonter, directement ou indirectement, jusqu'au ticket, par les seuls mécanismes natifs de GitHub (aucun dispositif ad hoc). La description de la PR devenant le corps du commit squashé (cf. `gestion-projet/depot.md` § « Conventions de commit »), le mot-clé de fermeture qu'elle contient figure dans le commit : GitHub y résout le ticket, et le suffixe `(#<PR>)` ajouté au titre par le squash merge mène à la PR. La référence au ticket est donc **toujours dans la description de la PR**, jamais seulement dans un commentaire ou dans le titre.
- **Statut** : l'état d'un chantier (à faire / fait) est **défini par l'état de son ticket** (ouvert / fermé). Le suivi de `main` le reflète uniquement par la mise en forme du numéro du ticket : **barré** (`~~#<numéro>~~`) lorsque le ticket est fermé, normal lorsqu'il est ouvert ; aucune autre colonne ou mention de statut. Sur la branche de la Pull Request d'un chantier, le numéro est barré **par le tout premier commit**, avant tout autre changement, alors que le ticket est encore ouvert : il part ainsi dès le premier push, et n'exige jamais un push ultérieur, qui déclencherait une revue Codex de plus (`gestion-projet/gouvernance.md` § Budget IA). Il n'arrive sur `main` qu'avec le merge de la PR, qui ferme le ticket.
- **Version** : le ticket d'un chantier du suivi est rattaché, dès sa création, au jalon de la version qui le livre, et sa Pull Request au même jalon ; les sous-tickets d'un ticket découpé, à celui de leur ticket parent. Les autres tickets et Pull Requests suivent `gestion-projet/depot.md` § Versions et releases.
- **Contenu du suivi** : pour chaque chantier, le suivi indique le **numéro du ticket** et en décrit succinctement le contenu (en pratique, son titre).
- **Cohérence suivi ↔ tickets** : toute correction apportée au découpage dans le suivi est **reportée dans le ticket concerné**, dans le respect de la définition du contenu d'un ticket ci-dessous.

### Contenu d'un ticket

Le **titre** d'un ticket est un libellé libre, qui décrit le chantier : il ne suit **pas** la convention Conventional Commits, réservée aux commits et aux titres de Pull Request (cf. `gestion-projet/depot.md` § « Conventions de commit »).

Le ticket est plus précis que la ligne du suivi, sans pour autant être un plan d'implémentation. Il reprend **tout ce qui est connu au moment du découpage de la phase et peut influencer l'implémentation du chantier** :

- la liste des **éléments à implémenter** ;
- les **contraintes** applicables (règles du `.speckit/`, dépendances avec d'autres chantiers, périmètre exclu) ;
- les **éléments techniques déjà statués** pendant ou avant le découpage de la phase.

Le ticket **ne décrit jamais comment implémenter** le chantier : la rédaction d'un ticket n'implique **pas** la création du plan d'implémentation, qui relève de l'agent au moment de réaliser le chantier.

## Lien entre Pull Requests et Issues

- Une Pull Request a **zéro ou un** ticket de référence (cf. § « Suivi par tickets », correspondance), jamais plusieurs ; un ticket n'a qu'une seule PR, celle dont il est le ticket de référence. Les tickets associés au ticket de référence sont cités dans la description de la PR **sans mot-clé de fermeture**, qui fermerait aussi un ticket partiellement réalisé.
- Une PR qui réalise un chantier du plan d'implémentation est associée à **son ticket**, qu'elle ferme au merge. Hors plan d'implémentation, une PR peut n'avoir aucun ticket.
- Si une PR **répond** à un ticket (correction d'un bug tracé, implémentation d'une fonctionnalité tracée), elle **doit le référencer** (ex. mention `#<numéro>` dans la description).
- Le ticket de référence étant **entièrement traité** par sa PR, sa référence utilise un mot-clé de fermeture automatique GitHub (`Closes`, `Fixes`, `Resolves #<numéro>`), afin qu'il soit **automatiquement clôturé au merge** de la PR.
- Un ticket qui ne pourrait être traité que **partiellement** par la PR dont il est le ticket de référence est à découper en plusieurs tickets, chacun traité entièrement par sa propre PR, dès le démarrage de son chantier (cf. § « Suivi par tickets », découpage au démarrage d'un chantier).
