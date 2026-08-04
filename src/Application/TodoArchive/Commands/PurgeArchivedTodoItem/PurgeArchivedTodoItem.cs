using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;

namespace CleanArchitecture.Application.TodoArchive.Commands.PurgeArchivedTodoItem;

[Authorize]
public record PurgeArchivedTodoItemCommand(int Id) : IRequest;

/// <summary>
/// Deletes an archived <see cref="Domain.Entities.TodoItem"/> from the shared TodoItems table.
/// This is the terminal state transition of the archive module and is only ever dispatched
/// after the manual approval gate has been satisfied by a human decision.
/// </summary>
public class PurgeArchivedTodoItemCommandHandler : IRequestHandler<PurgeArchivedTodoItemCommand>
{
    private readonly IApplicationDbContext _context;

    public PurgeArchivedTodoItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(PurgeArchivedTodoItemCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.TodoItems
            .FindAsync([request.Id], cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        _context.TodoItems.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
