namespace ThemeProto.Data;

// Raw, theme-agnostic data. Links are computed here so that no theme can forget one.

public sealed record NavItem(string Label, string Href, bool Active);
public sealed record SiteHeaderData(IReadOnlyList<NavItem> Nav, int AlbumCount);

public sealed record HeroData(string Greeting, int Albums, int Series, int Editions);

public sealed record NewArrival(string AlbumLabel, string Publisher, DateOnly? AcquiredOn, string Href);
public sealed record NewArrivalsData(IReadOnlyList<NewArrival> Items, string AllAlbumsHref);

public sealed record Money(decimal Current, decimal Today);
public enum PriceKind { Average, Median, Min, Max }
public sealed record PriceStat(PriceKind Kind, string Label, Money Amount);
public sealed record ValueData(
    Money Estimated, int EstimatedEditions, Money Known, int KnownEditions,
    IReadOnlyList<PriceStat> Prices, int NotConvertible, int FreeEditions);

public enum AlbumKind { Regular, Omnibus, Special }
public sealed record TypeShare(AlbumKind Kind, string Label, int Count, int Percent);
public sealed record TypeDistributionData(IReadOnlyList<TypeShare> Shares);

public sealed record CountedLink(string Name, int Count, string Href);
// "Others" is an aggregate, not a genre or a publisher: kept apart so that no theme ranks it.
public sealed record GenresData(IReadOnlyList<CountedLink> Genres, CountedLink? Others);
public sealed record PublishersData(IReadOnlyList<CountedLink> Publishers, CountedLink? Others);

public sealed record DashboardData(
    HeroData Hero, NewArrivalsData NewArrivals, ValueData Value,
    TypeDistributionData Types, GenresData Genres, PublishersData Publishers);

public sealed record AlbumListData(IReadOnlyList<CountedLink> Albums);
