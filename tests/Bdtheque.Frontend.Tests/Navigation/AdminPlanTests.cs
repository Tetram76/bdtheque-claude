using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Tests.Navigation;

/// <summary>
/// The plan of the administration (visuel/administration/en-tete-et-menu.md), whose order the menu and the cards of the home
/// follow (#477).
/// </summary>
public sealed class AdminPlanTests
{
    [Fact]
    public void PlaceOf_APageOfThePlan_IsItsPlaceAmongAllTheEntries()
    {
        // The series before their albums, the albums before their editions (#477).
        Assert.Equal(
            [0, 1, 2, 3],
            new[] { AppPaths.AdminHome, AppPaths.AdminSeries, AppPaths.AdminAlbums, AppPaths.AdminEditions }.Select(AdminPlan.PlaceOf));
    }

    [Fact]
    public void PlaceOf_APageOutsideThePlan_IsRefused()
    {
        // A page the plan does not have would have no place among the others: refused rather than put first or last.
        Assert.Throws<ArgumentException>(() => AdminPlan.PlaceOf(AppPaths.Home));
    }
}
