# Implementation Plan: SignalR Integration Testing

[Overview]
Add SignalR integration testing infrastructure to verify that LLM tool executions correctly publish events to the appropriate SignalR groups and that client subscribers properly handle those events.

This testing addresses synchronization bugs where events fire successfully on one dashboard (DM or Player) but fail to update the other. The tests will validate the complete event flow from ToolExecutor → NotificationService → SignalR Hub → Client Handlers without requiring browser automation.

The test strategy uses two layers:
1. **Service-Level Tests** - Fast, isolated tests that mock `IHubContext` to verify correct group targeting and payloads
2. **Hub-Level Integration Tests** - Tests with real SignalR connections to verify end-to-end event delivery

[Types]
Test project types and helper classes for SignalR client simulation.

### Test Client Helper
```csharp
public class TestSignalRClient : IAsyncDisposable
{
    public HubConnection Connection { get; }
    public string ClientId { get; }
    public string Group { get; }  // "dm", "players", or "all"
    public List<ReceivedEvent> ReceivedEvents { get; }
    
    public Task WaitForEventAsync(string eventName, TimeSpan timeout);
    public bool HasReceivedEvent(string eventName);
    public T? GetEventPayload<T>(string eventName);
}

public record ReceivedEvent(
    string EventName,
    object? Payload,
    DateTime ReceivedAt
);
```

### Test Fixtures
```csharp
public class SignalRTestFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory { get; }
    public HttpClient HttpClient { get; }
    public Guid TestCampaignId { get; }
    
    public Task<TestSignalRClient> CreateDmClientAsync(Guid campaignId);
    public Task<TestSignalRClient> CreatePlayerClientAsync(Guid campaignId, string? characterId = null);
}
```

[Files]
New test project and supporting files.

### New Files
- `tests/Riddle.Web.IntegrationTests/Riddle.Web.IntegrationTests.csproj` - xUnit test project
- `tests/Riddle.Web.IntegrationTests/Infrastructure/SignalRTestFixture.cs` - Test fixture for SignalR setup
- `tests/Riddle.Web.IntegrationTests/Infrastructure/TestSignalRClient.cs` - Helper for simulating connected clients
- `tests/Riddle.Web.IntegrationTests/Infrastructure/MockStateServices.cs` - Mock implementations of state services
- `tests/Riddle.Web.IntegrationTests/Services/NotificationServiceTests.cs` - Service-level tests
- `tests/Riddle.Web.IntegrationTests/Services/ToolExecutorNotificationTests.cs` - ToolExecutor → Notification tests
- `tests/Riddle.Web.IntegrationTests/Hub/CharacterStateEventTests.cs` - Hub-level tests for character updates
- `tests/Riddle.Web.IntegrationTests/Hub/CombatEventTests.cs` - Hub-level tests for combat events
- `tests/Riddle.Web.IntegrationTests/Hub/PlayerChoiceEventTests.cs` - Hub-level tests for player choices
- `tests/Riddle.Web.IntegrationTests/Hub/AtmosphericEventTests.cs` - Hub-level tests for atmospheric events

### Modified Files
- `src/Riddle.Web/Program.cs` - Ensure services are accessible for testing (may need `InternalsVisibleTo`)

[Functions]
Test methods organized by event flow.

### NotificationServiceTests
- `SendsCharacterStateUpdatedToAllGroup()` - Verify `_all` group targeting
- `SendsPlayerChoicesToPlayersGroupOnly()` - Verify `_players` group targeting
- `SendsReadAloudTextToDmGroupOnly()` - Verify `_dm` group targeting
- `SendsCombatStartedToAllGroup()` - Verify combat events reach all
- `SendsAtmospherePulseToPlayersOnly()` - Verify atmospheric events

### ToolExecutorNotificationTests
- `UpdateCharacterState_CurrentHp_NotifiesAllClients()` - HP update triggers notification
- `PresentPlayerChoices_NotifiesPlayersOnly()` - Choices sent to players
- `LogPlayerRoll_NotifiesAllClients()` - Roll results broadcast
- `DisplayReadAloudText_NotifiesDmOnly()` - Read aloud text to DM
- `StartCombat_NotifiesAllClients()` - Combat start broadcasts

### CharacterStateEventTests (Hub-Level)
- `DmAndPlayerBothReceiveCharacterStateUpdate()` - Verify both clients get event
- `CharacterStateUpdate_WithCurrentHpKey_ReceivedCorrectly()` - **KEY TEST** - Verifies payload key format
- `OnlyAllGroupReceivesCharacterStateUpdate()` - Verify isolation

### CombatEventTests (Hub-Level)
- `CombatStarted_ReceivedByBothDmAndPlayer()` - Combat start reaches all
- `TurnAdvanced_ReceivedByBothDmAndPlayer()` - Turn changes reach all
- `CombatEnded_ReceivedByBothDmAndPlayer()` - Combat end reaches all

### PlayerChoiceEventTests (Hub-Level)
- `PlayerChoicesReceived_OnlyByPlayers()` - DM should NOT receive
- `PlayerChoiceSubmitted_OnlyByDm()` - Players should NOT receive

### AtmosphericEventTests (Hub-Level)
- `AtmospherePulse_ReceivedByPlayersOnly()` - DM should NOT receive
- `NarrativeAnchor_ReceivedByPlayersOnly()` - DM should NOT receive

[Classes]
Helper classes for test infrastructure.

### New Classes

**SignalRTestFixture** (`Infrastructure/SignalRTestFixture.cs`)
- Manages `WebApplicationFactory<Program>` lifecycle
- Creates test campaign with mock data
- Provides factory methods for creating test clients
- Handles cleanup

**TestSignalRClient** (`Infrastructure/TestSignalRClient.cs`)
- Wraps `HubConnection` for testing
- Tracks all received events
- Provides async wait methods for event arrival
- Handles subscription to all known event types

**MockGameStateService** (`Infrastructure/MockStateServices.cs`)
- Implements `IGameStateService` with in-memory data
- Provides controllable campaign/character state

**MockCombatService** (`Infrastructure/MockStateServices.cs`)
- Implements `ICombatService` with in-memory data
- Provides controllable combat state

[Dependencies]
NuGet packages for the test project.

```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="10.0.0" />
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.0.2" />
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="FluentAssertions" Version="7.0.0" />
<PackageReference Include="coverlet.collector" Version="6.0.2" />
```

[Testing]
Test execution and validation approach.

### Running Tests
```bash
# Run all SignalR integration tests
dotnet test tests/Riddle.Web.IntegrationTests

# Run specific test class
dotnet test tests/Riddle.Web.IntegrationTests --filter "ClassName=CharacterStateEventTests"

# Run with verbose output
dotnet test tests/Riddle.Web.IntegrationTests --logger "console;verbosity=detailed"
```

### Expected Failures (Bugs to Discover)
The following test is expected to fail, revealing the `current_hp` vs `CurrentHp` key mismatch bug:
- `CharacterStateUpdate_WithCurrentHpKey_ReceivedCorrectly()` - Will fail because client handlers look for `CurrentHp` but `ToolExecutor` sends `current_hp`

### Coverage Goals
- All `NotificationService` public methods have coverage
- All `ToolExecutor` methods that call notifications have coverage
- All SignalR event types in `GameHubEvents` have at least one integration test

[Implementation Order]
Sequential steps for implementing the test infrastructure.

1. **Create test project** - Set up `Riddle.Web.IntegrationTests.csproj` with dependencies
2. **Create TestSignalRClient** - Build the client helper with event tracking
3. **Create SignalRTestFixture** - Build the test fixture for WebApplicationFactory
4. **Create mock services** - Implement `MockGameStateService` and `MockCombatService`
5. **Write NotificationService tests** - Service-level tests with mocked `IHubContext`
6. **Write ToolExecutor tests** - Verify tool → notification chain
7. **Write CharacterStateEventTests** - Hub-level tests including the bug-revealing test
8. **Write CombatEventTests** - Hub-level combat flow tests
9. **Write PlayerChoiceEventTests** - Hub-level choice flow tests
10. **Write AtmosphericEventTests** - Hub-level atmospheric event tests
11. **Add build.py integration** - Add `python build.py test` command
