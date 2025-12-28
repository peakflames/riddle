# Phase 2 Implementation Plan: LLM Integration

**Version:** 1.0  
**Date:** December 28, 2024  
**Status:** Ready for Implementation  
**Phase:** LLM Integration (Week 2)

---

## [Overview]

Phase 2 integrates the LLM Tornado SDK with OpenRouter to establish the core "brain" of Project Riddle. This phase implements the complete tool-calling infrastructure that allows the LLM to interact with game state, update characters, manage combat, and present narrative content to players.

**Key Objectives:**
1. Install and configure LLM Tornado SDK with OpenRouter
2. Implement `IRiddleLlmService`/`RiddleLlmService` for LLM communication
3. Implement `IGameStateService`/`GameStateService` for state management
4. Implement `IToolExecutor`/`ToolExecutor` as tool router
5. Create all 7 tool handler implementations
6. Build basic chat UI for DM-to-LLM communication
7. Test streaming responses and tool execution

**Success Criteria:**
- LLM responds to DM input via chat interface
- All 7 tools execute correctly when called by LLM
- Game state updates persist to database
- Streaming tokens display in real-time
- Tool results feed back to LLM for continued conversation

**Dependencies:**
- Phase 1 complete (v0.2.0) ✅
- OpenRouter API key configured
- LLM Tornado NuGet package installed

---

## [Objectives Breakdown]

### Objective 1: LLM Tornado SDK Setup
**Scope:** Install NuGet packages, configure API keys, verify connectivity
**Estimated Effort:** Small
**Files:** `Riddle.Web.csproj`, `appsettings.json`, `Program.cs`

### Objective 2: GameStateService Implementation
**Scope:** CRUD operations for campaign state, tool-friendly data access
**Estimated Effort:** Small
**Files:** `Services/IGameStateService.cs`, `Services/GameStateService.cs`

### Objective 3: ToolExecutor Implementation
**Scope:** Tool routing, argument parsing, result formatting
**Estimated Effort:** Medium
**Files:** `Services/IToolExecutor.cs`, `Services/ToolExecutor.cs`

### Objective 4: Tool Handler Implementations
**Scope:** All 7 tool handlers
**Estimated Effort:** Large
**Files:** `Tools/*.cs` (7 files)

### Objective 5: RiddleLlmService Implementation
**Scope:** LLM communication, streaming, tool call handling
**Estimated Effort:** Large
**Files:** `Services/IRiddleLlmService.cs`, `Services/RiddleLlmService.cs`

### Objective 6: DM Chat Interface
**Scope:** Basic chat UI with streaming support
**Estimated Effort:** Medium
**Files:** `Components/Pages/DM/Campaign.razor` (update), `Components/Chat/DmChat.razor`

### Objective 7: Integration Testing
**Scope:** End-to-end testing of LLM → Tool → State → UI flow
**Estimated Effort:** Medium
**Files:** `Components/Pages/Test/LlmTest.razor`

---

## [Types]

### Tool Function Definitions

The LLM uses these 7 tools to interact with the game:

```csharp
// Tool 1: get_game_state
// No parameters - retrieves full campaign state for context recovery
// Returns: JSON with campaign_id, name, current_location, party_state, active_quests, etc.

// Tool 2: update_character_state
// Parameters: character_id (string), key (enum), value (varies)
// Keys: "current_hp", "conditions", "status_notes", "initiative"
// Returns: { success: true }

// Tool 3: update_game_log
// Parameters: entry (string), importance (enum: "minor", "standard", "critical")
// Returns: { success: true }

// Tool 4: display_read_aloud_text
// Parameters: text (string)
// Returns: { success: true }

// Tool 5: present_player_choices
// Parameters: choices (string[])
// Returns: { success: true }

// Tool 6: log_player_roll
// Parameters: character_id (string), check_type (string), result (int), outcome (enum)
// Returns: { success: true }

// Tool 7: update_scene_image
// Parameters: description (string)
// Returns: { success: true, image_uri: string }
```

### Service Interfaces

```csharp
namespace Riddle.Web.Services;

/// <summary>
/// Service for LLM communication via OpenRouter
/// </summary>
public interface IRiddleLlmService
{
    /// <summary>
    /// Process DM input and stream response with tool handling
    /// </summary>
    Task ProcessDmInputAsync(
        Guid campaignId, 
        string dmMessage,
        Func<string, Task> onStreamToken,
        CancellationToken ct = default);
}

/// <summary>
/// Service for game state operations used by tools
/// </summary>
public interface IGameStateService
{
    Task<CampaignInstance?> GetCampaignAsync(Guid campaignId, CancellationToken ct = default);
    Task<CampaignInstance> UpdateCampaignAsync(CampaignInstance campaign, CancellationToken ct = default);
    Task<Character?> GetCharacterAsync(Guid campaignId, string characterId, CancellationToken ct = default);
    Task UpdateCharacterAsync(Guid campaignId, Character character, CancellationToken ct = default);
    Task AddLogEntryAsync(Guid campaignId, LogEntry entry, CancellationToken ct = default);
}

/// <summary>
/// Routes tool calls from LLM to appropriate handlers
/// </summary>
public interface IToolExecutor
{
    Task<string> ExecuteAsync(
        Guid campaignId, 
        string toolName, 
        string argumentsJson, 
        CancellationToken ct = default);
}
```

---

## [Files]

### New Files to Create

#### Services
| File | Description |
|------|-------------|
| `src/Riddle.Web/Services/IGameStateService.cs` | Game state service interface |
| `src/Riddle.Web/Services/GameStateService.cs` | Game state service implementation |
| `src/Riddle.Web/Services/IToolExecutor.cs` | Tool executor interface |
| `src/Riddle.Web/Services/ToolExecutor.cs` | Tool executor implementation |
| `src/Riddle.Web/Services/IRiddleLlmService.cs` | LLM service interface |
| `src/Riddle.Web/Services/RiddleLlmService.cs` | LLM service implementation |

#### Tools
| File | Description |
|------|-------------|
| `src/Riddle.Web/Tools/GetGameStateTool.cs` | Retrieves full campaign state |
| `src/Riddle.Web/Tools/UpdateCharacterStateTool.cs` | Updates character HP/conditions |
| `src/Riddle.Web/Tools/UpdateGameLogTool.cs` | Adds entries to narrative log |
| `src/Riddle.Web/Tools/DisplayReadAloudTextTool.cs` | Sets RATB content |
| `src/Riddle.Web/Tools/PresentPlayerChoicesTool.cs` | Sets player choice buttons |
| `src/Riddle.Web/Tools/LogPlayerRollTool.cs` | Records dice roll results |
| `src/Riddle.Web/Tools/UpdateSceneImageTool.cs` | Updates scene image |

#### Components
| File | Description |
|------|-------------|
| `src/Riddle.Web/Components/Chat/DmChat.razor` | DM-to-LLM chat component |
| `src/Riddle.Web/Components/Chat/ChatMessage.razor` | Individual chat message display |
| `src/Riddle.Web/Components/Pages/Test/LlmTest.razor` | LLM integration test page |

### Files to Modify

| File | Changes |
|------|---------|
| `src/Riddle.Web/Riddle.Web.csproj` | Add LlmTornado NuGet packages |
| `src/Riddle.Web/appsettings.json` | Add OpenRouter configuration section |
| `src/Riddle.Web/Program.cs` | Register new services |
| `src/Riddle.Web/Components/Pages/DM/Campaign.razor` | Add chat interface |

---

## [Classes]

### Objective 1: LLM Tornado SDK Setup

#### Riddle.Web.csproj (additions)
```xml
<!-- LLM Integration -->
<PackageReference Include="LlmTornado" Version="4.*" />
```

#### appsettings.json (additions)
```json
{
  "OpenRouter": {
    "ApiKey": "",
    "DefaultModel": "anthropic/claude-sonnet-4-20250514",
    "SiteUrl": "https://riddle.peakflames.com",
    "SiteName": "Project Riddle"
  }
}
```

#### Program.cs (service registration)
```csharp
// LLM & Tool Services
builder.Services.AddScoped<IGameStateService, GameStateService>();
builder.Services.AddScoped<IToolExecutor, ToolExecutor>();
builder.Services.AddScoped<IRiddleLlmService, RiddleLlmService>();
```

---

### Objective 2: GameStateService

#### IGameStateService.cs
```csharp
using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Service for game state operations used by LLM tools.
/// Provides a simplified interface for reading and updating campaign state.
/// </summary>
public interface IGameStateService
{
    /// <summary>
    /// Get a campaign by ID with all related data
    /// </summary>
    Task<CampaignInstance?> GetCampaignAsync(Guid campaignId, CancellationToken ct = default);
    
    /// <summary>
    /// Update a campaign's state
    /// </summary>
    Task<CampaignInstance> UpdateCampaignAsync(CampaignInstance campaign, CancellationToken ct = default);
    
    /// <summary>
    /// Get a specific character from a campaign's party
    /// </summary>
    Task<Character?> GetCharacterAsync(Guid campaignId, string characterId, CancellationToken ct = default);
    
    /// <summary>
    /// Update a character's state within a campaign
    /// </summary>
    Task UpdateCharacterAsync(Guid campaignId, Character character, CancellationToken ct = default);
    
    /// <summary>
    /// Add a log entry to the campaign's narrative log
    /// </summary>
    Task AddLogEntryAsync(Guid campaignId, LogEntry entry, CancellationToken ct = default);
    
    /// <summary>
    /// Update the campaign's read-aloud text
    /// </summary>
    Task SetReadAloudTextAsync(Guid campaignId, string text, CancellationToken ct = default);
    
    /// <summary>
    /// Update the player choices displayed
    /// </summary>
    Task SetPlayerChoicesAsync(Guid campaignId, List<string> choices, CancellationToken ct = default);
    
    /// <summary>
    /// Update the scene image URI
    /// </summary>
    Task SetSceneImageAsync(Guid campaignId, string imageUri, CancellationToken ct = default);
}
```

#### GameStateService.cs
```csharp
using Microsoft.EntityFrameworkCore;
using Riddle.Web.Data;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Implementation of game state service for LLM tool operations
/// </summary>
public class GameStateService : IGameStateService
{
    private readonly RiddleDbContext _dbContext;
    private readonly ILogger<GameStateService> _logger;

    public GameStateService(RiddleDbContext dbContext, ILogger<GameStateService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CampaignInstance?> GetCampaignAsync(Guid campaignId, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting campaign {CampaignId}", campaignId);
        return await _dbContext.CampaignInstances
            .FirstOrDefaultAsync(c => c.Id == campaignId, ct);
    }

    public async Task<CampaignInstance> UpdateCampaignAsync(CampaignInstance campaign, CancellationToken ct = default)
    {
        _logger.LogDebug("Updating campaign {CampaignId}", campaign.Id);
        campaign.LastActivityAt = DateTime.UtcNow;
        _dbContext.CampaignInstances.Update(campaign);
        await _dbContext.SaveChangesAsync(ct);
        return campaign;
    }

    public async Task<Character?> GetCharacterAsync(Guid campaignId, string characterId, CancellationToken ct = default)
    {
        var campaign = await GetCampaignAsync(campaignId, ct);
        return campaign?.PartyState.FirstOrDefault(c => c.Id == characterId);
    }

    public async Task UpdateCharacterAsync(Guid campaignId, Character character, CancellationToken ct = default)
    {
        var campaign = await GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        var partyState = campaign.PartyState;
        var index = partyState.FindIndex(c => c.Id == character.Id);
        
        if (index >= 0)
        {
            partyState[index] = character;
        }
        else
        {
            partyState.Add(character);
        }
        
        campaign.PartyState = partyState;
        await UpdateCampaignAsync(campaign, ct);
    }

    public async Task AddLogEntryAsync(Guid campaignId, LogEntry entry, CancellationToken ct = default)
    {
        var campaign = await GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        var log = campaign.NarrativeLog;
        log.Add(entry);
        campaign.NarrativeLog = log;
        
        await UpdateCampaignAsync(campaign, ct);
    }

    public async Task SetReadAloudTextAsync(Guid campaignId, string text, CancellationToken ct = default)
    {
        var campaign = await GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        campaign.CurrentReadAloudText = text;
        await UpdateCampaignAsync(campaign, ct);
    }

    public async Task SetPlayerChoicesAsync(Guid campaignId, List<string> choices, CancellationToken ct = default)
    {
        var campaign = await GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        campaign.ActivePlayerChoices = choices;
        await UpdateCampaignAsync(campaign, ct);
    }

    public async Task SetSceneImageAsync(Guid campaignId, string imageUri, CancellationToken ct = default)
    {
        var campaign = await GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        campaign.CurrentSceneImageUri = imageUri;
        await UpdateCampaignAsync(campaign, ct);
    }
}
```

---

### Objective 3: ToolExecutor

#### IToolExecutor.cs
```csharp
namespace Riddle.Web.Services;

/// <summary>
/// Routes tool calls from LLM to appropriate handlers and returns results
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Execute a tool by name with JSON arguments
    /// </summary>
    /// <param name="campaignId">The campaign context</param>
    /// <param name="toolName">Name of the tool to execute</param>
    /// <param name="argumentsJson">JSON-encoded arguments</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>JSON-encoded result</returns>
    Task<string> ExecuteAsync(
        Guid campaignId, 
        string toolName, 
        string argumentsJson, 
        CancellationToken ct = default);
}
```

#### ToolExecutor.cs
```csharp
using System.Text.Json;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Routes LLM tool calls to appropriate handlers
/// </summary>
public class ToolExecutor : IToolExecutor
{
    private readonly IGameStateService _stateService;
    private readonly ILogger<ToolExecutor> _logger;

    public ToolExecutor(IGameStateService stateService, ILogger<ToolExecutor> logger)
    {
        _stateService = stateService;
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(
        Guid campaignId, 
        string toolName, 
        string argumentsJson, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("Executing tool {ToolName} for campaign {CampaignId}", toolName, campaignId);
        _logger.LogDebug("Tool arguments: {Arguments}", argumentsJson);

        try
        {
            var result = toolName switch
            {
                "get_game_state" => await ExecuteGetGameStateAsync(campaignId, ct),
                "update_character_state" => await ExecuteUpdateCharacterStateAsync(campaignId, argumentsJson, ct),
                "update_game_log" => await ExecuteUpdateGameLogAsync(campaignId, argumentsJson, ct),
                "display_read_aloud_text" => await ExecuteDisplayReadAloudTextAsync(campaignId, argumentsJson, ct),
                "present_player_choices" => await ExecutePresentPlayerChoicesAsync(campaignId, argumentsJson, ct),
                "log_player_roll" => await ExecuteLogPlayerRollAsync(campaignId, argumentsJson, ct),
                "update_scene_image" => await ExecuteUpdateSceneImageAsync(campaignId, argumentsJson, ct),
                _ => JsonSerializer.Serialize(new { error = $"Unknown tool: {toolName}" })
            };

            _logger.LogDebug("Tool {ToolName} result: {Result}", toolName, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private async Task<string> ExecuteGetGameStateAsync(Guid campaignId, CancellationToken ct)
    {
        var campaign = await _stateService.GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            return JsonSerializer.Serialize(new { error = "Campaign not found" });
        }

        return JsonSerializer.Serialize(new
        {
            campaign_id = campaign.Id,
            name = campaign.Name,
            campaign_module = campaign.CampaignModule,
            current_chapter_id = campaign.CurrentChapterId,
            current_location_id = campaign.CurrentLocationId,
            party_state = campaign.PartyState,
            active_quests = campaign.ActiveQuests,
            active_combat = campaign.ActiveCombat,
            preferences = campaign.Preferences,
            last_narrative_summary = campaign.LastNarrativeSummary,
            completed_milestones = campaign.CompletedMilestones,
            known_npc_ids = campaign.KnownNpcIds,
            discovered_locations = campaign.DiscoveredLocations
        });
    }

    private async Task<string> ExecuteUpdateCharacterStateAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        var characterId = args.GetProperty("character_id").GetString()!;
        var key = args.GetProperty("key").GetString()!;
        var value = args.GetProperty("value");

        var character = await _stateService.GetCharacterAsync(campaignId, characterId, ct);
        if (character == null)
        {
            return JsonSerializer.Serialize(new { error = $"Character {characterId} not found" });
        }

        switch (key)
        {
            case "current_hp":
                character.CurrentHp = value.GetInt32();
                break;
            case "conditions":
                character.Conditions = value.EnumerateArray()
                    .Select(e => e.GetString()!)
                    .ToList();
                break;
            case "status_notes":
                character.StatusNotes = value.GetString();
                break;
            case "initiative":
                character.Initiative = value.GetInt32();
                break;
            default:
                return JsonSerializer.Serialize(new { error = $"Unknown key: {key}" });
        }

        await _stateService.UpdateCharacterAsync(campaignId, character, ct);
        return JsonSerializer.Serialize(new { success = true });
    }

    private async Task<string> ExecuteUpdateGameLogAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        var entry = args.GetProperty("entry").GetString()!;
        var importance = args.TryGetProperty("importance", out var imp) 
            ? imp.GetString() ?? "standard"
            : "standard";

        await _stateService.AddLogEntryAsync(campaignId, new LogEntry
        {
            Entry = entry,
            Importance = importance
        }, ct);

        return JsonSerializer.Serialize(new { success = true });
    }

    private async Task<string> ExecuteDisplayReadAloudTextAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        var text = args.GetProperty("text").GetString()!;

        await _stateService.SetReadAloudTextAsync(campaignId, text, ct);
        return JsonSerializer.Serialize(new { success = true });
    }

    private async Task<string> ExecutePresentPlayerChoicesAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        var choices = args.GetProperty("choices")
            .EnumerateArray()
            .Select(c => c.GetString()!)
            .ToList();

        await _stateService.SetPlayerChoicesAsync(campaignId, choices, ct);
        return JsonSerializer.Serialize(new { success = true });
    }

    private async Task<string> ExecuteLogPlayerRollAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        var characterId = args.GetProperty("character_id").GetString()!;
        var checkType = args.GetProperty("check_type").GetString()!;
        var result = args.GetProperty("result").GetInt32();
        var outcome = args.GetProperty("outcome").GetString()!;

        // Log the roll as a narrative entry
        await _stateService.AddLogEntryAsync(campaignId, new LogEntry
        {
            Entry = $"[Roll] {characterId}: {checkType} = {result} ({outcome})",
            Importance = "minor"
        }, ct);

        return JsonSerializer.Serialize(new { success = true, character_id = characterId, check_type = checkType, result, outcome });
    }

    private async Task<string> ExecuteUpdateSceneImageAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        var description = args.GetProperty("description").GetString()!;

        // For MVP, use placeholder image based on description hash
        // In production, integrate with image generation service
        var imageUri = $"/images/scenes/placeholder_{Math.Abs(description.GetHashCode()) % 10}.png";

        await _stateService.SetSceneImageAsync(campaignId, imageUri, ct);
        return JsonSerializer.Serialize(new { success = true, image_uri = imageUri });
    }
}
```

---

### Objective 5: RiddleLlmService

#### IRiddleLlmService.cs
```csharp
namespace Riddle.Web.Services;

/// <summary>
/// Service for LLM communication via OpenRouter using LLM Tornado SDK
/// </summary>
public interface IRiddleLlmService
{
    /// <summary>
    /// Process DM input and stream response with tool handling
    /// </summary>
    /// <param name="campaignId">The campaign context</param>
    /// <param name="dmMessage">The DM's message</param>
    /// <param name="onStreamToken">Callback for each streamed token</param>
    /// <param name="ct">Cancellation token</param>
    Task ProcessDmInputAsync(
        Guid campaignId, 
        string dmMessage,
        Func<string, Task> onStreamToken,
        CancellationToken ct = default);
}
```

#### RiddleLlmService.cs
```csharp
using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// LLM service using OpenRouter via LLM Tornado SDK
/// </summary>
public class RiddleLlmService : IRiddleLlmService
{
    private readonly TornadoApi _api;
    private readonly IToolExecutor _toolExecutor;
    private readonly IGameStateService _stateService;
    private readonly ILogger<RiddleLlmService> _logger;
    private readonly string _defaultModel;

    public RiddleLlmService(
        IConfiguration config,
        IToolExecutor toolExecutor,
        IGameStateService stateService,
        ILogger<RiddleLlmService> logger)
    {
        var apiKey = config["OpenRouter:ApiKey"] 
            ?? throw new InvalidOperationException("OpenRouter API key not configured");
        
        _defaultModel = config["OpenRouter:DefaultModel"] ?? "anthropic/claude-sonnet-4-20250514";
        
        _api = new TornadoApi(new List<ProviderAuthentication>
        {
            new(LLmProviders.OpenRouter, apiKey)
        });
        
        _toolExecutor = toolExecutor;
        _stateService = stateService;
        _logger = logger;
    }

    public async Task ProcessDmInputAsync(
        Guid campaignId, 
        string dmMessage,
        Func<string, Task> onStreamToken,
        CancellationToken ct = default)
    {
        var campaign = await _stateService.GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        var systemPrompt = BuildSystemPrompt(campaign);
        var tools = BuildToolDefinitions();

        var chat = _api.Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Of(_defaultModel, LLmProviders.OpenRouter),
            Tools = tools,
            // Let the model decide when to use tools
            ToolChoice = null, // null = auto, or use new OutboundToolChoice(OutboundToolChoiceModes.Required) to force
            Temperature = 0.7
        });

        chat.AppendSystemMessage(systemPrompt);
        chat.AppendUserInput(dmMessage);

        _logger.LogInformation("Processing DM input for campaign {CampaignId}: {Message}", 
            campaignId, dmMessage.Length > 100 ? dmMessage[..100] + "..." : dmMessage);

        await chat.StreamResponseRich(new ChatStreamEventHandler
        {
            MessageTokenHandler = async token =>
            {
                await onStreamToken(token);
            },
            FunctionCallHandler = async calls =>
            {
                _logger.LogInformation("LLM requested {Count} tool call(s)", calls.Count);
                
                foreach (var call in calls)
                {
                    _logger.LogInformation("Executing tool: {Tool}", call.Name);
                    
                    var result = await _toolExecutor.ExecuteAsync(
                        campaignId, 
                        call.Name, 
                        call.Arguments ?? "{}",
                        ct);
                    
                    call.Result = new FunctionResult(call, result, null);
                }
            },
            AfterFunctionCallsResolvedHandler = async (results, handler) =>
            {
                _logger.LogInformation("Continuing conversation after tool execution");
                await chat.StreamResponseRich(handler);
            },
            OnUnhandledError = error =>
            {
                _logger.LogError(error, "Error during LLM streaming");
                return Task.CompletedTask;
            }
        });
    }

    private string BuildSystemPrompt(CampaignInstance campaign)
    {
        return $"""
            <<role_definition>>
            You are "Riddle," an expert Dungeon Master and Narrative Engine for D&D 5th Edition campaigns.
            You are currently running the "{campaign.CampaignModule}" campaign for "{campaign.Name}".
            <</role_definition>>

            <<system_constraints>>
            **Context Window & Memory:**
            - You are stateless. Every conversation may be a fresh start.
            - **MANDATORY STARTUP:** Your first tool call MUST be `get_game_state()` to understand the current reality.
            - **NO HALLUCINATION:** Never guess HP, conditions, or locations. Use only data from GameState.
            - **MANDATORY LOGGING:** Call `update_game_log()` after major events to preserve history.
            <</system_constraints>>

            <<interaction_model>>
            1. **The Software:** Holds UI, character sheets, dice rollers, and persistent state.
            2. **You (Riddle):** The "Brain." You calculate mechanics, generate narrative, and decide outcomes.
            3. **The Human DM:** Provides player actions and dice rolls. They do not calculate mechanics.
            <</interaction_model>>

            <<workflow_protocol>>
            For each DM input:
            1. **Recover:** Call `get_game_state()` if this is a new conversation.
            2. **Analyze Context:** Check `PartyPreferences` for tone/combat level, `ActiveQuests` for hooks.
            3. **Process:** Apply D&D 5e rules. Calculate DCs, attack rolls, damage internally.
            4. **Persist:** Call `update_game_log()` for events. Call `update_character_state()` for HP/condition changes.
            5. **Output:**
               - Use `display_read_aloud_text()` for atmospheric narration.
               - Use `present_player_choices()` for decision points.
               - Use `log_player_roll()` to show mechanical results.
               - For DM-only info (e.g., hidden enemy stats), reply in chat directly.
            <</workflow_protocol>>

            <<current_game_state>>
            **Campaign:** {campaign.Name}
            **Module:** {campaign.CampaignModule}
            **Location:** {campaign.CurrentLocationId}
            **Chapter:** {campaign.CurrentChapterId}
            **Party Size:** {campaign.PartyState.Count} characters
            **Active Combat:** {(campaign.ActiveCombat?.IsActive == true ? $"Yes (Round {campaign.ActiveCombat.RoundNumber})" : "No")}
            **Last Summary:** {campaign.LastNarrativeSummary ?? "No previous summary available."}
            
            **Party Preferences:**
            - Combat Focus: {campaign.Preferences.CombatFocus}
            - Roleplay Focus: {campaign.Preferences.RoleplayFocus}
            - Pacing: {campaign.Preferences.Pacing}
            - Tone: {campaign.Preferences.Tone}
            <</current_game_state>>

            <<tone_and_style>>
            - Be a helpful mentor to the novice DM.
            - Explain the "why" behind mechanics briefly.
            - Be evocative and atmospheric in read-aloud text.
            - Adapt style based on PartyPreferences.
            <</tone_and_style>>
            """;
    }

    private List<Tool> BuildToolDefinitions()
    {
        return
        [
            new Tool(new ToolFunction(
                "get_game_state",
                "Retrieves the full game state including character HP, locations, quests, and combat status. MUST be called first in any new conversation.")),
            
            new Tool(new ToolFunction(
                "update_character_state",
                "Updates a character's HP, conditions, initiative, or status notes.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        character_id = new { type = "string", description = "ID of the character to update" },
                        key = new 
                        { 
                            type = "string", 
                            @enum = new[] { "current_hp", "conditions", "status_notes", "initiative" },
                            description = "The property to update" 
                        },
                        value = new { description = "New value (int for HP/initiative, string[] for conditions, string for notes)" }
                    },
                    required = new[] { "character_id", "key", "value" }
                })),
            
            new Tool(new ToolFunction(
                "update_game_log",
                "Records an event to the narrative log for context recovery in future conversations.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        entry = new { type = "string", description = "Description of the event" },
                        importance = new 
                        { 
                            type = "string", 
                            @enum = new[] { "minor", "standard", "critical" },
                            description = "Importance level of this event" 
                        }
                    },
                    required = new[] { "entry" }
                })),
            
            new Tool(new ToolFunction(
                "display_read_aloud_text",
                "Sends atmospheric, boxed narrative text to the DM's Read Aloud Text Box.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        text = new { type = "string", description = "The prose to display in the Read Aloud Text Box" }
                    },
                    required = new[] { "text" }
                })),
            
            new Tool(new ToolFunction(
                "present_player_choices",
                "Sends interactive choice buttons to player screens.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        choices = new 
                        { 
                            type = "array", 
                            items = new { type = "string" },
                            description = "List of choices for players" 
                        }
                    },
                    required = new[] { "choices" }
                })),
            
            new Tool(new ToolFunction(
                "log_player_roll",
                "Records a dice roll result to the player dashboard.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        character_id = new { type = "string", description = "Character who made the roll" },
                        check_type = new { type = "string", description = "Type of check (e.g., 'Perception')" },
                        result = new { type = "integer", description = "The dice roll result" },
                        outcome = new 
                        { 
                            type = "string", 
                            @enum = new[] { "Success", "Failure", "Critical Success", "Critical Failure" },
                            description = "Outcome of the roll"
                        }
                    },
                    required = new[] { "character_id", "check_type", "result", "outcome" }
                })),
            
            new Tool(new ToolFunction(
                "update_scene_image",
                "Updates the scene image displayed to players based on a description.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        description = new { type = "string", description = "Description for image generation or selection" }
                    },
                    required = new[] { "description" }
                }))
        ];
    }
}
```

---

## [Testing]

### Manual Testing Checklist

#### Objective 1: SDK Setup
- [ ] `python build.py` passes after adding LlmTornado package
- [ ] No missing dependency errors at runtime
- [ ] OpenRouter configuration loads from appsettings.json

#### Objective 2: GameStateService
- [ ] `GetCampaignAsync` returns campaign data
- [ ] `UpdateCampaignAsync` persists changes
- [ ] `GetCharacterAsync` returns character from party
- [ ] `UpdateCharacterAsync` updates character in party
- [ ] `AddLogEntryAsync` adds entry to narrative log
- [ ] `SetReadAloudTextAsync` updates campaign's read-aloud text
- [ ] `SetPlayerChoicesAsync` updates active choices
- [ ] `SetSceneImageAsync` updates scene image URI

#### Objective 3: ToolExecutor
- [ ] `get_game_state` returns full campaign JSON
- [ ] `update_character_state` updates HP correctly
- [ ] `update_character_state` updates conditions correctly
- [ ] `update_game_log` adds log entry
- [ ] `display_read_aloud_text` sets text
- [ ] `present_player_choices` sets choices array
- [ ] `log_player_roll` records roll
- [ ] `update_scene_image` sets image URI
- [ ] Unknown tool returns error JSON

#### Objective 5: RiddleLlmService
- [ ] Service initializes without error
- [ ] Simple prompt returns streamed response
- [ ] Tool calls are executed when LLM requests them
- [ ] Conversation continues after tool execution
- [ ] Errors are logged and don't crash the application

#### Objective 6: DM Chat Interface
- [ ] Chat messages display correctly
- [ ] User can type and send messages
- [ ] Streaming tokens appear incrementally
- [ ] Loading indicator shows during processing
- [ ] Chat history persists during session

#### Objective 7: Integration Testing
- [ ] LLM test page loads
- [ ] Can send test message to LLM
- [ ] LLM calls `get_game_state` tool
- [ ] Tool results appear in logs
- [ ] Response streams to UI

---

## [Implementation Order]

Execute objectives in this sequence to manage dependencies:

### Step 1: SDK Setup (Objective 1)
1. Add `LlmTornado` NuGet package to `Riddle.Web.csproj`
2. Add OpenRouter configuration section to `appsettings.json`
3. Run `python build.py` to verify package installation
4. Configure API key via user secrets or environment variable

### Step 2: GameStateService (Objective 2)
1. Create `Services/IGameStateService.cs` interface
2. Create `Services/GameStateService.cs` implementation
3. Register service in `Program.cs`
4. Test via existing `DataModelTest.razor` page

### Step 3: ToolExecutor (Objectives 3 & 4)
1. Create `Services/IToolExecutor.cs` interface
2. Create `Services/ToolExecutor.cs` with all 7 tool handlers
3. Register service in `Program.cs`
4. Create simple test endpoint to verify tool execution

### Step 4: RiddleLlmService (Objective 5)
1. Create `Services/IRiddleLlmService.cs` interface
2. Create `Services/RiddleLlmService.cs` implementation
3. Register service in `Program.cs`
4. Verify TornadoApi initialization

### Step 5: DM Chat Interface (Objective 6)
1. Create `Components/Chat/ChatMessage.razor` component
2. Create `Components/Chat/DmChat.razor` component
3. Update `Components/Pages/DM/Campaign.razor` to include chat
4. Test streaming response display

### Step 6: Integration Testing (Objective 7)
1. Create `Components/Pages/Test/LlmTest.razor` page
2. Add test scenarios for each tool
3. Verify end-to-end flow
4. Document any issues in lessons learned

---

## [DM Chat UI Specification]

### DmChat.razor Component
```razor
@using Riddle.Web.Services
@inject IRiddleLlmService LlmService
@inject ILogger<DmChat> Logger

<div class="flex flex-col h-full bg-white dark:bg-gray-800 rounded-lg shadow">
    <!-- Header -->
    <div class="p-4 border-b dark:border-gray-700">
        <h2 class="text-xl font-semibold text-gray-900 dark:text-white flex items-center gap-2">
            <span>🎲</span>
            <span>DM Console</span>
        </h2>
    </div>
    
    <!-- Messages Area -->
    <div class="flex-1 overflow-y-auto p-4 space-y-4" @ref="_messagesContainer">
        @foreach (var message in _messages)
        {
            <ChatMessage Role="@message.Role" Content="@message.Content" IsStreaming="@message.IsStreaming" />
        }
        
        @if (_isProcessing)
        {
            <div class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
                <Spinner Size="SpinnerSize.Sm" />
                <span>Riddle is thinking...</span>
            </div>
        }
    </div>

    <!-- Input Area -->
    <div class="p-4 border-t dark:border-gray-700">
        <form @onsubmit="HandleSubmitAsync" @onsubmit:preventDefault>
            <div class="flex gap-2">
                <TextInput @bind-Value="_inputText" 
                          Placeholder="Describe the action or ask Riddle for advice..."
                          Disabled="_isProcessing"
                          class="flex-1" />
                <Button Type="ButtonType.Submit" 
                        Disabled="@(string.IsNullOrWhiteSpace(_inputText) || _isProcessing)">
                    Send
                </Button>
            </div>
        </form>
    </div>
</div>

@code {
    [Parameter]
    public Guid CampaignId { get; set; }
    
    private List<ChatMessageModel> _messages = new();
    private string _inputText = "";
    private bool _isProcessing;
    private ElementReference _messagesContainer;

    private async Task HandleSubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(_inputText) || _isProcessing)
            return;

        var userMessage = _inputText;
        _inputText = "";
        _isProcessing = true;

        // Add user message
        _messages.Add(new ChatMessageModel
        {
            Role = "user",
            Content = userMessage
        });

        // Add placeholder for assistant response
        var assistantMessage = new ChatMessageModel
        {
            Role = "assistant",
            Content = "",
            IsStreaming = true
        };
        _messages.Add(assistantMessage);
        
        StateHasChanged();

        try
        {
            await LlmService.ProcessDmInputAsync(
                CampaignId,
                userMessage,
                async token =>
                {
                    assistantMessage.Content += token;
                    await InvokeAsync(StateHasChanged);
                });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing DM input");
            assistantMessage.Content = $"Error: {ex.Message}";
        }
        finally
        {
            assistantMessage.IsStreaming = false;
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private class ChatMessageModel
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public bool IsStreaming { get; set; }
    }
}
```

---

## [Dependencies]

### NuGet Packages to Add
```xml
<PackageReference Include="LlmTornado" Version="4.*" />
```

### Configuration Keys
| Key | Description | Required |
|-----|-------------|----------|
| `OpenRouter:ApiKey` | OpenRouter API key | Yes |
| `OpenRouter:DefaultModel` | Default LLM model | No (defaults to claude-sonnet) |
| `OpenRouter:SiteUrl` | Site URL for OpenRouter | No |
| `OpenRouter:SiteName` | Site name for OpenRouter | No |

### External Dependencies
- OpenRouter account with API key
- Network access to OpenRouter API

---

## [Commands for Phase 2 Implementation]

```bash
# Step 1: Add LlmTornado package
cd src/Riddle.Web
dotnet add package LlmTornado --prerelease

# Step 2: Configure API key (use user secrets for development)
dotnet user-secrets set "OpenRouter:ApiKey" "YOUR_API_KEY"

# Step 3: Build and verify
python build.py

# Step 4: Run and test
python build.py start

# Step 5: Check logs
type riddle.log
```

---

## [Phase 2 Completion Checklist]

### Objective 1: LLM Tornado SDK Setup
- [ ] LlmTornado NuGet package installed
- [ ] OpenRouter configuration in appsettings.json
- [ ] API key configured via user secrets
- [ ] Build passes with new dependencies

### Objective 2: GameStateService
- [ ] IGameStateService.cs created
- [ ] GameStateService.cs created
- [ ] Service registered in Program.cs
- [ ] All methods tested

### Objective 3: ToolExecutor
- [ ] IToolExecutor.cs created
- [ ] ToolExecutor.cs created with all 7 handlers
- [ ] Service registered in Program.cs
- [ ] All tools tested

### Objective 4: Tool Handlers (within ToolExecutor)
- [ ] get_game_state handler implemented
- [ ] update_character_state handler implemented
- [ ] update_game_log handler implemented
- [ ] display_read_aloud_text handler implemented
- [ ] present_player_choices handler implemented
- [ ] log_player_roll handler implemented
- [ ] update_scene_image handler implemented

### Objective 5: RiddleLlmService
- [ ] IRiddleLlmService.cs created
- [ ] RiddleLlmService.cs created
- [ ] System prompt builder implemented
- [ ] Tool definitions implemented
- [ ] Streaming response handling implemented
- [ ] Service registered in Program.cs

### Objective 6: DM Chat Interface
- [ ] ChatMessage.razor component created
- [ ] DmChat.razor component created
- [ ] Campaign.razor updated with chat
- [ ] Streaming display works
- [ ] Error handling implemented

### Objective 7: Integration Testing
- [ ] LlmTest.razor page created
- [ ] End-to-end flow verified
- [ ] Tool execution confirmed
- [ ] State persistence confirmed

### Version Bump
- [ ] Version updated to 0.3.0 in Riddle.Web.csproj
- [ ] CHANGELOG.md updated with Phase 2 changes
- [ ] Git commit with release notes

---

## [Risk Mitigation]

| Risk | Mitigation |
|------|------------|
| OpenRouter API unavailable | Add fallback error message, cache last response |
| LlmTornado API changes | Pin to specific version, review release notes |
| Streaming issues | Add timeout, fallback to non-streaming |
| Tool execution failures | Wrap in try-catch, return error JSON |
| Context window overflow | Implement summary compression (Phase 3) |

---

## Next Phase Preview

**Phase 3: SignalR & Real-time (Week 3)**
- Implement SignalR GameHub for multi-client sync
- Wire tool executor to broadcast events via SignalR
- Build real-time updates for Read Aloud Text Box
- Implement player choice submission flow
- Combat tracker real-time updates
