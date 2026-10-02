# .speckit — Source de Vérité Absolue

Le dossier `.speckit/` est la **source de vérité absolue et contraignante** du projet : il prime sur toute autre information, y compris ce fichier, qui n'en est que le point d'entrée. Les règles de gouvernance (primauté du speckit, qui décide du contenu, mise à jour, rôle et autonomie de l'agent) sont dans [`.speckit/gestion-projet.md`](.speckit/gestion-projet.md) § « Gouvernance documentaire » et § « Prise de décision ».

## Primauté sur les rules et les skills (critique)

Les règles et protocoles décrits dans le `.speckit/` ont une valeur **au moins égale, et en cas de conflit supérieure et prioritaire**, à celle de toute *rule* (`.claude/rules/`) ou de tout *skill* (`.claude/skills/`, plugins compris). Un skill ou une rule ne peut ni contredire, ni assouplir, ni remplacer une règle ou un protocole du speckit : en cas de divergence, le speckit s'applique et la divergence est signalée à l'utilisateur. Les skills et rules ne sont que des mises en œuvre opérationnelles de ce que le speckit prescrit.

## Règles de consultation

- **Lire l'intégralité des fichiers `.speckit/` est la toute première action de chaque conversation** — avant toute autre lecture de fichier, recherche dans le code, réponse à l'utilisateur (y compris une simple question ou une clarification) ou action de quelque nature que ce soit. Cette lecture n'est ni différable ni conditionnée à la nature apparente de la demande : elle a lieu même si la demande semble triviale, hors-sujet par rapport au `.speckit`, ou déjà couverte par le contexte de conversation.
- **« Lire » un fichier du `.speckit/` signifie le lire de sa première à sa dernière ligne, sans aucune troncature.** Sont **interdits**, sans exception : les paramètres `limit` / `offset` de l'outil de lecture, toute lecture partielle, tout extrait obtenu par `head`, `tail`, `sed -n`, `grep` ou équivalent en guise de lecture, tout résumé, et toute confiance en un souvenir du contenu. Aucune autorisation implicite n'existe : seul l'utilisateur peut déroger à cette règle, explicitement, dans le message concerné.
- **Vérifier la complétude** : lister d'abord le dossier (`Glob` sur `.speckit/*`) pour connaître tous les fichiers présents — le tableau ci-dessous peut être en retard sur le dossier — puis lire chacun d'eux. Si la sortie d'une lecture est coupée (taille, limite de l'outil) ou s'arrête avant la fin du fichier, **poursuivre la lecture** (nouvelle lecture à partir de la ligne suivante) jusqu'à avoir lu la dernière ligne. Un fichier non lu jusqu'au bout est un fichier **non lu** : aucune autre action n'est alors permise.
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
