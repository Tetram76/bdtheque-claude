using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// The acquisition of an edition and its value, written as a whole: each of these fields depends on
/// another — an amount requires an acquisition mode and a reference date, a free edition has no
/// amount, a purchase is never free (modele-metier.md § Édition) — so that separate setters would
/// succeed or fail depending on the order they are called in.
/// </summary>
/// <param name="Mode"><c>null</c> for an edition not owned, which then carries neither date nor amount.</param>
/// <param name="PriceAmount">Together with <paramref name="PriceCurrency"/>, or not at all.</param>
/// <param name="InitialValueAmount">Together with <paramref name="InitialValueCurrency"/>, or not at all.</param>
public sealed record EditionAcquisition(
    AcquisitionMode? Mode,
    DateOnly? Date,
    decimal? PriceAmount,
    string? PriceCurrency,
    bool IsFree,
    decimal? InitialValueAmount,
    string? InitialValueCurrency);
