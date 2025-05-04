using ChatSupportSystem.Application.Common.Interfaces;
using ChatSupportSystem.Domain.Entities;
using ChatSupportSystem.Domain.Enums;
using ChatSupportSystem.Infrastructure.Data;
using ChatSupportSystem.Infrastructure.Data.Interceptors;
using ChatSupportSystem.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ChatSupportSystem.Infrastructure.UnitTests.Repositories;

public class ChatSessionRepositoryTests
{
    private readonly DbContextOptions<ChatSupportDbContext> _options;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<MediatR.IMediator> _mockMediator;

    public ChatSessionRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<ChatSupportDbContext>()
            .UseInMemoryDatabase(databaseName: $"ChatSessionRepositoryTests_{Guid.NewGuid()}")
            .Options;

        // Create mocks for the dependencies of interceptors
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(x => x.UserId).Returns("test-user");

        _mockMediator = new Mock<MediatR.IMediator>();
    }

    private ChatSupportDbContext CreateDbContext()
    {
        // Create the actual interceptors with mocked dependencies
        var auditableEntityInterceptor = new AuditableEntityInterceptor(
            _mockCurrentUserService.Object);

        var dispatchDomainEventsInterceptor = new DispatchDomainEventsInterceptor(
            _mockMediator.Object);

        return new ChatSupportDbContext(
            _options,
            auditableEntityInterceptor,
            dispatchDomainEventsInterceptor);
    }

    [Fact]
    public async Task AddAsync_ShouldAddSessionToDatabase()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ChatSessionRepository(context);
        var session = new ChatSession("test@example.com");

        // Act
        await repository.AddAsync(session);

        // Assert
        var savedSession = await context.ChatSessions.FindAsync(session.Id);
        Assert.NotNull(savedSession);
        Assert.Equal("test@example.com", savedSession.UserId);
        Assert.Equal(SessionStatus.Queued, savedSession.Status);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingSession()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ChatSessionRepository(context);
        var session = new ChatSession("test@example.com");
        await repository.AddAsync(session);

        // Act
        session.UpdateLastPollTime(DateTime.UtcNow.AddMinutes(5));
        await repository.UpdateAsync(session, default);

        // Assert
        var updatedSession = await context.ChatSessions.FindAsync(session.Id);
        Assert.NotNull(updatedSession);
        Assert.Equal(session.LastPollTime, updatedSession.LastPollTime);
    }

    [Fact]
    public async Task CountQueuedSessionsAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ChatSessionRepository(context);

        // Add some queued sessions
        await repository.AddAsync(new ChatSession("user1@example.com"));
        await repository.AddAsync(new ChatSession("user2@example.com"));
        await repository.AddAsync(new ChatSession("user3@example.com"));

        // Add a non-queued session
        var activeSession = new ChatSession("active@example.com");
        activeSession.Status = SessionStatus.Active;
        await repository.AddAsync(activeSession);

        // Act
        var count = await repository.CountQueuedSessionsAsync();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetOldestQueuedSessionAsync_ShouldReturnOldestByQueueEntryTime()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ChatSessionRepository(context);

        // Add sessions with different queue entry times
        var oldestSession = new ChatSession("oldest@example.com")
        {
            QueueEntryTime = DateTime.UtcNow.AddMinutes(-10)
        };
        await repository.AddAsync(oldestSession);

        var newerSession = new ChatSession("newer@example.com")
        {
            QueueEntryTime = DateTime.UtcNow.AddMinutes(-5)
        };
        await repository.AddAsync(newerSession);

        var newestSession = new ChatSession("newest@example.com")
        {
            QueueEntryTime = DateTime.UtcNow.AddMinutes(-2)
        };
        await repository.AddAsync(newestSession);

        // Act
        var result = await repository.GetOldestQueuedSessionAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("oldest@example.com", result.UserId);
    }

    [Fact]
    public async Task GetOldestQueuedSessionAsync_ShouldRespectFIFO()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new ChatSessionRepository(context);

        // Add multiple sessions with the same entry time
        var session1 = new ChatSession("first@example.com");
        var baseTime = DateTime.UtcNow.AddMinutes(-10);
        session1.QueueEntryTime = baseTime;
        await repository.AddAsync(session1);

        var session2 = new ChatSession("second@example.com");
        session2.QueueEntryTime = baseTime;
        await repository.AddAsync(session2);

        // Act - Get oldest queued session twice
        var firstResult = await repository.GetOldestQueuedSessionAsync();

        // Mark as non-queued
        firstResult.Status = SessionStatus.Active;
        await repository.UpdateAsync(firstResult, default);

        var secondResult = await repository.GetOldestQueuedSessionAsync();

        // Assert - Should get sessions in FIFO order
        Assert.Equal("first@example.com", firstResult.UserId);
        Assert.Equal("second@example.com", secondResult.UserId);
    }
}