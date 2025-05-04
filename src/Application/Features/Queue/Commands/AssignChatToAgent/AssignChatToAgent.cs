using ChatSupportSystem.Application.Common.Interfaces;
using ChatSupportSystem.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Options;

namespace ChatSupportSystem.Application.Features.Queue.Commands.AssignChatToAgent;

public record AssignChatToAgentCommand : IRequest<AssignChatToAgentResponse>
{
    public int SessionId { get; init; }
    public int AgentId { get; init; }
}

public class AssignChatToAgentResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class AssignChatToAgentCommandHandler : IRequestHandler<AssignChatToAgentCommand, AssignChatToAgentResponse>
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IAgentService _agentService;
    private readonly QueueSettings _queueSettings;

    public AssignChatToAgentCommandHandler(
        IChatSessionRepository sessionRepository,
        IAgentService agentService,
        IOptions<QueueSettings> queueSettings)
    {
        _sessionRepository = sessionRepository;
        _agentService = agentService;
        _queueSettings = queueSettings.Value;
    }

    public async Task<AssignChatToAgentResponse> Handle(
        AssignChatToAgentCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);

            if (session == null)
            {
                return new AssignChatToAgentResponse
                {
                    Success = false,
                    Message = $"Session with ID {request.SessionId} not found"
                };
            }

            var agent = await _agentService.GetAgentByIdAsync(request.AgentId, cancellationToken);

            if (agent == null)
            {
                return new AssignChatToAgentResponse
                {
                    Success = false,
                    Message = $"Agent with ID {request.AgentId} not found"
                };
            }

            if (!agent.CanAcceptChat())
            {
                return new AssignChatToAgentResponse
                {
                    Success = false,
                    Message = $"Agent {request.AgentId} cannot accept more chats at this time"
                };
            }

            session.AssignToAgent(agent);
            agent.AssignChat(_queueSettings.MaxConcurrentChatsPerAgent);

            await _sessionRepository.UpdateAsync(session, cancellationToken);
            await _agentService.UpdateAgentAsync(agent, cancellationToken);

            return new AssignChatToAgentResponse
            {
                Success = true,
                Message = $"Session {request.SessionId} assigned to agent {request.AgentId}"
            };
        }
        catch (Exception ex)
        {
            return new AssignChatToAgentResponse
            {
                Success = false,
                Message = $"Failed to assign chat to agent: {ex.Message}"
            };
        }
    }
}