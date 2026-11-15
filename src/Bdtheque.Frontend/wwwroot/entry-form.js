// After a saving refused, what is to correct is brought into view (visuel/saisie-album/contenu.md): the first field to correct,
// which gets the focus, in the middle of the screen, out of reach of the save bar fixed at its bottom, rather than a field
// whose value it only clashes with (data-error-related); without one, the error presented above the form, which the user, at
// the bottom of the form, would not see. A control that aria-invalid does not apply to (the removal of the tag of a record
// created on the spot, which the saving refused) is marked by data-error-focus.
export function revealProblem(entryForm) {
    const invalid = ".entry-form-body :is([aria-invalid='true'], [data-error-focus])";
    const field = entryForm.querySelector(`${invalid}:not([data-error-related] *)`) ?? entryForm.querySelector(invalid);
    const target = field ?? entryForm.querySelector("[role='alert']");
    if (!target)
        return;
    target.scrollIntoView({ block: "center" });
    field?.focus({ preventScroll: true });
}
