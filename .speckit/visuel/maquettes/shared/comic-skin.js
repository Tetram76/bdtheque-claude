// « La planche encrée » on a screen mockup whose blocks are <section> elements titled by an <h2>: while the
// mockup shows this theme (its `theme` setting, « planche » by default), each block becomes a case of
// its own colour, whose edges comic-frames.js draws by hand, and its title a balloon that
// comic-bubbles.js draws, straddling the top edge of the case. Load it before those two scripts:
// its observer then marks each new page before they draw it.
(() => {
  const theme = new URLSearchParams(window.location.search).get('theme') ?? 'planche';
  if (theme !== 'planche') return;

  // Seven hues, most cases in their four-colour process version, about one in four in their softer
  // "ligne claire" version, dealt anew at each display: neighbouring cases never share a hue.
  const QUADRI = ['#FFC93C', '#4FB0E6', '#F2775A', '#63C36A', '#A47AE0', '#F483AE', '#33C2B1'];
  const SOFT = ['#F4D35E', '#8EC5E8', '#F2A07B', '#9FD39A', '#C9A7E3', '#F6B8C8', '#7FD1C7'];
  const order = [0, 1, 2, 3, 4, 5, 6].sort(() => Math.random() - 0.5);
  const softOne = Math.floor(Math.random() * 4);
  const colourOf = (n) => (n % 4 === softOne ? SOFT : QUADRI)[order[n % 7]];

  const style = document.createElement('style');
  style.textContent = `
[data-bubble]{padding:12px 24px !important;margin-top:-36px !important;margin-bottom:22px !important;border:0 !important;background:transparent !important;color:#111 !important;font-family:'Patrick Hand',cursive !important;font-weight:400 !important;font-style:normal !important;font-size:24px !important;line-height:1.12 !important;text-transform:none !important;text-shadow:none !important;transform:none !important}
[data-bubble][data-shape=cloud]{padding:20px 36px 18px !important;margin-top:-24px !important;margin-bottom:42px !important}
[data-bubble][data-shape=star]{padding:30px 44px !important;margin:-18px calc(5% + 16px) 42px !important}`;
  document.head.append(style);

  const kindOf = (text) => (/\?\s*$/.test(text) ? 'question' : /!\s*$/.test(text) ? 'exclamation' : 'statement');

  function skin() {
    document.body.dataset.bubbleMode = 'random';
    document.body.dataset.frameMode = 'sketch';
    document.querySelectorAll('section').forEach((section, n) => {
      if (section.dataset.frame !== undefined) return;
      section.dataset.frame = '';
      section.style.background = colourOf(n);
      // A scrolling row fades into what lies behind it: now the colour of the case, no longer the block's own.
      section.querySelectorAll('[data-scroll-hint]').forEach((hint) => hint.style.setProperty('--hint-bg', colourOf(n)));
      // A title kept for screen readers only (one pixel wide, clipped) stays hidden: no balloon.
      const title = section.querySelector('h2');
      if (title && title.offsetWidth > 1) title.dataset.bubble = kindOf(title.textContent.trim());
    });
  }

  new MutationObserver(skin).observe(document.documentElement, { childList: true, subtree: true });
  document.addEventListener('DOMContentLoaded', skin);
})();
