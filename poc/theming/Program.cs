using System.Globalization;
using ThemeProto.Components;
using ThemeProto.Themes.Bubbles;
using ThemeProto.Themes.Festival;
using ThemeProto.Themes.Gazette;
using ThemeProto.Themes.Magazine;
using ThemeProto.Theming;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("fr-FR");

const string BubblesFonts = "https://fonts.googleapis.com/css2?family=Fraunces:opsz,wght@9..144,600;9..144,800&family=DM+Sans:wght@400;500;700&display=swap";
const string GazetteFonts = "https://fonts.googleapis.com/css2?family=Fraunces:ital,opsz,wght@0,9..144,400;0,9..144,600;0,9..144,800;1,9..144,600&family=DM+Sans:wght@400;500;700&display=swap";
const string MagazineFonts = "https://fonts.googleapis.com/css2?family=Anton&family=Libre+Franklin:ital,wght@0,400;0,600;0,800;1,800&display=swap";
const string FestivalFonts = "https://fonts.googleapis.com/css2?family=Bricolage+Grotesque:opsz,wght@12..96,400;12..96,600;12..96,800&display=swap";

// The root theme binds every slot; the others only declare what they change.
var violine = new Theme("violine", "Cases et bulles, violine", parent: null, BubblesFonts,
    SiteSlots.Header.Use<BubbleHeader>(),
    DashboardSlots.Layout.Use<BubbleDashboardLayout>(),
    DashboardSlots.Hero.Use<BubbleHero>(),
    DashboardSlots.NewArrivals.Use<BubbleNewArrivals>(),
    DashboardSlots.Value.Use<BubbleValue>(),
    DashboardSlots.Types.Use<BookPiles>(),
    DashboardSlots.Genres.Use<GenreTags>(),
    DashboardSlots.Publishers.Use<PublisherBars>(),
    AlbumsSlots.Layout.Use<BubbleAlbumsLayout>());

var mure = new Theme("mure", "Cases et bulles, mûre", parent: violine, fontsUrl: null);

var gazette = new Theme("gazette", "La gazette", parent: violine, GazetteFonts,
    SiteSlots.Header.Use<GazetteHeader>(),
    DashboardSlots.Layout.Use<GazetteDashboardLayout>(),
    DashboardSlots.Types.Use<ShelfOfSpines>());

// Keeps the base header and genres block (restyled), and never uses the hero slot: the cover replaces it.
var magazine = new Theme("magazine", "Le sommaire de magazine", parent: violine, MagazineFonts,
    DashboardSlots.Layout.Use<MagazineDashboardLayout>(),
    DashboardSlots.NewArrivals.Use<MagazineArrivals>(),
    DashboardSlots.Value.Use<MagazineValue>(),
    DashboardSlots.Types.Use<MagazineTypeBar>(),
    DashboardSlots.Publishers.Use<MagazinePodium>());

// Keeps the base header and value block (restyled); every other station is its own.
var festival = new Theme("festival", "Le festival en métro", parent: violine, FestivalFonts,
    DashboardSlots.Layout.Use<FestivalDashboardLayout>(),
    DashboardSlots.Hero.Use<FestivalPoster>(),
    DashboardSlots.NewArrivals.Use<FestivalProgramme>(),
    DashboardSlots.Types.Use<FestivalPavilions>(),
    DashboardSlots.Genres.Use<FestivalLines>(),
    DashboardSlots.Publishers.Use<FestivalExhibitors>());

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents();
builder.Services.AddSingleton(new ThemeCatalog([violine, mure, gazette, magazine, festival], AllSlots.Keys));
builder.Services.AddScoped<CurrentTheme>();

var app = builder.Build();
app.UseMiddleware<ThemeSelectionMiddleware>();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>();
app.Run();
