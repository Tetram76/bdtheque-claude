---
type: Choix d'implémentation
title: 'Reprise des prix antérieurs à l''euro : devise retenue'
description: 'Règle déterministe de la devise d''un prix antérieur à l''euro ; à lire pour la reprise des données.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Reprise des prix antérieurs à l'euro : devise retenue

La devise d'un prix d'acquisition antérieur à l'euro ([Correspondances particulières](../../contraintes-techniques/reprise-donnees/reprise-des-donnees.md#correspondances-particulières)) suit une **règle déterministe**, pour qu'un même prix soit toujours repris dans la même devise :

- **Devise certaine**, d'après la date d'achat hors de la double circulation du franc et de l'euro (1er janvier au 16 février 2002, Banque de France) : euro à partir du 17 février 2002, franc avant 2002, ancien franc avant 1960. Sans date d'achat, une année de référence du prix (édition, à défaut première publication) postérieure ou égale à 2003 donne l'euro, l'achat ne pouvant précéder la parution.
- **Devise incertaine** (date d'achat dans la double circulation ; ou, sans date, achat antérieur à 2004, [Base existante](../../contraintes-techniques/reprise-donnees/reprise-des-donnees.md#base-existante), d'une édition de 2002 au plus tard) : la devise retenue est celle dont le montant est **vraisemblable comme prix affiché** :
  - en euros, des centimes multiples de 5 ou terminés par 9 ;
  - en francs, un prix multiple de 0,50 franc dont la conversion, arrondie au centime, donne le montant stocké, ce prix étant alors repris.
- Les deux devises vraisemblables, ou aucune, et toute année de référence antérieure à 1960 : **doute persistant**, le prix est soumis à l'arbitrage de l'utilisateur au lieu d'être repris d'office. Avant 1960, anciens francs, francs ou euros sont possibles, et un multiple de 0,50 ancien franc (moins d'un dixième de centime) rend tout montant vraisemblable : aucune comparaison ne départage les devises.

Mesuré sur la base existante : 64 prix de devise incertaine, dont 16 classés en francs, 43 en euros et 5 laissés à l'arbitrage (dont l'édition datée de l'an 200, année à corriger).
