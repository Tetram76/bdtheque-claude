namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The two families of routes of the API (choix-implementation.md § Organisation de l'API), on
/// which each resource maps its own group: <c>/admin</c> for data entry, reserved by
/// <c>frontend</c> to the authenticated administrator, and <c>/catalog</c> for public consultation.
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
}
