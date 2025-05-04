using ChatSupportSystem.Domain.Common;
using ChatSupportSystem.Domain.Enums;
using ChatSupportSystem.Domain.Events;

namespace ChatSupportSystem.Domain.Entities;

public class ChatSession : BaseAuditableEntity<int>
{
    public string UserId { get; set; } = string.Empty;
    public SessionStatus Status { get; set; }
    public DateTime LastPollTime { get; set; }
    public DateTime QueueEntryTime { get; set; }
    public int? AssignedAgentId { get; set; }

    public Agent? AssignedAgent { get; set; }

    private ChatSession()
    { }

    public ChatSession(string userId)
    {
        UserId = userId;
        Status = SessionStatus.Queued;
        LastPollTime = DateTime.UtcNow;
        QueueEntryTime = DateTime.UtcNow;

        AddDomainEvent(new ChatSessionCreatedEvent(this));
    }

    public void UpdateLastPollTime(DateTime pollTime)
    {
        if (Status == SessionStatus.Inactive || Status == SessionStatus.Completed)
        {
            throw new InvalidOperationException("Cannot update poll time for inactive or completed session");
        }

        LastPollTime = pollTime;
    }

    public void AssignToAgent(Agent agent)
    {
        if (Status != SessionStatus.Queued)
        {
            throw new InvalidOperationException("Only queued sessions can be assigned to an agent");
        }

        if (!agent.CanAcceptChat())
        {
            throw new InvalidOperationException("Agent cannot accept more chats at this time");
        }

        AssignedAgentId = agent.Id;
        AssignedAgent = agent;
        Status = SessionStatus.Active;

        AddDomainEvent(new ChatSessionAssignedEvent(this, agent.Id));
    }

    public void MarkInactive()
    {
        if (Status == SessionStatus.Inactive || Status == SessionStatus.Completed)
        {
            return;
        }

        var previousStatus = Status;
        Status = SessionStatus.Inactive;

        if (previousStatus == SessionStatus.Active && AssignedAgentId.HasValue)
        {
            AddDomainEvent(new ChatSessionInactivatedEvent(this, AssignedAgentId.Value));
        }
    }

    public void Complete()
    {
        if (Status == SessionStatus.Completed)
        {
            return;
        }

        Status = SessionStatus.Completed;

        if (AssignedAgentId.HasValue)
        {
            AddDomainEvent(new ChatSessionCompletedEvent(this, AssignedAgentId.Value));
        }
    }

    public void RequeueSession()
    {
        if (Status != SessionStatus.Queued)
        {
            Status = SessionStatus.Queued;
            QueueEntryTime = DateTime.UtcNow;

            if (AssignedAgentId.HasValue && AssignedAgent != null)
            {
                AssignedAgent.CompleteChat();
                AssignedAgentId = null;
                AssignedAgent = null;
            }
        }
    }
}