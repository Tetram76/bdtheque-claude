// Tokens of « La couverture du jour » for the screens other than the dashboard, whose mockups share
// one table of themes. A screen derives the theme from its festival, the closest theme by its rounded
// forms, and these tokens replace everything that makes the festival's own identity:
//   THEMES.couverture = { ...THEMES.festival, ...COUVERTURE_THEME };
// The colours are those of the day's featured cover, « Le Lotus bleu » in the mockups: its deep blue
// for the header and the accents, its dominant blue for links, its red for the highlights. Text set on
// a coloured fill keeps a 4.5:1 contrast.
(function () {
  'use strict';

  const DEEP = '#14295A';
  const DOMINANT = '#2F5BA8';
  const ACCENT = '#D8342B';
  const TINT = '#DCE6F6';
  const FONT = "'Inter Tight', system-ui, sans-serif";
  const SHADOW = '0 1px 2px rgba(20,41,90,.08), 0 18px 36px -22px rgba(20,41,90,.45)';

  window.COUVERTURE_THEME = {
    bg: '#ECEFF5', text: '#15151A', muted: '#5A5F6B', line: '#D5DAE4',
    acc: DEEP, onAcc: '#FFFFFF', link: DOMINANT, colBg: ACCENT, colFg: '#FFFFFF',
    warn: '#F29E4C', onWarn: '#15151A', infoBg: DOMINANT, infoFg: '#FFFFFF', nBg: TINT, nFg: DEEP,
    fd: FONT, fdw: '900', fb: FONT,
    cBg: '#FFFFFF', cB: '0', cBT: '0', cR: '24px', cSh: SHADOW, hiSh: SHADOW,
    tBg: '#F3F5F9', tB: '1px solid #DDE2EB', tR: '16px', rowB: '1px solid #E4E8EF',
    // The rounding of the fields stays the screen's own (inR): a pill where every field holds one line, smaller
    // corners on the forms, whose text areas a pill would crop.
    inBg: '#F3F5F9', inB: '1px solid #CDD3DF', btnR: '999px', secB: '1px solid ' + DEEP, secText: DEEP,
    // The header is the touch of the theme: the deep colour of the day's cover, as on the dashboard.
    hBg: DEEP, hText: '#FFFFFF', hLine: '0', hInBg: 'rgba(255,255,255,.14)', hInB: '1px solid rgba(255,255,255,.22)', hInText: '#FFFFFF',
    helpBg: ACCENT, helpText: '#FFFFFF', helpR: '50%', helpB: '0',
    lgMark: false, lgText: '#FFFFFF', lgFont: "900 21px 'Inter Tight', sans-serif", lgTT: 'none', lgDot: ACCENT,
    tagBg: 'rgba(255,255,255,.14)', tagText: '#FFFFFF', tagB: '0', tagR: '999px',
    h2c: '#15151A', h2size: '21px',
    // No station, no mention above the titles: those belong to the festival.
    mkDisp: 'none', kDisp: 'none', kPre: null,
    // The visual of a record is an album in volume, a little turned, as on the dashboard.
    cvPad: '0', cvB: '0', cvR: '8px', cvBg: 'transparent', cvRot: '0deg', cvTf: 'perspective(1100px) rotateY(16deg)',
    cvSh: '0 34px 44px -24px rgba(0,0,0,.6)', imgR: '6px',
    pal: [[DEEP, '#FFFFFF'], [ACCENT, '#FFFFFF'], [TINT, DEEP], ['#237A3E', '#FFFFFF'], ['#8E5BB8', '#FFFFFF'], ['#1F6F94', '#FFFFFF'], ['#E07A2C', '#15151A'], ['#E6E3DD', '#15151A']]
  };
})();
