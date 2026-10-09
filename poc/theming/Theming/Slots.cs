using ThemeProto.Data;

namespace ThemeProto.Theming;

// Every slot a theme can fill. A page renders its layout slot; the layout places the blocks.

public static class SiteSlots
{
    public static readonly Slot<SiteHeaderData> Header = new("site.header");
}

public static class DashboardSlots
{
    public static readonly Slot<DashboardData> Layout = new("dashboard.layout");
    public static readonly Slot<HeroData> Hero = new("dashboard.hero");
    public static readonly Slot<NewArrivalsData> NewArrivals = new("dashboard.new-arrivals");
    public static readonly Slot<ValueData> Value = new("dashboard.value");
    public static readonly Slot<TypeDistributionData> Types = new("dashboard.types");
    public static readonly Slot<GenresData> Genres = new("dashboard.genres");
    public static readonly Slot<PublishersData> Publishers = new("dashboard.publishers");
}

public static class AlbumsSlots
{
    public static readonly Slot<AlbumListData> Layout = new("albums.layout");
}

public static class AllSlots
{
    public static readonly string[] Keys =
    [
        SiteSlots.Header.Key,
        DashboardSlots.Layout.Key, DashboardSlots.Hero.Key, DashboardSlots.NewArrivals.Key, DashboardSlots.Value.Key,
        DashboardSlots.Types.Key, DashboardSlots.Genres.Key, DashboardSlots.Publishers.Key,
        AlbumsSlots.Layout.Key,
    ];
}
