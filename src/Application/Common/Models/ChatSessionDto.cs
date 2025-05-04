using ChatSupportSystem.Domain.Enums;

namespace ChatSupportSystem.Application.Common.Models;

public class ChatSessionDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public SessionStatus Status { get; set; }
    public DateTime LastPollTime { get; set; }
    public DateTime QueueEntryTime { get; set; }
    public int? AssignedAgentId { get; set; }
}