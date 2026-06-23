namespace TelephonyCallService;

public class CleanupService : BackgroundService
{
    private readonly SessionRepository _repo;
    private readonly ILogger<CleanupService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public CleanupService(SessionRepository repo, ILogger<CleanupService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);
            var deleted = _repo.DeleteOlderThan(Interval);
            _logger.LogInformation("Cleanup: removed {Count} stale session(s)", deleted);
        }
    }
}
