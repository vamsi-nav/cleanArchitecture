using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;

namespace CleanArchitecture.Application.TodoArchive.Commands.RestoreArchivedTodoItem;

[Authorize]
public record RestoreArchivedTodoItemCommand(int Id) : IRequest;

/// <summary>
/// Writes a completed <see cref="Domain.Entities.TodoItem"/> back to active by clearing its
/// Done flag, mutating the same mapped TodoItems rows the TodoItems module writes. The archive
/// module owns this write path directly against the shared DbContext.
/// </summary>
public class RestoreArchivedTodoItemCommandHandler : IRequestHandler<RestoreArchivedTodoItemCommand>
{
    private readonly IApplicationDbContext _context;

    public RestoreArchivedTodoItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(RestoreArchivedTodoItemCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.TodoItems
            .FindAsync([request.Id], cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Done = false;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
