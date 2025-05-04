using ChatSupportSystem.Application.Common.Interfaces;
using ChatSupportSystem.Application.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatSupportSystem.Infrastructure.BackgroundServices;

public class SessionInactivityService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionInactivityService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(1);
    private readonly TimeSpan _inactivityThreshold;

    public SessionInactivityService(
        IServiceProvider serviceProvider,
        ILogger<SessionInactivityService> logger,
        IOptions<QueueSettings> queueSettings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _inactivityThreshold = TimeSpan.FromSeconds(queueSettings.Value.InactivityThresholdSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session Inactivity Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorSessionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while monitoring sessions");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Session Inactivity Service is stopping");
    }

    private async Task MonitorSessionsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<IChatSessionRepository>();

        var activeSessions = await sessionRepository.GetActiveSessionsAsync(stoppingToken);
        var queuedSessions = await sessionRepository.GetQueuedSessionsAsync(stoppingToken);

        var sessionsToCheck = activeSessions.Concat(queuedSessions).ToList();
        var now = DateTime.UtcNow;

        foreach (var session in sessionsToCheck)
        {
            var timeSinceLastPoll = now - session.LastPollTime;

            if (timeSinceLastPoll > _inactivityThreshold)
            {
                _logger.LogInformation("Session {SessionId} inactive for {Seconds}s, marking as inactive",
                    session.Id, timeSinceLastPoll.TotalSeconds);

                session.MarkInactive();
                await sessionRepository.UpdateAsync(session, stoppingToken);
            }
        }
    }
}