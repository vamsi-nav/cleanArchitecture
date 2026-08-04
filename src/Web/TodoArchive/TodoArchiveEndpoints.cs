using CleanArchitecture.Application.TodoArchive.Commands.PurgeArchivedTodoItem;
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
        groupBuilder.MapGet(GetPurgeApprovals, "approval");
        groupBuilder.MapPost(RequestPurge, "requests");
        groupBuilder.MapPost(ApprovePurge, "approval");
        groupBuilder.MapPut(RestoreArchivedItem, "{id}/restore");
        groupBuilder.MapDelete(PurgeArchivedItem, "{id}");
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

    [EndpointSummary("Get purge approval states")]
    [EndpointDescription("Lists every purge request with its current manual-approval decision.")]
    public static Ok<IReadOnlyList<TodoArchivePurgeState>> GetPurgeApprovals(TodoArchiveApprovalGate gate)
    {
        return TypedResults.Ok(gate.Pending());
    }

    /// <summary>
    /// Registers a purge request and leaves it parked, pending approval.
    /// </summary>
    /// <remarks>
    /// Registering does not start any processing. The request sits in
    /// <see cref="TodoArchiveApprovalGate"/> as unapproved until a human calls
    /// <see cref="ApprovePurge"/>; nothing else in the system will advance it.
    /// </remarks>
    [EndpointSummary("Request a purge")]
    [EndpointDescription("Registers a purge request for an archived todo item, pending manual approval.")]
    public static Accepted RequestPurge(TodoArchiveApprovalGate gate, TodoArchivePurgeRequest request)
    {
        gate.Register(request.TodoItemId);

        return TypedResults.Accepted($"/api/TodoArchive/{request.TodoItemId}");
    }

    /// <summary>
    /// Records the human decision on a pending purge request.
    /// </summary>
    /// <remarks>
    /// This is the manual step the delivery gate waits on. It is the only writer of the approval
    /// flag read by <see cref="PurgeArchivedItem"/>, and it exists purely so an operator can
    /// release that block; no automated caller sets it.
    /// </remarks>
    [EndpointSummary("Approve a purge")]
    [EndpointDescription("Records an operator's approval decision, releasing the manual gate on a purge.")]
    public static NoContent ApprovePurge(TodoArchiveApprovalGate gate, TodoArchivePurgeApproval approval)
    {
        gate.Approve(approval.TodoItemId, approval.Approved);

        return TypedResults.NoContent();
    }

    /// <summary>
    /// Purges an archived todo item.
    /// </summary>
    /// <remarks>
    /// This code path BLOCKS ON MANUAL APPROVAL. The archived-to-purged state transition does not
    /// proceed until a human has approved the request through <see cref="ApprovePurge"/>
    /// (POST /api/TodoArchive/approval), which sets the approval flag on
    /// <see cref="TodoArchiveApprovalGate"/>. While that flag is unset the request is rejected with
    /// 409 Conflict and the item is left untouched. There is no timeout, no automatic escalation
    /// and no workflow engine: the wait is resolved only by the operator's decision, so the caller
    /// is expected to poll until the approval lands.
    /// </remarks>
    [EndpointSummary("Purge an archived Todo Item")]
    [EndpointDescription("Deletes an archived todo item. Blocked with 409 Conflict until an operator approves the purge.")]
    public static async Task<Results<NoContent, Conflict<string>>> PurgeArchivedItem(
        ISender sender,
        TodoArchiveApprovalGate gate,
        int id)
    {
        // Manual gate: refuse to advance the state transition until a human has approved it.
        if (!gate.IsApproved(id))
        {
            return TypedResults.Conflict(
                $"Purge of todo item {id} is awaiting manual approval via POST /api/TodoArchive/approval.");
        }

        await sender.Send(new PurgeArchivedTodoItemCommand(id));

        return TypedResults.NoContent();
    }
}
