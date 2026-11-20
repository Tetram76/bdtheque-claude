// The cross of a search field without a list behind it (the header) resets its form (Components/Controls/SearchClear.razor):
// the field emptied, the cross disappears with its text, and the keyboard focus it held goes back to the field, where the
// next search is typed (visuel/liste-albums/controles.md). Without this script, the field is emptied all the same.
document.addEventListener("reset", event => {
    const form = event.target;
    if (form instanceof HTMLFormElement && form.querySelector(":scope .search-clear[type='reset']")) {
        form.querySelector("input[type='search']")?.focus();
    }
}, true);
