// While the list of suggestions of a RecordPicker is open, the keys it answers keep from their default action: Enter would
// submit the form, Escape erase the text, the arrows move the caret. Blazor can only prevent the default action of every
// key or of none, which would also keep the user from typing.
const listKeys = new Set(["ArrowDown", "ArrowUp", "Enter", "Escape"]);

export function keepKeysForTheList(input) {
    input.addEventListener("keydown", event => {
        if (input.getAttribute("aria-expanded") === "true" && listKeys.has(event.key))
            event.preventDefault();
    });
}

// The focus staying on the text field (aria-activedescendant), the list does not scroll by itself to the active
// suggestion: it is brought into view, the list scrolling no further than needed.
export function reveal(optionId) {
    document.getElementById(optionId)?.scrollIntoView({ block: "nearest" });
}
