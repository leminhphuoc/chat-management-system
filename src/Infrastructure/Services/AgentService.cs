using ChatSupportSystem.Application.Common.Interfaces;
using ChatSupportSystem.Application.Common.Models;
using ChatSupportSystem.Domain.Entities;
using ChatSupportSystem.Domain.Enums;
using ChatSupportSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatSupportSystem.Infrastructure.Services;

public class AgentService : IAgentService
{
    private readonly ChatSupportDbContext _dbContext;
    private readonly ILogger<AgentService> _logger;
    private readonly QueueSettings _queueSettings;
    private TimeSpan? _currentTimeOverride = null;

    public AgentService(
        ChatSupportDbContext dbContext,
        ILogger<AgentService> logger,
        IOptions<QueueSettings> queueSettings)
    {
        _dbContext = dbContext;
        _logger = logger;
        _queueSettings = queueSettings.Value;
    }

    internal void SetCurrentTimeForTest(TimeSpan time)
    {
        _currentTimeOverride = time;
    }

    private TimeSpan GetCurrentTime()
    {
        return _currentTimeOverride ?? DateTime.Now.TimeOfDay;
    }

    public async Task<bool> ActivateOverflowTeamAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Activating overflow team due to high queue volume");

        var overflowAgents = await _dbContext.Agents
            .Where(a => a.IsOverflowAgent && !a.IsActive)
            .ToListAsync(cancellationToken);

        if (!overflowAgents.Any())
        {
            _logger.LogInformation("No inactive overflow agents found to activate");
            return false;
        }

        foreach (var agent in overflowAgents)
        {
            agent.IsActive = true;
            _logger.LogInformation("Activating agent {AgentId}: {AgentName}", agent.Id, agent.Name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeactivateOverflowTeamAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deactivating overflow team");

        var activeOverflowAgents = await _dbContext.Agents
            .Where(a => a.IsOverflowAgent && a.IsActive)
            .ToListAsync(cancellationToken);

        if (!activeOverflowAgents.Any())
        {
            _logger.LogInformation("No active overflow agents found");
            return false;
        }

        foreach (var agent in activeOverflowAgents)
        {
            agent.IsActive = false;
            _logger.LogInformation("Deactivating agent {AgentId}: {AgentName}", agent.Id, agent.Name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> CalculateCurrentTeamCapacityAsync(CancellationToken cancellationToken = default)
    {
        var agents = await _dbContext.Agents
            .Where(a => a.IsActive)
            .ToListAsync(cancellationToken);

        return agents.Sum(a => a.GetMaxConcurrentChats(_queueSettings.MaxConcurrentChatsPerAgent));
    }

    public async Task<IReadOnlyCollection<AgentDto>> GetAvailableAgentsAsync(bool includeAllAgents = false, CancellationToken cancellationToken = default)
    {
        var currentTime = GetCurrentTime();
        var query = _dbContext.Agents
            .Include(a => a.Team)
            .Include(a => a.ShiftSchedule)
            .AsQueryable();

        if (!includeAllAgents)
        {
            query = query.Where(a => a.IsActive);
        }

        var agents = await query.ToListAsync(cancellationToken);

        if (!includeAllAgents)
        {
            agents = agents
                .Where(a =>
                    // Overflow team members are available whenever they're active, regardless of shift
                    a.IsOverflowAgent ||
                    // Regular team members are only available during their shift hours
                    (a.ShiftSchedule?.IsWithinShiftHours(currentTime) ?? false))
                .ToList();
        }

        return agents.Select(a => new AgentDto
        {
            Id = a.Id,
            Name = a.Name,
            Seniority = a.Seniority,
            IsActive = a.IsActive,
            IsOverflowAgent = a.IsOverflowAgent,
            CurrentChatCount = a.CurrentChatCount,
            TeamName = a.Team?.Name ?? "Unassigned",
            TeamType = a.Team?.TeamType ?? TeamType.TeamA,
            MaxConcurrentChats = a.GetMaxConcurrentChats(_queueSettings.MaxConcurrentChatsPerAgent)
        }).ToList();
    }

    public async Task<Agent?> GetAgentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Agents
            .Include(a => a.Team)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task UpdateAgentAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        _dbContext.Agents.Update(agent);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public bool IsWithinOfficeHoursAsync(CancellationToken cancellationToken = default)
    {
        var currentTime = GetCurrentTime();

        var result = currentTime >= TimeSpan.FromHours(6) && currentTime < TimeSpan.FromHours(22);

        return result;
    }
}