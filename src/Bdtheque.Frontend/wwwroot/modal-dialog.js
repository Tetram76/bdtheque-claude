// Opens as a modal the dialog it wraps, whenever it enters the page: on the first load, by an enhanced navigation of Blazor
// or by an interactive rendering (choix-implementation/socle.md § Frontend). Only a script can open a dialog as a modal,
// which the browser keeps above an inert page, focus included. Escape follows the closing link of the dialog, rather than
// closing it in place, so that the page without it is the one shown.
customElements.define("app-modal-dialog", class extends HTMLElement {
    connectedCallback() {
        // Once the renderer that inserted the element has also inserted its content.
        queueMicrotask(() => {
            const dialog = this.querySelector("dialog");
            if (!this.isConnected || !dialog || dialog.open)
                return;

            // A property rather than a listener: a dialog inserted again is not given a second one.
            dialog.oncancel = event => {
                event.preventDefault();
                dialog.querySelector("[data-dialog-close]")?.click();
            };
            dialog.showModal();
        });
    }
});
