using Riddle.Web.Hubs;
using Riddle.Web.IntegrationTests.Infrastructure;

namespace Riddle.Web.IntegrationTests.HubTests;

/// <summary>
/// Hub-level integration tests for character state update events.
/// These events should be broadcast to both DM and players.
/// </summary>
[Collection("SignalR")]
public class CharacterStateEventTests : IAsyncLifetime
{
    private readonly SignalRTestFixture _fixture;
    private TestSignalRClient? _dmClient;
    private TestSignalRClient? _playerClient;
    private Guid _campaignId;
    
    public CharacterStateEventTests(SignalRTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    public async Task InitializeAsync()
    {
        // Setup test campaign with characters
        var campaign = await _fixture.SetupTestCampaignAsync();
        _campaignId = campaign.Id;
        
        _dmClient = await _fixture.CreateDmClientAsync(_campaignId);
        _playerClient = await _fixture.CreatePlayerClientAsync(_campaignId);
        
        await Task.Delay(100);
    }
    
    public async Task DisposeAsync()
    {
        if (_dmClient != null) await _dmClient.DisposeAsync();
        if (_playerClient != null) await _playerClient.DisposeAsync();
    }
    
    // === CharacterStateUpdated Event Tests ===
    
    [Fact]
    public async Task CharacterStateUpdated_BroadcastsToBothDmAndPlayers()
    {
        // Arrange - CharacterStatePayload uses (CharacterId, Key, Value) format
        var characterId = await _fixture.GetTestCharacterIdAsync(_campaignId);
        var payload = new CharacterStatePayload(
            CharacterId: characterId.ToString(),
            Key: "current_hp",
            Value: 35
        );
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act
        await notificationService.NotifyCharacterStateUpdatedAsync(_campaignId, payload);
        
        // Assert - both DM and player should receive
        var dmEvent = await _dmClient!.WaitForEventAsync(GameHubEvents.CharacterStateUpdated, TimeSpan.FromSeconds(5));
        var playerEvent = await _playerClient!.WaitForEventAsync(GameHubEvents.CharacterStateUpdated, TimeSpan.FromSeconds(5));
        
        dmEvent.Should().NotBeNull();
        playerEvent.Should().NotBeNull();
    }
    
    [Fact]
    public async Task CharacterStateUpdated_PayloadContainsCorrectData()
    {
        // Arrange
        var characterId = await _fixture.GetTestCharacterIdAsync(_campaignId);
        var payload = new CharacterStatePayload(
            CharacterId: characterId.ToString(),
            Key: "current_hp",
            Value: 30
        );
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act
        await notificationService.NotifyCharacterStateUpdatedAsync(_campaignId, payload);
        
        // Assert
        await _dmClient!.WaitForEventAsync(GameHubEvents.CharacterStateUpdated, TimeSpan.FromSeconds(5));
        var receivedPayload = _dmClient.GetEventPayload<CharacterStatePayload>(GameHubEvents.CharacterStateUpdated);
        
        receivedPayload.Should().NotBeNull();
        receivedPayload!.CharacterId.Should().Be(characterId.ToString());
        receivedPayload.Key.Should().Be("current_hp");
        // Value is object - JSON deserializes numbers as JsonElement, convert to string for comparison
        receivedPayload.Value?.ToString().Should().Be("30");
    }
    
    [Fact]
    public async Task CharacterStateUpdated_HpReductionBroadcasted()
    {
        // Arrange
        var characterId = await _fixture.GetTestCharacterIdAsync(_campaignId);
        var payload = new CharacterStatePayload(
            CharacterId: characterId.ToString(),
            Key: "current_hp",
            Value: 20 // HP reduced
        );
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act
        await notificationService.NotifyCharacterStateUpdatedAsync(_campaignId, payload);
        
        // Assert
        await _playerClient!.WaitForEventAsync(GameHubEvents.CharacterStateUpdated, TimeSpan.FromSeconds(5));
        var receivedPayload = _playerClient.GetEventPayload<CharacterStatePayload>(GameHubEvents.CharacterStateUpdated);
        
        receivedPayload.Should().NotBeNull();
        // Value is object - JSON deserializes numbers as JsonElement, convert to string for comparison
        receivedPayload!.Value?.ToString().Should().Be("20");
    }
    
    [Fact]
    public async Task CharacterStateUpdated_ConditionAddedBroadcasted()
    {
        // Arrange
        var characterId = await _fixture.GetTestCharacterIdAsync(_campaignId);
        var payload = new CharacterStatePayload(
            CharacterId: characterId.ToString(),
            Key: "conditions",
            Value: new List<string> { "Poisoned" }
        );
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act
        await notificationService.NotifyCharacterStateUpdatedAsync(_campaignId, payload);
        
        // Assert
        await _playerClient!.WaitForEventAsync(GameHubEvents.CharacterStateUpdated, TimeSpan.FromSeconds(5));
        var receivedPayload = _playerClient.GetEventPayload<CharacterStatePayload>(GameHubEvents.CharacterStateUpdated);
        
        receivedPayload.Should().NotBeNull();
        receivedPayload!.Key.Should().Be("conditions");
    }
    
    [Fact]
    public async Task CharacterStateUpdated_MultipleConditionsBroadcasted()
    {
        // Arrange
        var characterId = await _fixture.GetTestCharacterIdAsync(_campaignId);
        var payload = new CharacterStatePayload(
            CharacterId: characterId.ToString(),
            Key: "conditions",
            Value: new List<string> { "Poisoned", "Frightened", "Prone" }
        );
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act
        await notificationService.NotifyCharacterStateUpdatedAsync(_campaignId, payload);
        
        // Assert
        await _playerClient!.WaitForEventAsync(GameHubEvents.CharacterStateUpdated, TimeSpan.FromSeconds(5));
        var receivedPayload = _playerClient.GetEventPayload<CharacterStatePayload>(GameHubEvents.CharacterStateUpdated);
        
        receivedPayload.Should().NotBeNull();
    }
    
    [Fact]
    public async Task CharacterStateUpdated_TempHpBroadcasted()
    {
        // Arrange
        var characterId = await _fixture.GetTestCharacterIdAsync(_campaignId);
        var payload = new CharacterStatePayload(
            CharacterId: characterId.ToString(),
            Key: "temp_hp",
            Value: 10 // Temp HP added
        );
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act
        await notificationService.NotifyCharacterStateUpdatedAsync(_campaignId, payload);
        
        // Assert
        await _playerClient!.WaitForEventAsync(GameHubEvents.CharacterStateUpdated, TimeSpan.FromSeconds(5));
        var receivedPayload = _playerClient.GetEventPayload<CharacterStatePayload>(GameHubEvents.CharacterStateUpdated);
        
        receivedPayload.Should().NotBeNull();
        receivedPayload!.Key.Should().Be("temp_hp");
        // Value is object - JSON deserializes numbers as JsonElement, convert to string for comparison
        receivedPayload.Value?.ToString().Should().Be("10");
    }
    
    /// <summary>
    /// Bug regression test: Verifies the payload uses proper casing
    /// This ensures the SignalR serialization is correct for JS clients
    /// </summary>
    [Fact]
    public async Task CharacterStateUpdated_UsesCorrectPropertyCasing()
    {
        // Arrange
        var characterId = await _fixture.GetTestCharacterIdAsync(_campaignId);
        var payload = new CharacterStatePayload(
            CharacterId: characterId.ToString(),
            Key: "current_hp",
            Value: 35
        );
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        // Act
        await notificationService.NotifyCharacterStateUpdatedAsync(_campaignId, payload);
        
        // Assert - by successfully parsing the payload back to CharacterStatePayload,
        // we verify the JSON serialization matches the expected format
        await _dmClient!.WaitForEventAsync(GameHubEvents.CharacterStateUpdated, TimeSpan.FromSeconds(5));
        var receivedPayload = _dmClient.GetEventPayload<CharacterStatePayload>(GameHubEvents.CharacterStateUpdated);
        
        receivedPayload.Should().NotBeNull();
        receivedPayload!.CharacterId.Should().Be(characterId.ToString());
        receivedPayload.Key.Should().Be("current_hp");
        // Value is object - JSON deserializes numbers as JsonElement, convert to string for comparison
        receivedPayload.Value?.ToString().Should().Be("35");
    }
}
