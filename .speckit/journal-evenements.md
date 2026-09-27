# Journal des évènements externes

Ce fichier est un **journal**, pas un document de référence : contrairement aux quatre autres fichiers `.speckit/`, il déroge intentionnellement à leurs règles de mise à jour et de format (cf. [AGENTS.md](../AGENTS.md) § « Mise à jour du .speckit »).

- **Mise à jour** : uniquement sur demande explicite de l'utilisateur. Contrairement aux autres fichiers `.speckit/`, l'agent ne doit **jamais** y ajouter d'entrée de sa propre initiative, même s'il a connaissance d'un évènement pertinent (ex. déploiement effectué au cours de la conversation). Il peut le signaler à l'utilisateur, mais l'ajout attend sa demande explicite.
- **Contenu** : évènements **externes au repo** susceptibles d'influencer son développement (déploiement d'une version en test/production, incident constaté, changement d'infrastructure ou d'environnement externe, etc.). Les décisions ou changements internes au code/projet n'y figurent pas — ils relèvent des fichiers de référence appropriés.
- **Format** : chaque évènement est une entrée datée, ajoutée à la suite des précédentes (ordre chronologique) — contrairement aux autres fichiers, l'historique est ici la donnée elle-même et n'est jamais réécrit ni fusionné **à l'initiative de l'agent**. Chaque entrée précise au minimum la date, la nature de l'évènement et son impact/portée pour le développement.
- **Correction** : cette immutabilité protège contre une réécriture spontanée de l'agent, pas contre une correction demandée par l'utilisateur. Si l'utilisateur signale une erreur dans une entrée existante (date, nature, impact) ou demande le retrait d'une information sensible, l'entrée concernée est corrigée ou retirée sur sa demande explicite, au même titre que tout ajout au journal.

## Évènements

_Aucun évènement consigné pour le moment._
