// A row of entries that scrolls sideways rather than wraps (the menu of the consultation, the navigation par initiale,
// visuel/charte-graphique.md § Apparence propre à chaque alternative). Its scrollbar is hidden: without a touch screen, the
// mouse wheel and the chevrons are the only ways to scroll it, and a chevron shows only on a side with entries out of view.
// On opening the page (first load or enhanced navigation of Blazor), the row is centered on its current entry.
const reducedMotion = matchMedia("(prefers-reduced-motion: reduce)");

function itemsOf(row) {
    return row.querySelector(".scroll-row-items");
}

function update(row) {
    const items = itemsOf(row);
    if (!items)
        return;
    const max = items.scrollWidth - items.clientWidth;
    row.toggleAttribute("data-more-start", items.scrollLeft > 1);
    row.toggleAttribute("data-more-end", items.scrollLeft < max - 1);
}

function center(row) {
    const items = itemsOf(row);
    const current = items?.querySelector('[aria-current="page"]');
    if (current) {
        const offset = current.getBoundingClientRect().left - items.getBoundingClientRect().left + items.scrollLeft;
        items.scrollLeft = offset - (items.clientWidth - current.offsetWidth) / 2;
    }
    update(row);
}

customElements.define("app-scroll-row", class extends HTMLElement {
    connectedCallback() {
        // Once the renderer that inserted the element has also inserted its content.
        queueMicrotask(() => {
            if (!this.isConnected)
                return;
            if (!this.wired) {
                this.wired = true;
                // On the row rather than on its items, which an enhanced navigation may replace: a scroll, which does not
                // bubble, reaches it while capturing.
                this.addEventListener("scroll", () => update(this), { capture: true, passive: true });
                this.addEventListener("wheel", event => {
                    const items = itemsOf(this);
                    const max = items.scrollWidth - items.clientWidth;
                    if (max <= 0 || Math.abs(event.deltaX) > Math.abs(event.deltaY))
                        return;
                    const next = Math.min(max, Math.max(0, items.scrollLeft + event.deltaY));
                    if (next === items.scrollLeft)
                        return;
                    // The page does not scroll while the row does.
                    event.preventDefault();
                    items.scrollLeft = next;
                }, { passive: false });
                this.addEventListener("click", event => {
                    const chevron = event.target.closest(".scroll-row-chevron");
                    if (!chevron)
                        return;
                    const items = itemsOf(this);
                    const direction = chevron.dataset.direction === "start" ? -1 : 1;
                    items.scrollBy({ left: direction * items.clientWidth, behavior: reducedMotion.matches ? "instant" : "smooth" });
                });
                new ResizeObserver(() => update(this)).observe(this);
            }
            center(this);
        });
    }
});

// An enhanced navigation keeps the rows the new page shares with the previous one, and only changes their entries.
document.addEventListener("DOMContentLoaded", () =>
    window.Blazor?.addEventListener("enhancedload", () => document.querySelectorAll("app-scroll-row").forEach(center)));
