using ChatSupportSystem.Application.Common.Models;
using ChatSupportSystem.Domain.Entities;

namespace ChatSupportSystem.Application.Common.Interfaces;

public interface IAgentService
{
    Task<IReadOnlyCollection<AgentDto>> GetAvailableAgentsAsync(bool includeAllAgents = false, CancellationToken cancellationToken = default);

    Task<int> CalculateCurrentTeamCapacityAsync(CancellationToken cancellationToken = default);

    Task<bool> ActivateOverflowTeamAsync(CancellationToken cancellationToken = default);

    Task<bool> DeactivateOverflowTeamAsync(CancellationToken cancellationToken = default);

    bool IsWithinOfficeHoursAsync(CancellationToken cancellationToken = default);

    Task<Agent?> GetAgentByIdAsync(int id, CancellationToken cancellationToken = default);

    Task UpdateAgentAsync(Agent agent, CancellationToken cancellationToken = default);
}