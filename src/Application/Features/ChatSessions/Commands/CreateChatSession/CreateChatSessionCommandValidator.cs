using FluentValidation;

namespace ChatSupportSystem.Application.Features.ChatSessions.Commands.CreateChatSession;

public class CreateChatSessionCommandValidator : AbstractValidator<CreateChatSessionCommand>
{
    public CreateChatSessionCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required")
            .MaximumLength(100).WithMessage("User ID must not exceed 100 characters");
    }
}