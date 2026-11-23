// Closes the menus an enhanced navigation of Blazor leaves open (choix-implementation/socle/frontend.md, menus en popover): a
// menu is a popover of the browser, and Blazor keeps the elements of the page it receives, their place in the top layer with
// them, so that an entry chosen in the menu of the administration led to its page under the menu still open (#610). Once a
// navigation has brought its page, the entry chosen has served: the page is shown without the menu.
document.addEventListener("DOMContentLoaded", () =>
    window.Blazor?.addEventListener("enhancedload", () =>
        document.querySelectorAll("[popover]:popover-open").forEach(menu => menu.hidePopover())));
