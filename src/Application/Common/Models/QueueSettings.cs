namespace ChatSupportSystem.Application.Common.Models;

public class QueueSettings
{
    public int MaxConcurrentChatsPerAgent { get; set; } = 10;
    public double QueueCapacityMultiplier { get; set; } = 1.5;
    public int InactivityThresholdSeconds { get; set; } = 1000;
}