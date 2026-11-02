// « La planche encrée » (visuel/dashboard.md § La planche encrée): each block of the page (.block anchor of the base
// theme) becomes a case of its own tint, whose edges are drawn by hand, and its title (.block-title) a balloon straddling
// its top edge. Tints, edges, shapes of the balloons and their tails are drawn at random at each display of a page, then
// kept as long as it is displayed: a block redrawn at a new size keeps its drawing, only stretched to it.
// Nothing is inserted into the page, whose nodes belong to the renderer of Blazor: a drawing is an SVG image set as a
// custom property of its element (--block-art, --bubble-art), which themes/planche.css paints in a pseudo-element.
(() => {
    const SVG = "http://www.w3.org/2000/svg";
    // Room around a drawing for what goes past its element: a line past a corner, a tail, the spikes of a noise.
    const FRAME_MARGIN = 10;
    const BUBBLE_MARGIN = 48;
    const BUBBLE_STROKE = 2.5;
    const PENCIL = "#5A5A5A";
    const HUES = 7;

    // Park-Miller: a drawing is the same for the same seed, whatever the size it is drawn at.
    function seeded(seed) {
        let s = seed % 2147483647 || 1;
        return () => {
            s = (s * 16807) % 2147483647;
            return s / 2147483647;
        };
    }

    const newSeed = () => 1 + Math.floor(Math.random() * 2147483646);

    // The deal of a display: an order of the seven hues, so that blocks that follow each other, side by side as one above
    // the other, never share a hue, and the place in four of the blocks that take the softer version of their hue.
    let deal;
    let balloons;

    function newDisplay() {
        const order = [...Array(HUES).keys()];
        for (let i = order.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [order[i], order[j]] = [order[j], order[i]];
        }
        deal = { order, soft: Math.floor(Math.random() * 4) };
        balloons = { clouds: 0, stars: 0, maxClouds: 0, starChance: 0, cloudChance: 0, planned: false };
    }

    // Tints 1 to 7: the four-color process version of a hue; 8 to 14: its softer « ligne claire » version.
    const tintOf = (n) => (n % 4 === deal.soft ? HUES + 1 : 1) + deal.order[n % HUES];

    const kindOf = (text) => (/\?\s*$/.test(text) ? "question" : /!\s*$/.test(text) ? "exclamation" : "statement");

    // A smooth balloon (speech) for most titles; a cloud (thought) only on a question, a fifth of the balloons of a page
    // at most; a star (noise) only on an exclamation, one balloon in twenty over many displays, one per page at most.
    function planBalloons(titles) {
        const kinds = titles.map((title) => kindOf(title.textContent.trim()));
        const n = kinds.length;
        const questions = kinds.filter((kind) => kind === "question").length;
        balloons.maxClouds = Math.max(1, Math.floor(0.2 * n));
        balloons.cloudChance = questions ? Math.min(1, (0.15 * n) / questions) : 0;
        balloons.starChance = Math.min(1, 0.05 * n);
        const exclamations = kinds.map((kind, i) => (kind === "exclamation" ? i : -1)).filter((i) => i >= 0);
        balloons.star = Math.random() < balloons.starChance && exclamations.length
            ? exclamations[Math.floor(Math.random() * exclamations.length)]
            : -1;
        balloons.planned = true;
        return kinds;
    }

    function shapeFor(kind, index) {
        if (kind === "exclamation" && index === balloons.star && balloons.stars === 0) {
            balloons.stars++;
            return "star";
        }
        if (kind === "question" && balloons.clouds < balloons.maxClouds && Math.random() < balloons.cloudChance) {
            balloons.clouds++;
            return "cloud";
        }
        return "smooth";
    }

    const point = ([x, y]) => `${x.toFixed(1)} ${y.toFixed(1)}`;
    const closedPath = (points) => "M" + points.map(point).join("L") + "Z";

    function svgImage(width, height, body) {
        const svg = `<svg xmlns="${SVG}" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">${body}</svg>`;
        return `url("data:image/svg+xml,${encodeURIComponent(svg)}")`;
    }

    // A stroke from a to b, as a filled outline: it wavers across its course, and its width varies around `width`,
    // thinner at both ends, as a brush or nib line does.
    function stroke(a, b, width, waver, rand) {
        const [ax, ay] = a;
        const [bx, by] = b;
        const length = Math.hypot(bx - ax, by - ay) || 1;
        const ux = (bx - ax) / length;
        const uy = (by - ay) / length;
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
            const x = ax + ux * length * t - uy * offset;
            const y = ay + uy * length * t + ux * offset;
            left.push([x - uy * half, y + ux * half]);
            right.push([x + uy * half, y - ux * half]);
        }
        return closedPath([...left, ...right.reverse()]);
    }

    // The edges of a case: a frame whose corners are a little off square, filled with the tint of the case, under an
    // inked line that overshoots some corners, over a thin pencil line slightly off the ink.
    function frameArt(width, height, fill, ink, seed) {
        const rand = seeded(seed);
        const m = FRAME_MARGIN;
        const corner = (x, y) => [m + x + (rand() - 0.5) * 6, m + y + (rand() - 0.5) * 6];
        const inset = 1.5;
        const corners = [corner(inset, inset), corner(width - inset, inset), corner(width - inset, height - inset), corner(inset, height - inset)];
        const edges = corners.map((c, i) => [c, corners[(i + 1) % 4]]);
        const overshoot = ([a, b]) => {
            const length = Math.hypot(b[0] - a[0], b[1] - a[1]) || 1;
            const u = [(b[0] - a[0]) / length, (b[1] - a[1]) / length];
            const before = rand() < 0.35 ? 2 + rand() * 5 : 0;
            const after = rand() < 0.35 ? 2 + rand() * 5 : 0;
            return [[a[0] - u[0] * before, a[1] - u[1] * before], [b[0] + u[0] * after, b[1] + u[1] * after]];
        };
        let body = `<path d="${closedPath(corners)}" fill="${fill}"/>`;
        for (const edge of edges) {
            const [a, b] = overshoot(edge);
            const shift = () => [(rand() - 0.5) * 5, (rand() - 0.5) * 5];
            const [sa, sb] = [shift(), shift()];
            body += `<path d="${stroke([a[0] + sa[0], a[1] + sa[1]], [b[0] + sb[0], b[1] + sb[1]], 1.1, 1.4, rand)}" fill="${PENCIL}"/>`;
        }
        for (const edge of edges) {
            const [a, b] = overshoot(edge);
            body += `<path d="${stroke(a, b, 3.4, 0.9, rand)}" fill="${ink}"/>`;
        }
        return svgImage(width + 2 * m, height + 2 * m, body);
    }

    // A squircle drawn on the box of the balloon leaves room for the text in its padding, and looks more like a balloon
    // lettered by hand than an ellipse, which would cut the corners of a long sentence.
    function outline(w, h, scale, wobble, steps = 96) {
        const points = [];
        for (let i = 0; i < steps; i++) {
            const t = (i / steps) * 2 * Math.PI;
            const c = Math.cos(t);
            const s = Math.sin(t);
            const r = scale * (1 + wobble(t));
            points.push([w / 2 + (w / 2) * r * Math.sign(c) * Math.sqrt(Math.abs(c)), h / 2 + (h / 2) * r * Math.sign(s) * Math.sqrt(Math.abs(s))]);
        }
        return points;
    }

    // Lengths along a closed outline, from its first point.
    function lengthsAlong(points) {
        const along = [0];
        for (let i = 1; i <= points.length; i++) {
            const [x, y] = points[i % points.length];
            const [px, py] = points[i - 1];
            along.push(along[i - 1] + Math.hypot(x - px, y - py));
        }
        return along;
    }

    function cloudParts(w, h, rand, x0, dir) {
        const r = Math.max(9, Math.min(20, h * 0.3));
        const shift = ([x, y]) => [x + w * 0.05, y + h * 0.08];
        // Puffs evenly spaced all round the ring, interpolated between its samples: sampled at even angles, a squircle
        // leaves uneven steps in the middle of its long edges.
        const ring = outline(w * 0.9, h * 0.84, 1, () => 0, 720).map(shift);
        const along = lengthsAlong(ring);
        const perimeter = along[ring.length];
        const count = Math.max(6, Math.round(perimeter / (r * 1.3)));
        const parts = [{ d: closedPath(outline(w * 0.9, h * 0.84, 1, () => 0).map(shift)) }];
        let k = 0;
        for (let i = 0; i < count; i++) {
            const target = (i / count) * perimeter;
            while (k < ring.length - 1 && along[k + 1] < target) k++;
            const [x, y] = ring[k];
            const [nx, ny] = ring[(k + 1) % ring.length];
            const f = (target - along[k]) / (along[k + 1] - along[k] || 1);
            parts.push({ cx: x + (nx - x) * f, cy: y + (ny - y) * f, r: r * (0.9 + rand() * 0.2) });
        }
        const bottom = h * 0.92 + r;
        parts.push({ cx: x0 + dir * 4, cy: bottom + 8, r: 6 }, { cx: x0 + dir * 13, cy: bottom + 19, r: 4 }, { cx: x0 + dir * 20, cy: bottom + 27, r: 2.6 });
        return parts;
    }

    // A burst drawn as in printed comics: an irregular silhouette, many short spikes and a few long ones, never two long
    // side by side, whose sides curve slightly inwards.
    function starParts(w, h, rand, x0, dir) {
        const waves = [rand() * 6.3, rand() * 6.3];
        const radius = (t) => 1 + 0.05 * Math.sin(2 * t + waves[0]) + 0.035 * Math.sin(3 * t + waves[1]);
        const base = [];
        for (let i = 0; i < 720; i++) {
            const t = (i / 720) * 2 * Math.PI;
            const r = radius(t);
            base.push([w / 2 + (w / 2) * 1.08 * r * Math.cos(t), h / 2 + (h / 2) * 1.25 * r * Math.sin(t)]);
        }
        const along = lengthsAlong(base);
        const perimeter = along[base.length];
        // A point of the silhouette, with the direction a spike takes there: halfway between square to the outline and
        // away from the centre, so that the spikes neither spread flat at the ends nor lean sideways along the edges.
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
        let longBefore = false;
        let d = `M${point(hollows[0])}`;
        for (let i = 0; i < count; i++) {
            const long = !longBefore && rand() < 0.3;
            longBefore = long;
            const reach = long ? 28 + rand() * 16 : 10 + rand() * 8;
            const b = at((i + 0.5 + (rand() - 0.5) * 0.3) * step);
            const tip = [b.x + b.nx * reach, b.y + b.ny * reach];
            const axis = [b.x + (tip[0] - b.x) * 0.5, b.y + (tip[1] - b.y) * 0.5];
            const bend = (from) => {
                const mid = [(from[0] + tip[0]) / 2, (from[1] + tip[1]) / 2];
                return [mid[0] + (axis[0] - mid[0]) * 0.3, mid[1] + (axis[1] - mid[1]) * 0.3];
            };
            const to = hollows[(i + 1) % count];
            d += `Q${point(bend(hollows[i]))} ${point(tip)}Q${point(bend(to))} ${point(to)}`;
        }
        return [
            { d: d + "Z" },
            { d: `M${x0 - 13} ${h * 0.8}Q${x0 + dir * 2} ${h + 14} ${x0 + dir * 26} ${h + 42}Q${x0 + dir * 8} ${h + 12} ${x0 + 13} ${h * 0.8}Z` },
        ];
    }

    function smoothParts(w, h, rand, x0, dir) {
        const phase = [rand() * 6.3, rand() * 6.3];
        const wobble = (t) => 0.018 * Math.sin(2 * t + phase[0]) + 0.012 * Math.sin(3 * t + phase[1]);
        const tipX = x0 + dir * (12 + rand() * 10);
        const tipY = h + 16 + rand() * 6;
        return [
            { d: closedPath(outline(w, h, 1, wobble)) },
            { d: `M${x0 - 9} ${h * 0.6}Q${x0 - 3 + dir * 4} ${h + 4} ${tipX} ${tipY}Q${x0 + 5} ${h + 2} ${x0 + 9} ${h * 0.6}Z` },
        ];
    }

    const SHAPES = { smooth: smoothParts, cloud: cloudParts, star: starParts };

    // Every part is first drawn with a doubled black stroke, then filled in white over it: the parts merge into a single
    // balloon, whose tail joins its body without a seam.
    function bubbleArt(width, height, title, ink, paper) {
        const rand = seeded(Number(title.dataset.seed));
        const parts = SHAPES[title.dataset.shape](width, height, rand, width * Number(title.dataset.tailAt), title.dataset.tailFlip === "true" ? -1 : 1);
        const element = (part, attributes) =>
            part.d ? `<path d="${part.d}" ${attributes}/>` : `<circle cx="${part.cx.toFixed(1)}" cy="${part.cy.toFixed(1)}" r="${part.r.toFixed(1)}" ${attributes}/>`;
        const outlined = parts.map((part) => element(part, `fill="${ink}" stroke="${ink}" stroke-width="${2 * BUBBLE_STROKE}" stroke-linejoin="round"`)).join("");
        const filled = parts.map((part) => element(part, `fill="${paper}"`)).join("");
        const m = BUBBLE_MARGIN;
        return svgImage(width + 2 * m, height + 2 * m, `<g transform="translate(${m} ${m})">${outlined}${filled}</g>`);
    }

    // The size each element was last drawn at: a drawing is only redone when it changes.
    const drawnSizes = new WeakMap();

    function sizeChanged(element) {
        const size = `${element.offsetWidth}x${element.offsetHeight}`;
        if (drawnSizes.get(element) === size)
            return false;
        drawnSizes.set(element, size);
        return true;
    }

    const resizes = new ResizeObserver((entries) => entries.forEach((entry) => draw(entry.target)));

    function draw(element) {
        if (!element.isConnected || element.offsetWidth <= 1 || !sizeChanged(element))
            return;
        const page = getComputedStyle(document.documentElement);
        const ink = page.getPropertyValue("--color-text").trim();
        const width = element.offsetWidth;
        const height = element.offsetHeight;
        if (element.classList.contains("block")) {
            const fill = getComputedStyle(element).getPropertyValue("--block-fill").trim();
            const border = parseFloat(getComputedStyle(element).borderInlineStartWidth) || 0;
            element.style.setProperty("--block-art", frameArt(width, height, fill, ink, Number(element.dataset.seed)));
            element.style.setProperty("--block-art-inset", `${-(FRAME_MARGIN + border)}px`);
            element.dataset.drawn = "";
        } else {
            const paper = page.getPropertyValue("--color-surface").trim();
            element.style.setProperty("--bubble-art", bubbleArt(width, height, element, ink, paper));
            element.style.setProperty("--bubble-art-inset", `${-BUBBLE_MARGIN}px`);
        }
    }

    // Deals what a block or a title has not been dealt yet in this display, then draws it. A title whose shape changes its
    // size (a cloud or a star takes more room) is drawn once its new size is laid out, by the observer of its size.
    function apply() {
        const blocks = [...document.querySelectorAll(".block")];
        blocks.forEach((block, n) => {
            if (block.dataset.tint === undefined) {
                block.dataset.tint = String(tintOf(n));
                block.dataset.seed = String(newSeed());
                block.style.setProperty("--block-fill", `var(--color-block-tint-${block.dataset.tint})`);
            }
        });
        // A title kept for screen readers only (one pixel wide, clipped) is not a balloon.
        const titles = [...document.querySelectorAll(".block-title")].filter((title) => title.offsetWidth > 1);
        const fresh = titles.filter((title) => title.dataset.shape === undefined);
        const kinds = !balloons.planned ? planBalloons(titles) : null;
        fresh.forEach((title) => {
            const index = titles.indexOf(title);
            title.dataset.shape = shapeFor(kinds ? kinds[index] : kindOf(title.textContent.trim()), kinds ? index : -1);
            title.dataset.seed = String(newSeed());
            title.dataset.tailAt = (0.15 + Math.random() * 0.5).toFixed(3);
            title.dataset.tailFlip = String(Math.random() < 0.5);
        });
        for (const element of [...blocks, ...titles]) {
            resizes.observe(element);
            draw(element);
        }
    }

    newDisplay();
    apply();

    // Content added by an interactive component, or by an enhanced navigation before it ends, is dealt and drawn too, once
    // per frame whatever the number of changes: an interactive component changes the page at each key typed.
    let pending = false;
    new MutationObserver((records) => {
        if (pending || !records.some((record) => record.addedNodes.length))
            return;
        pending = true;
        requestAnimationFrame(() => {
            pending = false;
            apply();
        });
    }).observe(document.body, { childList: true, subtree: true });
    document.fonts?.ready.then(apply);

    // An enhanced navigation of Blazor displays a new page, which gets a new deal: its blocks may be those of the previous
    // page, whose attributes it set again.
    document.addEventListener("DOMContentLoaded", () =>
        window.Blazor?.addEventListener("enhancedload", () => {
            newDisplay();
            for (const element of document.querySelectorAll(".block, .block-title")) {
                for (const name of ["tint", "seed", "shape", "tailAt", "tailFlip", "drawn"])
                    delete element.dataset[name];
                drawnSizes.delete(element);
            }
            apply();
        }));
})();
