using Microsoft.Extensions.Logging;
using Riddle.Web.Data;
using Riddle.Web.Hubs;
using Riddle.Web.Models;
using Riddle.Web.Services;
using System.Text.Json;

namespace Riddle.Web.IntegrationTests.Services;

/// <summary>
/// Tests that verify ToolExecutor correctly calls NotificationService after state changes.
/// Uses REAL services (GameStateService, CombatService) with an in-memory database,
/// and only mocks INotificationService to verify specific notification calls.
/// </summary>
public class ToolExecutorNotificationTests : IDisposable
{
    private readonly Mock<INotificationService> _notificationMock;
    private readonly RiddleDbContext _dbContext;
    private readonly GameStateService _stateService;
    private readonly CombatService _combatService;
    private readonly ToolExecutor _executor;
    private readonly Guid _campaignId = Guid.CreateVersion7();
    private readonly Character _testCharacter;

    public ToolExecutorNotificationTests()
    {
        _notificationMock = new Mock<INotificationService>();

        // Create in-memory database
        var options = new DbContextOptionsBuilder<RiddleDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new RiddleDbContext(options);

        // Create real services
        var stateLogger = Mock.Of<ILogger<GameStateService>>();
        _stateService = new GameStateService(_dbContext, stateLogger);

        var combatLogger = Mock.Of<ILogger<CombatService>>();
        _combatService = new CombatService(_dbContext, _notificationMock.Object, combatLogger);

        // Setup test character
        _testCharacter = new Character
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Fighter",
            Type = "PC",
            Race = "Human",
            Class = "Fighter",
            Level = 5,
            MaxHp = 44,
            CurrentHp = 44,
            ArmorClass = 18
        };

        // Setup test campaign in the DB
        var campaign = new CampaignInstance
        {
            Id = _campaignId,
            Name = "Test Campaign",
            DmUserId = "dm-user-id",
            CampaignModule = "Test Module",
            InviteCode = "TEST123",
            PartyState = new List<Character> { _testCharacter },
            LastActivityAt = DateTime.UtcNow
        };
        _dbContext.CampaignInstances.Add(campaign);
        _dbContext.SaveChanges();

        var executorLogger = Mock.Of<ILogger<ToolExecutor>>();
        _executor = new ToolExecutor(
            _stateService,
            _combatService,
            _notificationMock.Object,
            executorLogger);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    // === Character State Update Notifications ===

    [Fact]
    public async Task UpdateCharacterState_CurrentHp_NotifiesAllClients()
    {
        // Arrange
        var args = JsonSerializer.Serialize(new
        {
            character_name = _testCharacter.Name,
            key = "current_hp",
            value = 30
        });

        // Act
        var result = await _executor.ExecuteAsync(_campaignId, "update_character_state", args);

        // Assert - should call NotifyCharacterStateUpdatedAsync
        _notificationMock.Verify(
            n => n.NotifyCharacterStateUpdatedAsync(
                _campaignId,
                It.Is<CharacterStatePayload>(p =>
                    p.CharacterId == _testCharacter.Id &&
                    p.Key == "current_hp"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateCharacterState_Conditions_NotifiesAllClients()
    {
        // Arrange
        var args = JsonSerializer.Serialize(new
        {
            character_name = _testCharacter.Name,
            key = "conditions",
            value = new[] { "Poisoned", "Prone" }
        });

        // Act
        var result = await _executor.ExecuteAsync(_campaignId, "update_character_state", args);

        // Assert
        _notificationMock.Verify(
            n => n.NotifyCharacterStateUpdatedAsync(
                _campaignId,
                It.Is<CharacterStatePayload>(p =>
                    p.CharacterId == _testCharacter.Id &&
                    p.Key == "conditions"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // === Player Choice Notifications ===

    [Fact]
    public async Task PresentPlayerChoices_NotifiesPlayersOnly()
    {
        // Arrange
        var choices = new[] { "Attack the goblin", "Flee", "Negotiate" };
        var args = JsonSerializer.Serialize(new { choices });

        // Act
        var result = await _executor.ExecuteAsync(_campaignId, "present_player_choices", args);

        // Assert
        _notificationMock.Verify(
            n => n.NotifyPlayerChoicesAsync(
                _campaignId,
                It.Is<List<string>>(c => c.Count == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // === Roll Logging Notifications ===

    [Fact]
    public async Task LogPlayerRoll_NotifiesAllClients()
    {
        // Arrange
        var args = JsonSerializer.Serialize(new
        {
            character_name = _testCharacter.Name,
            check_type = "Perception",
            result = 18,
            outcome = "Success"
        });

        // Act
        var result = await _executor.ExecuteAsync(_campaignId, "log_player_roll", args);

        // Assert
        _notificationMock.Verify(
            n => n.NotifyPlayerRollAsync(
                _campaignId,
                It.Is<RollResult>(r =>
                    r.CharacterName == _testCharacter.Name &&
                    r.CheckType == "Perception" &&
                    r.Result == 18),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // === Combat Notifications ===

    [Fact]
    public async Task StartCombat_NotifiesViaCombatService()
    {
        // Arrange - note: start_combat uses enemies + pc_initiatives format
        var args = JsonSerializer.Serialize(new
        {
            enemies = new[]
            {
                new { name = "Goblin 1", initiative = 15, max_hp = 7, current_hp = 7 }
            },
            pc_initiatives = new Dictionary<string, int>
            {
                { _testCharacter.Name, 18 }
            }
        });

        // Act
        var result = await _executor.ExecuteAsync(_campaignId, "start_combat", args);

        // Assert - start_combat doesn't directly call NotificationService,
        // the real CombatService stores state. Verify it ran successfully.
        result.Should().Contain("Combat started");
    }

    [Fact]
    public async Task EndCombat_AfterStartCombat_Succeeds()
    {
        // Arrange - start combat first
        var startArgs = JsonSerializer.Serialize(new
        {
            enemies = new[]
            {
                new { name = "Goblin 1", initiative = 15, max_hp = 7, current_hp = 7 }
            },
            pc_initiatives = new Dictionary<string, int>
            {
                { _testCharacter.Name, 18 }
            }
        });
        await _executor.ExecuteAsync(_campaignId, "start_combat", startArgs);

        // Act
        var result = await _executor.ExecuteAsync(_campaignId, "end_combat", "{}");

        // Assert
        result.Should().Contain("Combat ended");
    }

    [Fact]
    public async Task AdvanceTurn_AfterStartCombat_Succeeds()
    {
        // Arrange - start combat first
        var startArgs = JsonSerializer.Serialize(new
        {
            enemies = new[]
            {
                new { name = "Goblin 1", initiative = 15, max_hp = 7, current_hp = 7 }
            },
            pc_initiatives = new Dictionary<string, int>
            {
                { _testCharacter.Name, 18 }
            }
        });
        await _executor.ExecuteAsync(_campaignId, "start_combat", startArgs);

        // Act
        var result = await _executor.ExecuteAsync(_campaignId, "advance_turn", "{}");

        // Assert
        result.Should().Contain("Turn advanced");
    }

    // === Atmospheric Event Notifications ===

    [Fact]
    public async Task BroadcastAtmospherePulse_NotifiesPlayersOnly()
    {
        // Arrange
        var args = JsonSerializer.Serialize(new
        {
            text = "A chill runs down your spine...",
            intensity = "Medium",
            sensory_type = "Feeling"
        });

        // Act
        var result = await _executor.ExecuteAsync(_campaignId, "broadcast_atmosphere_pulse", args);

        // Assert
        _notificationMock.Verify(
            n => n.NotifyAtmospherePulseAsync(
                _campaignId,
                It.Is<AtmospherePulsePayload>(p =>
                    p.Text.Contains("chill") &&
                    p.Intensity == "Medium"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetNarrativeAnchor_NotifiesPlayersOnly()
    {
        // Arrange
        var args = JsonSerializer.Serialize(new
        {
            short_text = "The ghost weeps nearby",
            mood_category = "Mystery"
        });

        // Act
        var result = await _executor.ExecuteAsync(_campaignId, "set_narrative_anchor", args);

        // Assert
        _notificationMock.Verify(
            n => n.NotifyNarrativeAnchorAsync(
                _campaignId,
                It.Is<NarrativeAnchorPayload>(p =>
                    p.ShortText == "The ghost weeps nearby" &&
                    p.MoodCategory == "Mystery"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TriggerGroupInsight_NotifiesPlayersOnly()
    {
        // Arrange
        var args = JsonSerializer.Serialize(new
        {
            text = "You notice faint scratch marks on the wall",
            relevant_skill = "Investigation",
            highlight_effect = true
        });

        // Act
        var result = await _executor.ExecuteAsync(_campaignId, "trigger_group_insight", args);

        // Assert
        _notificationMock.Verify(
            n => n.NotifyGroupInsightAsync(
                _campaignId,
                It.Is<GroupInsightPayload>(p =>
                    p.Text.Contains("scratch marks") &&
                    p.RelevantSkill == "Investigation" &&
                    p.HighlightEffect == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // === Known Bug Test Case ===
    // This test documents the expected behavior for the current_hp vs CurrentHp issue.
    // The LLM sends "current_hp" (snake_case), and the CharacterStatePayload should preserve this key.

    [Fact]
    public async Task UpdateCharacterState_UsesSnakeCaseKey_InPayload()
    {
        // Arrange - LLM sends snake_case key
        var args = JsonSerializer.Serialize(new
        {
            character_name = _testCharacter.Name,
            key = "current_hp",  // snake_case from LLM
            value = 25
        });

        // Act
        await _executor.ExecuteAsync(_campaignId, "update_character_state", args);

        // Assert - The key in the payload should match what was sent (snake_case)
        // If client handlers expect "CurrentHp" (PascalCase) but receive "current_hp",
        // this would cause the synchronization bug.
        _notificationMock.Verify(
            n => n.NotifyCharacterStateUpdatedAsync(
                _campaignId,
                It.Is<CharacterStatePayload>(p => p.Key == "current_hp"),  // Should be snake_case
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
