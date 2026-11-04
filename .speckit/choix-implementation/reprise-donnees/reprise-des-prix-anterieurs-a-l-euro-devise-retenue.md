---
type: Choix d'implémentation
title: 'Reprise des prix antérieurs à l''euro : devise retenue'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Reprise des prix antérieurs à l'euro : devise retenue

Mise en œuvre déterministe de la règle de [Correspondances particulières](/contraintes-techniques/reprise-donnees/reprise-des-donnees.md#correspondances-particulières) (prix d'acquisition antérieurs à l'euro), pour qu'un même prix soit toujours repris dans la même devise :

- **Devise certaine** : date d'achat connue hors de la période de double circulation du franc et de l'euro (1er janvier au 16 février 2002, le franc n'ayant plus cours à partir du 17 février, Banque de France) : euro à partir du 17 février 2002, franc avant 2002, ancien franc avant 1960 ; ou, sans date d'achat, année de référence du prix (année d'édition, à défaut de première publication) postérieure ou égale à 2003, l'achat ne pouvant précéder la parution (euro).
- **Devise incertaine** : date d'achat dans la période de double circulation ; ou, sans date d'achat, achat antérieur à 2004 ([Base existante](/contraintes-techniques/reprise-donnees/reprise-des-donnees.md#base-existante)) d'une édition dont l'année de référence est antérieure ou égale à 2002, l'achat ayant pu avoir lieu avant le 17 février 2002. Pour une année de référence antérieure à 1960, le prix est toujours soumis à l'arbitrage de l'utilisateur (doute persistant ci-dessous) : l'achat a pu être payé en anciens francs, en francs ou en euros, et un multiple de 0,50 ancien franc (moins d'un dixième de centime d'euro) rend tout montant vraisemblable, si bien qu'aucune comparaison ne départage les devises. Sinon, le franc est comparé à l'euro ; un montant est **vraisemblable comme prix affiché** :
  - en euros, si ses centimes sont un multiple de 5 ou se terminent par 9 ;
  - en francs, s'il existe un prix multiple de 0,50 franc dont la conversion en euros, arrondie au centime, donne le montant stocké ; ce prix est alors le montant repris.
- Une seule des deux devises vraisemblable : elle est retenue. Les deux, ou aucune : **doute persistant**, le prix est soumis à l'arbitrage de l'utilisateur, sans être repris d'office.
  - Mesuré sur la base existante : 64 prix de devise incertaine, dont 16 classés en francs, 43 en euros et 5 laissés à l'arbitrage (dont l'édition datée de l'an 200, année à corriger).
