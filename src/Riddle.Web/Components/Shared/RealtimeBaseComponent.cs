using Microsoft.AspNetCore.Components;
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

    protected HubConnection? HubConnection { get; private set; }

    /// <summary>
    /// Gets the appropriate SignalR hub URL based on the runtime environment.
    /// In Docker: uses internal port (ASPNETCORE_HTTP_PORTS or default 8080)
    /// Locally: uses NavigationManager to resolve the absolute URI
    /// </summary>
    protected string GetSignalRHubUrl()
    {
        var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        
        if (isDocker)
        {
            var port = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "8080";
            var url = $"http://localhost:{port}/gamehub";
            Logger.LogDebug("Docker environment detected. Using internal SignalR URL: {Url}", url);
            return url;
        }
        
        var absoluteUrl = Navigation.ToAbsoluteUri("/gamehub").ToString();
        Logger.LogDebug("Local environment detected. Using SignalR URL: {Url}", absoluteUrl);
        return absoluteUrl;
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
                // Include cookies/credentials for authentication behind reverse proxies (Cloudflare Tunnel)
                options.UseDefaultCredentials = true;
            })
            .WithAutomaticReconnect([
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10)
            ])
            .Build();
        
        Logger.LogInformation("Created SignalR HubConnection to {Url}", url);
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
