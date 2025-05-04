using ChatSupportSystem.Application.Common.Interfaces;
using MediatR;

namespace ChatSupportSystem.Application.Features.ChatSessions.Commands.MarkSessionInactive;

public record MarkSessionInactiveCommand : IRequest<MarkSessionInactiveResponse>
{
    public int SessionId { get; init; }
}

public class MarkSessionInactiveResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class MarkSessionInactiveCommandHandler : IRequestHandler<MarkSessionInactiveCommand, MarkSessionInactiveResponse>
{
    private readonly IChatSessionRepository _sessionRepository;

    public MarkSessionInactiveCommandHandler(IChatSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<MarkSessionInactiveResponse> Handle(
        MarkSessionInactiveCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);

            if (session == null)
            {
                return new MarkSessionInactiveResponse
                {
                    Success = false,
                    Message = $"Session with ID {request.SessionId} not found"
                };
            }

            session.MarkInactive();
            await _sessionRepository.UpdateAsync(session, cancellationToken);

            return new MarkSessionInactiveResponse
            {
                Success = true,
                Message = $"Session {request.SessionId} marked as inactive"
            };
        }
        catch (Exception ex)
        {
            return new MarkSessionInactiveResponse
            {
                Success = false,
                Message = $"Failed to mark session as inactive: {ex.Message}"
            };
        }
    }
}