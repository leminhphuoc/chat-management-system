using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ChatSupportSystem.FunctionalTests;

public class TestRunner
{
    private readonly HttpClient _client;
    private readonly ILogger<TestRunner> _logger;
    private readonly string _baseUrl = "http://localhost:5066";

    public TestRunner(ILogger<TestRunner> logger)
    {
        _logger = logger;
        _client = new HttpClient();
    }

    public async Task RunAllTestScenarios()
    {
        _logger.LogInformation("Starting test scenarios...");

        await RunChatSessionCreationTests();
        await RunAgentAssignmentTests();
        await RunPollingTests();

        _logger.LogInformation("All test scenarios completed.");
    }

    private async Task RunChatSessionCreationTests()
    {
        _logger.LogInformation("Running Chat Session Creation & Queuing tests...");

        // TC-CS-001: Create session with available agents
        var response1 = await CreateChatSession("customer-cs001@example.com");
        LogResponse("TC-CS-001", response1);

        // TC-CS-002: Create session when agents are busy but queue ok
        var response2 = await CreateChatSession("customer-cs002@example.com");
        LogResponse("TC-CS-002", response2);

        // Create multiple sessions to fill up the queue for TC-CS-003, TC-CS-004, TC-CS-005
        for (int i = 1; i <= 20; i++)
        {
            await CreateChatSession($"queue-filler-{i}@example.com");
        }

        // TC-CS-003: Create session when main queue is full (no overflow)
        // Note: This test would need to have the time mocked to night hours
        var response3 = await CreateChatSession("customer-cs003@example.com");
        LogResponse("TC-CS-003", response3);

        // TC-CS-004: Create session when main queue is full (office hours, overflow available)
        var response4 = await CreateChatSession("customer-cs004@example.com");
        LogResponse("TC-CS-004", response4);

        // TC-CS-005: Create session when main and overflow queues are full
        // Create more sessions to fill up overflow capacity
        for (int i = 1; i <= 15; i++)
        {
            await CreateChatSession($"overflow-filler-{i}@example.com");
        }
        var response5 = await CreateChatSession("customer-cs005@example.com");
        LogResponse("TC-CS-005", response5);

        // TC-CS-006: Queue follows FIFO
        var queueStatus = await GetQueueStatus();
        _logger.LogInformation("TC-CS-006: Queue Status - {queueStatus}", queueStatus);
    }

    private async Task RunAgentAssignmentTests()
    {
        _logger.LogInformation("Running Agent Assignment & Capacity tests...");

        // TC-AA-001 through TC-AA-006: Agent assignments
        var agents = await GetAvailableAgents(includeAllAgents: true);
        _logger.LogInformation("Agent List: {agents}", agents);

        int sessionId = 1; 
        int agentId = await GetFirstAvailableAgentId();

        if (agentId > 0)
        {
            var assignResponse = await AssignChatToAgent(sessionId, agentId);
            _logger.LogInformation("TC-AA-002: Assignment result - {result}", assignResponse);
        }

        await SetAgentAvailability(2, true); 
        await SetAgentAvailability(3, false); 

        var queueStatus = await GetQueueStatus();
        _logger.LogInformation("TC-AA-006: Overflow team status - {queueStatus}", queueStatus);
    }

    private async Task RunPollingTests()
    {
        _logger.LogInformation("Running Polling & Inactivity tests...");

        // TC-PI-001: Successful polling
        var pollResponse1 = await PollSession(1);
        _logger.LogInformation("TC-PI-001: Poll result - {result}", pollResponse1);

        // TC-PI-002: Session marked inactive
        await MarkSessionInactive(2);
        var pollResponse2 = await PollSession(2);
        _logger.LogInformation("TC-PI-002: Poll after inactive - {result}", pollResponse2);

        // TC-PI-003: Polling resumes before timeout
        await Task.Delay(2000); // Wait almost to timeout
        var pollResponse3 = await PollSession(3);
        _logger.LogInformation("TC-PI-003: Poll after delay - {result}", pollResponse3);
    }

    // Helper methods for API calls
    private async Task<string> CreateChatSession(string userId)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { userId }),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync($"{_baseUrl}/api/ChatSessions", content);

        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> GetQueueStatus()
    {
        var response = await _client.GetAsync($"{_baseUrl}/api/Queue/status");

        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> GetAvailableAgents(bool includeAllAgents)
    {
        var response = await _client.GetAsync(
            $"{_baseUrl}/api/Agents/available?includeAllAgents={includeAllAgents}");

        return await response.Content.ReadAsStringAsync();
    }

    private async Task<int> GetFirstAvailableAgentId()
    {
        var response = await _client.GetAsync($"{_baseUrl}/api/Agents/available?includeAllAgents=false");
        var content = await response.Content.ReadAsStringAsync();

        try
        {
            using var document = JsonDocument.Parse(content);
            var agentsElement = document.RootElement.GetProperty("agents");

            if (agentsElement.GetArrayLength() > 0)
            {
                return agentsElement[0].GetProperty("id").GetInt32();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing agent response");
        }

        return -1;
    }

    private async Task<string> AssignChatToAgent(int sessionId, int agentId)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { sessionId, agentId }),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync($"{_baseUrl}/api/Queue/assign", content);

        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> SetAgentAvailability(int agentId, bool isActive)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { isActive }),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PutAsync(
            $"{_baseUrl}/api/Agents/{agentId}/availability", content);

        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> PollSession(int sessionId)
    {
        var response = await _client.GetAsync($"{_baseUrl}/api/ChatSessions/{sessionId}/poll");

        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> MarkSessionInactive(int sessionId)
    {
        var response = await _client.PutAsync($"{_baseUrl}/api/ChatSessions/{sessionId}/inactive", null);

        return await response.Content.ReadAsStringAsync();
    }

    private void LogResponse(string testId, string response)
    {
        _logger.LogInformation("{testId} Response: {response}", testId, response);
    }
}