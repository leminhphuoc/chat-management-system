using ChatSupportSystem.Application.Features.ChatSessions.Commands.CreateChatSession;
using FluentValidation.TestHelper;

namespace ChatSupportSystem.Application.UnitTests.Features.ChatSessions;

public class CreateChatSessionCommandValidatorTests
{
    private readonly CreateChatSessionCommandValidator _validator;

    public CreateChatSessionCommandValidatorTests()
    {
        _validator = new CreateChatSessionCommandValidator();
    }

    [Fact]
    public void ShouldHaveError_WhenUserIdIsEmpty()
    {
        // Arrange
        var command = new CreateChatSessionCommand { UserId = string.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public void ShouldHaveError_WhenUserIdExceedsMaxLength()
    {
        // Arrange
        var command = new CreateChatSessionCommand { UserId = new string('a', 101) };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public void ShouldNotHaveError_WhenUserIdIsValid()
    {
        // Arrange
        var command = new CreateChatSessionCommand { UserId = "valid-user-id" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(c => c.UserId);
    }
}