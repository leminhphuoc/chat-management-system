using ChatSupportSystem.Domain.Common;
using ChatSupportSystem.Domain.Enums;
using ChatSupportSystem.Domain.ValueObjects;

namespace ChatSupportSystem.Domain.Entities;

public class Agent : BaseAuditableEntity<int>
{
    private int _currentChatCount;
    public string Name { get; set; } = string.Empty;
    public AgentSeniority Seniority { get; set; }
    public ShiftSchedule ShiftSchedule { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public bool IsOverflowAgent { get; set; }

    public int CurrentChatCount
    {
        get => _currentChatCount;
        private set => _currentChatCount = value;
    }

    public int TeamId { get; set; }

    public Team? Team { get; set; }

    private Agent()
    { }

    public Agent(string name, AgentSeniority seniority, ShiftSchedule shiftSchedule, bool isOverflowAgent = false)
    {
        Name = name;
        Seniority = seniority;
        ShiftSchedule = shiftSchedule;
        IsOverflowAgent = isOverflowAgent;
        IsActive = !isOverflowAgent;
        _currentChatCount = 0;
    }

    public void AssignChat(int maxConcurrentChatsPerAgent = 10)
    {
        if (!CanAcceptChat(maxConcurrentChatsPerAgent))
        {
            throw new InvalidOperationException("Agent cannot accept more chats");
        }

        _currentChatCount++;
    }

    public void CompleteChat()
    {
        if (_currentChatCount <= 0)
        {
            throw new InvalidOperationException("Agent has no active chats to complete");
        }

        _currentChatCount--;
    }

    public bool CanAcceptChat(int maxConcurrentChatsPerAgent = 10)
    {
        if (!IsActive)
        {
            return false;
        }

        return _currentChatCount < GetMaxConcurrentChats(maxConcurrentChatsPerAgent);
    }

    public int GetMaxConcurrentChats(int maxConcurrentChatsPerAgent)
    {
        if (IsOverflowAgent)
        {
            return (int)Math.Floor(maxConcurrentChatsPerAgent * 0.4);
        }

        var multiplier = Seniority switch
        {
            AgentSeniority.Junior => 0.4,
            AgentSeniority.MidLevel => 0.6,
            AgentSeniority.Senior => 0.8,
            AgentSeniority.TeamLead => 0.5,
            _ => 0.4
        };

        return (int)Math.Floor(maxConcurrentChatsPerAgent * multiplier);
    }
}