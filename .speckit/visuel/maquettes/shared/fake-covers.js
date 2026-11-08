// Fictitious album covers drawn in CSS, for mockups whose theme puts the covers forward: a flat
// illustration (background layers), the series at the top and the title at the bottom. They only
// evoke an album by its colours and are never a copy of its real cover.
//
// A page renders a cover with this markup, `c` being one of FakeCovers.all:
//   <span class="cv" style="background: {{c.bg}}">
//     <span class="cv-art" style="background: {{c.art}}"></span>
//     <span class="cv-series" style="color: {{c.ink}}">{{c.series}}</span>
//     <span class="cv-title" style="color: {{c.titleInk}}">{{c.title}}</span>
//   </span>
// The title has its own ink, the bottom of a cover often being darker than its top. The text is sized on the cover's own width (container query units), whatever its size on the page.
(function () {
  'use strict';

  const STYLE = `
.cv{position:relative;display:block;aspect-ratio:3/4;overflow:hidden;container-type:inline-size;border-radius:4px}
.cv-art{position:absolute;inset:0}
.cv-series,.cv-title{position:absolute;left:8%;right:8%;font-family:system-ui,sans-serif;line-height:1.05;text-wrap:balance}
.cv-series{top:6%;font-size:7.5cqi;font-weight:800;letter-spacing:.06em;text-transform:uppercase}
.cv-title{bottom:6%;font-size:12cqi;font-weight:800;letter-spacing:-.01em}`;

  const covers = {
    lotus: {
      series: 'Les Aventures de Tintin', title: 'Le Lotus bleu', ink: '#FFFFFF', bg: '#2F5BA8', dominant: '#2F5BA8', deep: '#14295A', accent: '#D8342B',
      art: 'radial-gradient(circle at 68% 42%, #D8342B 0 21%, transparent 21.5%), linear-gradient(162deg, transparent 0 58%, #14295A 58% 100%), radial-gradient(circle at 20% 80%, #F2C230 0 6%, transparent 6.5%)'
    },
    blacksad: {
      series: 'Blacksad', title: 'Alors, tout tombe', ink: '#F4E7C8', bg: '#1F2633', dominant: '#3E4A5C', deep: '#12161F', accent: '#E9B63B',
      art: 'linear-gradient(90deg, transparent 0 58%, #12161F 58% 100%), linear-gradient(0deg, #12161F 0 30%, transparent 30%), linear-gradient(transparent 0 26%, #E9B63B 26% 46%, transparent 46%) 22% 0 / 18% 100% no-repeat, linear-gradient(#3E4A5C, #1F2633)'
    },
    iris: {
      series: 'Astérix', title: "L'Iris blanc", ink: '#1E4F2A', bg: '#F2C230', dominant: '#F2C230', deep: '#8A6A0E', accent: '#2E8B4A',
      art: 'radial-gradient(circle at 50% 46%, #FFFFFF 0 15%, transparent 15.5%), radial-gradient(circle at 50% 46%, #E9EEF7 0 24%, transparent 24.5%), linear-gradient(0deg, #2E8B4A 0 22%, transparent 22%)'
    },
    indes: {
      series: 'Ayroles · Guarnido', title: 'Les Indes fourbes', ink: '#3A1A10', titleInk: '#FCE1A8', bg: '#F2A25C', dominant: '#C9643A', deep: '#5B2A1C', accent: '#2C6E8A',
      art: 'radial-gradient(circle at 70% 40%, #FCE1A8 0 12%, transparent 12.5%), linear-gradient(180deg, #F2A25C 0 52%, #C9643A 52% 74%, #5B2A1C 74%)'
    },
    chateau: {
      series: 'Le Château des étoiles', title: 'Intégrale 1 à 2', ink: '#F4EBD6', bg: '#1C3F5C', dominant: '#2C6E8A', deep: '#0E2233', accent: '#E9C46A',
      art: 'radial-gradient(circle at 24% 26%, #FFFFFF 0 1.2%, transparent 1.6%), radial-gradient(circle at 78% 18%, #FFFFFF 0 1%, transparent 1.4%), radial-gradient(circle at 60% 34%, #FFFFFF 0 .9%, transparent 1.3%), radial-gradient(circle at 72% 44%, #F4EBD6 0 16%, transparent 16.5%), linear-gradient(0deg, #0E2233 0 26%, transparent 26%), linear-gradient(#1C3F5C, #2C6E8A)'
    },
    undertaker: {
      series: 'Undertaker', title: 'Mister Prairie', ink: '#2A1A10', titleInk: '#FBE3B0', bg: '#E8B26A', dominant: '#8A5A3C', deep: '#4A2E1C', accent: '#B23A2A',
      art: 'radial-gradient(ellipse 9% 20% at 50% 58%, #2A1A10 0 98%, transparent 100%), radial-gradient(circle at 50% 30%, #FBE3B0 0 14%, transparent 14.5%), linear-gradient(180deg, #E8B26A 0 62%, #8A5A3C 62%)'
    },
    spirou: {
      series: 'Spirou et Fantasio', title: 'Hors-série 2', ink: '#FFFFFF', bg: '#D9473B', dominant: '#D9473B', deep: '#7A1F18', accent: '#F6C343',
      art: 'radial-gradient(circle at 52% 48%, transparent 0 18%, #FFFFFF 18.5% 23%, transparent 23.5%), radial-gradient(circle at 52% 48%, #F6C343 0 10%, transparent 10.5%)'
    }
  };

  const style = document.createElement('style');
  style.textContent = STYLE;
  document.head.append(style);

  for (const cover of Object.values(covers)) cover.titleInk ??= cover.ink;

  window.FakeCovers = { all: covers };
})();
