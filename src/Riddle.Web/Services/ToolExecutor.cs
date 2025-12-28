using System.Text.Json;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Routes LLM tool calls to appropriate handlers.
/// Implements all 7 game state manipulation tools.
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

    /// <summary>
    /// Tool 1: get_game_state - Retrieves full campaign state for context recovery
    /// </summary>
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
            discovered_locations = campaign.DiscoveredLocations,
            current_read_aloud_text = campaign.CurrentReadAloudText,
            active_player_choices = campaign.ActivePlayerChoices,
            current_scene_image_uri = campaign.CurrentSceneImageUri
        });
    }

    /// <summary>
    /// Tool 2: update_character_state - Updates character HP, conditions, initiative, or status notes
    /// </summary>
    private async Task<string> ExecuteUpdateCharacterStateAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        if (!args.TryGetProperty("character_id", out var characterIdElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: character_id" });
        }
        
        if (!args.TryGetProperty("key", out var keyElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: key" });
        }
        
        if (!args.TryGetProperty("value", out var valueElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: value" });
        }

        var characterId = characterIdElement.GetString()!;
        var key = keyElement.GetString()!;

        var character = await _stateService.GetCharacterAsync(campaignId, characterId, ct);
        if (character == null)
        {
            return JsonSerializer.Serialize(new { error = $"Character {characterId} not found" });
        }

        switch (key)
        {
            case "current_hp":
                character.CurrentHp = valueElement.GetInt32();
                break;
            case "conditions":
                character.Conditions = valueElement.EnumerateArray()
                    .Select(e => e.GetString()!)
                    .ToList();
                break;
            case "status_notes":
                character.StatusNotes = valueElement.GetString();
                break;
            case "initiative":
                character.Initiative = valueElement.GetInt32();
                break;
            default:
                return JsonSerializer.Serialize(new { error = $"Unknown key: {key}. Valid keys: current_hp, conditions, status_notes, initiative" });
        }

        await _stateService.UpdateCharacterAsync(campaignId, character, ct);
        
        _logger.LogInformation("Updated character {CharacterId} {Key} to {Value}", characterId, key, valueElement.ToString());
        return JsonSerializer.Serialize(new { success = true, character_id = characterId, key, updated = true });
    }

    /// <summary>
    /// Tool 3: update_game_log - Records an event to the narrative log
    /// </summary>
    private async Task<string> ExecuteUpdateGameLogAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        if (!args.TryGetProperty("entry", out var entryElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: entry" });
        }

        var entry = entryElement.GetString()!;
        var importance = args.TryGetProperty("importance", out var imp) 
            ? imp.GetString() ?? "standard"
            : "standard";

        var logEntry = new LogEntry
        {
            Entry = entry,
            Importance = importance
        };

        await _stateService.AddLogEntryAsync(campaignId, logEntry, ct);

        _logger.LogInformation("Added log entry [{Importance}]: {Entry}", importance, 
            entry.Length > 50 ? entry[..50] + "..." : entry);
        return JsonSerializer.Serialize(new { success = true, log_id = logEntry.Id });
    }

    /// <summary>
    /// Tool 4: display_read_aloud_text - Sends atmospheric text to the Read Aloud Text Box
    /// </summary>
    private async Task<string> ExecuteDisplayReadAloudTextAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        if (!args.TryGetProperty("text", out var textElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: text" });
        }

        var text = textElement.GetString()!;
        await _stateService.SetReadAloudTextAsync(campaignId, text, ct);

        _logger.LogInformation("Set read-aloud text ({Length} chars)", text.Length);
        return JsonSerializer.Serialize(new { success = true, text_length = text.Length });
    }

    /// <summary>
    /// Tool 5: present_player_choices - Sets interactive choice buttons for players
    /// </summary>
    private async Task<string> ExecutePresentPlayerChoicesAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        if (!args.TryGetProperty("choices", out var choicesElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: choices" });
        }

        var choices = choicesElement.EnumerateArray()
            .Select(c => c.GetString()!)
            .ToList();

        await _stateService.SetPlayerChoicesAsync(campaignId, choices, ct);

        _logger.LogInformation("Set {Count} player choices", choices.Count);
        return JsonSerializer.Serialize(new { success = true, choice_count = choices.Count });
    }

    /// <summary>
    /// Tool 6: log_player_roll - Records a dice roll result to the log
    /// </summary>
    private async Task<string> ExecuteLogPlayerRollAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        if (!args.TryGetProperty("character_id", out var characterIdElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: character_id" });
        }
        
        if (!args.TryGetProperty("check_type", out var checkTypeElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: check_type" });
        }
        
        if (!args.TryGetProperty("result", out var resultElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: result" });
        }
        
        if (!args.TryGetProperty("outcome", out var outcomeElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: outcome" });
        }

        var characterId = characterIdElement.GetString()!;
        var checkType = checkTypeElement.GetString()!;
        var result = resultElement.GetInt32();
        var outcome = outcomeElement.GetString()!;

        // Log the roll as a narrative entry
        var logEntry = new LogEntry
        {
            Entry = $"[Roll] {characterId}: {checkType} = {result} ({outcome})",
            Importance = "minor"
        };
        
        await _stateService.AddLogEntryAsync(campaignId, logEntry, ct);

        _logger.LogInformation("Logged roll: {CharacterId} {CheckType} = {Result} ({Outcome})", 
            characterId, checkType, result, outcome);
        return JsonSerializer.Serialize(new 
        { 
            success = true, 
            character_id = characterId, 
            check_type = checkType, 
            result, 
            outcome,
            log_id = logEntry.Id
        });
    }

    /// <summary>
    /// Tool 7: update_scene_image - Updates the scene image URI
    /// </summary>
    private async Task<string> ExecuteUpdateSceneImageAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        if (!args.TryGetProperty("description", out var descriptionElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: description" });
        }

        var description = descriptionElement.GetString()!;

        // For MVP, use placeholder image based on description hash
        // In production, this would integrate with an image generation service
        var imageUri = $"/images/scenes/placeholder_{Math.Abs(description.GetHashCode()) % 10}.png";

        await _stateService.SetSceneImageAsync(campaignId, imageUri, ct);

        _logger.LogInformation("Set scene image: {ImageUri} (description: {Description})", 
            imageUri, description.Length > 30 ? description[..30] + "..." : description);
        return JsonSerializer.Serialize(new { success = true, image_uri = imageUri });
    }
}
