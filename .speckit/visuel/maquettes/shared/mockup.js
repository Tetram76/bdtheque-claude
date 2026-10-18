// Renders a mockup page without any external runtime.
//
// A mockup page holds its markup in <template id="view">, and a script that declares a View class
// (extending Mockup) whose renderVals() returns the values the markup displays. The markup uses:
//   - {{path}} in text and attributes, where path is a name or a dotted path (t.bg, e.name);
//     an attribute made of a single {{path}} takes the value itself: a function becomes an event
//     listener (onClick="{{toggle}}"), true sets an empty attribute, false/null/undefined removes it;
//   - <sc-for list="{{path}}" as="name"> to repeat its content for each item of a list;
//   - <sc-if value="{{path}}"> to keep its content only when the value is truthy;
//   - data-scroll-hint on a wrapper whose child [data-scroll] scrolls horizontally (by touch, by the mouse
//     wheel, or by clicking an edge chevron), with --hint-bg
//     set to the colour behind it, to show which edges have entries out of view.
// Settings (theme, example…) come from the query string, typed and defaulted by the settings
// declared with Mockup.mount; a viewer page embeds mockups in frames with these parameters.
(function () {
  'use strict';

  const EXPRESSION = /\{\{\s*([^{}]*?)\s*\}\}/g;
  const SINGLE_EXPRESSION = /^\{\{\s*([^{}]*?)\s*\}\}$/;

  class Mockup {
    constructor(props) {
      this.props = props;
      this.state = undefined;
    }

    setState(patch) {
      this.state = { ...(this.state ?? {}), ...patch };
      render();
    }
  }

  let view = null;
  let template = null;
  let root = null;
  let settings = {};

  function resolve(expression, scopes) {
    if (expression === 'true') return true;
    if (expression === 'false') return false;
    if (expression === 'null') return null;
    if (/^-?\d+(\.\d+)?$/.test(expression)) return Number(expression);
    const [head, ...path] = expression.split('.');
    let value;
    for (let i = scopes.length - 1; i >= 0; i--) {
      if (head in scopes[i]) {
        value = scopes[i][head];
        break;
      }
    }
    for (const key of path) value = value == null ? undefined : value[key];
    return value;
  }

  function interpolate(text, scopes) {
    return text.replace(EXPRESSION, (_, expression) => {
      const value = resolve(expression, scopes);
      return value == null ? '' : String(value);
    });
  }

  function expressionOf(node, attribute) {
    const match = (node.getAttribute(attribute) ?? '').trim().match(SINGLE_EXPRESSION);
    return match ? match[1] : '';
  }

  function renderElement(source, scopes) {
    const element = document.createElementNS(source.namespaceURI, source.localName);
    for (const attribute of source.attributes) {
      const single = attribute.value.trim().match(SINGLE_EXPRESSION);
      if (!single) {
        element.setAttributeNS(attribute.namespaceURI, attribute.name, interpolate(attribute.value, scopes));
        continue;
      }
      const value = resolve(single[1], scopes);
      if (typeof value === 'function') {
        if (attribute.name.toLowerCase().startsWith('on')) element.addEventListener(attribute.name.slice(2).toLowerCase(), value);
      } else if (value === true) {
        element.setAttributeNS(attribute.namespaceURI, attribute.name, '');
      } else if (value != null && value !== false) {
        element.setAttributeNS(attribute.namespaceURI, attribute.name, String(value));
      }
    }
    renderChildren(source, element, scopes);
    return element;
  }

  function renderChildren(source, target, scopes) {
    for (const node of source.childNodes) {
      if (node.nodeType === Node.TEXT_NODE) {
        target.appendChild(document.createTextNode(interpolate(node.nodeValue, scopes)));
      } else if (node.nodeType !== Node.ELEMENT_NODE) {
        continue;
      } else if (node.localName === 'sc-for') {
        const name = node.getAttribute('as');
        for (const item of resolve(expressionOf(node, 'list'), scopes) ?? []) {
          renderChildren(node, target, [...scopes, { [name]: item }]);
        }
      } else if (node.localName === 'sc-if') {
        if (resolve(expressionOf(node, 'value'), scopes)) renderChildren(node, target, scopes);
      } else {
        target.appendChild(renderElement(node, scopes));
      }
    }
  }

  // A render replaces the whole page: the reader's place (page and inner scrolling, focused control)
  // is carried over, so that a click on a thumbnail strip or a fold button does not lose it.
  function pathOf(node) {
    if (!node || node === root || !root.contains(node)) return null;
    const path = [];
    for (; node !== root; node = node.parentNode) {
      path.unshift(Array.prototype.indexOf.call(node.parentNode.children, node));
    }
    return path;
  }

  function nodeAt(path) {
    let node = root;
    for (const index of path) {
      node = node?.children[index];
    }
    return node;
  }

  function render() {
    const page = { x: window.scrollX, y: window.scrollY };
    const focused = pathOf(document.activeElement);
    const scrolled = [];
    for (const element of root.querySelectorAll('*')) {
      if (element.scrollLeft || element.scrollTop) scrolled.push([pathOf(element), element.scrollLeft, element.scrollTop]);
    }

    const content = document.createDocumentFragment();
    renderChildren(template.content, content, [view.renderVals()]);
    root.replaceChildren(content);

    for (const [path, left, top] of scrolled) {
      const element = nodeAt(path);
      if (element) {
        element.scrollLeft = left;
        element.scrollTop = top;
      }
    }
    if (focused) nodeAt(focused)?.focus({ preventScroll: true });
    window.scrollTo(page.x, page.y);
    root.querySelectorAll('[data-scroll-hint]').forEach(wireScrollHint);
    updateScrollHints();
    reportToViewer();
  }

  // A horizontal menu scrolls rather than wraps: a fading edge with a chevron shows on each side
  // where some entries are out of view, so that the reader knows there is more to reach.
  const SCROLL_HINT_STYLE = `
[data-scroll-hint]{position:relative;min-width:0}
[data-scroll]{overflow-x:auto;scrollbar-width:none}
[data-scroll]::-webkit-scrollbar{display:none}
[data-scroll-hint]::before,[data-scroll-hint]::after{position:absolute;top:0;bottom:0;width:34px;display:flex;align-items:center;font:800 20px/1 system-ui,sans-serif;pointer-events:none;opacity:0;transition:opacity .15s}
[data-scroll-hint]::before{content:'‹';left:0;justify-content:flex-start;background:linear-gradient(to left,transparent,var(--hint-bg) 55%)}
[data-scroll-hint]::after{content:'›';right:0;justify-content:flex-end;background:linear-gradient(to right,transparent,var(--hint-bg) 55%)}
[data-scroll-hint].more-left::before,[data-scroll-hint].more-right::after{opacity:1;pointer-events:auto;cursor:pointer}`;

  // Without a touch screen nothing scrolls a bar whose scrollbar is hidden: a chevron scrolls it by most
  // of its width when clicked (a click on a pseudo-element reaches its host), and the mouse wheel
  // scrolls it sideways.
  function wireScrollHint(hint) {
    const scroller = hint.querySelector('[data-scroll]');
    if (!scroller) return;
    scroller.addEventListener('scroll', () => updateScrollHint(hint), { passive: true });
    scroller.addEventListener('wheel', (event) => {
      const max = scroller.scrollWidth - scroller.clientWidth;
      if (max <= 0 || Math.abs(event.deltaX) > Math.abs(event.deltaY)) return;
      const next = Math.min(max, Math.max(0, scroller.scrollLeft + event.deltaY));
      if (next === scroller.scrollLeft) return;
      event.preventDefault();
      scroller.scrollLeft = next;
    }, { passive: false });
    hint.addEventListener('click', (event) => {
      if (event.target !== hint) return;
      const box = hint.getBoundingClientRect();
      const step = scroller.clientWidth * 0.8;
      scroller.scrollLeft += event.clientX - box.left < box.width / 2 ? -step : step;
    });
  }

  function updateScrollHint(hint) {
    const scroller = hint.querySelector('[data-scroll]');
    if (!scroller) return;
    const max = scroller.scrollWidth - scroller.clientWidth;
    hint.classList.toggle('more-left', scroller.scrollLeft > 1);
    hint.classList.toggle('more-right', scroller.scrollLeft < max - 1);
  }

  function updateScrollHints() {
    root?.querySelectorAll('[data-scroll-hint]').forEach(updateScrollHint);
  }

  // On opening the page, a scrolling menu shows its current entry in the middle of the visible part.
  function centerCurrentEntries() {
    for (const scroller of root.querySelectorAll('[data-scroll-hint] [data-scroll]')) {
      const current = scroller.querySelector('[aria-current="page"]');
      if (!current) continue;
      const offset = current.getBoundingClientRect().left - scroller.getBoundingClientRect().left + scroller.scrollLeft;
      scroller.scrollLeft = offset - (scroller.clientWidth - current.offsetWidth) / 2;
    }
    updateScrollHints();
  }

  function readProps() {
    const query = new URLSearchParams(window.location.search);
    const props = {};
    for (const [name, value] of query) props[name] = value;
    for (const [name, setting] of Object.entries(settings)) {
      if (!query.has(name)) props[name] = setting.default;
      else if (setting.editor === 'boolean') props[name] = query.get(name) === 'true';
    }
    return props;
  }

  // A viewer embeds the mockup in a frame: it needs the page height to size the frame, and the
  // settings to offer them. A message works even between pages opened from the file system.
  let reportedHeight = 0;

  function reportToViewer() {
    if (window.parent === window) return;
    const height = document.documentElement.scrollHeight;
    if (Math.abs(height - reportedHeight) < 2) return;
    reportedHeight = height;
    window.parent.postMessage({ source: 'bdtheque-mockup', height, settings }, '*');
  }

  Mockup.mount = function (View, declaredSettings) {
    settings = declaredSettings ?? {};
    const style = document.createElement('style');
    style.textContent = SCROLL_HINT_STYLE;
    document.head.append(style);
    template = document.getElementById('view');
    root = document.getElementById('mockup');
    view = new View(readProps());
    render();
    view.componentDidMount?.();
    centerCurrentEntries();
    new ResizeObserver(() => { updateScrollHints(); reportToViewer(); }).observe(document.documentElement);
    // Web fonts change the width of the entries: the current one is centred again once they are loaded.
    document.fonts?.ready.then(() => { centerCurrentEntries(); reportToViewer(); });
    window.addEventListener('load', reportToViewer);
  };

  window.Mockup = Mockup;
})();
