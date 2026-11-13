// After a saving refused, what is to correct is brought into view (visuel/saisie-album/contenu.md): the first field to correct,
// which gets the focus, in the middle of the screen, out of reach of the save bar fixed at its bottom, rather than a field
// whose value it only clashes with (data-error-related); without one, the error presented above the form, which the user, at
// the bottom of the form, would not see.
export function revealProblem(entryForm) {
    const invalid = ".entry-form-body [aria-invalid='true']";
    const field = entryForm.querySelector(`${invalid}:not([data-error-related] *)`) ?? entryForm.querySelector(invalid);
    const target = field ?? entryForm.querySelector("[role='alert']");
    if (!target)
        return;
    target.scrollIntoView({ block: "center" });
    field?.focus({ preventScroll: true });
}
