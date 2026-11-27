namespace Bdtheque.Currencies;

/// <summary>
/// The number of decimals of an amount in a currency: the minor unit ISO 4217 gives it
/// (fonctionnel/valorisation/gestion-des-devises.md, précision d'un montant), which the domain enforces and the frontend presents
/// and lets type (choix-implementation/domaine/representation-de-la-devise.md).
/// </summary>
public static class CurrencyDecimals
{
    /// <summary>
    /// The decimals of a currency ISO 4217 gives no minor unit: a withdrawn one (the franc), one whose minor unit is not
    /// applicable (gold), the old franc (<c>QZF</c>), or a code it does not know. Two is the minor unit of most currencies.
    /// </summary>
    public const int Default = 2;

    // The currencies whose minor unit is not two, from list one of ISO 4217, published by its maintenance agency, SIX
    // (list-one.xml, edition of 2026-09-17); those whose minor unit is not applicable (N.A.: precious metals, units of
    // account, testing and no-currency codes) are left to the default.
    private static readonly Dictionary<string, int> Exceptions = new(StringComparer.Ordinal)
    {
        ["BHD"] = 3, ["BIF"] = 0, ["CLF"] = 4, ["CLP"] = 0, ["DJF"] = 0, ["GNF"] = 0, ["IQD"] = 3, ["ISK"] = 0,
        ["JOD"] = 3, ["JPY"] = 0, ["KMF"] = 0, ["KRW"] = 0, ["KWD"] = 3, ["LYD"] = 3, ["OMR"] = 3, ["PYG"] = 0,
        ["RWF"] = 0, ["TND"] = 3, ["UGX"] = 0, ["UYI"] = 0, ["UYW"] = 4, ["VND"] = 0, ["VUV"] = 0, ["XAF"] = 0,
        ["XOF"] = 0, ["XPF"] = 0,
    };

    /// <summary>The number of decimals of an amount in <paramref name="currency"/> (a code of ISO 4217, or <c>QZF</c>).</summary>
    public static int Of(string currency) => Exceptions.GetValueOrDefault(currency, Default);

    /// <summary>Whether <paramref name="amount"/> has no more decimals than <paramref name="currency"/>; trailing zeros do not count.</summary>
    public static bool Fit(decimal amount, string currency) => decimal.Round(amount, Of(currency)) == amount;
}
