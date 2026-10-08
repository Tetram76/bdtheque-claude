# .speckit — Source de Vérité Absolue

Le dossier `.speckit/` est la **source de vérité absolue et contraignante** du projet : il prime sur toute autre information, y compris ce fichier, qui n'en est que le point d'entrée. Les règles de gouvernance (primauté du speckit, qui décide du contenu, mise à jour, rôle et autonomie de l'agent) sont dans [`.speckit/gestion-projet.md`](.speckit/gestion-projet.md) § « Gouvernance documentaire » et § « Prise de décision ».

## Primauté sur les rules et les skills (critique)

Les règles et protocoles décrits dans le `.speckit/` ont une valeur **au moins égale, et en cas de conflit supérieure et prioritaire**, à celle de toute *rule* (`.claude/rules/`) ou de tout *skill* (`.claude/skills/`, plugins compris). Un skill ou une rule ne peut ni contredire, ni assouplir, ni remplacer une règle ou un protocole du speckit : en cas de divergence, le speckit s'applique et la divergence est signalée à l'utilisateur. Les skills et rules ne sont que des mises en œuvre opérationnelles de ce que le speckit prescrit.

## Règles de consultation

- **Lire l'intégralité des fichiers `.speckit/` est la toute première action de chaque conversation** — avant toute autre lecture de fichier, recherche dans le code, réponse à l'utilisateur (y compris une simple question ou une clarification) ou action de quelque nature que ce soit. Cette première action **comprend le listage du dossier** (`Glob` sur `.speckit/*`, cf. ci-dessous), qui en fait partie : c'est la seule opération permise avant la lecture du premier fichier. Cette lecture n'est ni différable ni conditionnée à la nature apparente de la demande : elle a lieu même si la demande semble triviale, hors-sujet par rapport au `.speckit`, ou déjà couverte par le contexte de conversation.
- **« Lire » un fichier du `.speckit/` signifie en prendre connaissance en entier : chaque ligne, de la première à la dernière, et chaque ligne en entier.** C'est une obligation de résultat, quel que soit l'outil employé. Est **interdit**, sans exception, tout moyen de se dispenser d'en lire une partie : limiter le nombre de lignes lues, sauter des lignes, s'arrêter avant la fin, ou remplacer la lecture par un extrait (`head`, `tail`, `sed -n`, `grep`…), un résumé ou un souvenir du contenu. Aucune autorisation implicite n'existe : seul l'utilisateur peut déroger à cette règle, explicitement, dans le message concerné.
- **Vérifier la complétude** : le listage du dossier (`Glob` sur `.speckit/*`), premier temps de la lecture, donne tous les fichiers présents (le tableau ci-dessous peut être en retard sur le dossier) ; chacun d'eux est ensuite lu. Si l'outil coupe sa sortie — avant la fin du fichier, ou au milieu d'une ligne trop longue —, la lecture est **complétée** par une lecture ciblée de ce qui manque, et de cela seulement (reprise à la première ligne non lue, lecture intégrale de la ligne coupée par un autre moyen), jusqu'à ce que tout ait été lu : compléter ainsi une sortie coupée n'est pas une lecture partielle. Un fichier dont une partie n'a pas été lue est un fichier **non lu** : aucune autre action n'est alors permise.
- **Exception : le dossier `.speckit/visuel/`** (charte graphique, présentation des écrans, maquettes) n'entre pas dans cette lecture systématique. Il n'est lu que lorsque le chantier entrepris le nécessite (conception ou réalisation d'un écran, d'un composant d'interface, d'un élément de la charte…) ; les fichiers qui concernent ce chantier sont alors lus en entier, selon les mêmes exigences.
- Cette lecture intégrale est **également requise avant toute décision architecturale, technique ou fonctionnelle** prise plus tard dans la conversation, même si le `.speckit` a déjà été lu en début de conversation.
- **Relecture après modification** : un fichier du `.speckit/` modifié depuis sa dernière lecture (notification de modification sur disque, `pull`, merge, changement de branche, édition, PR fusionnée) est **relu intégralement** avant toute action qui en dépend. Une édition ne se fait jamais à partir d'un contenu périmé.

## Fichiers

Le périmètre détaillé de chaque fichier est exposé en tête du fichier lui-même.

| Fichier | Contenu |
| --- | --- |
| `gestion-projet.md` | Gouvernance du projet (documentaire, branches, commits, outillage, CI/CD), hors application elle-même. |
| `fonctionnel.md` | Fonctionnalités et règles métier de l'application. |
| `modele-metier.md` | Modèle du domaine : entités, attributs, relations, contraintes d'intégrité, glossaire. |
| `contraintes-techniques.md` | Contraintes techniques **imposées** (et non-contraintes explicites). |
| `choix-implementation.md` | Choix techniques de l'agent, de sa propre initiative. |
| `journal-evenements.md` | Journal des évènements externes au repo (déroge aux règles de mise à jour). |
| `suivi-implementation.md` | Avancement du plan d'implémentation. |
| `visuel/` | Charte graphique, présentation des écrans et maquettes de référence (lecture selon le chantier, cf. ci-dessus). |
