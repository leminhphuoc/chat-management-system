using AutoMapper;
using ChatSupportSystem.Application.Common.Interfaces;
using ChatSupportSystem.Application.Common.Models;
using ChatSupportSystem.Domain.Enums;
using MediatR;

namespace ChatSupportSystem.Application.Features.ChatSessions.Queries.PollChatSession;

public record PollChatSessionQuery : IRequest<PollChatSessionResponse>
{
    public int SessionId { get; init; }
}

public class PollChatSessionResponse
{
    public bool Success { get; init; }
    public SessionStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public ChatSessionDto? Session { get; init; }
}

public class PollChatSessionQueryHandler : IRequestHandler<PollChatSessionQuery, PollChatSessionResponse>
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IMapper _mapper;

    public PollChatSessionQueryHandler(
        IChatSessionRepository sessionRepository,
        IMapper mapper)
    {
        _sessionRepository = sessionRepository;
        _mapper = mapper;
    }

    public async Task<PollChatSessionResponse> Handle(
        PollChatSessionQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);

        if (session == null)
        {
            return new PollChatSessionResponse
            {
                Success = false,
                Status = SessionStatus.Invalid,
                Message = "Session not found"
            };
        }

        session.UpdateLastPollTime(DateTime.UtcNow);
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        return new PollChatSessionResponse
        {
            Success = true,
            Status = session.Status,
            Message = $"Session status: {session.Status}",
            Session = new ChatSessionDto
            {
                Id = session.Id,
                UserId = session.UserId,
                Status = session.Status,
                LastPollTime = session.LastPollTime,
                QueueEntryTime = session.QueueEntryTime,
                AssignedAgentId = session.AssignedAgentId
            }
        };
    }
}