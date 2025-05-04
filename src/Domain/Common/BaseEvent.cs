namespace ChatSupportSystem.Domain.Common;

public abstract class BaseEvent
{
    public DateTime DateOccurred { get; protected set; } = DateTime.UtcNow;
}