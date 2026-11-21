# Déployer BDthèque

Ce paquet contient ce qu'il faut pour déployer cette version de BDthèque avec Docker Compose, par exemple sur un NAS Synology avec Container Manager, sans les sources de l'application. Son `docker-compose.yml` télécharge les images publiques de la version depuis `ghcr.io`, sans authentification.

Le détail de chaque réglage et de l'exploitation (sauvegarde des données, mise à jour de l'image PostgreSQL) se trouve dans le [README du projet](https://github.com/Tetram76/bdtheque-claude/blob/v{{VERSION}}/README.md#déploiement), tel que l'a laissé cette version.

## Installation

1. Décompressez le paquet dans un dossier du NAS dédié à l'application, par exemple `/volume1/docker/bdtheque`.
2. Copiez `.env.example` sous le nom `.env`, puis renseignez-le :
   - `POSTGRES_PASSWORD`, le mot de passe de la base ;
   - `INTERNAL_API_KEY`, un secret aléatoire, par exemple obtenu par `openssl rand -base64 32` ;
   - `ADMIN_LOGIN` et `ADMIN_PASSWORD`, le compte qui se connecte à l'administration, avec un mot de passe long, par exemple une phrase de plusieurs mots ;
   - `FRONTEND_PORT`, le port du NAS sur lequel l'application répond ;
   - `VISUELS_HOST_PATH`, le dossier du NAS où sont rangés les visuels ;
   - `API_USER`, facultatif : l'utilisateur sous lequel tourne le conteneur `api`, sous la forme `uid:gid` (étape suivante) ;
   - `PUBLIC_URL`, seulement si l'application est servie derrière le reverse proxy du NAS : c'est alors l'adresse sous laquelle le navigateur l'atteint, par exemple `https://bdtheque.example.com`.
3. Créez le dossier des visuels, que le conteneur `api` doit pouvoir écrire, faute de quoi l'application refuse de démarrer. Sans SSH : créez-le depuis File Station, et renseignez dans `API_USER` un utilisateur du NAS qui peut y écrire, par exemple le vôtre, sans qu'il en soit forcément le propriétaire ; le [README du projet](https://github.com/Tetram76/bdtheque-claude/blob/v{{VERSION}}/README.md#dossier-des-visuels) explique comment trouver son `uid` et son `gid` depuis le Panneau de configuration. Sans `API_USER`, donnez le dossier à l'utilisateur de l'image (UID `1654`), en SSH, depuis le dossier de l'application :

   ```bash
   mkdir -p ./data/visuels && sudo chown 1654:1654 ./data/visuels
   ```

4. Démarrez l'application. Dans Container Manager, créez un projet sur ce dossier, à partir de son `docker-compose.yml`. En SSH, depuis ce dossier :

   ```bash
   docker compose pull && docker compose up -d
   ```

5. Vérifiez que l'application répond sur `http://<NAS>:<FRONTEND_PORT>/health`.

Le chiffrement HTTPS est assuré par le reverse proxy du NAS, qui relaie les requêtes en HTTP vers `FRONTEND_PORT` : l'application elle-même ne sert que du HTTP.
