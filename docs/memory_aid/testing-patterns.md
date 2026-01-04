# Testing Patterns

> **Keywords:** WebApplicationFactory, Playwright, integration tests, E2E, test isolation
> **Related:** [SignalR Patterns](./signalr-patterns.md), [EF Core Patterns](./ef-core-patterns.md)

This document covers integration and end-to-end testing patterns.

---

## WebApplicationFactory Setup

The `CustomWebApplicationFactory` configures the test host with an in-memory database:

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove production DbContext
            var descriptor = services.SingleOrDefault(d => 
                d.ServiceType == typeof(DbContextOptions<RiddleDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add test database
            services.AddDbContext<RiddleDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });
        });
    }
}
```

---

## E2E Test Structure

E2E tests use Playwright for browser automation:

```csharp
[Collection("E2E Tests")]
public class CombatEncounterTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _playwright;
    private IBrowser? _browser;
    private IPage? _page;

    public async Task InitializeAsync()
    {
        _browser = await _playwright.Playwright.Chromium.LaunchAsync();
        _page = await _browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_browser != null) await _browser.DisposeAsync();
    }
}
```

---

## Test Verification Workflow

For feature implementation verification:

1. **Run specific test:** `dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"`
2. **Run all tests:** `dotnet test tests/Riddle.Web.IntegrationTests`
3. **Check logs:** `python build.py log --level error` after test failures

---

## Database Seeding for Tests

Tests should seed their own data, not rely on shared state:

```csharp
private async Task SeedTestDataAsync()
{
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RiddleDbContext>();
    
    var campaign = new CampaignInstance { Name = "Test Campaign" };
    db.CampaignInstances.Add(campaign);
    await db.SaveChangesAsync();
}
