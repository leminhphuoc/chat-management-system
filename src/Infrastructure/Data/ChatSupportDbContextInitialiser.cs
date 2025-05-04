using ChatSupportSystem.Domain.Entities;
using ChatSupportSystem.Domain.Enums;
using ChatSupportSystem.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatSupportSystem.Infrastructure.Data;

public class ChatSupportDbContextInitialiser
{
    private readonly ILogger<ChatSupportDbContextInitialiser> _logger;
    private readonly ChatSupportDbContext _context;

    public ChatSupportDbContextInitialiser(ILogger<ChatSupportDbContextInitialiser> logger, ChatSupportDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task InitializeAsync()
    {
        try
        {
            if (_context.Database.IsInMemory())
            {
                await _context.Database.EnsureCreatedAsync();
            }
            else
            {
                await _context.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task TrySeedAsync()
    {
        if (!_context.Teams.Any())
        {
            await SeedTeamsAsync();
        }

        if (!_context.Agents.Any())
        {
            await SeedAgentsAsync();
        }

        if (!_context.ChatSessions.Any())
        {
            await SeedChatSessionsAsync();
        }
    }

    private async Task SeedTeamsAsync()
    {
        _logger.LogInformation("Seeding teams");

        var now = DateTime.UtcNow;

        var teams = new List<Team>
        {
            new Team("Team A", TeamType.TeamA, "Day shift team with mixed seniority levels")
            {
                Created = now,
                CreatedBy = "system"
            },
            new Team("Team B", TeamType.TeamB, "Day shift team with mixed seniority levels")
            {
                Created = now,
                CreatedBy = "system"
            },
            new Team("Team C", TeamType.TeamC, "Night shift team")
            {
                Created = now,
                CreatedBy = "system"
            },
            new Team("Overflow Team", TeamType.Overflow, "Activated during high volume periods")
            {
                Created = now,
                CreatedBy = "system"
            }
        };

        _context.Teams.AddRange(teams);
        await _context.SaveChangesAsync();
    }

    private async Task SeedAgentsAsync()
    {
        _logger.LogInformation("Seeding agents");

        var now = DateTime.UtcNow;

        var teamA = await _context.Teams.FirstOrDefaultAsync(t => t.TeamType == TeamType.TeamA);
        var teamB = await _context.Teams.FirstOrDefaultAsync(t => t.TeamType == TeamType.TeamB);
        var teamC = await _context.Teams.FirstOrDefaultAsync(t => t.TeamType == TeamType.TeamC);
        var overflowTeam = await _context.Teams.FirstOrDefaultAsync(t => t.TeamType == TeamType.Overflow);

        if (teamA == null || teamB == null || teamC == null || overflowTeam == null)
        {
            throw new InvalidOperationException("Teams must be seeded before agents");
        }

        // Team A: 1x team lead, 2x mid-level, 1x junior
        var teamAAgents = new List<Agent>
        {
            new Agent("Alice Smith", AgentSeniority.TeamLead, new ShiftSchedule(ShiftType.Morning))
            {
                Created = now,
                CreatedBy = "system"
            },
            new Agent("Bob Johnson", AgentSeniority.MidLevel, new ShiftSchedule(ShiftType.Morning))
            {
                Created = now,
                CreatedBy = "system"
            },
            new Agent("Charlie Davis", AgentSeniority.MidLevel, new ShiftSchedule(ShiftType.Morning))
            {
                Created = now,
                CreatedBy = "system"
            },
            new Agent("Diana Wilson", AgentSeniority.Junior, new ShiftSchedule(ShiftType.Morning))
            {
                Created = now,
                CreatedBy = "system"
            }
        };

        foreach (var agent in teamAAgents)
        {
            agent.TeamId = teamA.Id;
        }

        // Team B: 1x senior, 1x mid-level, 2x junior
        var teamBAgents = new List<Agent>
        {
            new Agent("Eric Brown", AgentSeniority.Senior, new ShiftSchedule(ShiftType.Afternoon))
            {
                Created = now,
                CreatedBy = "system"
            },
            new Agent("Fiona Garcia", AgentSeniority.MidLevel, new ShiftSchedule(ShiftType.Afternoon))
            {
                Created = now,
                CreatedBy = "system"
            },
            new Agent("George Martinez", AgentSeniority.Junior, new ShiftSchedule(ShiftType.Afternoon))
            {
                Created = now,
                CreatedBy = "system"
            },
            new Agent("Hannah Kim", AgentSeniority.Junior, new ShiftSchedule(ShiftType.Afternoon))
            {
                Created = now,
                CreatedBy = "system"
            }
        };

        foreach (var agent in teamBAgents)
        {
            agent.TeamId = teamB.Id;
        }

        // Team C: 2x mid-level (night shift team)
        var teamCAgents = new List<Agent>
        {
            new Agent("Ian Lee", AgentSeniority.MidLevel, new ShiftSchedule(ShiftType.Night))
            {
                Created = now,
                CreatedBy = "system"
            },
            new Agent("Julia Chen", AgentSeniority.MidLevel, new ShiftSchedule(ShiftType.Night))
            {
                Created = now,
                CreatedBy = "system"
            }
        };

        foreach (var agent in teamCAgents)
        {
            agent.TeamId = teamC.Id;
        }

        // Overflow team: x6 considered Junior
        var overflowAgents = new List<Agent>();
        for (int i = 1; i <= 6; i++)
        {
            var agent = new Agent($"Overflow Agent {i}", AgentSeniority.Junior, new ShiftSchedule(ShiftType.Morning), isOverflowAgent: true)
            {
                Created = now,
                CreatedBy = "system",
                TeamId = overflowTeam.Id
            };
            overflowAgents.Add(agent);
        }

        _context.Agents.AddRange(teamAAgents);
        _context.Agents.AddRange(teamBAgents);
        _context.Agents.AddRange(teamCAgents);
        _context.Agents.AddRange(overflowAgents);

        await _context.SaveChangesAsync();
    }

    private async Task SeedChatSessionsAsync()
    {
        _logger.LogInformation("Seeding chat sessions");

        var now = DateTime.UtcNow;

        var sessions = new List<ChatSession>
        {
            new ChatSession("user1@example.com")
            {
                Created = now,
                CreatedBy = "system"
            },
            new ChatSession("user2@example.com")
            {
                Created = now,
                CreatedBy = "system"
            },
            new ChatSession("user3@example.com")
            {
                Created = now,
                CreatedBy = "system"
            },
            new ChatSession("user4@example.com")
            {
                Created = now,
                CreatedBy = "system"
            },
            new ChatSession("user5@example.com")
            {
                Created = now,
                CreatedBy = "system"
            }
        };

        var agents = await _context.Agents
            .Where(a => !a.IsOverflowAgent)
            .Take(2)
            .ToListAsync();

        if (agents.Count >= 2)
        {
            sessions[0].AssignToAgent(agents[0]);
            agents[0].AssignChat(10);

            sessions[1].AssignToAgent(agents[1]);
            agents[1].AssignChat(10);

            sessions[2].MarkInactive();
        }

        _context.ChatSessions.AddRange(sessions);
        await _context.SaveChangesAsync();
    }
}