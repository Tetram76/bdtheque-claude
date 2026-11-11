// « Tout déplier » and « Tout replier » of the bibliography of an author (visuel/fiche-auteur/contenu.md): each series is a
// disclosure of the browser (details), which no feature of HTML opens or closes all at once. The element opens or closes
// every series of its list, whose albums the page already holds: no request, the page staying where it is. It only sets the
// state of the disclosures, and inserts no node into the page Blazor rendered; its buttons are shown once it is defined
// (Bibliography.razor.css).
customElements.define("app-bibliography-folds", class extends HTMLElement {
    connectedCallback() {
        if (this.wired)
            return;
        this.wired = true;
        this.addEventListener("click", event => {
            const button = event.target.closest("[data-fold-all]");
            if (!button || !this.contains(button))
                return;
            const open = button.dataset.foldAll === "unfold";
            this.querySelectorAll("details.entry-albums").forEach(series => series.open = open);
        });
    }
});
