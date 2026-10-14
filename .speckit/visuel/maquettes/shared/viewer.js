// Viewer of the mockups: the list of the mockups (Viewer.list), and for each mockup its versions
// side by side (Viewer.show). Each version is a mockup page shown in a frame at its real width,
// scaled down to fit; the page reports its height and its settings (see mockup.js).
(function () {
  'use strict';

  const WIDTHS = { desktop: 1440, phone: 390 };
  // Until a page reports its height, and whenever its width changes: a frame taller than its page
  // would stretch the pages that fill the viewport (min-height: 100vh), which would report it back.
  const INITIAL_HEIGHT = 600;
  const frames = [];

  function el(tag, attributes = {}, children = []) {
    const element = document.createElement(tag);
    for (const [name, value] of Object.entries(attributes)) {
      if (name === 'text') element.textContent = value;
      else if (name.startsWith('on')) element.addEventListener(name.slice(2), value);
      else element.setAttribute(name, value);
    }
    element.append(...children);
    return element;
  }

  function urlOf(page, params) {
    const query = new URLSearchParams(params).toString();
    return query ? `${page}?${query}` : page;
  }

  function createFrame(title, { maxHeight = Infinity, preview = false } = {}) {
    const iframe = el('iframe', { title });
    if (preview) {
      iframe.setAttribute('loading', 'lazy');
      iframe.setAttribute('tabindex', '-1');
      iframe.setAttribute('aria-hidden', 'true');
    }
    const box = el('div', { class: 'frame-box' }, [iframe]);
    const frame = { iframe, box, width: WIDTHS.desktop, height: INITIAL_HEIGHT, onSettings: null };

    frame.layout = () => {
      const available = box.clientWidth;
      if (!available) return;
      const scale = Math.min(1, available / frame.width);
      iframe.style.width = `${frame.width}px`;
      iframe.style.height = `${frame.height}px`;
      iframe.style.transform = `scale(${scale})`;
      iframe.style.marginLeft = `${(available - frame.width * scale) / 2}px`;
      box.style.height = `${Math.ceil(Math.min(frame.height, maxHeight) * scale)}px`;
    };
    frame.load = (url) => {
      frame.height = INITIAL_HEIGHT;
      frame.layout();
      iframe.src = url;
    };
    // Reloaded rather than resized in place: a browser may hold back the rendering of a frame out of
    // view, which would then not report its new height, whereas a page always reports on loading.
    frame.resize = (width) => {
      frame.width = width;
      frame.load(iframe.src);
    };

    new ResizeObserver(frame.layout).observe(box);
    frames.push(frame);
    return frame;
  }

  window.addEventListener('message', (event) => {
    if (event.data?.source !== 'bdtheque-mockup') return;
    const frame = frames.find((f) => f.iframe.contentWindow === event.source);
    if (!frame) return;
    frame.height = event.data.height;
    frame.layout();
    frame.onSettings?.(event.data.settings);
  });

  function settingControl(name, setting, value, onChange) {
    if (setting.editor === 'boolean') {
      const input = el('input', { type: 'checkbox', onchange: () => onChange(String(input.checked)) });
      input.checked = String(value) === 'true';
      return el('label', { class: 'control' }, [input, setting.label ?? name]);
    }
    const select = el('select', { onchange: () => onChange(select.value) },
      (setting.options ?? []).map((option) => el('option', { value: option, text: option })));
    select.value = value;
    return el('label', { class: 'control' }, [el('span', { text: setting.label ?? name }), select]);
  }

  function segmented(legend, name, options, current, onChange) {
    return el('fieldset', { class: 'control' }, [
      el('legend', { text: legend }),
      el('div', { class: 'segmented' }, options.map(([value, label]) => {
        const input = el('input', { type: 'radio', name, value, onchange: () => onChange(value) });
        input.checked = value === current;
        return el('label', {}, [input, label]);
      }))
    ]);
  }

  // Versions of one mockup, side by side. A setting that every version offers, and that no version
  // fixes, applies to all of them from the toolbar; the others stay with their version.
  function show({ title, lead, back = '../index.html', versions }) {
    document.title = `${title} — maquettes`;
    const shared = {};
    const globalSettings = el('div', { class: 'control' });
    const grid = el('div', { class: 'versions' });

    const cards = versions.map((version) => {
      const card = { version, values: {}, schema: null };
      card.frame = createFrame(version.title);
      card.open = el('a', { href: '#', target: '_blank', rel: 'noopener', text: 'Ouvrir seule ↗' });
      card.settings = el('div', { class: 'settings' });
      card.url = () => urlOf(version.page, { ...version.params, ...shared, ...card.values });
      card.reload = () => {
        card.open.href = card.url();
        card.frame.load(card.url());
      };
      card.frame.onSettings = (schema) => {
        if (card.schema) return;
        card.schema = schema;
        if (cards.every((c) => c.schema)) buildSettings();
      };
      grid.append(el('section', { class: 'version' }, [
        el('header', {}, [el('h2', { text: version.title }), card.open, card.settings]),
        card.frame.box
      ]));
      return card;
    });

    function buildSettings() {
      const fixed = (card, name) => card.version.params && name in card.version.params;
      const common = Object.keys(cards[0].schema).filter((name) =>
        cards.every((card) => name in card.schema && !fixed(card, name)));
      for (const name of common) {
        const setting = cards[0].schema[name];
        globalSettings.append(settingControl(name, setting, setting.default, (value) => {
          shared[name] = value;
          cards.forEach((card) => card.reload());
        }));
      }
      for (const card of cards) {
        for (const [name, setting] of Object.entries(card.schema)) {
          if (common.includes(name) || fixed(card, name)) continue;
          card.settings.append(settingControl(name, setting, setting.default, (value) => {
            card.values[name] = value;
            card.reload();
          }));
        }
      }
    }

    document.body.append(el('main', { class: 'page' }, [
      el('a', { class: 'back', href: back, text: '← Toutes les maquettes' }),
      el('h1', { text: title }),
      lead ? el('p', { class: 'lead', text: lead }) : '',
      el('div', { class: 'toolbar' }, [
        segmented('Largeur', 'width', [['desktop', 'Bureau · 1440 px'], ['phone', 'Smartphone · 390 px']], 'desktop',
          (value) => cards.forEach((card) => card.frame.resize(WIDTHS[value]))),
        el('div', { class: 'columns-control' }, [
          segmented('Colonnes', 'columns', [['1', '1'], ['2', '2'], ['3', '3'], ['4', '4']], '2',
            (value) => grid.style.setProperty('--columns', value))
        ]),
        globalSettings
      ]),
      grid
    ]));
    cards.forEach((card) => card.reload());
  }

  // List of the mockups, by section, each with a preview of the top of its first version.
  function list({ title, lead, sections }) {
    document.title = title;
    document.body.append(el('main', { class: 'page' }, [
      el('h1', { text: title }),
      lead ? el('p', { class: 'lead', text: lead }) : '',
      ...sections.flatMap((section) => [
        el('h2', { text: section.title }),
        el('ul', { class: 'screens' }, section.screens.map((screen) => {
          const frame = createFrame(screen.title, { maxHeight: 900, preview: true });
          frame.load(screen.preview);
          return el('li', {}, [el('a', { class: 'screen', href: screen.href }, [
            frame.box,
            el('div', { class: 'body' }, [
              el('h3', { text: screen.title }),
              el('p', { text: screen.description })
            ])
          ])]);
        }))
      ])
    ]));
  }

  window.Viewer = { show, list };
})();
