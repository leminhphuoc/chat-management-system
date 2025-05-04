using ChatSupportSystem.Application.Common.Interfaces;
using ChatSupportSystem.Application.Common.Models;
using MediatR;

namespace ChatSupportSystem.Application.Features.Agents.Queries.GetAvailableAgents;

public record GetAvailableAgentsQuery : IRequest<GetAvailableAgentsResponse>
{
    public bool IncludeAllAgents { get; init; } = false;
}

public class GetAvailableAgentsResponse
{
    public bool Success { get; init; }
    public List<AgentDto> Agents { get; init; } = new();
    public int TotalCapacity { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class GetAvailableAgentsQueryHandler : IRequestHandler<GetAvailableAgentsQuery, GetAvailableAgentsResponse>
{
    private readonly IAgentService _agentService;

    public GetAvailableAgentsQueryHandler(IAgentService agentService)
    {
        _agentService = agentService;
    }

    public async Task<GetAvailableAgentsResponse> Handle(
        GetAvailableAgentsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var agents = await _agentService.GetAvailableAgentsAsync(cancellationToken: cancellationToken);
            var totalCapacity = await _agentService.CalculateCurrentTeamCapacityAsync(cancellationToken);

            return new GetAvailableAgentsResponse
            {
                Success = true,
                Agents = agents.ToList(),
                TotalCapacity = totalCapacity,
                Message = $"Found {agents.Count()} available agents with total capacity {totalCapacity}"
            };
        }
        catch (Exception ex)
        {
            return new GetAvailableAgentsResponse
            {
                Success = false,
                Message = $"Failed to get available agents: {ex.Message}"
            };
        }
    }
}