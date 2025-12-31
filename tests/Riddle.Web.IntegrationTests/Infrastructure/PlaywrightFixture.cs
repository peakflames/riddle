using Microsoft.Playwright;

namespace Riddle.Web.IntegrationTests.Infrastructure;

/// <summary>
/// xUnit fixture for Playwright browser lifecycle management.
/// Shared across test classes via ICollectionFixture.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;
    
    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }
    
    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        Playwright.Dispose();
    }
    
    /// <summary>
    /// Creates a new browser context and page for test isolation.
    /// Each test should call this to get a fresh page.
    /// </summary>
    public async Task<IPage> NewPageAsync()
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });
        return await context.NewPageAsync();
    }
}

// E2ETestCollection moved to KestrelTestFixture.cs - uses CustomWebApplicationFactory for proper Playwright integration
