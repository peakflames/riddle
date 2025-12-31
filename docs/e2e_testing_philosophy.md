# E2E Testing Philosophy for Blazor Server + SignalR

This document captures the testing philosophy and patterns for end-to-end tests in Project Riddle.

## Why E2E Tests?

Transport-layer integration tests verify that SignalR events are sent and received, but they **cannot** catch sender/receiver contract mismatches. For example:

- **Sender (ToolExecutor.cs)**: Sends `Key="current_hp"` (snake_case)
- **Receiver (CombatTracker.razor)**: Expects `Key=="CurrentHp"` (PascalCase)

Transport tests pass because the message flows correctly. But the UI never updates because the key comparison fails silently. Only E2E tests that verify actual DOM state changes can catch these bugs.

## The Donbavand/Costello Pattern

`WebApplicationFactory.Server.BaseAddress` returns `http://localhost/` - an in-memory TestServer that Playwright cannot connect to. The solution is the **dual-host pattern**:

```csharp
protected override IHost CreateHost(IHostBuilder builder)
{
    // Host 1: TestServer (required by WebApplicationFactory internals)
    var testHost = builder.Build();
    
    // Host 2: Real Kestrel server for Playwright
    builder.ConfigureWebHost(wb => wb.UseKestrel(o => o.ListenLocalhost(0)));
    _kestrelHost = builder.Build();
    _kestrelHost.Start();
    
    // Capture dynamic port
    var server = _kestrelHost.Services.GetRequiredService<IServer>();
    var addresses = server.Features.Get<IServerAddressesFeature>();
    ClientOptions.BaseAddress = new Uri(addresses!.Addresses.First());
    
    testHost.Start();
    return testHost;  // Return TestServer (required)
}
```

## Blazor Server Async Rendering

Blazor Server uses a two-phase rendering model:

1. **`OnInitialized`** (sync) - Initial component setup
2. **`OnAfterRenderAsync`** (async) - Data fetching, SignalR connections

When navigating to a page, the initial HTML arrives quickly but async data loads later. Use `WaitUntil.NetworkIdle` to ensure the page is fully hydrated:

```csharp
await page.GotoAsync(url, new PageGotoOptions 
{ 
    WaitUntil = WaitUntilState.NetworkIdle 
});
```

## Expect() Polling for SignalR Propagation

SignalR events are asynchronous. Never use immediate assertions - use Playwright's `Expect()` which polls until the condition is met or timeout:

```csharp
// ❌ WRONG - Immediate assertion, races with SignalR
var text = await page.Locator("[data-testid='hp-current']").TextContentAsync();
text.Should().Be("20");

// ✅ CORRECT - Polls until HP updates or timeout
await Expect(page.Locator("[data-testid='hp-current']"))
    .ToHaveTextAsync("20", new LocatorAssertionsToHaveTextOptions { Timeout = 5000 });
```

Alternative using `WaitForFunctionAsync`:

```csharp
await page.WaitForFunctionAsync("""
    () => {
        const el = document.querySelector('[data-testid="hp-current"]');
        return el && el.textContent === '20';
    }
""", new PageWaitForFunctionOptions { Timeout = 5000 });
```

## Page Readiness Assertions

Before testing SignalR updates, verify the page is in a known state:

```csharp
// 1. Navigate with NetworkIdle
await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

// 2. Wait for specific element to exist
await page.WaitForSelectorAsync("[data-testid='combatant-hero-001']", 
    new PageWaitForSelectorOptions { Timeout = 10000 });

// 3. Verify initial state before acting
await Expect(page.Locator("[data-testid='hp-current']")).ToHaveTextAsync("30");

// 4. Now perform the action and verify the change
```

## Trace Capture for Debugging

Record traces for failed tests to debug timing issues and race conditions:

```csharp
// In test setup
_context = await _browser.NewContextAsync(new BrowserNewContextOptions
{
    RecordVideoDir = "test-videos/",
});

// Start tracing
await _context.Tracing.StartAsync(new TracingStartOptions
{
    Screenshots = true,
    Snapshots = true,
    Sources = true
});

// In teardown - save on failure
await _context.Tracing.StopAsync(new TracingStopOptions
{
    Path = $"traces/{TestContext.CurrentContext.Test.Name}.zip"
});
```

View traces at: https://trace.playwright.dev/

## data-testid Selectors

Components must expose `data-testid` attributes for reliable test selectors:

```razor
<!-- CombatantCard.razor -->
<div data-testid="combatant-@Character.Id">
    <span data-testid="hp-current">@Character.CurrentHp</span>
    <span data-testid="hp-max">@Character.MaxHp</span>
</div>
```

Benefits:
- Decoupled from CSS classes (styling changes don't break tests)
- Decoupled from text content (localization doesn't break tests)
- Explicit test contract between components and E2E tests

## Belt & Suspenders Philosophy

E2E tests are inherently flaky due to timing. Use multiple layers of verification:

1. **NetworkIdle** - Wait for initial page load
2. **WaitForSelector** - Wait for specific element existence
3. **Initial State Assertion** - Verify starting conditions
4. **Action** - Trigger the change (e.g., tool execution)
5. **Polling Assertion** - Wait for expected end state
6. **Final Verification** - Explicit assertion for test output clarity

```csharp
// Belt & suspenders example
await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
await page.WaitForSelectorAsync("[data-testid='combatant-hero']");
await Expect(page.Locator("[data-testid='hp-current']")).ToHaveTextAsync("30");

// Execute action via DI
await toolExecutor.ExecuteAsync(campaignId, "update_character_state", args);

// Polling wait
await Expect(page.Locator("[data-testid='hp-current']"))
    .ToHaveTextAsync("20", new() { Timeout = 5000 });

// Final verification (for test output clarity)
var finalHp = await page.Locator("[data-testid='hp-current']").TextContentAsync();
finalHp.Should().Be("20", "HP should update after tool execution");
```

## Test Structure Pattern

```csharp
[Fact]
public async Task Should_UpdateUI_When_SignalREventReceived()
{
    // Arrange - Setup data
    var campaign = await _factory.SetupTestCampaignAsync(...);
    
    // Arrange - Navigate and verify initial state
    await _page.GotoAsync($"{_factory.ServerAddress}/dm/campaign/{campaign.Id}",
        new() { WaitUntil = WaitUntilState.NetworkIdle });
    await _page.WaitForSelectorAsync("[data-testid='target-element']");
    await Expect(...).ToHaveTextAsync("initial value");
    
    // Act - Trigger change via server-side action
    using var scope = _factory.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<IService>();
    await service.DoSomethingAsync(...);
    
    // Assert - Wait for UI update with polling
    await Expect(...).ToHaveTextAsync("updated value", new() { Timeout = 5000 });
}
```

## Common Pitfalls

### 1. Using TestServer BaseAddress with Playwright
```csharp
// ❌ WRONG - TestServer is in-memory only
var baseUrl = _factory.Server.BaseAddress.ToString();

// ✅ CORRECT - Use CustomWebApplicationFactory.ServerAddress
var baseUrl = _factory.ServerAddress;
```

### 2. Immediate Assertions After SignalR
```csharp
// ❌ WRONG - Races with async SignalR propagation
await notificationService.SendAsync(...);
var text = await page.Locator(...).TextContentAsync();
text.Should().Be("expected");  // FLAKY!

// ✅ CORRECT - Poll until condition or timeout
await Expect(page.Locator(...)).ToHaveTextAsync("expected", new() { Timeout = 5000 });
```

### 3. Forgetting NetworkIdle for Blazor
```csharp
// ❌ WRONG - HTML arrives but data not loaded
await page.GotoAsync(url);

// ✅ CORRECT - Wait for async rendering complete
await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
```

### 4. Using DI from Wrong Host
```csharp
// ❌ WRONG - Gets services from TestServer (might differ from Kestrel)
var service = _factory.Services.GetRequiredService<IService>();

// ✅ CORRECT - Use CreateScope() which accesses Kestrel host
using var scope = _factory.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<IService>();
```

---

## Lessons Learned: ASP.NET Core + Blazor + Playwright Integration

These hard-won lessons came from debugging E2E test failures. Each represents a gotcha that caused hours of troubleshooting.

### 1. Dual-Host Pattern: Kestrel Config MUST Apply BEFORE Build

The naive approach creates a Kestrel host from a separate builder, but this loses all `ConfigureWebHost` configuration (environment, services, authentication).

```csharp
// ❌ WRONG - Kestrel host misses ConfigureWebHost settings
protected override IHost CreateHost(IHostBuilder builder)
{
    var testHost = builder.Build();  // Gets all ConfigureWebHost config
    
    var kestrelBuilder = new HostBuilder();  // FRESH builder - no config!
    kestrelBuilder.ConfigureWebHost(wb => wb.UseKestrel());
    _kestrelHost = kestrelBuilder.Build();  // Missing auth, DB, services
    ...
}

// ✅ CORRECT - Configure Kestrel THEN build (config already applied)
protected override IHost CreateHost(IHostBuilder builder)
{
    // ConfigureWebHost already called by this point - add Kestrel to same builder
    builder.ConfigureWebHost(wb => wb.UseKestrel(o => o.Listen(IPAddress.Loopback, 0)));
    
    _kestrelHost = builder.Build();  // Has ALL configuration
    _kestrelHost.Start();
    
    // Return a dummy TestServer for WAF internals
    var dummyBuilder = new HostBuilder();
    dummyBuilder.ConfigureWebHost(wb => wb.UseTestServer().Configure(app => { }));
    return dummyBuilder.Build();
}
```

### 2. Blazor Server Requires AuthenticationStateProvider (Not Just AuthenticationHandler)

When replacing Identity services with test auth, you need BOTH:
- `AuthenticationHandler` - For HTTP request authentication (middleware)
- `AuthenticationStateProvider` - For Blazor component auth state (`<AuthorizeView>`, `@attribute [Authorize]`)

```csharp
// ❌ WRONG - HTTP auth works but Blazor components throw
services.AddAuthentication("Test")
    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);

// ✅ CORRECT - Both HTTP and Blazor auth covered
services.AddAuthentication("Test")
    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
services.AddScoped<AuthenticationStateProvider, TestAuthenticationStateProvider>();
```

**Error without AuthenticationStateProvider:**
```
InvalidOperationException: No service for type 
'Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider' has been registered.
```

### 3. In-Memory Database: Must Remove ALL EF Core Registrations

SQLite and InMemory providers conflict. When switching to InMemory for tests, remove ALL EF Core services - not just `DbContextOptions`:

```csharp
// Remove DbContext options (generic and typed)
var dbContextOptionsDescriptors = services
    .Where(d => d.ServiceType == typeof(DbContextOptions<RiddleDbContext>) ||
               d.ServiceType == typeof(DbContextOptions))
    .ToList();
foreach (var descriptor in dbContextOptionsDescriptors)
    services.Remove(descriptor);

// Remove DbContext registrations
services.RemoveAll<RiddleDbContext>();

// Remove any IDbContextFactory registrations
services.RemoveAll(typeof(IDbContextFactory<RiddleDbContext>));

// Remove ALL EF Core-related service registrations (providers cache internally)
var efCoreDescriptors = services
    .Where(d => d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true ||
               d.ImplementationType?.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true)
    .ToList();
foreach (var descriptor in efCoreDescriptors)
    services.Remove(descriptor);

// NOW add InMemory
services.AddDbContext<RiddleDbContext>(options => 
    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
```

### 4. HTTPS Redirect Must Be Disabled for E2E Tests

If your app uses `UseHttpsRedirection()`, HTTP requests will redirect to HTTPS - but your Kestrel test server only listens on HTTP. Either:

A) Add to `Program.cs`:
```csharp
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}
```

B) Or use this workaround in test navigation:
```csharp
_context = await _browser.NewContextAsync(new BrowserNewContextOptions
{
    IgnoreHTTPSErrors = true  // Allows HTTP even when app wants HTTPS
});
```

### 5. Identity Endpoints Must Skip in Testing Environment

If using ASP.NET Identity, the `MapAdditionalIdentityEndpoints()` extension adds routes that conflict with test authentication. Skip them in Testing:

```csharp
// In Program.cs
if (!app.Environment.IsEnvironment("Testing"))
{
    app.MapAdditionalIdentityEndpoints();
}
```

**Error without this:**
```
InvalidOperationException: A suitable method 'MapIdentityApi<TUser>' could not be found
```

### 6. Kestrel Requires IPAddress.Loopback, Not "localhost" String

When binding Kestrel to a dynamic port, use `IPAddress.Loopback` not `"localhost"`:

```csharp
// ❌ WRONG - Kestrel doesn't accept localhost:0
options.ListenLocalhost(0);  // May fail or bind incorrectly

// ✅ CORRECT - Explicit loopback IP with dynamic port
options.Listen(IPAddress.Loopback, 0);
```

### 7. Enable Developer Exception Page for Debugging 500 Errors

When E2E tests get 500 errors, the default error page hides details. Enable developer exceptions in Testing:

```csharp
// In Program.cs  
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseDeveloperExceptionPage();
}
```

Then save the page content for inspection:
```csharp
var response = await page.GotoAsync(url);
if ((int)response.Status >= 400)
{
    var content = await page.ContentAsync();
    await File.WriteAllTextAsync("error-page.html", content);
    throw new Exception($"Server returned {response.Status}. View error page at: error-page.html");
}
```

### 8. Remove ALL Authentication Services Before Adding Test Auth

ASP.NET Identity registers many services. Remove them ALL before adding test authentication:

```csharp
// Remove ALL existing authentication services
var authDescriptors = services
    .Where(d => d.ServiceType.FullName?.Contains("Authentication") == true ||
               d.ServiceType.FullName?.Contains("Identity") == true ||
               d.ImplementationType?.FullName?.Contains("Authentication") == true ||
               d.ImplementationType?.FullName?.Contains("Identity") == true)
    .ToList();
foreach (var descriptor in authDescriptors)
    services.Remove(descriptor);

// NOW add test auth with Test as DEFAULT scheme
services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Test";
    options.DefaultChallengeScheme = "Test";
    options.DefaultScheme = "Test";
})
.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
```

### 9. Test User Constants Should Be Shared

Define test user info as constants and share between `TestAuthHandler` and `TestAuthenticationStateProvider`:

```csharp
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string TestUserId = "test-dm-user-123";
    public const string TestUserName = "Test DM";
    public const string TestUserEmail = "testdm@test.com";
    
    // Use these constants in both classes
}

public class TestAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestAuthHandler.TestUserId),  // Same user!
            // ...
        };
        // ...
    }
}
```
