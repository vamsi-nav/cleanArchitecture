namespace CleanArchitecture.Web.TodoArchive;

/// <summary>
/// Scheduled entry point for the todo archive module.
/// <para>
/// Wakes on a fixed interval and records that a reconciliation pass is due. The pass itself is
/// deliberately not performed here: this worker only owns the schedule, so it stays free of I/O
/// and can start in any environment (including tests) without touching the database.
/// </para>
/// </summary>
public class TodoArchiveReconciliationWorker : BackgroundService
{
    /// <summary>How often a reconciliation pass becomes due.</summary>
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly ILogger<TodoArchiveReconciliationWorker> _logger;

    public TodoArchiveReconciliationWorker(ILogger<TodoArchiveReconciliationWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Todo archive reconciliation worker started with a {IntervalHours}h interval.",
            Interval.TotalHours);

        using var timer = new PeriodicTimer(Interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _logger.LogInformation(
                    "Todo archive reconciliation pass due at {DueAt:o}.",
                    DateTimeOffset.UtcNow);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on host shutdown.
        }

        _logger.LogInformation("Todo archive reconciliation worker stopped.");
    }
}
