using Microsoft.AspNetCore.SignalR.Client;
using Riddle.Web.Hubs;

namespace Riddle.Web.IntegrationTests.Infrastructure;

/// <summary>
/// A single received SignalR event with metadata
/// </summary>
public record ReceivedEvent(
    string EventName,
    object?[] Args,
    DateTime ReceivedAt
);

/// <summary>
/// Test helper that wraps a HubConnection for testing SignalR events.
/// Tracks all received events and provides helper methods for assertions.
/// </summary>
public class TestSignalRClient : IAsyncDisposable
{
    public HubConnection Connection { get; }
    public string ClientId { get; }
    public string ClientType { get; } // "dm" or "player"
    
    private readonly List<ReceivedEvent> _receivedEvents = new();
    private readonly SemaphoreSlim _eventLock = new(1, 1);
    private readonly Dictionary<string, TaskCompletionSource<ReceivedEvent>> _eventWaiters = new();
    
    public IReadOnlyList<ReceivedEvent> ReceivedEvents => _receivedEvents.AsReadOnly();
    
    public TestSignalRClient(HubConnection connection, string clientId, string clientType)
    {
        Connection = connection;
        ClientId = clientId;
        ClientType = clientType;
        
        // Register handlers for all known events
        RegisterEventHandlers();
    }
    
    /// <summary>
    /// Create a test client connected to the given hub URL
    /// </summary>
    public static async Task<TestSignalRClient> CreateAsync(string hubUrl, string clientType)
    {
        var clientId = Guid.NewGuid().ToString();
        
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();
        
        var client = new TestSignalRClient(connection, clientId, clientType);
        await connection.StartAsync();
        
        return client;
    }
    
    /// <summary>
    /// Join a campaign as DM or Player
    /// </summary>
    public async Task JoinCampaignAsync(Guid campaignId, string userId, string? characterId, bool isDm)
    {
        await Connection.InvokeAsync("JoinCampaign", campaignId, userId, characterId, isDm);
    }
    
    /// <summary>
    /// Leave a campaign
    /// </summary>
    public async Task LeaveCampaignAsync(Guid campaignId)
    {
        await Connection.InvokeAsync("LeaveCampaign", campaignId);
    }
    
    /// <summary>
    /// Submit a player choice
    /// </summary>
    public async Task SubmitChoiceAsync(Guid campaignId, string characterId, string characterName, string choice)
    {
        await Connection.InvokeAsync("SubmitChoice", campaignId, characterId, characterName, choice);
    }
    
    /// <summary>
    /// Check if an event with the given name has been received
    /// </summary>
    public bool HasReceivedEvent(string eventName)
    {
        return _receivedEvents.Any(e => e.EventName == eventName);
    }
    
    /// <summary>
    /// Get the count of events received with the given name
    /// </summary>
    public int GetEventCount(string eventName)
    {
        return _receivedEvents.Count(e => e.EventName == eventName);
    }
    
    /// <summary>
    /// Get the first event with the given name, or null if not found
    /// </summary>
    public ReceivedEvent? GetEvent(string eventName)
    {
        return _receivedEvents.FirstOrDefault(e => e.EventName == eventName);
    }
    
    /// <summary>
    /// Get all events with the given name
    /// </summary>
    public IReadOnlyList<ReceivedEvent> GetEvents(string eventName)
    {
        return _receivedEvents.Where(e => e.EventName == eventName).ToList().AsReadOnly();
    }
    
    // JSON options for deserializing SignalR payloads (camelCase from server -> PascalCase records)
    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    /// <summary>
    /// Get the payload from the first event with the given name, deserialized to type T
    /// </summary>
    public T? GetEventPayload<T>(string eventName) where T : class
    {
        var evt = GetEvent(eventName);
        if (evt?.Args.Length > 0)
        {
            var arg = evt.Args[0];
            if (arg is T typed)
                return typed;
            
            // If it's a JsonElement or anonymous object, try JSON round-trip with case-insensitive matching
            if (arg != null)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(arg);
                return System.Text.Json.JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
        }
        return null;
    }
    
    /// <summary>
    /// Wait for an event with the given name to be received, with timeout
    /// </summary>
    public async Task<ReceivedEvent> WaitForEventAsync(string eventName, TimeSpan timeout)
    {
        // Check if already received
        var existing = GetEvent(eventName);
        if (existing != null)
            return existing;
        
        // Set up waiter
        var tcs = new TaskCompletionSource<ReceivedEvent>();
        
        await _eventLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            existing = GetEvent(eventName);
            if (existing != null)
                return existing;
            
            _eventWaiters[eventName] = tcs;
        }
        finally
        {
            _eventLock.Release();
        }
        
        // Wait with timeout
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            cts.Token.Register(() => tcs.TrySetCanceled());
            return await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Timeout waiting for event '{eventName}' after {timeout.TotalMilliseconds}ms");
        }
        finally
        {
            await _eventLock.WaitAsync();
            try
            {
                _eventWaiters.Remove(eventName);
            }
            finally
            {
                _eventLock.Release();
            }
        }
    }
    
    /// <summary>
    /// Clear all received events (useful between test cases)
    /// </summary>
    public void ClearReceivedEvents()
    {
        _receivedEvents.Clear();
    }
    
    private void RegisterEventHandlers()
    {
        // Character & Player Events
        RegisterHandler(GameHubEvents.CharacterClaimed);
        RegisterHandler(GameHubEvents.CharacterReleased);
        RegisterHandler(GameHubEvents.PlayerConnected);
        RegisterHandler(GameHubEvents.PlayerDisconnected);
        
        // Game State Events
        RegisterHandler(GameHubEvents.CharacterStateUpdated);
        RegisterHandler(GameHubEvents.ReadAloudTextReceived);
        RegisterHandler(GameHubEvents.SceneImageUpdated);
        RegisterHandler(GameHubEvents.PlayerChoicesReceived);
        RegisterHandler(GameHubEvents.PlayerChoiceSubmitted);
        RegisterHandler(GameHubEvents.PlayerRollLogged);
        
        // Atmospheric Events
        RegisterHandler(GameHubEvents.AtmospherePulseReceived);
        RegisterHandler(GameHubEvents.NarrativeAnchorUpdated);
        RegisterHandler(GameHubEvents.GroupInsightTriggered);
        
        // Combat Events
        RegisterHandler(GameHubEvents.CombatStarted);
        RegisterNoArgHandler(GameHubEvents.CombatEnded);  // No payload
        RegisterHandler(GameHubEvents.TurnAdvanced);
        RegisterHandler(GameHubEvents.InitiativeSet);
        RegisterHandler(GameHubEvents.CombatantAdded);
        RegisterHandler(GameHubEvents.CombatantRemoved);
        
        // Connection Events
        RegisterHandler(GameHubEvents.ConnectionStatusChanged);
    }
    
    private void RegisterHandler(string eventName)
    {
        // Register a single handler that captures the payload as a single argument.
        // SignalR's client library will deserialize the first argument as object.
        // Most events in our hub send a single payload object.
        Connection.On<object?>(eventName, (arg1) => 
        {
            RecordEvent(eventName, [arg1]);
        });
    }
    
    private void RegisterNoArgHandler(string eventName)
    {
        // Register a handler for events with no arguments (e.g., CombatEnded)
        Connection.On(eventName, () => 
        {
            RecordEvent(eventName, []);
        });
    }
    
    private void RecordEvent(string eventName, object?[] args)
    {
        var evt = new ReceivedEvent(eventName, args, DateTime.UtcNow);
        
        _eventLock.Wait();
        try
        {
            _receivedEvents.Add(evt);
            
            // Notify any waiters
            if (_eventWaiters.TryGetValue(eventName, out var tcs))
            {
                tcs.TrySetResult(evt);
            }
        }
        finally
        {
            _eventLock.Release();
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        if (Connection.State != HubConnectionState.Disconnected)
        {
            await Connection.StopAsync();
        }
        await Connection.DisposeAsync();
        _eventLock.Dispose();
    }
}
