// After a saving refused, what is to correct is brought into view (visuel/saisie-album/contenu.md): the first field in error,
// which gets the focus, in the middle of the screen, out of reach of the save bar fixed at its bottom; without one, the error
// presented above the form, which the user, at the bottom of the form, would not see.
export function revealProblem(entryForm) {
    const field = entryForm.querySelector(".entry-form-body [aria-invalid='true']");
    const target = field ?? entryForm.querySelector("[role='alert']");
    if (!target)
        return;
    target.scrollIntoView({ block: "center" });
    field?.focus({ preventScroll: true });
}
