// Frames of a comic page drawn by hand: each element marked [data-frame] loses its CSS border and
// background, which an SVG drawn behind its content replaces. The ancestor's data-frame-mode
// chooses the drawing:
//   - "ruler": nothing is drawn, the CSS border stays;
//   - "hand": an inked line, slightly wavering and of uneven thickness, on a frame whose corners
//     are a little off square, the lines overshooting some corners;
//   - "sketch": the same, over a thin pencil line slightly off the inked one.
// The drawing of each frame is chosen once per page load and kept when the page re-renders.
(() => {
  const SVG = 'http://www.w3.org/2000/svg';
  const INK = '#111';
  const PENCIL = '#5a5a5a';
  const seeds = [];

  function random(seed) {
    let s = seed % 2147483647 || 1;
    return () => { s = (s * 16807) % 2147483647; return s / 2147483647; };
  }

  // A stroke from a to b, as a filled outline: it wavers by up to `waver` px across its course,
  // and its width varies around `width`, thinner at both ends, as a brush or nib line does.
  function stroke(a, b, width, waver, rand) {
    const [ax, ay] = a;
    const [bx, by] = b;
    const length = Math.hypot(bx - ax, by - ay) || 1;
    const ux = (bx - ax) / length;
    const uy = (by - ay) / length;
    const nx = -uy;
    const ny = ux;
    const phase = [rand() * 6.3, rand() * 6.3, rand() * 6.3];
    const waves = 1 + rand() * 2;
    const steps = Math.max(4, Math.ceil(length / 8));
    const left = [];
    const right = [];
    for (let i = 0; i <= steps; i++) {
      const t = i / steps;
      const offset = waver * (0.6 * Math.sin(2 * Math.PI * waves * t + phase[0]) + 0.4 * Math.sin(2 * Math.PI * 2.7 * waves * t + phase[1]));
      const taper = 0.55 + 0.45 * Math.sin(Math.PI * Math.min(1, Math.max(0, t * 1.15 - 0.075)));
      const half = (width / 2) * taper * (1 + 0.18 * Math.sin(2 * Math.PI * 1.3 * t + phase[2]));
      const x = ax + ux * length * t + nx * offset;
      const y = ay + uy * length * t + ny * offset;
      left.push([x + nx * half, y + ny * half]);
      right.push([x - nx * half, y - ny * half]);
    }
    const points = [...left, ...right.reverse()];
    return 'M' + points.map(([x, y]) => `${x.toFixed(1)} ${y.toFixed(1)}`).join('L') + 'Z';
  }

  function node(tag, attributes) {
    const element = document.createElementNS(SVG, tag);
    for (const [name, value] of Object.entries(attributes)) element.setAttribute(name, value);
    return element;
  }

  function drawFrame(frame, seed, mode) {
    const w = frame.offsetWidth;
    const h = frame.offsetHeight;
    const key = `${mode} ${w}x${h}`;
    const existing = frame.querySelector(':scope > svg[data-frame-art]');
    if (existing && existing.dataset.key === key) return;
    const rand = random(seed);
    // The fill takes over the background, so that it follows the corners off square.
    const fill = frame.dataset.fill ?? getComputedStyle(frame).backgroundColor;
    frame.dataset.fill = fill;
    frame.style.background = 'transparent';
    frame.style.borderColor = 'transparent';
    const inset = 1.5;
    const corner = (x, y) => [x + (rand() - 0.5) * 6, y + (rand() - 0.5) * 6];
    const corners = [corner(inset, inset), corner(w - inset, inset), corner(w - inset, h - inset), corner(inset, h - inset)];
    const svg = node('svg', { width: w, height: h, 'aria-hidden': 'true' });
    svg.dataset.frameArt = '';
    svg.dataset.decoration = '';
    svg.dataset.key = key;
    // The SVG covers the border box: the frame keeps its (transparent) border, so that its
    // layout does not change, and an absolute position starts inside it.
    const border = getComputedStyle(frame).borderLeftWidth;
    svg.style.cssText = `position:absolute;left:-${border};top:-${border};overflow:visible;z-index:-1;pointer-events:none`;
    svg.append(node('path', { d: 'M' + corners.map(([x, y]) => `${x.toFixed(1)} ${y.toFixed(1)}`).join('L') + 'Z', fill }));
    const edges = corners.map((c, i) => [c, corners[(i + 1) % 4]]);
    // A line overshoots a corner now and then, as when the inker does not stop exactly at it.
    const extend = ([a, b]) => {
      const length = Math.hypot(b[0] - a[0], b[1] - a[1]) || 1;
      const u = [(b[0] - a[0]) / length, (b[1] - a[1]) / length];
      const before = rand() < 0.35 ? 2 + rand() * 5 : 0;
      const after = rand() < 0.35 ? 2 + rand() * 5 : 0;
      return [[a[0] - u[0] * before, a[1] - u[1] * before], [b[0] + u[0] * after, b[1] + u[1] * after]];
    };
    if (mode === 'sketch') {
      for (const edge of edges) {
        const [a, b] = extend(edge);
        const shift = () => [(rand() - 0.5) * 5, (rand() - 0.5) * 5];
        const [sa, sb] = [shift(), shift()];
        svg.append(node('path', { d: stroke([a[0] + sa[0], a[1] + sa[1]], [b[0] + sb[0], b[1] + sb[1]], 1.1, 1.4, rand), fill: PENCIL }));
      }
    }
    for (const edge of edges) {
      const [a, b] = extend(edge);
      svg.append(node('path', { d: stroke(a, b, 3.4, 0.9, rand), fill: INK }));
    }
    if (existing) existing.replaceWith(svg);
    else frame.prepend(svg);
  }

  function apply() {
    const frames = [...document.querySelectorAll('[data-frame]')];
    frames.forEach((frame, i) => {
      const mode = frame.closest('[data-frame-mode]')?.dataset.frameMode ?? 'ruler';
      if (mode === 'ruler') return;
      seeds[i] ??= 1 + Math.floor(Math.random() * 2147483646);
      frame.style.isolation = 'isolate';
      drawFrame(frame, seeds[i], mode);
      resizes.observe(frame);
    });
  }

  const resizes = new ResizeObserver(() => apply());
  new MutationObserver((records) => {
    if (records.some((r) => [...r.addedNodes].some((n) => !(n instanceof SVGElement)))) apply();
  }).observe(document.documentElement, { childList: true, subtree: true });
  document.addEventListener('DOMContentLoaded', apply);
  document.fonts?.ready.then(apply);
})();
