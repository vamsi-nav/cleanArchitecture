namespace CleanArchitecture.Web.TodoArchive;

/// <summary>
/// Command message asking the todo archive module to archive the completed items of a list.
/// This is the single message type accepted by <see cref="TodoArchiveMessageConsumer"/>.
/// </summary>
/// <param name="TodoListId">The list whose completed items should be archived.</param>
/// <param name="RequestedBy">Identifier of the caller that raised the request.</param>
public sealed record ArchiveTodoListRequested(int TodoListId, string RequestedBy);
