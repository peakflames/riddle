# LLM Tool E2E Testing Philosophy

This document captures the testing philosophy and patterns for end-to-end tests of LLM tools in Project Riddle.

## Core Philosophy: Test Tools, Not Transport

The primary purpose of E2E tests is to verify that **when the LLM invokes a tool, the expected UI changes occur**. We test the complete vertical slice:

```
LLM Tool Call → ToolExecutor → Service Layer → SignalR → Blazor UI → DOM
```

### Why Not Just Test SignalR Events?

Transport-layer tests verify that SignalR events are sent and received correctly. However, they **cannot** catch sender/receiver contract mismatches. For example:

- **Sender (ToolExecutor.cs)**: Sends `Key="current_hp"` (snake_case from LLM JSON)
- **Receiver (CombatTracker.razor)**: Expects `Key=="CurrentHp"` (PascalCase)

Transport tests pass because the message flows correctly. But the UI never updates because the key comparison fails silently. **Only E2E tests that verify actual DOM state changes can catch these bugs.**

### What We Actually Test

Each E2E test class corresponds to an LLM tool:

| Test Class | LLM Tool | What It Verifies |
|------------|----------|------------------|
| `UpdateCharacterStateToolTests` | `update_character_state` | HP/stats changes reflect in CombatTracker |
| `StartCombatToolTests` | `start_combat` | Combat UI appears with correct initiative order |
| `SendMessageToolTests` | `send_message` | Messages appear in chat panel |

## Project Structure

```
tests/Riddle.Web.IntegrationTests/
├── E2ETests/
│   └── UpdateCharacterStateToolTests.cs    # One class per LLM tool
├── Infrastructure/
│   ├── CustomWebApplicationFactory.cs       # Dual-host Kestrel server
│   ├── PlaywrightFixture.cs                 # Browser lifecycle
│   └── E2ETestCollection.cs                 # xUnit collection definition
└── GlobalUsings.cs
```

## Test Naming Convention

**Pattern:** `{ToolName}ToolTests`

Tests are named for the LLM tool they exercise, making it immediately clear what system capability is being validated:

```csharp
public class UpdateCharacterStateToolTests : IAsyncLifetime
public class StartCombatToolTests : IAsyncLifetime  
public class RollDiceToolTests : IAsyncLifetime
```

## Canonical Test Structure

Every tool test follows this pattern:

```csharp
[Fact]
public async Task Should_UpdateUI_When_ToolExecutes()
{
    // 1. ARRANGE: Seed database with required state
    var campaign = await _factory.SetupTestCampaignAsync(...);
    
    // 2. ARRANGE: Navigate browser to page showing affected UI
    await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
        new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    
    // 3. ARRANGE: Wait for UI to render and verify initial state
    await _page.WaitForSelectorAsync("[data-testid='target-element']");
    await Expect(hpLocator).ToHaveTextAsync("30");
    
    // 4. ACT: Execute the LLM tool via DI container
    using var scope = _factory.CreateScope();
    var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
    await toolExecutor.ExecuteAsync(campaignId, "tool_name", argsJson);
    
    // 5. ASSERT: Verify UI reflects the change (with polling)
    await Expect(hpLocator).ToHaveTextAsync("20", 
        new LocatorAssertionsToHaveTextOptions { Timeout = 5000 });
}
```

## Real Example: UpdateCharacterStateToolTests

```csharp
[Fact]
public async Task Should_UpdateCombatTrackerHp_When_ToolExecutorChangesCharacterHp()
{
    // Arrange - Create test campaign with character
    const string testCharacterId = "test-hero-001";
    const string testCharacterName = "TestHero";
    const int initialHp = 30;
    const int updatedHp = 20;
    
    var campaign = await _factory.SetupTestCampaignAsync(
        name: "E2E Test Campaign",
        dmUserId: TestAuthHandler.TestUserId,
        party:
        [
            new Character
            {
                Id = testCharacterId,
                Name = testCharacterName,
                Type = "PC",
                MaxHp = initialHp,
                CurrentHp = initialHp,
                // ...
            }
        ]);
    
    // Arrange - Start combat (required for CombatTracker to display)
    using (var scope = _factory.CreateScope())
    {
        var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
        await combatService.StartCombatAsync(campaign.Id, [...]);
    }
    
    // Arrange - Navigate and verify initial state
    await _page.GotoAsync($"{_factory.ServerAddress}/dm/{campaign.Id}",
        new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    
    var combatantSelector = $"[data-testid='combatant-{testCharacterId}']";
    await _page.WaitForSelectorAsync(combatantSelector);
    
    var hpLocator = _page.Locator($"{combatantSelector} [data-testid='hp-current']");
    await Expect(hpLocator).ToHaveTextAsync(initialHp.ToString());
    
    // Act - Execute the LLM tool
    using (var scope = _factory.CreateScope())
    {
        var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
        
        var argumentsJson = $$"""
        {
            "character_name": "{{testCharacterName}}",
            "key": "current_hp",
            "value": {{updatedHp}}
        }
        """;
        
        await toolExecutor.ExecuteAsync(campaign.Id, "update_character_state", argumentsJson);
    }
    
    // Assert - UI should update via SignalR
    await Expect(hpLocator).ToHaveTextAsync(updatedHp.ToString(),
        new LocatorAssertionsToHaveTextOptions { Timeout = 5000 });
}
```

## Infrastructure: The Donbavand/Costello Pattern

`WebApplicationFactory.Server.BaseAddress` returns `http://localhost/` - an in-memory TestServer that Playwright cannot connect to. The solution is the **dual-host pattern**:

```csharp
protected override IHost CreateHost(IHostBuilder builder)
{
    // Configure Kestrel on the EXISTING builder (preserves all config)
    builder.ConfigureWebHost(webBuilder =>
    {
        webBuilder.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
    });
    
    // Build and start Kestrel host
    _kestrelHost = builder.Build();
    _kestrelHost.Start();
    
    // Capture the dynamic port
    var server = _kestrelHost.Services.GetRequiredService<IServer>();
    var addresses = server.Features.Get<IServerAddressesFeature>()!;
    ServerAddress = addresses.Addresses.First();
    
    // Return a dummy TestServer (required by WebApplicationFactory internals)
    var dummyBuilder = new HostBuilder();
    dummyBuilder.ConfigureWebHost(wb => wb.UseTestServer().Configure(_ => { }));
    var dummyHost = dummyBuilder.Build();
    dummyHost.Start();
    return dummyHost;
}
```

## Key Patterns

### 1. Use `data-testid` Attributes

Components must expose `data-testid` attributes for reliable selectors:

```razor
<!-- CombatantCard.razor -->
<div data-testid="combatant-@Character.Id">
    <span data-testid="hp-current">@Character.CurrentHp</span>
</div>
```

### 2. Use `Expect()` Polling for SignalR

SignalR events are asynchronous. Never use immediate assertions:

```csharp
// ❌ WRONG - Races with async SignalR
var text = await page.Locator("[data-testid='hp-current']").TextContentAsync();
text.Should().Be("20");

// ✅ CORRECT - Polls until condition met or timeout
await Expect(page.Locator("[data-testid='hp-current']"))
    .ToHaveTextAsync("20", new() { Timeout = 5000 });
```

### 3. Use `NetworkIdle` for Blazor Navigation

Blazor Server renders asynchronously. Wait for the page to fully hydrate:

```csharp
await page.GotoAsync(url, new PageGotoOptions 
{ 
    WaitUntil = WaitUntilState.NetworkIdle 
});
```

### 4. Get Services from Kestrel Host via `CreateScope()`

```csharp
// ✅ CORRECT - Uses Kestrel host's DI container
using var scope = _factory.CreateScope();
var toolExecutor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
```

---

## Hard-Won Lessons

These lessons came from debugging E2E test failures. Each represents hours of troubleshooting.

### 1. Kestrel Config Must Apply BEFORE Build

The `ConfigureWebHost` calls must apply to the builder BEFORE calling `Build()`:

```csharp
// ❌ WRONG - Separate builder loses all config
var kestrelBuilder = new HostBuilder();  // Fresh builder!
_kestrelHost = kestrelBuilder.Build();   // Missing auth, DB, services

// ✅ CORRECT - Same builder, add Kestrel config
builder.ConfigureWebHost(wb => wb.UseKestrel(...));
_kestrelHost = builder.Build();  // Has ALL configuration
```

### 2. Blazor Requires AuthenticationStateProvider

When replacing Identity with test auth, you need BOTH:

```csharp
// HTTP auth handler (for middleware)
services.AddAuthentication("Test")
    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);

// Blazor auth state (for <AuthorizeView> and [Authorize])
services.AddScoped<AuthenticationStateProvider, TestAuthenticationStateProvider>();
```

### 3. Remove ALL EF Core Services Before Adding InMemory

SQLite and InMemory providers conflict. Remove everything:

```csharp
// Remove DbContextOptions (both generic and typed)
var dbOptions = services.Where(d => 
    d.ServiceType == typeof(DbContextOptions<RiddleDbContext>) ||
    d.ServiceType == typeof(DbContextOptions)).ToList();
foreach (var d in dbOptions) services.Remove(d);

// Remove DbContext and factory
services.RemoveAll<RiddleDbContext>();
services.RemoveAll(typeof(IDbContextFactory<RiddleDbContext>));

// Remove all EF Core namespace services (they cache provider info)
var efServices = services.Where(d => 
    d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true)
    .ToList();
foreach (var d in efServices) services.Remove(d);

// NOW add InMemory
services.AddDbContext<RiddleDbContext>(o => 
    o.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
```

### 4. Disable HTTPS Redirect for Tests

```csharp
// In Program.cs
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}
```

### 5. Skip Identity Endpoints in Testing

```csharp
// In Program.cs
if (!app.Environment.IsEnvironment("Testing"))
{
    app.MapAdditionalIdentityEndpoints();
}
```

### 6. Share Test User Constants

```csharp
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string TestUserId = "test-dm-user-123";
    public const string TestUserName = "Test DM";
    // Use in both TestAuthHandler and TestAuthenticationStateProvider
}
```

---

## What We Deliberately Don't Test

We removed transport-layer tests (e.g., `CharacterStateEventTests`, `PlayerChoiceEventTests`) because:

1. **They don't catch contract mismatches** - The bug that inspired E2E testing was a snake_case/PascalCase mismatch that passed all transport tests
2. **They test implementation, not behavior** - Verifying "SignalR sends X event" isn't valuable if the UI ignores it
3. **They duplicate coverage** - If E2E tests pass, transport must be working

The E2E tests are our **single source of truth** for LLM tool behavior.
