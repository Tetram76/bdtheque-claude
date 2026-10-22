// The tails of the speech bubbles of the "Cases et bulles" theme vary, as in a hand-lettered
// comic: each tail ([data-tail]) gets its own position along its bubble and its own leaning.
// Both are drawn once per page load and kept when the page re-renders, so that a tail does not
// jump around while the page is being used.
(() => {
  const draws = [];
  const draw = (i) => (draws[i] ??= { left: 10 + Math.random() * 55, flip: Math.random() < 0.5 });
  const apply = () => document.querySelectorAll('[data-tail]').forEach((el, i) => {
    const { left, flip } = draw(i);
    el.dataset.base ??= el.style.transform || '';
    el.style.left = left + '%';
    el.style.transform = (el.dataset.base + (flip ? ' scaleX(-1)' : '')).trim() || 'none';
  });
  new MutationObserver(apply).observe(document.documentElement, { childList: true, subtree: true });
  document.addEventListener('DOMContentLoaded', apply);
})();
