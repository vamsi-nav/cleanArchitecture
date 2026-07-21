using CleanArchitecture.Application.TodoArchive.Commands.RestoreArchivedTodoItem;
using CleanArchitecture.Application.TodoArchive.Queries.GetArchivedTodoItems;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CleanArchitecture.Web.TodoArchive;

/// <summary>
/// Http entry points for the todo archive module. The route prefix is set explicitly so the
/// paths stay <c>/api/TodoArchive/*</c> rather than following the class name.
/// </summary>
public class TodoArchiveEndpoints : IEndpointGroup
{
    public static string? RoutePrefix => "/api/TodoArchive";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetArchivedItems);
        groupBuilder.MapPut(RestoreArchivedItem, "{id}/restore");
    }

    [EndpointSummary("Get archived Todo Items")]
    [EndpointDescription("Retrieves the completed todo items held by the archive module.")]
    public static async Task<Ok<IReadOnlyList<ArchivedTodoItemDto>>> GetArchivedItems(ISender sender)
    {
        var items = await sender.Send(new GetArchivedTodoItemsQuery());

        return TypedResults.Ok(items);
    }

    [EndpointSummary("Restore an archived Todo Item")]
    [EndpointDescription("Returns an archived todo item to the active list by clearing its done flag.")]
    public static async Task<NoContent> RestoreArchivedItem(ISender sender, int id)
    {
        await sender.Send(new RestoreArchivedTodoItemCommand(id));

        return TypedResults.NoContent();
    }
}
