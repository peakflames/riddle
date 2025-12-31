using Riddle.Web.Hubs;
using Riddle.Web.IntegrationTests.Infrastructure;

namespace Riddle.Web.IntegrationTests.HubTests;

/// <summary>
/// Hub-level integration tests for atmospheric/ambient events.
/// These events are player-only to create immersive experiences without cluttering the DM's view.
/// 
/// WebApplicationFactory is shared via collection fixture, but each test creates fresh
/// SignalR connections for isolation.
/// </summary>
[Collection("SignalR")]
public class AtmosphericEventTests
{
    private readonly SignalRTestFixture _fixture;
    
    public AtmosphericEventTests(SignalRTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    /// <summary>
    /// Creates fresh clients for a test. Caller must dispose.
    /// </summary>
    private async Task<(TestSignalRClient dm, TestSignalRClient player, Guid campaignId)> CreateTestClientsAsync()
    {
        var campaign = await _fixture.SetupTestCampaignAsync();
        var dmClient = await _fixture.CreateDmClientAsync(campaign.Id);
        var playerClient = await _fixture.CreatePlayerClientAsync(campaign.Id);
        await Task.Delay(50); // Brief delay for group registration
        return (dmClient, playerClient, campaign.Id);
    }
    
    // === AtmospherePulse Event Tests ===
    
    [Fact]
    public async Task AtmospherePulse_OnlyPlayersReceive()
    {
        var (dmClient, playerClient, campaignId) = await CreateTestClientsAsync();
        await using var _ = dmClient;
        await using var __ = playerClient;
        
        var payload = new AtmospherePulsePayload("A cold wind whispers...", "Medium", "Sound");
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        await notificationService.NotifyAtmospherePulseAsync(campaignId, payload);
        
        var playerEvent = await playerClient.WaitForEventAsync(GameHubEvents.AtmospherePulseReceived, TimeSpan.FromSeconds(2));
        playerEvent.Should().NotBeNull();
        
        await Task.Delay(100);
        dmClient.HasReceivedEvent(GameHubEvents.AtmospherePulseReceived).Should().BeFalse("AtmospherePulse is player-only");
    }
    
    [Fact]
    public async Task AtmospherePulse_PayloadContainsCorrectData()
    {
        var (_, playerClient, campaignId) = await CreateTestClientsAsync();
        await using var __ = playerClient;
        
        var payload = new AtmospherePulsePayload("The torch flickers ominously...", "High", "Sight");
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        await notificationService.NotifyAtmospherePulseAsync(campaignId, payload);
        
        await playerClient.WaitForEventAsync(GameHubEvents.AtmospherePulseReceived, TimeSpan.FromSeconds(2));
        var receivedPayload = playerClient.GetEventPayload<AtmospherePulsePayload>(GameHubEvents.AtmospherePulseReceived);
        
        receivedPayload.Should().NotBeNull();
        receivedPayload!.Text.Should().Contain("torch flickers");
        receivedPayload.Intensity.Should().Be("High");
        receivedPayload.SensoryType.Should().Be("Sight");
    }
    
    [Theory]
    [InlineData("Low")]
    [InlineData("Medium")]
    [InlineData("High")]
    public async Task AtmospherePulse_AllIntensityLevelsDelivered(string intensity)
    {
        var (_, playerClient, campaignId) = await CreateTestClientsAsync();
        await using var __ = playerClient;
        
        var payload = new AtmospherePulsePayload($"Event at {intensity} intensity", intensity, "Feeling");
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        await notificationService.NotifyAtmospherePulseAsync(campaignId, payload);
        
        var evt = await playerClient.WaitForEventAsync(GameHubEvents.AtmospherePulseReceived, TimeSpan.FromSeconds(2));
        evt.Should().NotBeNull();
    }
    
    // === NarrativeAnchor Event Tests ===
    
    [Fact]
    public async Task NarrativeAnchor_OnlyPlayersReceive()
    {
        var (dmClient, playerClient, campaignId) = await CreateTestClientsAsync();
        await using var _ = dmClient;
        await using var __ = playerClient;
        
        var payload = new NarrativeAnchorPayload("The ancient tomb lies ahead", "Mystery");
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        await notificationService.NotifyNarrativeAnchorAsync(campaignId, payload);
        
        var playerEvent = await playerClient.WaitForEventAsync(GameHubEvents.NarrativeAnchorUpdated, TimeSpan.FromSeconds(2));
        playerEvent.Should().NotBeNull();
        
        await Task.Delay(100);
        dmClient.HasReceivedEvent(GameHubEvents.NarrativeAnchorUpdated).Should().BeFalse("NarrativeAnchor is player-only");
    }
    
    [Fact]
    public async Task NarrativeAnchor_PayloadContainsCorrectData()
    {
        var (_, playerClient, campaignId) = await CreateTestClientsAsync();
        await using var __ = playerClient;
        
        var payload = new NarrativeAnchorPayload("Danger lurks in the shadows", "Tension");
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        await notificationService.NotifyNarrativeAnchorAsync(campaignId, payload);
        
        await playerClient.WaitForEventAsync(GameHubEvents.NarrativeAnchorUpdated, TimeSpan.FromSeconds(2));
        var receivedPayload = playerClient.GetEventPayload<NarrativeAnchorPayload>(GameHubEvents.NarrativeAnchorUpdated);
        
        receivedPayload.Should().NotBeNull();
        receivedPayload!.ShortText.Should().Be("Danger lurks in the shadows");
        receivedPayload.MoodCategory.Should().Be("Tension");
    }
    
    // === GroupInsight Event Tests ===
    
    [Fact]
    public async Task GroupInsight_OnlyPlayersReceive()
    {
        var (dmClient, playerClient, campaignId) = await CreateTestClientsAsync();
        await using var _ = dmClient;
        await using var __ = playerClient;
        
        var payload = new GroupInsightPayload("You notice faint footprints", "Perception", true);
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        await notificationService.NotifyGroupInsightAsync(campaignId, payload);
        
        var playerEvent = await playerClient.WaitForEventAsync(GameHubEvents.GroupInsightTriggered, TimeSpan.FromSeconds(2));
        playerEvent.Should().NotBeNull();
        
        await Task.Delay(100);
        dmClient.HasReceivedEvent(GameHubEvents.GroupInsightTriggered).Should().BeFalse("GroupInsight is player-only");
    }
    
    [Fact]
    public async Task GroupInsight_PayloadContainsCorrectData()
    {
        var (_, playerClient, campaignId) = await CreateTestClientsAsync();
        await using var __ = playerClient;
        
        var payload = new GroupInsightPayload("The runes spell a Dwarvish warning", "History", false);
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        await notificationService.NotifyGroupInsightAsync(campaignId, payload);
        
        await playerClient.WaitForEventAsync(GameHubEvents.GroupInsightTriggered, TimeSpan.FromSeconds(2));
        var receivedPayload = playerClient.GetEventPayload<GroupInsightPayload>(GameHubEvents.GroupInsightTriggered);
        
        receivedPayload.Should().NotBeNull();
        receivedPayload!.Text.Should().Contain("Dwarvish");
        receivedPayload.RelevantSkill.Should().Be("History");
        receivedPayload.HighlightEffect.Should().BeFalse();
    }
    
    // === Multiple Atmospheric Events Flow ===
    
    [Fact]
    public async Task MultipleAtmosphericEvents_AllDeliveredToPlayers()
    {
        var (dmClient, playerClient, campaignId) = await CreateTestClientsAsync();
        await using var _ = dmClient;
        await using var __ = playerClient;
        
        using var scope = _fixture.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<Riddle.Web.Services.INotificationService>();
        
        await notificationService.NotifyAtmospherePulseAsync(campaignId, new AtmospherePulsePayload("A chill", "Low", "Feeling"));
        await notificationService.NotifyNarrativeAnchorAsync(campaignId, new NarrativeAnchorPayload("Dungeon alive", "Tension"));
        await notificationService.NotifyGroupInsightAsync(campaignId, new GroupInsightPayload("Footsteps", "Perception", true));
        await notificationService.NotifyAtmospherePulseAsync(campaignId, new AtmospherePulsePayload("Moisture", "Medium", "Smell"));
        
        await Task.Delay(200);
        
        playerClient.HasReceivedEvent(GameHubEvents.AtmospherePulseReceived).Should().BeTrue();
        playerClient.HasReceivedEvent(GameHubEvents.NarrativeAnchorUpdated).Should().BeTrue();
        playerClient.HasReceivedEvent(GameHubEvents.GroupInsightTriggered).Should().BeTrue();
        playerClient.GetEventCount(GameHubEvents.AtmospherePulseReceived).Should().Be(2);
        
        dmClient.HasReceivedEvent(GameHubEvents.AtmospherePulseReceived).Should().BeFalse();
        dmClient.HasReceivedEvent(GameHubEvents.NarrativeAnchorUpdated).Should().BeFalse();
        dmClient.HasReceivedEvent(GameHubEvents.GroupInsightTriggered).Should().BeFalse();
    }
}
