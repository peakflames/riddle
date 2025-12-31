using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Riddle.Web.Data;

namespace Riddle.Web.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory that starts a REAL Kestrel server on a TCP port.
/// Required for Playwright E2E tests which need actual HTTP connectivity.
/// 
/// Uses the Donbavand/Costello pattern: creates TWO hosts in CreateHost() -
/// a TestServer (required by WebApplicationFactory internals) and a real Kestrel
/// server that Playwright can connect to.
/// 
/// References:
/// - https://www.yourdevblog.com/posts/integration-testing-asp-net-core-with-playwright
/// - https://blog.martincostello.com/integration-testing-aspnetcore-with-playwright/
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private IHost? _kestrelHost;
    
    /// <summary>
    /// The base URL of the running Kestrel server (e.g., "http://localhost:5432")
    /// </summary>
    public string ServerAddress
    {
        get
        {
            EnsureServer();
            return ClientOptions.BaseAddress.ToString().TrimEnd('/');
        }
    }
    
    /// <summary>
    /// Campaign ID for tests - use Guid.CreateVersion7() for each test to avoid conflicts
    /// </summary>
    public Guid TestCampaignId { get; } = Guid.CreateVersion7();
    
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Configure Kestrel BEFORE the first build - this ensures the Kestrel host
        // gets all the same ConfigureWebHost configuration (including Testing environment)
        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseKestrel(options =>
            {
                // Listen on 127.0.0.1:0 for dynamic port binding (localhost:0 not supported)
                options.Listen(IPAddress.Loopback, 0);
            });
        });
        
        // Build and start the Kestrel host (this will have ALL our test configuration)
        _kestrelHost = builder.Build();
        _kestrelHost.Start();
        
        // Capture the dynamic port from Kestrel
        var server = _kestrelHost.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        
        if (addresses?.Addresses.Any() == true)
        {
            var address = addresses.Addresses.First();
            ClientOptions.BaseAddress = new Uri(address);
        }
        else
        {
            throw new InvalidOperationException("Kestrel server did not provide any addresses");
        }
        
        // Create a "dummy" TestServer host for WebApplicationFactory internals
        // The key insight: we only need ONE real host (Kestrel), but WAF requires
        // a return value from CreateHost. We return a minimal host wrapper.
        var dummyBuilder = new HostBuilder();
        dummyBuilder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseTestServer()
                .Configure(app => { }); // Empty pipeline - not used
        });
        var dummyHost = dummyBuilder.Build();
        dummyHost.Start();
        return dummyHost;
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
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
            var uniqueDbName = $"E2E_TestDb_{Guid.NewGuid()}";
            services.AddDbContext<RiddleDbContext>(options =>
            {
                options.UseInMemoryDatabase(uniqueDbName);
            });
            
            // REAL services (IGameStateService, ICombatService, INotificationService)
            // are used as-is - no mocking. They'll work with the in-memory DB.
            
            // CRITICAL: Remove ALL existing authentication services
            // ASP.NET Identity registers many services that must be cleared
            var authDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("Authentication") == true ||
                           d.ServiceType.FullName?.Contains("Identity") == true ||
                           d.ImplementationType?.FullName?.Contains("Authentication") == true ||
                           d.ImplementationType?.FullName?.Contains("Identity") == true)
                .ToList();
            foreach (var descriptor in authDescriptors)
            {
                services.Remove(descriptor);
            }
            
            // Configure test authentication with Test as DEFAULT scheme
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
            
            // CRITICAL: Add AuthenticationStateProvider for Blazor components
            // This was removed with Identity services but Blazor still needs it
            services.AddScoped<AuthenticationStateProvider, TestAuthenticationStateProvider>();
            
            // Override authorization - allow all authenticated users
            services.AddAuthorizationBuilder()
                .AddFallbackPolicy("TestFallback", policy => policy.RequireAuthenticatedUser());
        });
        
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Warning);
        });
    }
    
    /// <summary>
    /// Ensures the server is started by triggering CreateHost if needed.
    /// </summary>
    private void EnsureServer()
    {
        if (_kestrelHost is null)
        {
            // Trigger CreateHost by accessing CreateDefaultClient
            using var _ = CreateDefaultClient();
        }
    }
    
    /// <summary>
    /// Creates a scoped service provider for accessing services.
    /// Caller is responsible for disposing the scope.
    /// </summary>
    public IServiceScope CreateScope()
    {
        EnsureServer();
        return _kestrelHost!.Services.CreateScope();
    }
    
    /// <summary>
    /// Gets a service from the Kestrel host's DI container.
    /// </summary>
    public T GetService<T>() where T : notnull
    {
        EnsureServer();
        return _kestrelHost!.Services.GetRequiredService<T>();
    }
    
    /// <summary>
    /// Sets up a test campaign in the database.
    /// Always use a unique ID per test to avoid conflicts.
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
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _kestrelHost?.StopAsync().GetAwaiter().GetResult();
            _kestrelHost?.Dispose();
        }
        
        base.Dispose(disposing);
    }
}

/// <summary>
/// Test AuthenticationStateProvider for Blazor components.
/// Returns a fixed authenticated user state without requiring cookies/Identity.
/// </summary>
public class TestAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestAuthHandler.TestUserId),
            new Claim(ClaimTypes.Name, TestAuthHandler.TestUserName),
            new Claim(ClaimTypes.Email, TestAuthHandler.TestUserEmail)
        };
        
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        return Task.FromResult(new AuthenticationState(principal));
    }
}

/// <summary>
/// Test authentication handler that automatically authenticates all requests
/// with a fake user. Used for E2E tests that need to bypass real authentication.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string TestUserId = "test-dm-user-123";
    public const string TestUserName = "Test DM";
    public const string TestUserEmail = "testdm@test.com";
    
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }
    
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId),
            new Claim(ClaimTypes.Name, TestUserName),
            new Claim(ClaimTypes.Email, TestUserEmail)
        };
        
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
