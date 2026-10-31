// Balloons of a comic page: each element marked [data-bubble] gets, behind its text, the outline of
// a balloon drawn in SVG, whose shape is chosen at random once per page load:
//   - smooth (speech), most of them;
//   - cloud (thought), only on a question: on average 15 % of the balloons, never more than
//     a fifth of a page (one at least allowed);
//   - star (noise, an exclamation), only on an exclamation: one balloon in twenty over many
//     displays, never more than one per page.
// data-bubble gives the kind of sentence (question, exclamation, statement) that decides which
// shapes a balloon may take. An ancestor with data-bubble-mode="demo" forces one cloud and one
// star, to review every shape at once. The shapes, the position of the tails and their leaning
// are kept when the page re-renders, so that nothing moves while the page is being used.
(() => {
  const STROKE = 2.5;
  const SVG = 'http://www.w3.org/2000/svg';
  let plan = null;

  function random(seed) {
    let s = seed % 2147483647 || 1;
    return () => { s = (s * 16807) % 2147483647; return s / 2147483647; };
  }

  function assignShapes(kinds, demo) {
    const n = kinds.length;
    const shapes = kinds.map(() => 'smooth');
    const pick = (kind) => {
      const free = kinds.map((k, i) => (k === kind && shapes[i] === 'smooth' ? i : -1)).filter((i) => i >= 0);
      return free.length ? free[Math.floor(Math.random() * free.length)] : -1;
    };
    if (demo) {
      const cloud = pick('question');
      if (cloud >= 0) shapes[cloud] = 'cloud';
      const star = pick('exclamation');
      if (star >= 0) shapes[star] = 'star';
      return shapes;
    }
    // A star on a page of n balloons with a probability of 5 % × n: one balloon in twenty on average.
    if (Math.random() < Math.min(1, 0.05 * n)) {
      const star = pick('exclamation');
      if (star >= 0) shapes[star] = 'star';
    }
    const questions = kinds.filter((k) => k === 'question').length;
    const chance = questions ? Math.min(1, (0.15 * n) / questions) : 0;
    const maxClouds = Math.max(1, Math.floor(0.2 * n));
    let clouds = 0;
    kinds.forEach((kind, i) => {
      if (kind === 'question' && shapes[i] === 'smooth' && clouds < maxClouds && Math.random() < chance) {
        shapes[i] = 'cloud';
        clouds++;
      }
    });
    return shapes;
  }

  // A squircle drawn on the balloon's box leaves room for the text in its padding, and looks more
  // like a hand-lettered balloon than an ellipse, which would cut the corners of a long sentence.
  function outline(w, h, scale, wobble, steps = 96) {
    const points = [];
    for (let i = 0; i < steps; i++) {
      const t = (i / steps) * 2 * Math.PI;
      const c = Math.cos(t);
      const s = Math.sin(t);
      const r = scale * (1 + wobble(t));
      points.push([
        w / 2 + (w / 2) * r * Math.sign(c) * Math.sqrt(Math.abs(c)),
        h / 2 + (h / 2) * r * Math.sign(s) * Math.sqrt(Math.abs(s))
      ]);
    }
    return points;
  }

  const pathOf = (points) => 'M' + points.map(([x, y]) => `${x.toFixed(1)} ${y.toFixed(1)}`).join('L') + 'Z';

  function shapesFor(kind, w, h, draw) {
    const rand = random(draw.seed);
    const phase = [rand() * 6.3, rand() * 6.3];
    const wobble = (t) => 0.018 * Math.sin(2 * t + phase[0]) + 0.012 * Math.sin(3 * t + phase[1]);
    const x0 = w * draw.at;
    const dir = draw.flip ? -1 : 1;
    if (kind === 'cloud') {
      const r = Math.max(9, Math.min(20, h * 0.3));
      const ring = outline(w * 0.9, h * 0.84, 1, () => 0, 720).map(([x, y]) => [x + w * 0.05, y + h * 0.08]);
      // Bumps evenly spaced all round the ring, about 1.3 radius apart: placed every 1.3 radius
      // from a starting point, the last one fell too close to the first and notched the outline.
      // Each one is interpolated between the ring's samples: sampled at even angles, a squircle
      // leaves steps of some twenty pixels in the middle of its top and bottom edges, where bumps
      // landing on samples were spaced unevenly.
      const along = [0];
      for (let i = 1; i <= ring.length; i++) {
        const [x, y] = ring[i % ring.length];
        const [px, py] = ring[i - 1];
        along.push(along[i - 1] + Math.hypot(x - px, y - py));
      }
      const count = Math.max(6, Math.round(along[ring.length] / (r * 1.3)));
      const bumps = [];
      let k = 0;
      for (let i = 0; i < count; i++) {
        const target = (i / count) * along[ring.length];
        while (k < ring.length - 1 && along[k + 1] < target) k++;
        const [x, y] = ring[k];
        const [nx, ny] = ring[(k + 1) % ring.length];
        const f = (target - along[k]) / (along[k + 1] - along[k] || 1);
        bumps.push({ cx: x + (nx - x) * f, cy: y + (ny - y) * f, r: r * (0.9 + rand() * 0.2) });
      }
      const bottom = h * 0.92 + r;
      return [
        { d: pathOf(outline(w * 0.9, h * 0.84, 1, () => 0).map(([x, y]) => [x + w * 0.05, y + h * 0.08])) },
        ...bumps,
        { cx: x0 + dir * 4, cy: bottom + 8, r: 6 },
        { cx: x0 + dir * 13, cy: bottom + 19, r: 4 },
        { cx: x0 + dir * 20, cy: bottom + 27, r: 2.6 }
      ];
    }
    if (kind === 'star') {
      // A drawn burst, as in printed comics: an irregular silhouette, spikes of very unequal
      // lengths (many short ones, a few long ones), whose sides curve inwards. Regular geometric
      // spikes on an ellipse or a stadium read as a notched box, and their ends as flags.
      // The silhouette is an ellipse wide enough to keep the text inside (its balloon has more
      // padding above and below than a speech balloon), its radius gently waved at random.
      const waves = [rand() * 6.3, rand() * 6.3];
      const radius = (t) => 1 + 0.05 * Math.sin(2 * t + waves[0]) + 0.035 * Math.sin(3 * t + waves[1]);
      const base = [];
      for (let i = 0; i < 720; i++) {
        const t = (i / 720) * 2 * Math.PI;
        const r = radius(t);
        base.push([w / 2 + (w / 2) * 1.08 * r * Math.cos(t), h / 2 + (h / 2) * 1.25 * r * Math.sin(t)]);
      }
      const along = [0];
      for (let i = 1; i <= base.length; i++) {
        const [x, y] = base[i % base.length];
        const [px, py] = base[i - 1];
        along.push(along[i - 1] + Math.hypot(x - px, y - py));
      }
      const perimeter = along[base.length];
      // A point of the silhouette at a length along it, with the direction a spike takes there:
      // halfway between square to the outline and away from the centre (square only, the spikes at
      // both ends of a long balloon spread flat; radial only, those of its long edges lean sideways).
      // The outline runs clockwise on screen: the outward normal of the tangent (tx, ty) is (ty, -tx).
      const at = (length) => {
        const target = ((length % perimeter) + perimeter) % perimeter;
        let k = 0;
        while (k < base.length - 1 && along[k + 1] < target) k++;
        const [x, y] = base[k];
        const [ax, ay] = base[(k + base.length - 3) % base.length];
        const [bx, by] = base[(k + 3) % base.length];
        const tangent = Math.hypot(bx - ax, by - ay) || 1;
        const radial = Math.hypot(x - w / 2, y - h / 2) || 1;
        const mx = (by - ay) / tangent + (x - w / 2) / radial;
        const my = -(bx - ax) / tangent + (y - h / 2) / radial;
        const m = Math.hypot(mx, my) || 1;
        return { x, y, nx: mx / m, ny: my / m };
      };
      const count = Math.max(12, Math.round(perimeter / 42));
      const step = perimeter / count;
      const hollows = [];
      for (let i = 0; i < count; i++) {
        const p = at(i * step);
        hollows.push([p.x - p.nx * 3, p.y - p.ny * 3]);
      }
      // About one spike in four is long, never two side by side.
      let longBefore = false;
      let d = `M${hollows[0][0].toFixed(1)} ${hollows[0][1].toFixed(1)}`;
      for (let i = 0; i < count; i++) {
        const long = !longBefore && rand() < 0.3;
        longBefore = long;
        const reach = long ? 28 + rand() * 16 : 10 + rand() * 8;
        const b = at((i + 0.5 + (rand() - 0.5) * 0.3) * step);
        const tip = [b.x + b.nx * reach, b.y + b.ny * reach];
        const from = hollows[i];
        const to = hollows[(i + 1) % count];
        // Each side bends a little towards the axis of its spike: a slightly concave side, sharper
        // than a straight one. Bent further, the hollows round off and the burst turns into a
        // scalloped flower.
        const axis = [b.x + (tip[0] - b.x) * 0.5, b.y + (tip[1] - b.y) * 0.5];
        const bend = (h0) => {
          const mid = [(h0[0] + tip[0]) / 2, (h0[1] + tip[1]) / 2];
          return [mid[0] + (axis[0] - mid[0]) * 0.3, mid[1] + (axis[1] - mid[1]) * 0.3];
        };
        const [c1x, c1y] = bend(from);
        const [c2x, c2y] = bend(to);
        d += `Q${c1x.toFixed(1)} ${c1y.toFixed(1)} ${tip[0].toFixed(1)} ${tip[1].toFixed(1)}`;
        d += `Q${c2x.toFixed(1)} ${c2y.toFixed(1)} ${to[0].toFixed(1)} ${to[1].toFixed(1)}`;
      }
      return [
        { d: d + 'Z' },
        { d: `M${x0 - 13} ${h * 0.8}Q${x0 + dir * 2} ${h + 14} ${x0 + dir * 26} ${h + 42}Q${x0 + dir * 8} ${h + 12} ${x0 + 13} ${h * 0.8}Z` }
      ];
    }
    const tipX = x0 + dir * (12 + rand() * 10);
    const tipY = h + 16 + rand() * 6;
    return [
      { d: pathOf(outline(w, h, 1, wobble)) },
      { d: `M${x0 - 9} ${h * 0.6}Q${x0 - 3 + dir * 4} ${h + 4} ${tipX} ${tipY}Q${x0 + 5} ${h + 2} ${x0 + 9} ${h * 0.6}Z` }
    ];
  }

  function element(spec, fill, stroke) {
    const node = document.createElementNS(SVG, spec.d ? 'path' : 'circle');
    for (const [name, value] of Object.entries(spec)) node.setAttribute(name, value);
    node.setAttribute('fill', fill);
    if (stroke) {
      node.setAttribute('stroke', stroke);
      node.setAttribute('stroke-width', 2 * STROKE);
      node.setAttribute('stroke-linejoin', 'round');
    }
    return node;
  }

  // Every part is first drawn with a doubled black stroke, then filled in white on top: the
  // parts merge into a single balloon, and the tail joins its body without a seam.
  function drawBubble(bubble, draw) {
    const w = bubble.offsetWidth;
    const h = bubble.offsetHeight;
    const size = `${w}x${h}`;
    const existing = bubble.querySelector(':scope > svg[data-bubble-art]');
    if (existing && existing.dataset.size === size) return;
    const specs = shapesFor(draw.shape, w, h, draw);
    const svg = document.createElementNS(SVG, 'svg');
    svg.dataset.bubbleArt = '';
    svg.dataset.size = size;
    svg.setAttribute('aria-hidden', 'true');
    svg.setAttribute('width', w);
    svg.setAttribute('height', h);
    svg.style.cssText = 'position:absolute;left:0;top:0;overflow:visible;z-index:-1;pointer-events:none';
    for (const spec of specs) svg.append(element(spec, '#111', '#111'));
    for (const spec of specs) svg.append(element(spec, '#fff'));
    bubble.dataset.shape = draw.shape;
    if (existing) existing.replaceWith(svg);
    else bubble.prepend(svg);
  }

  function apply() {
    const bubbles = [...document.querySelectorAll('[data-bubble]')];
    if (!bubbles.length) return;
    const kinds = bubbles.map((b) => b.dataset.bubble);
    if (!plan || plan.kinds !== kinds.join()) {
      const demo = !!document.querySelector('[data-bubble-mode="demo"]');
      const shapes = assignShapes(kinds, demo);
      plan = shapes.map((shape) => ({
        shape,
        at: 0.15 + Math.random() * 0.5,
        flip: Math.random() < 0.5,
        seed: 1 + Math.floor(Math.random() * 2147483646)
      }));
      plan.kinds = kinds.join();
    }
    bubbles.forEach((bubble, i) => {
      bubble.style.position = 'relative';
      bubble.style.isolation = 'isolate';
      bubble.dataset.shape = plan[i].shape;
      drawBubble(bubble, plan[i]);
      resizes.observe(bubble);
    });
  }

  const resizes = new ResizeObserver(() => apply());
  new MutationObserver((records) => {
    if (records.some((r) => [...r.addedNodes].some((n) => !(n instanceof SVGElement)))) apply();
  }).observe(document.documentElement, { childList: true, subtree: true });
  document.addEventListener('DOMContentLoaded', apply);
  document.fonts?.ready.then(apply);
  window.ComicBubbles = { shapes: () => plan?.map((p) => p.shape) };
})();
