# Prototype : empreinte de rendu des captures

Solution **réalisée puis écartée** pour le contrôle du rendu des écrans (#285), conservée pour pouvoir la reprendre si elle devenait pertinente. Rien de ce qu'elle contient n'est statué : elle ne fait partie ni de la solution (`Bdtheque.slnx`), ni du CI. Le choix retenu et la raison de l'écart sont dans `.speckit/choix-implementation/socle/themes.md` (captures de référence).

## Idée

Chaque page reste rendue dans chaque thème et à chaque largeur, mais sa capture n'est prise et comparée que si l'**empreinte** de ce que le navigateur peint a changé depuis la référence ; c'est l'idée de [TurboSnap](https://docs.chromatic.com/docs/setup-turbosnap), appliquée à l'état rendu plutôt qu'aux fichiers modifiés.

## Mécanisme (`empreinte-de-rendu.patch`)

- **Empreinte** (`Captures/RenderFingerprint.cs`) : une ligne par entrée, chacune avec son hash SHA-256, pour qu'un changement d'empreinte nomme ce qui a changé :
  - options de la capture, navigateur, sa version et ses commutateurs ;
  - document tel que les scripts l'ont laissé (balises, attributs triés, textes, défilements, focus, valeurs des champs), largeur et hauteur de la fenêtre ;
  - règles des feuilles de style appliquées (CSSOM, feuilles construites comprises) ;
  - octets de chaque police et image que la page désigne (`url()` des règles et des attributs `style`, image affichée des `<img>`), relus dans la page depuis le cache du navigateur.
  - En sont exclus, parce qu'ils changent d'un rendu à l'autre sans être peints : commentaires de Blazor, attributs `_bl_`, attribut `value` d'un champ (Blazor le retire quand le champ devient interactif ; la valeur peinte est gardée), champs cachés (jeton antiforgery), Guid, adresse de l'application (port tiré). Les scripts aussi : ce qu'ils font est dans l'état qu'ils laissent.
- **Références** (`Captures/CaptureStore.cs`) : `{écran}.fingerprint` à côté de `{écran}.png`. Empreinte identique : pas de capture. Empreinte changée, pixels identiques : l'empreinte est mise à jour en local, le CI échoue en demandant de la committer (sinon la capture serait reprise à chaque passage, sans que personne le voie). `-Update` écrit capture et empreinte.
- **Contrôle local** (`scripts/Test-Captures.ps1`) : rapporte du conteneur les empreintes mises à jour.
- **Tests** (`Captures/RenderFingerprintTests.cs`) : même empreinte pour un nouveau rendu et pour une page devenue interactive ; empreinte différente pour un style, un texte, un défilement ou une police modifiés. Chaque test de sensibilité a été vérifié en échec sans la partie de l'empreinte qu'il fixe.

## Défauts rencontrés, et leur correction

- Les adresses des images contenaient un Guid tiré à chaque passage : normalisées comme le document.
- Le corps d'une réponse lu par Playwright (`IResponse.BodyAsync`) n'arrive jamais quand sa requête est abandonnée après ses en-têtes : un passage s'est bloqué. Lire les octets par un client à part (`APIRequest`) contournerait le routage de la page : ils sont relus dans la page, par `fetch` borné.
- L'attribut `value` d'un champ, retiré par Blazor au rendu interactif, rendait l'empreinte de la page des contrôles et de l'accueil dépendante du moment : exclu.
- Les polices relevées par les évènements réseau variaient d'un passage à l'autre, une police pouvant être demandée plus tard : la liste est tirée de l'état de la page.

Une fois corrigées, les 72 empreintes étaient identiques sur deux passages complets consécutifs.

## Mesures (2026-11-07, 72 captures, conteneur de `scripts/Test-Captures.ps1`, moteur Docker à 2 processeurs et 2 Go)

| Par capture | Sans empreinte | Avec empreinte à jour |
| --- | --- | --- |
| Capture et comparaison | 103 ms en moyenne | aucune |
| Calcul de l'empreinte | — | 43 à 61 ms |
| Tests des captures, au total | 34 s | 27 s |

## Pourquoi elle est écartée

Les correctifs de #283 et #285 (attente sur l'état de la page, contextes fermés, comparaison sur les tampons de pixels) ont ramené la capture et sa comparaison à une centaine de millisecondes. L'essentiel du coût restant est le **rendu** de chaque page dans chaque thème, que l'empreinte ne supprime pas : le coût croît toujours avec les thèmes et les écrans, pour environ 50 ms gagnés par capture. En échange, elle ajoute un fichier par capture à tenir à jour et un échec du CI à chaque changement du rendu qui ne change pas les pixels. Elle ne couvre pas non plus les polices du système (repli), qui ne sont dans aucune empreinte.

## Quand la reprendre

Si la capture et sa comparaison redevenaient la part dominante du coût d'un contrôle, par exemple avec des pages bien plus longues, davantage de largeurs, ou un rendu devenu bien moins cher que la capture.

## Reprendre

Le correctif est établi sur le code de `main` tel que l'a laissé la Pull Request qui l'a conservé ici (#292), et versionné avec lui : aucune branche n'est gardée pour lui. Sur un code qui a évolué depuis, il peut demander une adaptation ; `git apply --check` le dit avant de rien modifier.

```bash
git apply poc/empreinte-de-rendu/empreinte-de-rendu.patch
```

```powershell
./scripts/Test-Captures.ps1
```

Le premier contrôle prend et compare chaque capture, puis écrit son empreinte à côté de sa référence ; les empreintes sont à committer avec le code. Le choix est alors à documenter de nouveau dans `.speckit/choix-implementation/socle/themes.md`.
