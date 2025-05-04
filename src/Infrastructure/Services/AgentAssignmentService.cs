using ChatSupportSystem.Application.Common.Interfaces;
using ChatSupportSystem.Application.Common.Models;
using ChatSupportSystem.Domain.Entities;
using ChatSupportSystem.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatSupportSystem.Infrastructure.BackgroundServices;

public class AgentAssignmentService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentAssignmentService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(2);
    private readonly QueueSettings _queueSettings;

    public AgentAssignmentService(
        IServiceProvider serviceProvider,
        ILogger<AgentAssignmentService> logger,
        IOptions<QueueSettings> queueSettings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _queueSettings = queueSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Agent Assignment Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing the queue");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Agent Assignment Service is stopping");
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var queueService = scope.ServiceProvider.GetRequiredService<IChatQueueService>();
        var agentService = scope.ServiceProvider.GetRequiredService<IAgentService>();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<IChatSessionRepository>();

        var queueSize = await queueService.GetQueueSizeAsync(stoppingToken);
        if (queueSize == 0)
        {
            return;
        }

        var session = await queueService.DequeueSessionAsync(stoppingToken);
        if (session == null)
        {
            _logger.LogWarning("No session could be dequeued despite queue reporting size {Size}", queueSize);
            return;
        }

        var sessionEntity = await sessionRepository.GetByIdAsync(session.Id, stoppingToken);
        if (sessionEntity == null)
        {
            _logger.LogWarning("Session {SessionId} was dequeued but could not be found in repository", session.Id);
            return;
        }

        var agentsByTeam = await GetAvailableAgentsAsync(agentService, stoppingToken);

        if (!agentsByTeam.Any())
        {
            _logger.LogInformation("No agents available to accept chats. Returning session {SessionId} to queue", session.Id);

            if (sessionEntity.Status != SessionStatus.Queued)
            {
                sessionEntity.RequeueSession();
                await sessionRepository.UpdateAsync(sessionEntity, stoppingToken);
            }
            else
            {
                await sessionRepository.UpdateAsync(sessionEntity, stoppingToken);
            }

            return;
        }

        var assigned = await AssignToMostAppropriateAgentAsync(
            sessionEntity, agentsByTeam, agentService, sessionRepository, stoppingToken);

        if (!assigned)
        {
            _logger.LogWarning("Could not assign session {SessionId} to any agent, returning to queue", session.Id);

            if (sessionEntity.Status != SessionStatus.Queued)
            {
                sessionEntity.RequeueSession();
                await sessionRepository.UpdateAsync(sessionEntity, stoppingToken);
            }
            else
            {
                await sessionRepository.UpdateAsync(sessionEntity, stoppingToken);
            }
        }
    }

    private async Task<Dictionary<TeamType, List<Agent>>> GetAvailableAgentsAsync(
        IAgentService agentService, CancellationToken stoppingToken)
    {
        var result = new Dictionary<TeamType, List<Agent>>();

        var allAgentDtos = await agentService.GetAvailableAgentsAsync(cancellationToken: stoppingToken);

        foreach (var agentDto in allAgentDtos.Where(a => a.IsActive))
        {
            var entity = await agentService.GetAgentByIdAsync(agentDto.Id, stoppingToken);

            if (entity != null && entity.CanAcceptChat(_queueSettings.MaxConcurrentChatsPerAgent))
            {
                var teamType = entity.Team?.TeamType ?? TeamType.TeamA;

                if (!result.ContainsKey(teamType))
                {
                    result[teamType] = new List<Agent>();
                }

                result[teamType].Add(entity);
            }
        }

        return result;
    }

    private async Task<bool> AssignToMostAppropriateAgentAsync(
        ChatSession session,
        Dictionary<TeamType, List<Agent>> agentsByTeam,
        IAgentService agentService,
        IChatSessionRepository sessionRepository,
        CancellationToken stoppingToken)
    {
        var currentTime = DateTime.UtcNow.TimeOfDay;

        var teamOrder = new List<TeamType>();

        // During day hours, prefer day shift teams
        if (currentTime >= TimeSpan.FromHours(6) && currentTime < TimeSpan.FromHours(22))
        {
            teamOrder.Add(TeamType.TeamA);
            teamOrder.Add(TeamType.TeamB);
            teamOrder.Add(TeamType.TeamC);
        }
        else // During night hours, prefer night shift team
        {
            teamOrder.Add(TeamType.TeamC);
            teamOrder.Add(TeamType.TeamA);
            teamOrder.Add(TeamType.TeamB);
        }

        // Overflow team is always last choice
        teamOrder.Add(TeamType.Overflow);

        foreach (var teamType in teamOrder)
        {
            if (!agentsByTeam.TryGetValue(teamType, out var teamAgents) || !teamAgents.Any())
            {
                continue;
            }

            // For each team, group agents by seniority
            var agentsBySeniority = teamAgents
                .GroupBy(a => a.Seniority)
                .OrderBy(g => g.Key) // Junior first
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var seniority in Enum.GetValues<AgentSeniority>().OrderBy(s => s))
            {
                if (!agentsBySeniority.TryGetValue(seniority, out var agents) || !agents.Any())
                {
                    continue;
                }

                var agent = agents.OrderBy(a => a.CurrentChatCount).First();

                if (agent.CanAcceptChat(_queueSettings.MaxConcurrentChatsPerAgent))
                {
                    try
                    {
                        session.AssignToAgent(agent);
                        agent.AssignChat(_queueSettings.MaxConcurrentChatsPerAgent);

                        await sessionRepository.UpdateAsync(session, stoppingToken);
                        await agentService.UpdateAgentAsync(agent, stoppingToken);

                        _logger.LogInformation("Session {SessionId} assigned to agent {AgentName} ({Seniority}, Team {Team})",
                            session.Id, agent.Name, agent.Seniority, teamType);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error assigning session {SessionId} to agent {AgentId}",
                            session.Id, agent.Id);
                    }
                }
            }
        }

        return false;
    }
}