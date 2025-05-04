using ChatSupportSystem.Domain.Common;
using ChatSupportSystem.Domain.Entities;

namespace ChatSupportSystem.Domain.Events;

public class ChatSessionCreatedEvent : BaseEvent
{
    public ChatSession Session { get; }

    public ChatSessionCreatedEvent(ChatSession session)
    {
        Session = session;
    }
}

public class ChatSessionAssignedEvent : BaseEvent
{
    public ChatSession Session { get; }
    public int AgentId { get; }

    public ChatSessionAssignedEvent(ChatSession session, int agentId)
    {
        Session = session;
        AgentId = agentId;
    }
}

public class ChatSessionInactivatedEvent : BaseEvent
{
    public ChatSession Session { get; }
    public int AgentId { get; }

    public ChatSessionInactivatedEvent(ChatSession session, int agentId)
    {
        Session = session;
        AgentId = agentId;
    }
}

public class ChatSessionCompletedEvent : BaseEvent
{
    public ChatSession Session { get; }
    public int AgentId { get; }

    public ChatSessionCompletedEvent(ChatSession session, int agentId)
    {
        Session = session;
        AgentId = agentId;
    }
}