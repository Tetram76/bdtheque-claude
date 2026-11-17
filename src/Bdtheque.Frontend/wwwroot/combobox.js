// The keys a combobox answers keep from their default action, which Blazor can only prevent for every key or for none.

// While the list of suggestions of a RecordPicker is open: Enter would submit the form, Escape erase the text, the arrows move
// the caret. Closed, the field is typed in as any other.
const suggestionKeys = new Set(["ArrowDown", "ArrowUp", "Enter", "Escape"]);

export function keepKeysForTheList(input) {
    input.addEventListener("keydown", event => {
        if (input.getAttribute("aria-expanded") === "true" && suggestionKeys.has(event.key))
            event.preventDefault();
    });
}

// On the button of a ListPicker, open or not: Enter and Space would click it, the arrows and Home and End scroll the page. Escape
// only while its list is open; Tab always leaves the button.
const choiceKeys = new Set(["ArrowDown", "ArrowUp", "Home", "End", "Enter", " "]);

export function keepKeysForTheChoice(button) {
    button.addEventListener("keydown", event => {
        if (choiceKeys.has(event.key) || (event.key === "Escape" && button.getAttribute("aria-expanded") === "true"))
            event.preventDefault();
    });
}

// The focus staying on the combobox (aria-activedescendant), the list does not scroll by itself to the active option: it is
// brought into view, the list scrolling no further than needed.
export function reveal(optionId) {
    document.getElementById(optionId)?.scrollIntoView({ block: "nearest" });
}
