namespace Riddle.Web.IntegrationTests.Infrastructure;

/// <summary>
/// Collection definition for E2E tests using real Kestrel server + Playwright.
/// Uses CustomWebApplicationFactory (Donbavand/Costello pattern) for proper Playwright integration.
/// </summary>
[CollectionDefinition("E2E")]
public class E2ETestCollection : ICollectionFixture<CustomWebApplicationFactory>, ICollectionFixture<PlaywrightFixture>
{
}
