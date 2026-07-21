namespace CleanArchitecture.Application.TodoArchive.Queries.GetArchivedTodoItems;

/// <summary>
/// Archive-module projection of a <see cref="Domain.Entities.TodoItem"/>. Shaped by the archive
/// module for its own screens, independently of the TodoLists projections of the same entity.
/// </summary>
public class ArchivedTodoItemDto
{
    public int Id { get; init; }

    public int ListId { get; init; }

    public string? Title { get; init; }

    public string? Note { get; init; }

    public DateTimeOffset ArchivedAt { get; init; }
}
