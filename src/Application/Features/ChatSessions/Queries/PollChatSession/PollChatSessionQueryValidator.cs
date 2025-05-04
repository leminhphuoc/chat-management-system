using FluentValidation;

namespace ChatSupportSystem.Application.Features.ChatSessions.Queries.PollChatSession;

public class PollChatSessionQueryValidator : AbstractValidator<PollChatSessionQuery>
{
    public PollChatSessionQueryValidator()
    {
        RuleFor(x => x.SessionId)
            .GreaterThan(0).WithMessage("Session ID must be greater than 0");
    }
}