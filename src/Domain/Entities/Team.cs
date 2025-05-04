using ChatSupportSystem.Domain.Common;
using ChatSupportSystem.Domain.Enums;

namespace ChatSupportSystem.Domain.Entities;

public class Team : BaseAuditableEntity<int>
{
    private readonly List<Agent> _agents = new();

    public string Name { get; set; } = string.Empty;
    public TeamType TeamType { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsOverflowTeam { get; set; }
    public string Description { get; set; } = string.Empty;

    // Navigation property
    public IReadOnlyCollection<Agent> Agents => _agents.AsReadOnly();

    // For EF Core
    private Team()
    { }

    public Team(string name, TeamType teamType, string description = "")
    {
        Name = name;
        TeamType = teamType;
        Description = description;
        IsOverflowTeam = teamType == TeamType.Overflow;
        IsActive = teamType != TeamType.Overflow; // Overflow teams start inactive
    }

    public void AddAgent(Agent agent)
    {
        if (agent == null)
        {
            throw new ArgumentNullException(nameof(agent));
        }

        _agents.Add(agent);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}