using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;

namespace CleanArchitecture.Application.TodoArchive.Queries.GetArchivedTodoItems;

[Authorize]
public record GetArchivedTodoItemsQuery : IRequest<IReadOnlyList<ArchivedTodoItemDto>>;

/// <summary>
/// Reads completed <see cref="Domain.Entities.TodoItem"/> rows straight from the shared
/// DbContext. The archive module owns this read path outright: it does not route through the
/// TodoItems or TodoLists handlers, so the TodoItems table has two independent readers.
/// </summary>
public class GetArchivedTodoItemsQueryHandler
    : IRequestHandler<GetArchivedTodoItemsQuery, IReadOnlyList<ArchivedTodoItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetArchivedTodoItemsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ArchivedTodoItemDto>> Handle(
        GetArchivedTodoItemsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.TodoItems
            .AsNoTracking()
            .Where(item => item.Done)
            .OrderByDescending(item => item.LastModified)
            .Select(item => new ArchivedTodoItemDto
            {
                Id = item.Id,
                ListId = item.ListId,
                Title = item.Title,
                Note = item.Note,
                ArchivedAt = item.LastModified
            })
            .ToListAsync(cancellationToken);
    }
}
