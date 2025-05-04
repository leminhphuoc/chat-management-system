using FluentValidation;

namespace ChatSupportSystem.Application.Features.ChatSessions.Commands.MarkSessionInactive;

public class MarkSessionInactiveCommandValidator : AbstractValidator<MarkSessionInactiveCommand>
{
    public MarkSessionInactiveCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .GreaterThan(0).WithMessage("Session ID must be greater than 0");
    }
}