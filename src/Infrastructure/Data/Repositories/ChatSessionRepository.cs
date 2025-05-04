using ChatSupportSystem.Application.Common.Interfaces;
using ChatSupportSystem.Domain.Entities;
using ChatSupportSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ChatSupportSystem.Infrastructure.Data.Repositories;

public class ChatSessionRepository : IChatSessionRepository
{
    private readonly ChatSupportDbContext _context;

    public ChatSessionRepository(ChatSupportDbContext context)
    {
        _context = context;
    }

    public async Task<ChatSession?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ChatSessions
            .Include(s => s.AssignedAgent)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ChatSession>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ChatSessions
            .Include(s => s.AssignedAgent)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ChatSessions
            .Include(s => s.AssignedAgent)
            .Where(s => s.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatSession>> GetQueuedSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ChatSessions
            .Where(s => s.Status == SessionStatus.Queued)
            .OrderBy(s => s.QueueEntryTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatSession?> GetOldestQueuedSessionAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ChatSessions
            .Where(s => s.Status == SessionStatus.Queued)
            .OrderBy(s => s.QueueEntryTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CountQueuedSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ChatSessions
            .CountAsync(s => s.Status == SessionStatus.Queued, cancellationToken);
    }

    public async Task<int> AddAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
        return session.Id;
    }

    public async Task UpdateAsync(ChatSession session, CancellationToken cancellationToken)
    {
        var tracked = _context.ChatSessions.Local.FirstOrDefault(e => e.Id == session.Id);

        if (tracked != null && tracked != session)
        {
            _context.Entry(tracked).CurrentValues.SetValues(session);
        }
        else
        {
            _context.ChatSessions.Update(session);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}