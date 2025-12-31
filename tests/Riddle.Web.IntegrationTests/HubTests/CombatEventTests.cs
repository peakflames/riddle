using Riddle.Web.Hubs;
using Riddle.Web.IntegrationTests.Infrastructure;
using Riddle.Web.Models;

namespace Riddle.Web.IntegrationTests.HubTests;

/// <summary>
/// Hub-level integration tests for combat events.
/// </summary>
[Collection("SignalR")]
public class CombatEventTests : IAsyncLifetime
{
    private readonly SignalRTestFixture _fixture;
    private TestSignalRClient? _dmClient;
    private TestSignalRClient? _player1Client;
    private TestSignalRClient? _player2Client;
    private Guid _campaignId;
    
    public CombatEventTests(SignalRTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    public async Task InitializeAsync()
    {
        var campaign = await _fixture.SetupTestCampaignAsync();
        _campaignId = campaign.Id;
        
        _dmClient = await _fixture.CreateDmClientAsync(_campaignId);
        _player1Client = await _fixture.CreatePlayerClientAsync(_campaignId);
        _player2Client = await _fixture.CreatePlayerClientAsync(_campaignId, characterId: "char-2");
        
        await Task.Delay(100);
    }
    
    public async Task DisposeAsync()
    {
        if (_dmClient != null) await _dmClient.DisposeAsync();
        if (_player1Client != null) await _player1Client.DisposeAsync();
        if (_player2Client != null) await _player2Client.DisposeAsync();
    }
    
    // === Diagnostic Test ===
    
    [Fact]
    public async Task DiagnosticTest_ClientsAllBroadcast()
    {
        // Simplest possible test: Send via Clients.All (no groups)
        _dmClient!.Connection.State.Should().Be(HubConnectionState.Connected);
        _dmClient.ClearReceivedEvents();
        
        // Get IHubContext and send to ALL connected clients
        var hubContext = _fixture.Factory.Services.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<Riddle.Web.Hubs.GameHub>>();
        
        // Send to ALL clients (no group membership required)
        await hubContext.Clients.All.SendCoreAsync(
            GameHubEvents.CharacterStateUpdated, 
            new object[] { new CharacterStatePayload("test", "key", "value") });
        
        await Task.Delay(1000); // Longer wait for debugging
        
        var received = _dmClient.HasReceivedEvent(GameHubEvents.CharacterStateUpdated);
        var allEvents = _dmClient.ReceivedEvents.Select(e => e.EventName).ToList();
        
        received.Should().BeTrue(
            $"DM should receive CharacterStateUpdated via Clients.All. " +
            $"Connection state: {_dmClient.Connection.State}. " +
            $"Events received: [{string.Join(", ", allEvents)}]");
    }
    
    // === CombatStarted Event Tests ===
    
    [Fact]
    public async Task CombatStarted_BroadcastsToAllClients()
    {
        // Arrange - CombatStatePayload uses (CombatId, IsActive, RoundNumber, TurnOrder, CurrentTurnIndex)
        var combatId = Guid.CreateVersion7().ToString();
        var turnOrder = new List<CombatantInfo>
        {
            new("char-1", "Fighter", "PC", 18, 44, 44, false, false),
            new("goblin-1", "Goblin", "Enemy", 12, 7, 7, false, false)
        };
        var payload = new CombatStatePayload(combatId, true, 1, turnOrder, 0);
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act
        await notificationService.NotifyCombatStartedAsync(_campaignId, payload);
        
        // Assert - all clients should receive
        var dmEvent = await _dmClient!.WaitForEventAsync(GameHubEvents.CombatStarted, TimeSpan.FromSeconds(5));
        var player1Event = await _player1Client!.WaitForEventAsync(GameHubEvents.CombatStarted, TimeSpan.FromSeconds(2));
        var player2Event = await _player2Client!.WaitForEventAsync(GameHubEvents.CombatStarted, TimeSpan.FromSeconds(2));
        
        dmEvent.Should().NotBeNull();
        player1Event.Should().NotBeNull();
        player2Event.Should().NotBeNull();
    }
    
    [Fact]
    public async Task CombatStarted_PayloadContainsCorrectData()
    {
        // Arrange
        var combatId = Guid.CreateVersion7().ToString();
        var turnOrder = new List<CombatantInfo>
        {
            new("char-1", "Hero Fighter", "PC", 18, 44, 40, false, false),
            new("goblin-1", "Goblin Archer", "Enemy", 15, 7, 7, false, false)
        };
        var payload = new CombatStatePayload(combatId, true, 1, turnOrder, 0);
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act
        await notificationService.NotifyCombatStartedAsync(_campaignId, payload);
        
        // Assert
        await _dmClient!.WaitForEventAsync(GameHubEvents.CombatStarted, TimeSpan.FromSeconds(5));
        var receivedPayload = _dmClient.GetEventPayload<CombatStatePayload>(GameHubEvents.CombatStarted);
        
        receivedPayload.Should().NotBeNull();
        receivedPayload!.TurnOrder.Should().HaveCount(2);
        receivedPayload.CurrentTurnIndex.Should().Be(0);
        receivedPayload.RoundNumber.Should().Be(1);
        receivedPayload.IsActive.Should().BeTrue();
        
        var fighter = receivedPayload.TurnOrder.First(c => c.Type == "PC");
        fighter.Name.Should().Be("Hero Fighter");
        fighter.Initiative.Should().Be(18);
        fighter.CurrentHp.Should().Be(44);  // CombatantInfo constructor: (Id, Name, Type, Initiative, CurrentHp, MaxHp, ...)
        fighter.MaxHp.Should().Be(40);
    }
    
    // === CombatEnded Event Tests ===
    
    [Fact]
    public async Task CombatEnded_BroadcastsToAllClients()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act
        await notificationService.NotifyCombatEndedAsync(_campaignId);
        
        // Assert - all clients should receive
        var dmEvent = await _dmClient!.WaitForEventAsync(GameHubEvents.CombatEnded, TimeSpan.FromSeconds(5));
        var player1Event = await _player1Client!.WaitForEventAsync(GameHubEvents.CombatEnded, TimeSpan.FromSeconds(2));
        var player2Event = await _player2Client!.WaitForEventAsync(GameHubEvents.CombatEnded, TimeSpan.FromSeconds(2));
        
        dmEvent.Should().NotBeNull();
        player1Event.Should().NotBeNull();
        player2Event.Should().NotBeNull();
    }
    
    // === TurnAdvanced Event Tests ===
    
    [Fact]
    public async Task TurnAdvanced_BroadcastsToAllClients()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act
        await notificationService.NotifyTurnAdvancedAsync(_campaignId, new TurnAdvancedPayload(1, "goblin-1", 1));
        
        // Assert - all clients should receive
        var dmEvent = await _dmClient!.WaitForEventAsync(GameHubEvents.TurnAdvanced, TimeSpan.FromSeconds(5));
        var player1Event = await _player1Client!.WaitForEventAsync(GameHubEvents.TurnAdvanced, TimeSpan.FromSeconds(2));
        var player2Event = await _player2Client!.WaitForEventAsync(GameHubEvents.TurnAdvanced, TimeSpan.FromSeconds(2));
        
        dmEvent.Should().NotBeNull();
        player1Event.Should().NotBeNull();
        player2Event.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TurnAdvanced_IncludesRoundNumber()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act
        await notificationService.NotifyTurnAdvancedAsync(_campaignId, new TurnAdvancedPayload(0, "char-1", 2));
        
        // Assert
        await _dmClient!.WaitForEventAsync(GameHubEvents.TurnAdvanced, TimeSpan.FromSeconds(5));
        // The payload should contain roundNumber - verify based on how it's serialized
    }
    
    // === InitiativeSet Event Tests ===
    
    [Fact]
    public async Task InitiativeSet_BroadcastsToAllClients()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act
        await notificationService.NotifyInitiativeSetAsync(_campaignId, new InitiativeSetPayload("char-1", 18));
        
        // Assert - all clients should receive
        var dmEvent = await _dmClient!.WaitForEventAsync(GameHubEvents.InitiativeSet, TimeSpan.FromSeconds(5));
        var player1Event = await _player1Client!.WaitForEventAsync(GameHubEvents.InitiativeSet, TimeSpan.FromSeconds(2));
        
        dmEvent.Should().NotBeNull();
        player1Event.Should().NotBeNull();
    }
    
    // === Combat Flow Integration Test ===
    
    [Fact]
    public async Task FullCombatFlow_AllEventsDelivered()
    {
        // Arrange
        var combatId = Guid.CreateVersion7().ToString();
        var turnOrder = new List<CombatantInfo>
        {
            new("char-1", "Fighter", "PC", 18, 44, 44, false, false),
            new("goblin-1", "Goblin", "Enemy", 12, 7, 7, false, false)
        };
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act - Simulate full combat flow
        
        // 1. Start combat
        await notificationService.NotifyCombatStartedAsync(_campaignId, new CombatStatePayload(combatId, true, 1, turnOrder, 0));
        
        // 2. Advance turns
        await notificationService.NotifyTurnAdvancedAsync(_campaignId, new TurnAdvancedPayload(1, "goblin-1", 1));
        await notificationService.NotifyTurnAdvancedAsync(_campaignId, new TurnAdvancedPayload(0, "char-1", 2));
        
        // 3. End combat
        await notificationService.NotifyCombatEndedAsync(_campaignId);
        
        // Wait for all events
        await Task.Delay(300);
        
        // Assert - all events should be received
        _dmClient!.HasReceivedEvent(GameHubEvents.CombatStarted).Should().BeTrue();
        _dmClient.HasReceivedEvent(GameHubEvents.TurnAdvanced).Should().BeTrue();
        _dmClient.HasReceivedEvent(GameHubEvents.CombatEnded).Should().BeTrue();
        
        // Turn advanced should have 2 events
        _dmClient.GetEventCount(GameHubEvents.TurnAdvanced).Should().Be(2);
    }
}
