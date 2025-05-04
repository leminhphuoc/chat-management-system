using ChatSupportSystem.Application.Common.Interfaces;
using MediatR;

namespace ChatSupportSystem.Application.Features.Agents.Commands.UpdateAgentAvailability;

public record UpdateAgentAvailabilityCommand : IRequest<UpdateAgentAvailabilityResponse>
{
    public int AgentId { get; init; }
    public bool IsActive { get; init; }
}

public class UpdateAgentAvailabilityResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class UpdateAgentAvailabilityCommandHandler : IRequestHandler<UpdateAgentAvailabilityCommand, UpdateAgentAvailabilityResponse>
{
    private readonly IAgentService _agentService;

    public UpdateAgentAvailabilityCommandHandler(IAgentService agentService)
    {
        _agentService = agentService;
    }

    public async Task<UpdateAgentAvailabilityResponse> Handle(
        UpdateAgentAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var agent = await _agentService.GetAgentByIdAsync(request.AgentId, cancellationToken);

            if (agent == null)
            {
                return new UpdateAgentAvailabilityResponse
                {
                    Success = false,
                    Message = $"Agent with ID {request.AgentId} not found"
                };
            }

            agent.IsActive = request.IsActive;
            await _agentService.UpdateAgentAsync(agent, cancellationToken);

            return new UpdateAgentAvailabilityResponse
            {
                Success = true,
                Message = $"Agent {request.AgentId} availability updated to {request.IsActive}"
            };
        }
        catch (Exception ex)
        {
            return new UpdateAgentAvailabilityResponse
            {
                Success = false,
                Message = $"Failed to update agent availability: {ex.Message}"
            };
        }
    }
}