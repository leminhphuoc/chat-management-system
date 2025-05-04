using ChatSupportSystem.Application.Common.Interfaces;
using ChatSupportSystem.Domain.Entities;
using MediatR;

namespace ChatSupportSystem.Application.Features.ChatSessions.Commands.CreateChatSession;

public record CreateChatSessionCommand : IRequest<CreateChatSessionResponse>
{
    public string UserId { get; init; } = string.Empty;
}

public class CreateChatSessionResponse
{
    public bool Success { get; init; }
    public int? SessionId { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class CreateChatSessionCommandHandler : IRequestHandler<CreateChatSessionCommand, CreateChatSessionResponse>
{
    private readonly IChatQueueService _queueService;
    private readonly IChatSessionRepository _sessionRepository;

    public CreateChatSessionCommandHandler(
        IChatQueueService queueService,
        IChatSessionRepository sessionRepository)
    {
        _queueService = queueService;
        _sessionRepository = sessionRepository;
    }

    public async Task<CreateChatSessionResponse> Handle(
        CreateChatSessionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if the queue has space
            var canEnqueue = await _queueService.CanEnqueueSessionAsync(cancellationToken);

            if (!canEnqueue)
            {
                return new CreateChatSessionResponse
                {
                    Success = false,
                    Message = "Queue is full. Please try again later."
                };
            }

            // Create a new chat session
            var session = new ChatSession(request.UserId);

            // Save to repository
            var sessionId = await _sessionRepository.AddAsync(session, cancellationToken);

            // Enqueue the session
            await _queueService.EnqueueSessionAsync(sessionId, cancellationToken);

            return new CreateChatSessionResponse
            {
                Success = true,
                SessionId = sessionId,
                Message = "Chat session created successfully."
            };
        }
        catch (Exception ex)
        {
            return new CreateChatSessionResponse
            {
                Success = false,
                Message = $"Failed to create chat session: {ex.Message}"
            };
        }
    }
}