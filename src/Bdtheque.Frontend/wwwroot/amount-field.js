// An amount takes no more decimals than its currency admits (fonctionnel/valorisation/gestion-des-devises.md, précision d'un
// montant): a key typed or a text pasted that would put more digits after the decimal separator is kept from the field, which
// Blazor cannot do alone. The field says how many (data-decimals) and its separator (data-decimal-separator), both set by the
// application (Components/Controls/AmountField.razor), changing with the currency. The separator itself is always let in, even
// for a currency without decimals: kept out, the digits typed after it would join the units, 1500,5 yens becoming 15005; let in,
// they are kept out, and the amount read is 1500. A deletion is always let through. Without this script, the saving refuses an
// amount too precise all the same.
document.addEventListener("beforeinput", event => {
    const field = event.target;
    if (!(field instanceof HTMLInputElement) || !field.matches("input[data-decimals]") || event.data === null)
        return;
    const separator = field.dataset.decimalSeparator;
    const text = field.value.slice(0, field.selectionStart ?? field.value.length) + event.data
        + field.value.slice(field.selectionEnd ?? field.value.length);
    const at = text.lastIndexOf(separator);
    if (at >= 0 && text.slice(at + separator.length).replace(/\D/g, "").length > Number(field.dataset.decimals))
        event.preventDefault();
}, true);
