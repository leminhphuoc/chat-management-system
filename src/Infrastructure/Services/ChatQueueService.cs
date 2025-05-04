using ChatSupportSystem.Application.Common.Interfaces;
using ChatSupportSystem.Application.Common.Models;
using ChatSupportSystem.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatSupportSystem.Infrastructure.Services;

public class ChatQueueService : IChatQueueService
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IAgentService _agentService;
    private readonly ILogger<ChatQueueService> _logger;
    private readonly QueueSettings _queueSettings;

    public ChatQueueService(
        IChatSessionRepository sessionRepository,
        IAgentService agentService,
        ILogger<ChatQueueService> logger,
        IOptions<QueueSettings> queueSettings)
    {
        _sessionRepository = sessionRepository;
        _agentService = agentService;
        _logger = logger;
        _queueSettings = queueSettings.Value;
    }

    public async Task<bool> CanEnqueueSessionAsync(CancellationToken cancellationToken = default)
    {
        var currentQueueSize = await GetQueueSizeAsync(cancellationToken);
        var maxQueueLength = await CalculateMaxQueueLengthAsync(cancellationToken);

        if (currentQueueSize < maxQueueLength)
        {
            return true;
        }

        var isOfficeHours = _agentService.IsWithinOfficeHoursAsync(cancellationToken);

        if (isOfficeHours)
        {
            var overflowActivated = await _agentService.ActivateOverflowTeamAsync(cancellationToken);

            if (overflowActivated)
            {
                maxQueueLength = await CalculateMaxQueueLengthAsync(cancellationToken);
                return currentQueueSize < maxQueueLength;
            }
        }

        return false;
    }

    public async Task EnqueueSessionAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        if (!await CanEnqueueSessionAsync(cancellationToken))
        {
            throw new QueueFullException("Cannot enqueue session, queue is full");
        }

        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);

        if (session == null)
        {
            throw new ApplicationException($"Session with ID {sessionId} not found");
        }

        _logger.LogInformation("Session {SessionId} enqueued successfully at {Time}",
            sessionId, DateTime.UtcNow);
    }

    public async Task<int> GetQueueSizeAsync(CancellationToken cancellationToken = default)
    {
        return await _sessionRepository.CountQueuedSessionsAsync(cancellationToken);
    }

    public async Task<int> CalculateMaxQueueLengthAsync(CancellationToken cancellationToken = default)
    {
        var teamCapacity = await _agentService.CalculateCurrentTeamCapacityAsync(cancellationToken);
        return (int)Math.Floor(teamCapacity * _queueSettings.QueueCapacityMultiplier);
    }

    public async Task<ChatSessionDto?> DequeueSessionAsync(CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetOldestQueuedSessionAsync(cancellationToken);

        if (session == null)
        {
            return null;
        }

        return new ChatSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            Status = session.Status,
            LastPollTime = session.LastPollTime,
            QueueEntryTime = session.QueueEntryTime,
            AssignedAgentId = session.AssignedAgentId
        };
    }

    public async Task CheckOverflowTeamStatusAsync(CancellationToken cancellationToken = default)
    {
        var queueSize = await GetQueueSizeAsync(cancellationToken);

        var teamCapacity = await _agentService.CalculateCurrentTeamCapacityAsync(cancellationToken);
        var maxQueueLength = await CalculateMaxQueueLengthAsync(cancellationToken);

        var isOfficeHours = _agentService.IsWithinOfficeHoursAsync(cancellationToken);

        if (isOfficeHours)
        {
            if (queueSize >= maxQueueLength * 0.8) // 80% of max capacity as threshold
            {
                await _agentService.ActivateOverflowTeamAsync(cancellationToken);
                _logger.LogInformation("Activated overflow team due to high queue volume: {QueueSize}/{MaxQueueLength}", queueSize, maxQueueLength);
            }
            else if (queueSize <= maxQueueLength * 0.3) // 30% of max capacity as threshold
            {
                var result = await _agentService.DeactivateOverflowTeamAsync(cancellationToken);
                if (result)
                {
                    _logger.LogInformation("Deactivated overflow team due to reduced queue volume: {QueueSize}/{MaxQueueLength}", queueSize, maxQueueLength);
                }
            }
        }
        else
        {
            await _agentService.DeactivateOverflowTeamAsync(cancellationToken);
        }
    }
}