using FluentValidation;

namespace ChatSupportSystem.Application.Features.Queue.Commands.AssignChatToAgent;

public class AssignChatToAgentCommandValidator : AbstractValidator<AssignChatToAgentCommand>
{
    public AssignChatToAgentCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .GreaterThan(0).WithMessage("Session ID must be greater than 0");

        RuleFor(x => x.AgentId)
            .GreaterThan(0).WithMessage("Agent ID must be greater than 0");
    }
}