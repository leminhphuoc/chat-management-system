using ChatSupportSystem.Application.Common.Models;

namespace ChatSupportSystem.Application.Common.Interfaces;

public interface IChatQueueService
{
    Task<bool> CanEnqueueSessionAsync(CancellationToken cancellationToken = default);

    Task EnqueueSessionAsync(int sessionId, CancellationToken cancellationToken = default);

    Task<int> GetQueueSizeAsync(CancellationToken cancellationToken = default);

    Task<int> CalculateMaxQueueLengthAsync(CancellationToken cancellationToken = default);

    Task<ChatSessionDto?> DequeueSessionAsync(CancellationToken cancellationToken = default);
}