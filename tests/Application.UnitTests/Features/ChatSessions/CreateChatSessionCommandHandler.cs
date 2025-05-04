using ChatSupportSystem.Application.Common.Interfaces;
using ChatSupportSystem.Application.Features.ChatSessions.Commands.CreateChatSession;
using ChatSupportSystem.Domain.Entities;
using Moq;

namespace ChatSupportSystem.Application.UnitTests.Features.ChatSessions.Commands.CreateChatSession;

public class CreateChatSessionCommandHandlerTests
{
    private readonly Mock<IChatQueueService> _mockQueueService;
    private readonly Mock<IChatSessionRepository> _mockSessionRepository;
    private readonly CreateChatSessionCommandHandler _handler;

    public CreateChatSessionCommandHandlerTests()
    {
        _mockQueueService = new Mock<IChatQueueService>();
        _mockSessionRepository = new Mock<IChatSessionRepository>();

        _handler = new CreateChatSessionCommandHandler(
            _mockQueueService.Object,
            _mockSessionRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenSessionCreatedAndEnqueued()
    {
        // Arrange
        var command = new CreateChatSessionCommand { UserId = "user123" };

        _mockQueueService.Setup(q => q.CanEnqueueSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockSessionRepository.Setup(r => r.AddAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(42, result.SessionId);
        Assert.Contains("successfully", result.Message);

        _mockSessionRepository.Verify(r => r.AddAsync(It.Is<ChatSession>(s => s.UserId == "user123"), It.IsAny<CancellationToken>()), Times.Once);
        _mockQueueService.Verify(q => q.EnqueueSessionAsync(42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenQueueIsFull()
    {
        // Arrange
        var command = new CreateChatSessionCommand { UserId = "user123" };

        _mockQueueService.Setup(q => q.CanEnqueueSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.SessionId);
        Assert.Contains("Queue is full", result.Message);

        _mockSessionRepository.Verify(r => r.AddAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}