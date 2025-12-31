using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Riddle.Web.Hubs;
using Riddle.Web.Services;

namespace Riddle.Web.IntegrationTests.Services;

/// <summary>
/// Service-level tests for NotificationService with mocked IHubContext.
/// Verifies correct group targeting and payload structure.
/// </summary>
public class NotificationServiceTests
{
    private readonly Mock<IHubContext<GameHub>> _hubContextMock;
    private readonly Mock<IHubClients> _hubClientsMock;
    private readonly Mock<IClientProxy> _allGroupMock;
    private readonly Mock<IClientProxy> _dmGroupMock;
    private readonly Mock<IClientProxy> _playersGroupMock;
    private readonly NotificationService _service;
    private readonly Guid _campaignId = Guid.NewGuid();
    
    public NotificationServiceTests()
    {
        _hubContextMock = new Mock<IHubContext<GameHub>>();
        _hubClientsMock = new Mock<IHubClients>();
        _allGroupMock = new Mock<IClientProxy>();
        _dmGroupMock = new Mock<IClientProxy>();
        _playersGroupMock = new Mock<IClientProxy>();
        
        // Setup group routing
        _hubClientsMock.Setup(c => c.Group($"campaign_{_campaignId}_all"))
            .Returns(_allGroupMock.Object);
        _hubClientsMock.Setup(c => c.Group($"campaign_{_campaignId}_dm"))
            .Returns(_dmGroupMock.Object);
        _hubClientsMock.Setup(c => c.Group($"campaign_{_campaignId}_players"))
            .Returns(_playersGroupMock.Object);
        
        _hubContextMock.Setup(c => c.Clients).Returns(_hubClientsMock.Object);
        
        var logger = Mock.Of<ILogger<NotificationService>>();
        _service = new NotificationService(_hubContextMock.Object, logger);
    }
    
    // === Character State Events ===
    
    [Fact]
    public async Task SendsCharacterStateUpdatedToAllGroup()
    {
        // Arrange - CharacterStatePayload uses (CharacterId, Key, Value)
        var payload = new CharacterStatePayload("char-123", "current_hp", 25);
        
        // Act
        await _service.NotifyCharacterStateUpdatedAsync(_campaignId, payload);
        
        // Assert
        _allGroupMock.Verify(
            c => c.SendCoreAsync(
                GameHubEvents.CharacterStateUpdated,
                It.Is<object?[]>(args => args.Length == 1 && args[0] == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Verify NOT sent to other groups directly (only via _all)
        _dmGroupMock.Verify(
            c => c.SendCoreAsync(GameHubEvents.CharacterStateUpdated, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _playersGroupMock.Verify(
            c => c.SendCoreAsync(GameHubEvents.CharacterStateUpdated, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    // === Player Choices Events ===
    
    [Fact]
    public async Task SendsPlayerChoicesToPlayersGroup()
    {
        // Arrange
        var choices = new List<string> { "Attack the goblin", "Flee into the forest", "Attempt diplomacy" };
        
        // Act
        await _service.NotifyPlayerChoicesAsync(_campaignId, choices);
        
        // Assert - PlayerChoicesReceived goes to players group only (they make the choice)
        _playersGroupMock.Verify(
            c => c.SendCoreAsync(
                GameHubEvents.PlayerChoicesReceived,
                It.Is<object?[]>(args => args.Length == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Verify NOT sent to DM or all groups
        _dmGroupMock.Verify(
            c => c.SendCoreAsync(GameHubEvents.PlayerChoicesReceived, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _allGroupMock.Verify(
            c => c.SendCoreAsync(GameHubEvents.PlayerChoicesReceived, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    [Fact]
    public async Task SendsPlayerChoiceSubmittedToDmGroup()
    {
        // Arrange - PlayerChoicePayload uses (CharacterId, CharacterName, Choice, Timestamp)
        var payload = new PlayerChoicePayload("char-123", "Test Fighter", "Attack the goblin", DateTime.UtcNow);
        
        // Act
        await _service.NotifyPlayerChoiceSubmittedAsync(_campaignId, payload);
        
        // Assert - PlayerChoiceSubmitted goes to DM group (DM needs to see player choices)
        _dmGroupMock.Verify(
            c => c.SendCoreAsync(
                GameHubEvents.PlayerChoiceSubmitted,
                It.Is<object?[]>(args => args.Length == 1 && args[0] == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Verify NOT sent to players or all groups
        _playersGroupMock.Verify(
            c => c.SendCoreAsync(GameHubEvents.PlayerChoiceSubmitted, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _allGroupMock.Verify(
            c => c.SendCoreAsync(GameHubEvents.PlayerChoiceSubmitted, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    // === Read Aloud Text Events ===
    
    [Fact]
    public async Task SendsReadAloudTextToDmGroupOnly()
    {
        // Arrange
        var text = "As you enter the dungeon, a chill runs down your spine...";
        
        // Act
        await _service.NotifyReadAloudTextAsync(_campaignId, text);
        
        // Assert
        _dmGroupMock.Verify(
            c => c.SendCoreAsync(
                GameHubEvents.ReadAloudTextReceived,
                It.Is<object?[]>(args => args.Length == 1 && (string)args[0]! == text),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Verify NOT sent to players or all groups
        _playersGroupMock.Verify(
            c => c.SendCoreAsync(GameHubEvents.ReadAloudTextReceived, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _allGroupMock.Verify(
            c => c.SendCoreAsync(GameHubEvents.ReadAloudTextReceived, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    // === Combat Events ===
    
    [Fact]
    public async Task SendsCombatStartedToAllGroup()
    {
        // Arrange - CombatStatePayload uses (CombatId, IsActive, RoundNumber, TurnOrder, CurrentTurnIndex)
        var turnOrder = new List<CombatantInfo>
        {
            new("char-1", "Test Fighter", "PC", 18, 44, 44, false, false),
            new("enemy-1", "Goblin", "Enemy", 15, 7, 7, false, false)
        };
        var payload = new CombatStatePayload("combat-1", true, 1, turnOrder, 0);
        
        // Act
        await _service.NotifyCombatStartedAsync(_campaignId, payload);
        
        // Assert
        _allGroupMock.Verify(
            c => c.SendCoreAsync(
                GameHubEvents.CombatStarted,
                It.Is<object?[]>(args => args.Length == 1 && args[0] == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task SendsCombatEndedToAllGroup()
    {
        // Act
        await _service.NotifyCombatEndedAsync(_campaignId);
        
        // Assert - CombatEnded is sent with no payload (cancellationToken only)
        _allGroupMock.Verify(
            c => c.SendCoreAsync(
                GameHubEvents.CombatEnded,
                It.Is<object?[]>(args => args.Length == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task SendsTurnAdvancedToAllGroup()
    {
        // Arrange
        var payload = new TurnAdvancedPayload(1, "char-2", 2);
        
        // Act
        await _service.NotifyTurnAdvancedAsync(_campaignId, payload);
        
        // Assert - now sends single payload argument
        _allGroupMock.Verify(
            c => c.SendCoreAsync(
                GameHubEvents.TurnAdvanced,
                It.Is<object?[]>(args => args.Length == 1 && args[0] == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    // === Atmospheric Events ===
    
    [Fact]
    public async Task SendsAtmospherePulseToPlayersOnly()
    {
        // Arrange
        var payload = new AtmospherePulsePayload(
            Text: "A cold wind howls through the corridor...",
            Intensity: "Medium",
            SensoryType: "Feeling"
        );
        
        // Act
        await _service.NotifyAtmospherePulseAsync(_campaignId, payload);
        
        // Assert
        _playersGroupMock.Verify(
            c => c.SendCoreAsync(
                GameHubEvents.AtmospherePulseReceived,
                It.Is<object?[]>(args => args.Length == 1 && args[0] == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Verify NOT sent to DM or all groups
        _dmGroupMock.Verify(
            c => c.SendCoreAsync(GameHubEvents.AtmospherePulseReceived, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _allGroupMock.Verify(
            c => c.SendCoreAsync(GameHubEvents.AtmospherePulseReceived, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    [Fact]
    public async Task SendsNarrativeAnchorToPlayersOnly()
    {
        // Arrange
        var payload = new NarrativeAnchorPayload(
            ShortText: "The ghost weeps nearby",
            MoodCategory: "Mystery"
        );
        
        // Act
        await _service.NotifyNarrativeAnchorAsync(_campaignId, payload);
        
        // Assert
        _playersGroupMock.Verify(
            c => c.SendCoreAsync(
                GameHubEvents.NarrativeAnchorUpdated,
                It.Is<object?[]>(args => args.Length == 1 && args[0] == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Verify NOT sent to DM or all groups
        _dmGroupMock.Verify(
            c => c.SendCoreAsync(GameHubEvents.NarrativeAnchorUpdated, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    [Fact]
    public async Task SendsGroupInsightToPlayersOnly()
    {
        // Arrange
        var payload = new GroupInsightPayload(
            Text: "You notice faint scratches on the wall forming a pattern",
            RelevantSkill: "Investigation",
            HighlightEffect: true
        );
        
        // Act
        await _service.NotifyGroupInsightAsync(_campaignId, payload);
        
        // Assert
        _playersGroupMock.Verify(
            c => c.SendCoreAsync(
                GameHubEvents.GroupInsightTriggered,
                It.Is<object?[]>(args => args.Length == 1 && args[0] == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Verify NOT sent to DM or all groups
        _dmGroupMock.Verify(
            c => c.SendCoreAsync(GameHubEvents.GroupInsightTriggered, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    // === Character Claim Events ===
    
    [Fact]
    public async Task SendsCharacterClaimedToDmGroupOnly()
    {
        // Arrange
        var payload = new CharacterClaimPayload(
            CharacterId: "char-123",
            CharacterName: "Test Fighter",
            PlayerId: "player-456",
            PlayerName: "John Doe",
            IsClaimed: true
        );
        
        // Act
        await _service.NotifyCharacterClaimedAsync(_campaignId, payload);
        
        // Assert
        _dmGroupMock.Verify(
            c => c.SendCoreAsync(
                GameHubEvents.CharacterClaimed,
                It.Is<object?[]>(args => args.Length == 1 && args[0] == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Verify NOT sent to players or all groups
        _playersGroupMock.Verify(
            c => c.SendCoreAsync(GameHubEvents.CharacterClaimed, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    // === Player Roll Events ===
    
    [Fact]
    public async Task SendsPlayerRollToAllGroup()
    {
        // Arrange
        var roll = new Riddle.Web.Models.RollResult
        {
            Id = Guid.CreateVersion7(),
            CharacterId = "char-123",
            CharacterName = "Test Fighter",
            CheckType = "Attack Roll",
            Result = 18,
            Outcome = "Hit",
            Timestamp = DateTime.UtcNow
        };
        
        // Act
        await _service.NotifyPlayerRollAsync(_campaignId, roll);
        
        // Assert
        _allGroupMock.Verify(
            c => c.SendCoreAsync(
                GameHubEvents.PlayerRollLogged,
                It.Is<object?[]>(args => args.Length == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    // === Scene Image Events ===
    
    [Fact]
    public async Task SendsSceneImageToAllGroup()
    {
        // Arrange
        var imageUri = "https://example.com/dungeon-entrance.jpg";
        
        // Act
        await _service.NotifySceneImageAsync(_campaignId, imageUri);
        
        // Assert
        _allGroupMock.Verify(
            c => c.SendCoreAsync(
                GameHubEvents.SceneImageUpdated,
                It.Is<object?[]>(args => args.Length == 1 && (string)args[0]! == imageUri),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
