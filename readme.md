# Chat Management System

A comprehensive system for managing customer chat support with intelligent queue management, agent assignments, and overflow handling.

## 🏗️ System Architecture

- **Domain Layer**: Core business entities and domain rules
- **Application Layer**: Use cases and application logic
- **Infrastructure Layer**: Database interactions and external service integrations
- **Web Layer**: Minimal API endpoints and HTTP configuration

## 🚀 Getting Started

### ✅ Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- SQL Server (or use the in-memory provider for testing)

### ▶️ Running the Application

```bash
# Navigate to the project directory
cd ChatManagementSystem

# Run the application
dotnet run --project src/Web/Web.csproj
```

## 🧪 Testing Flow

Follow this sequence to test the complete functionality of the Chat Management System:

| Step | Description | Details |
|------|-------------|---------|
| 1️⃣ | **System Health Check** | `GET http://localhost:5066/health`<br>*Expected: 200 OK response* |
| 2️⃣ | **Check Initial Queue Status** | `GET http://localhost:5066/api/Queue/status`<br>*Expected: Queue metrics details* |
| 3️⃣ | **View Available Agents** | `GET http://localhost:5066/api/Agents/available?includeAllAgents=false`<br>*Expected: List of available agents* |
| 4️⃣ | **Create a New Chat Session** | `POST http://localhost:5066/api/ChatSessions`<br>*Body:* `{"userId": "customer123"}`<br>*Expected: New session details with sessionId* |
| 5️⃣ | **Poll the Chat Session** | `GET http://localhost:5066/api/ChatSessions/{sessionId}/poll`<br>*Expected: Session status (initially "Queued")* |
| 6️⃣ | **Agent Assignment Process** | Automatic or Manual via:<br>`POST http://localhost:5066/api/Queue/assign`<br>*Body:* `{"sessionId": 1, "agentId": 2}` |
| 7️⃣ | **Check Updated Queue Status** | `GET http://localhost:5066/api/Queue/status`<br>*Expected: Updated queue metrics* |
| 8️⃣ | **Poll the Session Again** | `GET http://localhost:5066/api/ChatSessions/{sessionId}/poll`<br>*Expected: Status "Active" with agent info* |
| 9️⃣ | **Update Agent Availability** | `PUT http://localhost:5066/api/Agents/{agentId}/availability`<br>*Body:* `{"isActive": false}` |
| 🔟 | **Test Overflow** | Create multiple sessions until queue approaches capacity |
| 1️⃣1️⃣ | **Mark Session Inactive** | `PUT http://localhost:5066/api/ChatSessions/{sessionId}/inactive` |

> **Note:** For step 8, set `QueueSettings.InactivityThresholdSeconds` in appsettings.json to 1000 for testing.

### ☑️ Verification Checklist

- ✓ Chat sessions can be created by customers
- ✓ Chats are properly queued when all agents are busy
- ✓ Chats are assigned based on agent capacity and seniority
- ✓ Agents can be marked available/unavailable
- ✓ Overflow team is activated during high volume periods
- ✓ Queue metrics are accurately calculated and reported
- ✓ Sessions can be marked as inactive when completed

## 👥 Team Structure

| Team | Composition | Schedule |
|------|-------------|----------|
| **Team A** | 1 team lead, 2 mid-level, 1 junior agent | Morning shift |
| **Team B** | 1 senior, 1 mid-level, 2 junior agents | Afternoon shift |
| **Team C** | 2 mid-level agents | Night shift |
| **Overflow** | 6 junior-level agents | Activated on high volume |

## 🧪 Running the Test Suite

```bash
# From the solution root:
.\RunAllTests.bat
```

This will:
- Run all unit tests (Domain, Application, Infrastructure)
- Start the application in a test environment
- Run the functional tests that exercise the full API
- Automatically shut down the test environment when complete

The test results will be displayed in the console output.
