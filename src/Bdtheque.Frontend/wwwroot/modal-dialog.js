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
    dialog.onkeydown = event => {
        if (submitsThePageBehind(dialog, event)) {
            event.preventDefault();
            // The value typed is committed first, as leaving the field for the action with the mouse commits it.
            event.target.dispatchEvent(new Event("change", { bubbles: true }));
            dialog.querySelector(".dialog-actions [data-variant='primary']")?.click();
        }
    };
    dialog.showModal();
}

// Enter in a field of the dialog whose form the dialog does not send (the dialog of a record created on the spot, within the
// form of the record that cites it, sent by its save bar or its side panel) would submit that form, saving the record without
// what the dialog adds: it performs the main action of the dialog instead. A dialog that holds the submission of the form of
// its field (a side panel, a confirmation) keeps it.
const fieldsSubmittingOnEnter = "input:not([type=checkbox], [type=radio], [type=button], [type=submit], [type=reset], [type=image], [type=file])";

function submitsThePageBehind(dialog, event) {
    const field = event.target;
    return event.key === "Enter" && !event.defaultPrevented && !event.isComposing && field.matches?.(fieldsSubmittingOnEnter)
        && field.form !== null && ![...field.form.elements].some(element => element.type === "submit" && dialog.contains(element));
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
