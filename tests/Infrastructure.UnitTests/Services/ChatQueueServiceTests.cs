using ChatSupportSystem.Application.Common.Interfaces;
using ChatSupportSystem.Application.Common.Models;
using ChatSupportSystem.Domain.Entities;
using ChatSupportSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ChatSupportSystem.Infrastructure.UnitTests.Services;

public class ChatQueueServiceTests
{
    private readonly Mock<IChatSessionRepository> _mockSessionRepository;
    private readonly Mock<IAgentService> _mockAgentService;
    private readonly Mock<ILogger<ChatQueueService>> _mockLogger;
    private readonly Mock<IOptions<QueueSettings>> _mockQueueSettings;
    private readonly ChatQueueService _chatQueueService;

    public ChatQueueServiceTests()
    {
        _mockSessionRepository = new Mock<IChatSessionRepository>();
        _mockAgentService = new Mock<IAgentService>();
        _mockLogger = new Mock<ILogger<ChatQueueService>>();

        var queueSettings = new QueueSettings
        {
            MaxConcurrentChatsPerAgent = 10,
            QueueCapacityMultiplier = 1.5,
            InactivityThresholdSeconds = 3
        };

        _mockQueueSettings = new Mock<IOptions<QueueSettings>>();
        _mockQueueSettings.Setup(x => x.Value).Returns(queueSettings);

        _chatQueueService = new ChatQueueService(
            _mockSessionRepository.Object,
            _mockAgentService.Object,
            _mockLogger.Object,
            _mockQueueSettings.Object);
    }

    [Fact]
    public async Task CanEnqueueSessionAsync_ShouldReturnTrue_WhenQueueHasSpace()
    {
        // Arrange
        _mockSessionRepository.Setup(r => r.CountQueuedSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        _mockAgentService.Setup(s => s.CalculateCurrentTeamCapacityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        // Act
        var result = await _chatQueueService.CanEnqueueSessionAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanEnqueueSessionAsync_ShouldReturnFalse_WhenQueueIsFull_AndOutsideOfficeHours()
    {
        // Arrange
        _mockSessionRepository.Setup(r => r.CountQueuedSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(15);

        _mockAgentService.Setup(s => s.CalculateCurrentTeamCapacityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        _mockAgentService.Setup(s => s.IsWithinOfficeHoursAsync(It.IsAny<CancellationToken>()))
            .Returns(false);

        // Act
        var result = await _chatQueueService.CanEnqueueSessionAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanEnqueueSessionAsync_ShouldActivateOverflow_WhenQueueIsFull_AndInOfficeHours()
    {
        // Arrange
        _mockSessionRepository.Setup(r => r.CountQueuedSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(16);

        int callCount = 0;
        _mockAgentService.Setup(s => s.CalculateCurrentTeamCapacityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? 10 : 20;
            });

        _mockAgentService.Setup(s => s.IsWithinOfficeHoursAsync(It.IsAny<CancellationToken>()))
            .Returns(true);

        _mockAgentService.Setup(s => s.ActivateOverflowTeamAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _chatQueueService.CanEnqueueSessionAsync();

        // Assert
        Assert.True(result);
        _mockAgentService.Verify(s => s.ActivateOverflowTeamAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CanEnqueueSessionAsync_ShouldReturnFalse_WhenMainAndOverflowQueuesAreFull()
    {
        // Arrange
        _mockSessionRepository.Setup(r => r.CountQueuedSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(35);

        _mockAgentService.Setup(s => s.CalculateCurrentTeamCapacityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(20);

        _mockAgentService.Setup(s => s.IsWithinOfficeHoursAsync(It.IsAny<CancellationToken>()))
            .Returns(true);

        _mockAgentService.Setup(s => s.ActivateOverflowTeamAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _chatQueueService.CanEnqueueSessionAsync();

        // Assert
        Assert.False(result);
        _mockAgentService.Verify(s => s.ActivateOverflowTeamAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetQueueSizeAsync_ShouldReturnCorrectSize()
    {
        // Arrange
        _mockSessionRepository.Setup(r => r.CountQueuedSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        // Act
        var result = await _chatQueueService.GetQueueSizeAsync();

        // Assert
        Assert.Equal(7, result);
    }

    [Fact]
    public async Task CalculateMaxQueueLengthAsync_ShouldCalculateCorrectly()
    {
        // Arrange
        _mockAgentService.Setup(s => s.CalculateCurrentTeamCapacityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        // Act
        var result = await _chatQueueService.CalculateMaxQueueLengthAsync();

        // Assert
        Assert.Equal(15, result); // 10 * 1.5 = 15
    }

    [Fact]
    public async Task CalculateMaxQueueLengthAsync_ShouldUseConfiguredMultiplier()
    {
        // Arrange
        _mockAgentService.Setup(s => s.CalculateCurrentTeamCapacityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        var customSettings = new Mock<IOptions<QueueSettings>>();
        customSettings.Setup(x => x.Value).Returns(new QueueSettings
        {
            QueueCapacityMultiplier = 2.0
        });

        var customService = new ChatQueueService(
            _mockSessionRepository.Object,
            _mockAgentService.Object,
            _mockLogger.Object,
            customSettings.Object);

        // Act
        var result = await customService.CalculateMaxQueueLengthAsync();

        // Assert
        Assert.Equal(20, result); // 10 * 2.0 = 20
    }

    [Fact]
    public async Task DequeueSessionAsync_ShouldReturnOldestSession()
    {
        // Arrange
        var session = new ChatSession("user123");

        _mockSessionRepository.Setup(r => r.GetOldestQueuedSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _chatQueueService.DequeueSessionAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user123", result.UserId);
    }

    [Fact]
    public async Task DequeueSessionAsync_ShouldFollowFIFO()
    {
        // Arrange
        var session1 = new ChatSession("user1") { Id = 1 };
        var session2 = new ChatSession("user2") { Id = 2 };

        var calls = 0;
        _mockSessionRepository.Setup(r => r.GetOldestQueuedSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                calls++;
                return calls == 1 ? session1 : session2;
            });

        // Act
        var result1 = await _chatQueueService.DequeueSessionAsync();
        var result2 = await _chatQueueService.DequeueSessionAsync();

        // Assert
        Assert.Equal("user1", result1.UserId);
        Assert.Equal("user2", result2.UserId);
    }
}

public interface ISequence
{
    int Next();
}