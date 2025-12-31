using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Riddle.Web.Data;
using System.Net;
using System.Net.Sockets;

namespace Riddle.Web.IntegrationTests.Infrastructure;

/// <summary>
/// Test fixture that starts a real Kestrel server on a TCP port.
/// Required for Playwright E2E tests which need actual HTTP connectivity.
/// </summary>
public class KestrelTestFixture : IAsyncLifetime
{
    private IHost? _host;
    private int _port;
    
    /// <summary>
    /// The base URL of the running test server (e.g., "http://localhost:5555")
    /// </summary>
    public string BaseUrl => $"http://localhost:{_port}";
    
    /// <summary>
    /// Campaign ID for tests
    /// </summary>
    public Guid TestCampaignId { get; } = Guid.CreateVersion7();
    
    public async Task InitializeAsync()
    {
        _port = GetAvailablePort();
        
        _host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Program>();
                webBuilder.UseUrls($"http://localhost:{_port}");
                webBuilder.UseEnvironment("Testing");
                
                webBuilder.ConfigureServices(services =>
                {
                    // Remove ALL database-related registrations
                    var dbContextOptionsDescriptors = services
                        .Where(d => d.ServiceType == typeof(DbContextOptions<RiddleDbContext>) ||
                                   d.ServiceType == typeof(DbContextOptions))
                        .ToList();
                    foreach (var descriptor in dbContextOptionsDescriptors)
                    {
                        services.Remove(descriptor);
                    }
                    
                    services.RemoveAll<RiddleDbContext>();
                    services.RemoveAll(typeof(IDbContextFactory<RiddleDbContext>));
                    
                    // Remove EF Core services that cache SQLite
                    var efCoreDescriptors = services
                        .Where(d => d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true ||
                                   d.ImplementationType?.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true)
                        .ToList();
                    foreach (var descriptor in efCoreDescriptors)
                    {
                        services.Remove(descriptor);
                    }
                    
                    // Add fresh in-memory database
                    var uniqueDbName = $"E2E_TestDb_{Guid.NewGuid()}";
                    services.AddDbContext<RiddleDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(uniqueDbName);
                    });
                });
                
                webBuilder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Warning);
                });
            })
            .Build();
        
        await _host.StartAsync();
    }
    
    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
    
    /// <summary>
    /// Get services from the running host's DI container.
    /// </summary>
    public IServiceScope CreateScope()
    {
        return _host!.Services.CreateScope();
    }
    
    /// <summary>
    /// Sets up a test campaign in the database.
    /// </summary>
    public async Task<CampaignInstance> SetupTestCampaignAsync(
        Guid? campaignId = null,
        string name = "Test Campaign",
        string dmUserId = "dm-user-123",
        List<Character>? party = null)
    {
        var id = campaignId ?? Guid.CreateVersion7();
        
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RiddleDbContext>();
        
        var campaign = new CampaignInstance
        {
            Id = id,
            Name = name,
            DmUserId = dmUserId,
            CampaignModule = "Test Module",
            InviteCode = $"TEST{id.ToString()[..4].ToUpper()}",
            PartyState = party ?? new List<Character>
            {
                new Character
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Test Fighter",
                    Type = "PC",
                    Class = "Fighter",
                    Race = "Human",
                    Level = 1,
                    MaxHp = 12,
                    CurrentHp = 12,
                    ArmorClass = 16
                }
            },
            LastActivityAt = DateTime.UtcNow
        };
        
        dbContext.CampaignInstances.Add(campaign);
        await dbContext.SaveChangesAsync();
        
        return campaign;
    }
    
    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

/// <summary>
/// Collection definition for E2E tests using real Kestrel server + Playwright.
/// Uses CustomWebApplicationFactory (Donbavand/Costello pattern) for proper Playwright integration.
/// </summary>
[CollectionDefinition("E2E")]
public class E2ETestCollection : ICollectionFixture<CustomWebApplicationFactory>, ICollectionFixture<PlaywrightFixture>
{
}
