using ChatSupportSystem.Application.Common.Interfaces;
using MediatR;

namespace ChatSupportSystem.Application.Features.Queue.Queries.GetQueueStatus;

public record GetQueueStatusQuery : IRequest<GetQueueStatusResponse>;

public class GetQueueStatusResponse
{
    public bool Success { get; init; }
    public int CurrentQueueSize { get; init; }
    public int MaxQueueLength { get; init; }
    public bool IsWithinOfficeHours { get; init; }
    public bool IsOverflowTeamActive { get; init; }
    public double QueueOccupancyPercentage { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class GetQueueStatusQueryHandler : IRequestHandler<GetQueueStatusQuery, GetQueueStatusResponse>
{
    private readonly IChatQueueService _queueService;
    private readonly IAgentService _agentService;

    public GetQueueStatusQueryHandler(
        IChatQueueService queueService,
        IAgentService agentService)
    {
        _queueService = queueService;
        _agentService = agentService;
    }

    public async Task<GetQueueStatusResponse> Handle(
        GetQueueStatusQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentQueueSize = await _queueService.GetQueueSizeAsync(cancellationToken);
            var maxQueueLength = await _queueService.CalculateMaxQueueLengthAsync(cancellationToken);
            var isWithinOfficeHours = _agentService.IsWithinOfficeHoursAsync(cancellationToken);

            var availableAgents = await _agentService.GetAvailableAgentsAsync(cancellationToken: cancellationToken);
            var isOverflowTeamActive = availableAgents.Any(a => a.IsOverflowAgent && a.IsActive);

            var queueOccupancyPercentage = maxQueueLength > 0
                ? Math.Round(((double)currentQueueSize / maxQueueLength) * 100, 2)
                : 0;

            return new GetQueueStatusResponse
            {
                Success = true,
                CurrentQueueSize = currentQueueSize,
                MaxQueueLength = maxQueueLength,
                IsWithinOfficeHours = isWithinOfficeHours,
                IsOverflowTeamActive = isOverflowTeamActive,
                QueueOccupancyPercentage = queueOccupancyPercentage,
                Message = $"Queue is at {queueOccupancyPercentage}% capacity ({currentQueueSize}/{maxQueueLength})"
            };
        }
        catch (Exception ex)
        {
            return new GetQueueStatusResponse
            {
                Success = false,
                Message = $"Failed to get queue status: {ex.Message}"
            };
        }
    }
}