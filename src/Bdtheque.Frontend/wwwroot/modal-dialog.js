// Opens as a modal the dialog it wraps, whenever it enters the page: on the first load, by an enhanced navigation of Blazor
// or by an interactive rendering (choix-implementation/socle/frontend.md). Only a script can open a dialog as a modal,
// which the browser keeps above an inert page, focus included. Escape follows the closing link of the dialog, rather than
// closing it in place, so that the page without it is the one shown.
function open(element) {
    const dialog = element.querySelector("dialog");
    if (!element.isConnected || !dialog || dialog.open)
        return;

    // A property rather than a listener: a dialog inserted again is not given a second one.
    dialog.oncancel = event => {
        event.preventDefault();
        dialog.querySelector("[data-dialog-close]")?.click();
    };
    dialog.showModal();
}

customElements.define("app-modal-dialog", class extends HTMLElement {
    connectedCallback() {
        // Once the renderer that inserted the element has also inserted its content.
        queueMicrotask(() => open(this));
    }
});

// An enhanced navigation from a page with a dialog to the same dialog in another state (the viewer of the visuals of a record,
// on its next visual) keeps the element, and takes from the dialog the attribute that showed it open, which the page it
// received does not carry: the dialog is opened again.
document.addEventListener("DOMContentLoaded", () =>
    window.Blazor?.addEventListener("enhancedload", () => document.querySelectorAll("app-modal-dialog").forEach(open)));
