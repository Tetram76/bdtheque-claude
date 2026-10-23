namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The families of routes of the API (choix-implementation/api.md § Organisation de l'API), on
/// which each resource maps its own group: <c>/admin</c> for data entry, reserved by
/// <c>frontend</c> to the authenticated administrator, <c>/catalog</c> for public consultation, and
/// <c>/audience</c>, where <c>frontend</c> records the audience measurement for any user.
/// </summary>
/// <remarks>
/// Each family documents the error responses its endpoints can return, all of them ProblemDetails
/// whose <c>type</c> is the error category (see <see cref="Errors.ApiExceptionHandler"/>).
/// </remarks>
internal static class RouteGroups
{
    public static RouteGroupBuilder MapAdmin(this IEndpointRouteBuilder app) =>
        app.MapGroup("/admin")
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

    public static RouteGroupBuilder MapCatalog(this IEndpointRouteBuilder app) =>
        app.MapGroup("/catalog")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

    public static RouteGroupBuilder MapAudience(this IEndpointRouteBuilder app) =>
        app.MapGroup("/audience")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
}
