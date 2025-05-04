using ChatSupportSystem.Application.Common.Interfaces;
using ChatSupportSystem.Application.Common.Models;
using ChatSupportSystem.Infrastructure.BackgroundServices;
using ChatSupportSystem.Infrastructure.Data;
using ChatSupportSystem.Infrastructure.Data.Interceptors;
using ChatSupportSystem.Infrastructure.Data.Repositories;
using ChatSupportSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatSupportSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DispatchDomainEventsInterceptor>();

        if (configuration.GetValue<bool>("UseInMemoryDatabase"))
        {
            services.AddDbContext<ChatSupportDbContext>(options =>
                options.UseInMemoryDatabase("ChatSupportDb"));
        }
        else
        {
            services.AddDbContext<ChatSupportDbContext>(options =>
                options.UseSqlServer(connectionString));
        }

        services.AddScoped<ChatSupportDbContextInitialiser>();

        // Register services
        services.AddScoped<IChatQueueService, ChatQueueService>();
        services.AddScoped<IAgentService, AgentService>();

        // Register background services
        services.AddHostedService<QueueMonitorService>();
        services.AddHostedService<SessionInactivityService>();
        services.AddHostedService<AgentAssignmentService>();
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();

        services.Configure<QueueSettings>(configuration.GetSection("QueueSettings"));

        return services;
    }
}