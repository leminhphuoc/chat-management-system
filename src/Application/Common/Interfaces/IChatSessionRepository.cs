using ChatSupportSystem.Domain.Entities;

namespace ChatSupportSystem.Application.Common.Interfaces;

public interface IChatSessionRepository
{
    Task<ChatSession?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<ChatSession>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<ChatSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<ChatSession>> GetQueuedSessionsAsync(CancellationToken cancellationToken = default);

    Task<ChatSession?> GetOldestQueuedSessionAsync(CancellationToken cancellationToken = default);

    Task<int> CountQueuedSessionsAsync(CancellationToken cancellationToken = default);

    Task<int> AddAsync(ChatSession session, CancellationToken cancellationToken = default);

    Task UpdateAsync(ChatSession session, CancellationToken cancellationToken = default);
}