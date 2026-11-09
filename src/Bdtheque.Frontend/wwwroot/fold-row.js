// The names of a role of a record on one line, at the width of the screen (the contributions of an album,
// visuel/fiche-album/contenu.md): the names the line does not hold are marked as folded, the last ones first, until the
// link that unfolds them, « et N autres », holds on the line too. Only the browser knows where the names fall, so the row
// folds them again when its width changes, and once the first loading of fonts is laid out, which changes the width of the
// names (as a scroll row, scroll-row.js). Unfolded, the row shows every name; folded again, it measures them anew. The
// script marks the names and writes the number of the folded ones: it inserts no node into the page Blazor rendered.

// On the first line: its top above the bottom of the first name, whatever the height of each.
function onFirstLine(element, first) {
    return element.getBoundingClientRect().top < first.getBoundingClientRect().bottom;
}

function fold(row) {
    const toggle = row.querySelector(":scope > .contributions-toggle");
    const count = toggle?.querySelector(".contributions-more");
    if (!toggle || !count || toggle.open)
        return;
    const names = [...row.querySelectorAll(":scope > .contributor")];
    names.forEach(name => name.removeAttribute("data-folded"));
    row.removeAttribute("data-folding");
    if (names.length < 2 || onFirstLine(names.at(-1), names[0]))
        return;

    row.setAttribute("data-folding", "");
    for (let folded = 1; folded < names.length; folded++) {
        names[names.length - folded].setAttribute("data-folded", "");
        count.textContent = (folded === 1 ? count.dataset.one : count.dataset.other).replace("{0}", String(folded));
        if (onFirstLine(toggle, names[0]))
            return;
    }
}

customElements.define("app-fold-row", class extends HTMLElement {
    connectedCallback() {
        // Once the renderer that inserted the element has also inserted its content.
        queueMicrotask(() => {
            if (!this.isConnected)
                return;
            if (!this.wired) {
                this.wired = true;
                // Folded again on closing; on opening, the style shows every name.
                this.addEventListener("toggle", () => fold(this), { capture: true });
                let width = 0;
                new ResizeObserver(entries => {
                    const current = entries.at(-1).contentRect.width;
                    if (current !== width) {
                        width = current;
                        fold(this);
                    }
                }).observe(this);
            }
            fold(this);
        });
    }
});

document.fonts?.addEventListener("loadingdone", () => document.querySelectorAll("app-fold-row").forEach(fold), { once: true });
