using System.Reflection;
using ChatSupportSystem.Domain.Entities;
using ChatSupportSystem.Infrastructure.Data.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace ChatSupportSystem.Infrastructure.Data;

public class ChatSupportDbContext : DbContext
{
    private readonly AuditableEntityInterceptor _auditableEntityInterceptor;
    private readonly DispatchDomainEventsInterceptor _dispatchDomainEventsInterceptor;

    public ChatSupportDbContext(
        DbContextOptions<ChatSupportDbContext> options,
        AuditableEntityInterceptor auditableEntityInterceptor,
        DispatchDomainEventsInterceptor dispatchDomainEventsInterceptor)
        : base(options)
    {
        _auditableEntityInterceptor = auditableEntityInterceptor;
        _dispatchDomainEventsInterceptor = dispatchDomainEventsInterceptor;
    }

    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<Team> Teams => Set<Team>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(builder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditableEntityInterceptor, _dispatchDomainEventsInterceptor);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}