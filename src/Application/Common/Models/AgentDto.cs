using ChatSupportSystem.Domain.Enums;

namespace ChatSupportSystem.Application.Common.Models;

public class AgentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AgentSeniority Seniority { get; set; }
    public bool IsActive { get; set; }
    public bool IsOverflowAgent { get; set; }
    public int CurrentChatCount { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public TeamType TeamType { get; set; }
    public int MaxConcurrentChats { get; set; }
}