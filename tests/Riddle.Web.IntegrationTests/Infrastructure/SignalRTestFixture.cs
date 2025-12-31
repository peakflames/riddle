using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Riddle.Web.Data;

namespace Riddle.Web.IntegrationTests.Infrastructure;

/// <summary>
/// Test fixture that provides a WebApplicationFactory configured for SignalR integration testing.
/// Uses REAL services with an in-memory database for realistic testing without mocks.
/// </summary>
public class SignalRTestFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;
    private readonly List<HubConnection> _connections = [];
    
    /// <summary>
    /// A test campaign ID that can be used across tests
    /// </summary>
    public Guid TestCampaignId { get; } = Guid.CreateVersion7();
    
    public WebApplicationFactory<Program> Factory => _factory 
        ?? throw new InvalidOperationException("Fixture not initialized");
    
    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                
                builder.ConfigureServices(services =>
                {
                    // Remove ALL database-related registrations to avoid provider conflicts
                    // This is critical: SQLite and InMemory cannot coexist
                    
                    // Remove DbContext options (generic and typed)
                    var dbContextOptionsDescriptors = services
                        .Where(d => d.ServiceType == typeof(DbContextOptions<RiddleDbContext>) ||
                                   d.ServiceType == typeof(DbContextOptions))
                        .ToList();
                    foreach (var descriptor in dbContextOptionsDescriptors)
                    {
                        services.Remove(descriptor);
                    }
                    
                    // Remove DbContext registrations
                    services.RemoveAll<RiddleDbContext>();
                    
                    // Remove any IDbContextFactory registrations
                    services.RemoveAll(typeof(IDbContextFactory<RiddleDbContext>));
                    
                    // Remove ALL EF Core-related service registrations that might cache SQLite
                    var efCoreDescriptors = services
                        .Where(d => d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true ||
                                   d.ImplementationType?.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true)
                        .ToList();
                    foreach (var descriptor in efCoreDescriptors)
                    {
                        services.Remove(descriptor);
                    }
                    
                    // Add fresh in-memory database with unique name per test run
                    var uniqueDbName = $"TestDb_{Guid.NewGuid()}";
                    services.AddDbContext<RiddleDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(uniqueDbName);
                    });
                    
                    // REAL services (IGameStateService, ICombatService, INotificationService)
                    // are used as-is - no mocking. They'll work with the in-memory DB.
                    
                    // NOTE: SignalR is already configured in Program.cs
                    // Do NOT add it again here - that would create duplicate hub contexts!
                });
                
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Debug);
                    logging.AddDebug();
                });
            });
        
        // Ensure the server is started
        _ = _factory.Server;
        
        await Task.CompletedTask;
    }
    
    public async Task DisposeAsync()
    {
        foreach (var connection in _connections)
        {
            if (connection.State != HubConnectionState.Disconnected)
            {
                await connection.StopAsync();
            }
            await connection.DisposeAsync();
        }
        _connections.Clear();
        
        _factory?.Dispose();
    }
    
    /// <summary>
    /// Creates a SignalR hub connection for testing.
    /// </summary>
    public HubConnection CreateHubConnection()
    {
        var server = Factory.Server;
        var connection = new HubConnectionBuilder()
            .WithUrl($"{server.BaseAddress}gamehub", options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .Build();
        
        _connections.Add(connection);
        return connection;
    }
    
    /// <summary>
    /// Creates a test SignalR client connected to the hub.
    /// </summary>
    public async Task<TestSignalRClient> CreateClientAsync(string clientType = "player")
    {
        var connection = CreateHubConnection();
        var client = new TestSignalRClient(connection, Guid.NewGuid().ToString(), clientType);
        await connection.StartAsync();
        return client;
    }
    
    /// <summary>
    /// Creates a DM client and joins a campaign.
    /// </summary>
    public async Task<TestSignalRClient> CreateDmClientAsync(Guid campaignId, string? userId = null)
    {
        var client = await CreateClientAsync("dm");
        await client.JoinCampaignAsync(campaignId, userId ?? Guid.NewGuid().ToString(), null, isDm: true);
        return client;
    }
    
    /// <summary>
    /// Creates a Player client and joins a campaign.
    /// </summary>
    public async Task<TestSignalRClient> CreatePlayerClientAsync(Guid campaignId, string? userId = null, string? characterId = null)
    {
        var client = await CreateClientAsync("player");
        await client.JoinCampaignAsync(campaignId, userId ?? Guid.NewGuid().ToString(), characterId, isDm: false);
        return client;
    }
    
    /// <summary>
    /// Creates a scoped service provider for accessing services.
    /// Caller is responsible for disposing the scope.
    /// </summary>
    public IServiceScope CreateScope()
    {
        return Factory.Services.CreateScope();
    }
    
    /// <summary>
    /// Sets up a test campaign in the real database.
    /// Always uses a unique ID to avoid conflicts between tests.
    /// </summary>
    public async Task<CampaignInstance> SetupTestCampaignAsync(
        Guid? campaignId = null,
        string name = "Test Campaign",
        string dmUserId = "dm-user-123",
        List<Character>? party = null)
    {
        // Always generate unique ID per call - don't use shared TestCampaignId
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
    
    /// <summary>
    /// Gets a test character ID from the campaign (first character in party).
    /// </summary>
    public async Task<string?> GetTestCharacterIdAsync(Guid campaignId)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RiddleDbContext>();
        var campaign = await dbContext.CampaignInstances.FindAsync(campaignId);
        return campaign?.PartyState.FirstOrDefault()?.Id;
    }
    
    /// <summary>
    /// Gets the NotificationService from the DI container.
    /// </summary>
    public INotificationService GetNotificationService()
    {
        return Factory.Services.GetRequiredService<INotificationService>();
    }
    
    /// <summary>
    /// Gets a service from the DI container.
    /// </summary>
    public T GetService<T>() where T : notnull
    {
        return Factory.Services.GetRequiredService<T>();
    }
}
