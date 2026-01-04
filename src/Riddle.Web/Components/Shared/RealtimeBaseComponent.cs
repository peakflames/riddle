using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Riddle.Web.Components.Shared;

/// <summary>
/// Base component for all components that need SignalR real-time connectivity.
/// Centralizes hub URL resolution logic to handle Docker vs local environment differences.
/// </summary>
/// <remarks>
/// Docker containers cannot reach the external mapped port (e.g., localhost:1983).
/// They must use the internal port (e.g., localhost:8080) which is what the server binds to.
/// This base class detects the environment and returns the appropriate URL.
/// </remarks>
public abstract class RealtimeBaseComponent : ComponentBase, IAsyncDisposable
{
    [Inject] protected NavigationManager Navigation { get; set; } = null!;
    [Inject] protected IConfiguration Configuration { get; set; } = null!;
    [Inject] protected ILogger<RealtimeBaseComponent> Logger { get; set; } = null!;
    [Inject] protected IServer Server { get; set; } = null!;

    protected HubConnection? HubConnection { get; private set; }

    /// <summary>
    /// Gets the appropriate SignalR hub URL for server-side connections.
    /// Always uses localhost since the HubConnection runs ON the server and must connect internally,
    /// not through external proxies (e.g., Cloudflare) which may block WebSocket upgrades.
    /// </summary>
    protected string GetSignalRHubUrl()
    {
        // The server-side HubConnection must ALWAYS connect to localhost
        // NavigationManager returns the external URL (e.g., riddle.peakflames.org)
        // but that goes through Cloudflare which blocks the WebSocket upgrade with 403
        
        // BEST: Get actual bound addresses from Kestrel at runtime
        // This works for ANY port configuration - dev, Docker, self-hosted, etc.
        var addressFeature = Server.Features.Get<IServerAddressesFeature>();
        var addresses = addressFeature?.Addresses;
        
        if (addresses?.Count > 0)
        {
            // Get the first HTTP address (prefer non-HTTPS for internal connections)
            var address = addresses.FirstOrDefault(a => a.StartsWith("http://")) 
                       ?? addresses.First();
            
            // Replace wildcard bindings with localhost
            var normalizedAddress = address
                .Replace("*", "localhost")
                .Replace("+", "localhost")
                .Replace("0.0.0.0", "localhost")
                .Replace("[::]", "localhost");
            
            var uri = new Uri(normalizedAddress);
            var url = $"http://localhost:{uri.Port}/gamehub";
            Logger.LogInformation("Using internal SignalR URL from IServer: {Url} (bound address: {BoundAddress})", url, address);
            return url;
        }
        
        // FALLBACK: Try environment variables
        var aspNetCoreUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        var httpPorts = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS");
        
        string port;
        if (!string.IsNullOrEmpty(httpPorts))
        {
            port = httpPorts.Split(';')[0];
        }
        else if (!string.IsNullOrEmpty(aspNetCoreUrls))
        {
            var uri = new Uri(aspNetCoreUrls.Split(';')[0]);
            port = uri.Port.ToString();
        }
        else
        {
            var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
            port = isDocker ? "8080" : "5000";
        }
        
        var fallbackUrl = $"http://localhost:{port}/gamehub";
        Logger.LogWarning("IServerAddressesFeature unavailable, using fallback SignalR URL: {Url}", fallbackUrl);
        return fallbackUrl;
    }

    /// <summary>
    /// Creates and configures a HubConnection with automatic reconnect policy.
    /// </summary>
    /// <returns>The configured HubConnection (also stored in HubConnection property)</returns>
    protected HubConnection CreateHubConnection()
    {
        var url = GetSignalRHubUrl();
        
        HubConnection = new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                // Skip negotiate POST request and go directly to WebSocket
                // This bypasses antiforgery validation issues with the negotiate endpoint
                options.SkipNegotiation = true;
                options.Transports = HttpTransportType.WebSockets;
            })
            .WithAutomaticReconnect([
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10)
            ])
            .Build();
        
        Logger.LogInformation("Created SignalR HubConnection to {Url} (WebSocket direct, skip negotiate)", url);
        return HubConnection;
    }

    /// <summary>
    /// Disposes the HubConnection if it exists.
    /// Override this method to add additional cleanup, but always call base.DisposeAsync().
    /// </summary>
    public virtual async ValueTask DisposeAsync()
    {
        if (HubConnection != null)
        {
            Logger.LogDebug("Disposing SignalR HubConnection");
            await HubConnection.DisposeAsync();
        }
    }
}
