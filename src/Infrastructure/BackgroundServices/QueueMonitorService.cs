using ChatSupportSystem.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatSupportSystem.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that monitors the chat queue status and triggers actions based on thresholds
/// </summary>
public class QueueMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueueMonitorService> _logger;
    private readonly TimeSpan _monitorInterval;
    private readonly int _highLoadThreshold;
    private readonly int _criticalLoadThreshold;
    private bool _overflowTeamActivated = false;

    public QueueMonitorService(
        IServiceProvider serviceProvider,
        ILogger<QueueMonitorService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Get configuration settings
        _monitorInterval = TimeSpan.FromSeconds(
            configuration.GetValue<double>("QueueMonitor:CheckIntervalSeconds", 30));

        _highLoadThreshold = configuration.GetValue<int>("QueueMonitor:HighLoadThresholdPercent", 70);
        _criticalLoadThreshold = configuration.GetValue<int>("QueueMonitor:CriticalLoadThresholdPercent", 90);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queue Monitor Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorQueueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while monitoring queue");
            }

            await Task.Delay(_monitorInterval, stoppingToken);
        }

        _logger.LogInformation("Queue Monitor Service is stopping");
    }

    private async Task MonitorQueueAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var queueService = scope.ServiceProvider.GetRequiredService<IChatQueueService>();
        var agentService = scope.ServiceProvider.GetRequiredService<IAgentService>();

        var currentQueueSize = await queueService.GetQueueSizeAsync(stoppingToken);
        var maxQueueLength = await queueService.CalculateMaxQueueLengthAsync(stoppingToken);

        double occupancyPercentage = maxQueueLength > 0
            ? (double)currentQueueSize / maxQueueLength * 100
            : 0;

        _logger.LogInformation(
            "Queue status: {CurrentSize}/{MaxSize} ({OccupancyPercent:F1}%)",
            currentQueueSize,
            maxQueueLength,
            occupancyPercentage);

        if (occupancyPercentage >= _criticalLoadThreshold)
        {
            _logger.LogWarning(
                "CRITICAL QUEUE LOAD: {OccupancyPercent:F1}% - Queue has reached critical capacity",
                occupancyPercentage);

            // Activate overflow team if not already activated
            await EnsureOverflowTeamActivatedAsync(agentService, stoppingToken);
        }
        else if (occupancyPercentage >= _highLoadThreshold)
        {
            _logger.LogWarning(
                "HIGH QUEUE LOAD: {OccupancyPercent:F1}% - Queue is nearing capacity",
                occupancyPercentage);

            // Activate overflow team during office hours if high load
            var isWithinOfficeHours = agentService.IsWithinOfficeHoursAsync(stoppingToken);
            if (isWithinOfficeHours)
            {
                await EnsureOverflowTeamActivatedAsync(agentService, stoppingToken);
            }
        }
        else if (_overflowTeamActivated && occupancyPercentage < _highLoadThreshold / 2)
        {
            // Deactivate overflow team if queue load has significantly decreased
            _logger.LogInformation(
                "Queue load reduced to {OccupancyPercent:F1}% - Deactivating overflow team",
                occupancyPercentage);

            var deactivated = await agentService.DeactivateOverflowTeamAsync(stoppingToken);
            if (deactivated)
            {
                _overflowTeamActivated = false;
                _logger.LogInformation("Overflow team has been deactivated due to reduced queue load");
            }
        }

        await LogQueueMetricsAsync(queueService, agentService, stoppingToken);
    }

    private async Task EnsureOverflowTeamActivatedAsync(IAgentService agentService, CancellationToken stoppingToken)
    {
        if (!_overflowTeamActivated)
        {
            var activated = await agentService.ActivateOverflowTeamAsync(stoppingToken);
            if (activated)
            {
                _overflowTeamActivated = true;
                _logger.LogInformation("Overflow team has been activated due to high queue load");
            }
        }
    }

    private async Task LogQueueMetricsAsync(
        IChatQueueService queueService,
        IAgentService agentService,
        CancellationToken stoppingToken)
    {
        try
        {
            var now = DateTime.UtcNow;
            var availableAgents = await agentService.GetAvailableAgentsAsync(cancellationToken: stoppingToken);
            var teamCapacity = await agentService.CalculateCurrentTeamCapacityAsync(stoppingToken);
            var isWithinOfficeHours = agentService.IsWithinOfficeHoursAsync(stoppingToken);

            _logger.LogInformation(
                "Queue metrics at {Time}: Available Agents: {AgentCount}, " +
                "Team Capacity: {Capacity}, Office Hours: {OfficeHours}, " +
                "Overflow Active: {OverflowActive}",
                now.ToString("HH:mm:ss"),
                availableAgents.Count(),
                teamCapacity,
                isWithinOfficeHours ? "Yes" : "No",
                _overflowTeamActivated ? "Yes" : "No");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging queue metrics");
        }
    }
}