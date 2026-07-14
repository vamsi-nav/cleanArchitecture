namespace CleanArchitecture.Web.TodoArchive;

/// <summary>
/// Message entry point for the todo archive module.
/// <para>
/// <see cref="HandleAsync"/> is the module's only public handler and takes a single message type,
/// <see cref="ArchiveTodoListRequested"/>. A transport adapter dispatches to it; the consumer
/// itself is registered in DI and holds no transport concerns, so it stays free of I/O.
/// </para>
/// </summary>
public class TodoArchiveMessageConsumer
{
    private readonly ILogger<TodoArchiveMessageConsumer> _logger;

    public TodoArchiveMessageConsumer(ILogger<TodoArchiveMessageConsumer> logger)
    {
        _logger = logger;
    }

    /// <summary>Handles one <see cref="ArchiveTodoListRequested"/> message.</summary>
    public Task HandleAsync(ArchiveTodoListRequested message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Received archive request for todo list {TodoListId} from {RequestedBy}.",
            message.TodoListId,
            message.RequestedBy);

        return Task.CompletedTask;
    }
}
