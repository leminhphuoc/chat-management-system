using ChatSupportSystem.Application.Common.Interfaces;
using ChatSupportSystem.Application.Common.Models;
using ChatSupportSystem.Domain.Entities;
using ChatSupportSystem.Domain.Enums;
using ChatSupportSystem.Domain.ValueObjects;
using ChatSupportSystem.Infrastructure.Data;
using ChatSupportSystem.Infrastructure.Data.Interceptors;
using ChatSupportSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ChatSupportSystem.Infrastructure.UnitTests.Services;

public class AgentServiceTests
{
    private readonly DbContextOptions<ChatSupportDbContext> _options;
    private readonly Mock<ILogger<AgentService>> _mockLogger;
    private readonly Mock<IOptions<QueueSettings>> _mockQueueSettings;
    private readonly QueueSettings _queueSettings;

    // Dependencies for interceptors
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;

    private readonly Mock<MediatR.IMediator> _mockMediator;

    public AgentServiceTests()
    {
        _options = new DbContextOptionsBuilder<ChatSupportDbContext>()
            .UseInMemoryDatabase(databaseName: $"AgentServiceTests_{Guid.NewGuid()}")
            .Options;

        _mockLogger = new Mock<ILogger<AgentService>>();

        _queueSettings = new QueueSettings
        {
            MaxConcurrentChatsPerAgent = 10,
            QueueCapacityMultiplier = 1.5,
            InactivityThresholdSeconds = 3
        };

        _mockQueueSettings = new Mock<IOptions<QueueSettings>>();
        _mockQueueSettings.Setup(x => x.Value).Returns(_queueSettings);

        // Setup mocks for interceptor dependencies
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
    public async Task CalculateCurrentTeamCapacityAsync_ShouldCalculateCorrectly_ForTeamA()
    {
        // Arrange
        await using var context = CreateDbContext();
        var service = new AgentService(context, _mockLogger.Object, _mockQueueSettings.Object);

        var team = new Team("Team A", TeamType.TeamA);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        // Team A: 1 TL (0.5), 2 Mid (0.6), 1 Jun (0.4)
        var teamLead = new Agent("Team Lead", AgentSeniority.TeamLead, new ShiftSchedule(ShiftType.Morning))
        {
            TeamId = team.Id,
            IsActive = true
        };

        var midLevel1 = new Agent("Mid 1", AgentSeniority.MidLevel, new ShiftSchedule(ShiftType.Morning))
        {
            TeamId = team.Id,
            IsActive = true
        };

        var midLevel2 = new Agent("Mid 2", AgentSeniority.MidLevel, new ShiftSchedule(ShiftType.Morning))
        {
            TeamId = team.Id,
            IsActive = true
        };

        var junior = new Agent("Junior", AgentSeniority.Junior, new ShiftSchedule(ShiftType.Morning))
        {
            TeamId = team.Id,
            IsActive = true
        };

        context.Agents.AddRange(teamLead, midLevel1, midLevel2, junior);
        await context.SaveChangesAsync();

        // Act
        var capacity = await service.CalculateCurrentTeamCapacityAsync();

        // Assert
        // Expected: 1*10*0.5 + 2*10*0.6 + 1*10*0.4 = 5 + 12 + 4 = 21
        Assert.Equal(21, capacity);
    }

    [Fact]
    public async Task CalculateCurrentTeamCapacityAsync_ShouldCalculateCorrectly_ForCustomTeam()
    {
        // Arrange
        await using var context = CreateDbContext();
        var service = new AgentService(context, _mockLogger.Object, _mockQueueSettings.Object);

        var team = new Team("Custom Team", TeamType.TeamC);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        // Team: 2 Mid (0.6), 1 Jun (0.4)
        var midLevel1 = new Agent("Mid 1", AgentSeniority.MidLevel, new ShiftSchedule(ShiftType.Night))
        {
            TeamId = team.Id,
            IsActive = true
        };

        var midLevel2 = new Agent("Mid 2", AgentSeniority.MidLevel, new ShiftSchedule(ShiftType.Night))
        {
            TeamId = team.Id,
            IsActive = true
        };

        var junior = new Agent("Junior", AgentSeniority.Junior, new ShiftSchedule(ShiftType.Night))
        {
            TeamId = team.Id,
            IsActive = true
        };

        context.Agents.AddRange(midLevel1, midLevel2, junior);
        await context.SaveChangesAsync();

        // Act
        var capacity = await service.CalculateCurrentTeamCapacityAsync();

        // Assert
        // Expected: 2*10*0.6 + 1*10*0.4 = 12 + 4 = 16
        Assert.Equal(16, capacity);
    }

    [Fact]
    public async Task GetAvailableAgentsAsync_ShouldReturnOnlyActiveAgents_WhenIncludeAllAgentsFalse()
    {
        // Arrange
        await using var context = CreateDbContext();
        var service = new AgentService(context, _mockLogger.Object, _mockQueueSettings.Object);

        var team = new Team("Team B", TeamType.TeamB);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var activeAgent = new Agent("Active", AgentSeniority.MidLevel, new ShiftSchedule(ShiftType.Afternoon))
        {
            TeamId = team.Id,
            IsActive = true
        };

        var inactiveAgent = new Agent("Inactive", AgentSeniority.MidLevel, new ShiftSchedule(ShiftType.Afternoon))
        {
            TeamId = team.Id,
            IsActive = false
        };

        context.Agents.AddRange(activeAgent, inactiveAgent);
        await context.SaveChangesAsync();

        // Act
        var agents = await service.GetAvailableAgentsAsync(includeAllAgents: false);

        // Assert
        Assert.Single(agents);
        Assert.Equal("Active", agents.First().Name);
    }

    [Fact]
    public async Task GetAvailableAgentsAsync_ShouldReturnAllAgents_WhenIncludeAllAgentsTrue()
    {
        // Arrange
        await using var context = CreateDbContext();
        var service = new AgentService(context, _mockLogger.Object, _mockQueueSettings.Object);

        var team = new Team("Team B", TeamType.TeamB);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var activeAgent = new Agent("Active", AgentSeniority.MidLevel, new ShiftSchedule(ShiftType.Afternoon))
        {
            TeamId = team.Id,
            IsActive = true
        };

        var inactiveAgent = new Agent("Inactive", AgentSeniority.MidLevel, new ShiftSchedule(ShiftType.Afternoon))
        {
            TeamId = team.Id,
            IsActive = false
        };

        context.Agents.AddRange(activeAgent, inactiveAgent);
        await context.SaveChangesAsync();

        // Act
        var agents = await service.GetAvailableAgentsAsync(includeAllAgents: true);

        // Assert
        Assert.Equal(2, agents.Count());
    }

    [Fact]
    public async Task ActivateOverflowTeamAsync_ShouldSetOverflowAgentsActive()
    {
        // Arrange
        await using var context = CreateDbContext();
        var service = new AgentService(context, _mockLogger.Object, _mockQueueSettings.Object);

        var team = new Team("Overflow", TeamType.Overflow);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var overflowAgent1 = new Agent("Overflow 1", AgentSeniority.Junior, new ShiftSchedule(ShiftType.Morning))
        {
            TeamId = team.Id,
            IsOverflowAgent = true,
            IsActive = false
        };

        var overflowAgent2 = new Agent("Overflow 2", AgentSeniority.Junior, new ShiftSchedule(ShiftType.Morning))
        {
            TeamId = team.Id,
            IsOverflowAgent = true,
            IsActive = false
        };

        context.Agents.AddRange(overflowAgent1, overflowAgent2);
        await context.SaveChangesAsync();

        // Act
        var result = await service.ActivateOverflowTeamAsync();

        // Assert
        Assert.True(result);
        var agents = await context.Agents.Where(a => a.IsOverflowAgent).ToListAsync();
        Assert.All(agents, a => Assert.True(a.IsActive));
    }

    [Fact]
    public async Task DeactivateOverflowTeamAsync_ShouldSetOverflowAgentsInactive()
    {
        // Arrange
        await using var context = CreateDbContext();
        var service = new AgentService(context, _mockLogger.Object, _mockQueueSettings.Object);

        var team = new Team("Overflow", TeamType.Overflow);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var overflowAgent1 = new Agent("Overflow 1", AgentSeniority.Junior, new ShiftSchedule(ShiftType.Morning))
        {
            TeamId = team.Id,
            IsOverflowAgent = true,
            IsActive = true
        };

        var overflowAgent2 = new Agent("Overflow 2", AgentSeniority.Junior, new ShiftSchedule(ShiftType.Morning))
        {
            TeamId = team.Id,
            IsOverflowAgent = true,
            IsActive = true
        };

        context.Agents.AddRange(overflowAgent1, overflowAgent2);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeactivateOverflowTeamAsync();

        // Assert
        Assert.True(result);
        var agents = await context.Agents.Where(a => a.IsOverflowAgent).ToListAsync();
        Assert.All(agents, a => Assert.False(a.IsActive));
    }

    [Fact]
    public async Task IsWithinOfficeHoursAsync_ShouldReturnTrue_ForDayShift()
    {
        // Arrange
        await using var context = CreateDbContext();
        var service = new AgentService(context, _mockLogger.Object, _mockQueueSettings.Object);

        // Mock time to day hours (8 AM)
        var mockTime = new TimeSpan(8, 0, 0);
        service.SetCurrentTimeForTest(mockTime);

        // Act
        var result = service.IsWithinOfficeHoursAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsWithinOfficeHoursAsync_ShouldReturnFalse_ForNightShift()
    {
        // Arrange
        await using var context = CreateDbContext();
        var service = new AgentService(context, _mockLogger.Object, _mockQueueSettings.Object);

        // Mock time to night hours (2 AM)
        var mockTime = new TimeSpan(2, 0, 0);
        service.SetCurrentTimeForTest(mockTime);

        // Act
        var result = service.IsWithinOfficeHoursAsync();

        // Assert
        Assert.False(result);
    }
}

// Extension methods for testing
public static class AgentServiceExtensions
{
    public static void SetCurrentTimeForTest(this AgentService service, TimeSpan time)
    {
        // Use reflection to set the private field
        var field = typeof(AgentService).GetField("_currentTimeOverride",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        field?.SetValue(service, time);
    }
}