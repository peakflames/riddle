using Riddle.Web.Hubs;
using Riddle.Web.IntegrationTests.Infrastructure;

namespace Riddle.Web.IntegrationTests.HubTests;

/// <summary>
/// Hub-level integration tests for player choice events.
/// PlayerChoices goes to players, PlayerChoiceSubmitted goes to DM.
/// </summary>
[Collection("SignalR")]
public class PlayerChoiceEventTests : IAsyncLifetime
{
    private readonly SignalRTestFixture _fixture;
    private TestSignalRClient? _dmClient;
    private TestSignalRClient? _player1Client;
    private TestSignalRClient? _player2Client;
    private Guid _campaignId;
    
    public PlayerChoiceEventTests(SignalRTestFixture fixture)
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
    
    // === PlayerChoices Event Tests ===
    
    [Fact]
    public async Task PlayerChoices_BroadcastsToPlayers()
    {
        // Arrange
        var choices = new List<string> { "Attack the goblin", "Try to negotiate", "Retreat" };
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act
        await notificationService.NotifyPlayerChoicesAsync(_campaignId, choices);
        
        // Assert - players should receive choices (DM doesn't need them - they sent them)
        var player1Event = await _player1Client!.WaitForEventAsync(GameHubEvents.PlayerChoicesReceived, TimeSpan.FromSeconds(5));
        var player2Event = await _player2Client!.WaitForEventAsync(GameHubEvents.PlayerChoicesReceived, TimeSpan.FromSeconds(2));
        
        player1Event.Should().NotBeNull();
        player2Event.Should().NotBeNull();
        
        // DM should NOT receive PlayerChoices - they originate from DM
        _dmClient!.GetEventCount(GameHubEvents.PlayerChoicesReceived).Should().Be(0);
    }
    
    [Fact]
    public async Task PlayerChoices_PayloadContainsCorrectChoices()
    {
        // Arrange
        var choices = new List<string> 
        { 
            "Attack the goblin with sword", 
            "Cast fireball at the enemy group", 
            "Attempt to sneak past" 
        };
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act
        await notificationService.NotifyPlayerChoicesAsync(_campaignId, choices);
        
        // Assert
        await _player1Client!.WaitForEventAsync(GameHubEvents.PlayerChoicesReceived, TimeSpan.FromSeconds(5));
        var receivedChoices = _player1Client.GetEventPayload<List<string>>(GameHubEvents.PlayerChoicesReceived);
        
        receivedChoices.Should().NotBeNull();
        receivedChoices.Should().HaveCount(3);
        receivedChoices.Should().Contain("Attack the goblin with sword");
        receivedChoices.Should().Contain("Cast fireball at the enemy group");
        receivedChoices.Should().Contain("Attempt to sneak past");
    }
    
    // === PlayerChoiceSubmitted Event Tests ===
    
    [Fact]
    public async Task PlayerChoiceSubmitted_BroadcastsToAllClients()
    {
        // Arrange - PlayerChoicePayload uses (CharacterId, CharacterName, Choice, Timestamp)
        var characterId = await _fixture.GetTestCharacterIdAsync(_campaignId);
        var payload = new PlayerChoicePayload(
            CharacterId: characterId.ToString(),
            CharacterName: "Test Fighter",
            Choice: "Attack the goblin",
            Timestamp: DateTime.UtcNow
        );
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act
        await notificationService.NotifyPlayerChoiceSubmittedAsync(_campaignId, payload);
        
        // Assert - all clients should receive (DM needs to see what players chose)
        var dmEvent = await _dmClient!.WaitForEventAsync(GameHubEvents.PlayerChoiceSubmitted, TimeSpan.FromSeconds(5));
        
        dmEvent.Should().NotBeNull();
    }
    
    [Fact]
    public async Task PlayerChoiceSubmitted_PayloadContainsCorrectData()
    {
        // Arrange
        var characterId = await _fixture.GetTestCharacterIdAsync(_campaignId);
        var payload = new PlayerChoicePayload(
            CharacterId: characterId.ToString(),
            CharacterName: "Thorn the Brave",
            Choice: "Cast fireball at the enemy",
            Timestamp: DateTime.UtcNow
        );
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act
        await notificationService.NotifyPlayerChoiceSubmittedAsync(_campaignId, payload);
        
        // Assert
        await _dmClient!.WaitForEventAsync(GameHubEvents.PlayerChoiceSubmitted, TimeSpan.FromSeconds(5));
        var receivedPayload = _dmClient.GetEventPayload<PlayerChoicePayload>(GameHubEvents.PlayerChoiceSubmitted);
        
        receivedPayload.Should().NotBeNull();
        receivedPayload!.CharacterId.Should().Be(characterId.ToString());
        receivedPayload.CharacterName.Should().Be("Thorn the Brave");
        receivedPayload.Choice.Should().Be("Cast fireball at the enemy");
    }
    
    // === Full Choice Flow Integration Test ===
    
    [Fact]
    public async Task FullChoiceFlow_ChoicesDeliveredAndResponseReceived()
    {
        // Arrange
        var characterId = await _fixture.GetTestCharacterIdAsync(_campaignId);
        var choices = new List<string> { "Fight", "Flee", "Parley" };
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act - Step 1: DM sends choices
        await notificationService.NotifyPlayerChoicesAsync(_campaignId, choices);
        
        // Wait for players to receive
        await _player1Client!.WaitForEventAsync(GameHubEvents.PlayerChoicesReceived, TimeSpan.FromSeconds(5));
        
        // Act - Step 2: Player submits choice
        var choicePayload = new PlayerChoicePayload(
            CharacterId: characterId.ToString(),
            CharacterName: "Fighter",
            Choice: "Fight",
            Timestamp: DateTime.UtcNow
        );
        await notificationService.NotifyPlayerChoiceSubmittedAsync(_campaignId, choicePayload);
        
        // Assert - DM receives player choice
        await _dmClient!.WaitForEventAsync(GameHubEvents.PlayerChoiceSubmitted, TimeSpan.FromSeconds(5));
        var receivedChoice = _dmClient.GetEventPayload<PlayerChoicePayload>(GameHubEvents.PlayerChoiceSubmitted);
        
        receivedChoice.Should().NotBeNull();
        receivedChoice!.Choice.Should().Be("Fight");
    }
    
    [Fact]
    public async Task MultiplePlayersSubmitChoices_AllReceivedByDm()
    {
        // Arrange
        var choices = new List<string> { "Investigate", "Ignore", "Report to guards" };
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act - Step 1: Broadcast choices
        await notificationService.NotifyPlayerChoicesAsync(_campaignId, choices);
        await Task.Delay(100);
        
        // Act - Step 2: Multiple players submit choices
        var payload1 = new PlayerChoicePayload(
            CharacterId: "char-1",
            CharacterName: "Fighter",
            Choice: "Investigate",
            Timestamp: DateTime.UtcNow
        );
        var payload2 = new PlayerChoicePayload(
            CharacterId: "char-2",
            CharacterName: "Rogue",
            Choice: "Report to guards",
            Timestamp: DateTime.UtcNow
        );
        
        await notificationService.NotifyPlayerChoiceSubmittedAsync(_campaignId, payload1);
        await notificationService.NotifyPlayerChoiceSubmittedAsync(_campaignId, payload2);
        
        // Wait for all events
        await Task.Delay(300);
        
        // Assert - DM received both choices
        _dmClient!.GetEventCount(GameHubEvents.PlayerChoiceSubmitted).Should().Be(2);
    }
}
