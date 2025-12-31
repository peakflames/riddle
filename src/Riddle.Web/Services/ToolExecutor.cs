using System.Text;
using System.Text.Json;
using Riddle.Web.Hubs;
using Riddle.Web.Models;

namespace Riddle.Web.Services;

/// <summary>
/// Routes LLM tool calls to appropriate handlers.
/// Implements game state manipulation and combat management tools.
/// </summary>
public class ToolExecutor : IToolExecutor
{
    private readonly IGameStateService _stateService;
    private readonly ICombatService _combatService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ToolExecutor> _logger;

    /// <summary>
    /// Mapping of property names to getter functions for safe property access on Character.
    /// </summary>
    private static readonly Dictionary<string, Func<Character, object?>> CharacterPropertyGetters = new()
    {
        // Identity
        ["Id"] = c => c.Id,
        ["Name"] = c => c.Name,
        ["Type"] = c => c.Type,
        ["Race"] = c => c.Race,
        ["Class"] = c => c.Class,
        ["Level"] = c => c.Level,
        ["Background"] = c => c.Background,
        ["Alignment"] = c => c.Alignment,
        
        // Ability Scores
        ["Strength"] = c => c.Strength,
        ["Dexterity"] = c => c.Dexterity,
        ["Constitution"] = c => c.Constitution,
        ["Intelligence"] = c => c.Intelligence,
        ["Wisdom"] = c => c.Wisdom,
        ["Charisma"] = c => c.Charisma,
        
        // Modifiers (computed)
        ["StrengthModifier"] = c => c.StrengthModifier,
        ["DexterityModifier"] = c => c.DexterityModifier,
        ["ConstitutionModifier"] = c => c.ConstitutionModifier,
        ["IntelligenceModifier"] = c => c.IntelligenceModifier,
        ["WisdomModifier"] = c => c.WisdomModifier,
        ["CharismaModifier"] = c => c.CharismaModifier,
        
        // Combat
        ["ArmorClass"] = c => c.ArmorClass,
        ["MaxHp"] = c => c.MaxHp,
        ["CurrentHp"] = c => c.CurrentHp,
        ["TemporaryHp"] = c => c.TemporaryHp,
        ["Initiative"] = c => c.Initiative,
        ["Speed"] = c => c.Speed,
        
        // Skills & Proficiencies
        ["PassivePerception"] = c => c.PassivePerception,
        ["SavingThrowProficiencies"] = c => string.Join(", ", c.SavingThrowProficiencies),
        ["SkillProficiencies"] = c => string.Join(", ", c.SkillProficiencies),
        ["ToolProficiencies"] = c => string.Join(", ", c.ToolProficiencies),
        ["Languages"] = c => string.Join(", ", c.Languages),
        
        // Spellcasting
        ["IsSpellcaster"] = c => c.IsSpellcaster,
        ["SpellcastingAbility"] = c => c.SpellcastingAbility,
        ["SpellSaveDC"] = c => c.SpellSaveDC,
        ["SpellAttackBonus"] = c => c.SpellAttackBonus,
        ["Cantrips"] = c => string.Join(", ", c.Cantrips),
        ["SpellsKnown"] = c => string.Join(", ", c.SpellsKnown),
        
        // Equipment
        ["Equipment"] = c => string.Join(", ", c.Equipment),
        ["Weapons"] = c => string.Join(", ", c.Weapons),
        ["GoldPieces"] = c => c.GoldPieces,
        
        // Roleplay
        ["PersonalityTraits"] = c => c.PersonalityTraits,
        ["Ideals"] = c => c.Ideals,
        ["Bonds"] = c => c.Bonds,
        ["Flaws"] = c => c.Flaws,
        ["Backstory"] = c => c.Backstory,
        
        // State
        ["Conditions"] = c => string.Join(", ", c.Conditions),
        ["StatusNotes"] = c => c.StatusNotes,
        ["DeathSaveSuccesses"] = c => c.DeathSaveSuccesses,
        ["DeathSaveFailures"] = c => c.DeathSaveFailures,
        ["IsStable"] = c => c.IsStable,
        ["IsDead"] = c => c.IsDead,
        
        // Player Linking
        ["PlayerId"] = c => c.PlayerId,
        ["PlayerName"] = c => c.PlayerName,
        ["IsClaimed"] = c => c.IsClaimed,
    };

    public ToolExecutor(
        IGameStateService stateService, 
        ICombatService combatService,
        INotificationService notificationService,
        ILogger<ToolExecutor> logger)
    {
        _stateService = stateService;
        _combatService = combatService;
        _notificationService = notificationService;
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
                "get_game_log" => await ExecuteGetGameLogAsync(campaignId, argumentsJson, ct),
                "get_player_roll_log" => await ExecuteGetPlayerRollLogAsync(campaignId, argumentsJson, ct),
                "get_character_property_names" => ExecuteGetCharacterPropertyNamesAsync(),
                "get_character_properties" => await ExecuteGetCharacterPropertiesAsync(campaignId, argumentsJson, ct),
                // Combat Management Tools
                "get_combat_state" => await ExecuteGetCombatStateAsync(campaignId, ct),
                "start_combat" => await ExecuteStartCombatAsync(campaignId, argumentsJson, ct),
                "end_combat" => await ExecuteEndCombatAsync(campaignId, ct),
                "advance_turn" => await ExecuteAdvanceTurnAsync(campaignId, ct),
                "add_combatant" => await ExecuteAddCombatantAsync(campaignId, argumentsJson, ct),
                "remove_combatant" => await ExecuteRemoveCombatantAsync(campaignId, argumentsJson, ct),
                // Atmospheric Tools (Player Screens)
                "broadcast_atmosphere_pulse" => await ExecuteBroadcastAtmospherePulseAsync(campaignId, argumentsJson, ct),
                "set_narrative_anchor" => await ExecuteSetNarrativeAnchorAsync(campaignId, argumentsJson, ct),
                "trigger_group_insight" => await ExecuteTriggerGroupInsightAsync(campaignId, argumentsJson, ct),
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
    /// Supports both party characters (PCs) and combat combatants (enemies/allies).
    /// </summary>
    private async Task<string> ExecuteUpdateCharacterStateAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        // Accept both character_name (preferred) and character_id (legacy) for backward compatibility
        if (!args.TryGetProperty("character_name", out var characterNameElement) && 
            !args.TryGetProperty("character_id", out characterNameElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: character_name" });
        }
        
        if (!args.TryGetProperty("key", out var keyElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: key" });
        }
        
        if (!args.TryGetProperty("value", out var valueElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: value" });
        }

        var characterNameOrId = characterNameElement.GetString()!;
        var key = keyElement.GetString()!;

        // First, try to find in party state (PCs) by ID or normalized name
        var campaign = await _stateService.GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            return JsonSerializer.Serialize(new { error = "Campaign not found" });
        }

        var character = campaign.PartyState.FirstOrDefault(c => 
            c.Id == characterNameOrId || 
            NormalizeName(c.Name) == NormalizeName(characterNameOrId));
        
        // If not found in party state, check combat combatants (enemies/allies)
        if (character == null)
        {
            var combatState = await _combatService.GetCombatStateAsync(campaignId, ct);
            if (combatState?.IsActive == true)
            {
                // Match by ID or normalized name
                var combatant = combatState.TurnOrder.FirstOrDefault(c => 
                    c.Id == characterNameOrId || 
                    NormalizeName(c.Name) == NormalizeName(characterNameOrId));
                
                if (combatant != null)
                {
                    // Handle combatant update (limited to HP and initiative for combat entities)
                    return await UpdateCombatantStateAsync(campaignId, combatant, key, valueElement, ct);
                }
            }
            
            return JsonSerializer.Serialize(new { error = $"Character '{characterNameOrId}' not found in party or combat" });
        }

        switch (key)
        {
            case "current_hp":
                var (hpSuccess, hpValue, hpError) = ParseIntValue(valueElement, "current_hp");
                if (!hpSuccess)
                {
                    return JsonSerializer.Serialize(new { error = hpError });
                }
                var previousHp = character.CurrentHp;
                character.CurrentHp = Math.Max(0, hpValue); // HP can't go below 0
                
                // Apply death save auto-rules
                await ApplyDeathSaveRulesOnHpChangeAsync(campaignId, character, previousHp, ct);
                // Note: PC HP is NOT synced to ActiveCombat.Combatants here.
                // CombatService.BuildCombatStatePayload reads PC HP from PartyState (single source of truth).
                break;
            
            case "death_save_success":
                // Increment death save successes (supports natural 20 with value "nat20")
                if (character.CurrentHp > 0)
                {
                    return JsonSerializer.Serialize(new { error = "Cannot record death save - character is not at 0 HP" });
                }
                if (character.IsDead)
                {
                    return JsonSerializer.Serialize(new { error = "Cannot record death save - character is dead" });
                }
                
                var isNat20 = valueElement.ValueKind == JsonValueKind.String && 
                              valueElement.GetString()?.Equals("nat20", StringComparison.OrdinalIgnoreCase) == true;
                
                if (isNat20)
                {
                    // Natural 20: regain 1 HP, remove Unconscious, reset death saves
                    character.CurrentHp = 1;
                    character.DeathSaveSuccesses = 0;
                    character.DeathSaveFailures = 0;
                    character.Conditions.Remove("Unconscious");
                    _logger.LogInformation("Natural 20 death save! {CharacterName} regains 1 HP and is conscious", character.Name);
                }
                else
                {
                    var (successCountParsed, successCount, successError) = ParseIntValue(valueElement, "death_save_success");
                    if (!successCountParsed) successCount = 1; // Default to 1 success
                    
                    character.DeathSaveSuccesses = Math.Min(3, character.DeathSaveSuccesses + successCount);
                    
                    // Check for stabilization (3 successes)
                    if (character.IsStable && !character.Conditions.Contains("Stable"))
                    {
                        character.Conditions.Add("Stable");
                        _logger.LogInformation("{CharacterName} is stable with 3 death save successes", character.Name);
                    }
                }
                
                // Broadcast death save update
                await NotifyDeathSaveChangedAsync(campaignId, character, ct);
                break;
            
            case "death_save_failure":
                // Increment death save failures (supports crit failure with value 2)
                if (character.CurrentHp > 0)
                {
                    return JsonSerializer.Serialize(new { error = "Cannot record death save - character is not at 0 HP" });
                }
                if (character.IsDead)
                {
                    return JsonSerializer.Serialize(new { error = "Cannot record death save - character is already dead" });
                }
                
                var (failureCountParsed, failureCount, failureError) = ParseIntValue(valueElement, "death_save_failure");
                if (!failureCountParsed) failureCount = 1; // Default to 1 failure
                
                character.DeathSaveFailures = Math.Min(3, character.DeathSaveFailures + failureCount);
                
                // Check for death (3 failures)
                if (character.IsDead)
                {
                    character.Conditions.Remove("Stable");
                    if (!character.Conditions.Contains("Dead"))
                    {
                        character.Conditions.Add("Dead");
                    }
                    _logger.LogInformation("{CharacterName} has died with 3 death save failures", character.Name);
                }
                
                // Broadcast death save update
                await NotifyDeathSaveChangedAsync(campaignId, character, ct);
                break;
            
            case "stabilize":
                // Manually stabilize character (e.g., from Medicine check or Spare the Dying)
                if (character.CurrentHp > 0)
                {
                    return JsonSerializer.Serialize(new { error = "Cannot stabilize - character is not at 0 HP" });
                }
                if (character.IsDead)
                {
                    return JsonSerializer.Serialize(new { error = "Cannot stabilize - character is dead" });
                }
                
                character.DeathSaveSuccesses = 3;
                if (!character.Conditions.Contains("Stable"))
                {
                    character.Conditions.Add("Stable");
                }
                _logger.LogInformation("{CharacterName} has been stabilized", character.Name);
                
                // Broadcast death save update
                await NotifyDeathSaveChangedAsync(campaignId, character, ct);
                break;
            case "conditions":
                character.Conditions = valueElement.EnumerateArray()
                    .Select(e => e.GetString()!)
                    .ToList();
                break;
            
            case "add_condition":
                var conditionToAdd = valueElement.GetString()!;
                if (!character.Conditions.Contains(conditionToAdd))
                {
                    character.Conditions.Add(conditionToAdd);
                    
                    // When Dead is added, remove Unconscious (you can't be unconscious if you're dead)
                    if (conditionToAdd == "Dead")
                    {
                        character.Conditions.Remove("Unconscious");
                        character.Conditions.Remove("Stable");
                    }
                }
                break;
            
            case "remove_condition":
                var conditionToRemove = valueElement.GetString()!;
                character.Conditions.Remove(conditionToRemove);
                break;
            case "status_notes":
                character.StatusNotes = valueElement.GetString();
                break;
            case "initiative":
                var (initSuccess, initValue, initError) = ParseIntValue(valueElement, "initiative");
                if (!initSuccess)
                {
                    return JsonSerializer.Serialize(new { error = initError });
                }
                character.Initiative = initValue;
                break;
            default:
                return JsonSerializer.Serialize(new { error = $"Unknown key: {key}. Valid keys: current_hp, conditions, status_notes, initiative, death_save_success, death_save_failure, stabilize" });
        }

        await _stateService.UpdateCharacterAsync(campaignId, character, ct);
        
        // Broadcast state change to all connected clients (DM + Players)
        var payload = new CharacterStatePayload(character.Id, key, valueElement.ToString());
        await _notificationService.NotifyCharacterStateUpdatedAsync(campaignId, payload, ct);
        
        _logger.LogInformation("Updated character {CharacterName} ({CharacterId}) {Key} to {Value}", 
            character.Name, character.Id, key, valueElement.ToString());
        return JsonSerializer.Serialize(new { success = true, character_name = character.Name, character_id = character.Id, key, updated = true });
    }

    /// <summary>
    /// Updates combatant state for entities in combat (enemies, allies) that aren't in the party state.
    /// Supports current_hp and initiative updates via CombatService.
    /// </summary>
    private async Task<string> UpdateCombatantStateAsync(Guid campaignId, CombatantInfo combatant, string key, JsonElement valueElement, CancellationToken ct)
    {
        switch (key)
        {
            case "current_hp":
                var (hpSuccess, newHp, hpError) = ParseIntValue(valueElement, "current_hp");
                if (!hpSuccess)
                {
                    return JsonSerializer.Serialize(new { error = hpError });
                }
                await _combatService.UpdateCombatantHpAsync(campaignId, combatant.Id, newHp, ct);
                
                // Broadcast state change to all connected clients (DM + Players)
                var hpPayload = new CharacterStatePayload(combatant.Id, key, newHp);
                await _notificationService.NotifyCharacterStateUpdatedAsync(campaignId, hpPayload, ct);
                
                _logger.LogInformation("Updated combatant {CombatantName} ({CombatantId}) {Key} to {Value}", 
                    combatant.Name, combatant.Id, key, newHp);
                return JsonSerializer.Serialize(new { success = true, character_id = combatant.Id, character_name = combatant.Name, key, updated = true });
                
            case "initiative":
                var (initSuccess, newInit, initError) = ParseIntValue(valueElement, "initiative");
                if (!initSuccess)
                {
                    return JsonSerializer.Serialize(new { error = initError });
                }
                await _combatService.SetInitiativeAsync(campaignId, combatant.Id, newInit, ct);
                
                // Broadcast state change to all connected clients (DM + Players)
                var initPayload = new CharacterStatePayload(combatant.Id, key, newInit);
                await _notificationService.NotifyCharacterStateUpdatedAsync(campaignId, initPayload, ct);
                
                _logger.LogInformation("Updated combatant {CombatantName} ({CombatantId}) {Key} to {Value}", 
                    combatant.Name, combatant.Id, key, newInit);
                return JsonSerializer.Serialize(new { success = true, character_id = combatant.Id, character_name = combatant.Name, key, updated = true });
                
            case "conditions":
            case "status_notes":
                // Combat combatants don't have these properties - they're combat-only entities
                return JsonSerializer.Serialize(new { 
                    error = $"Combat combatants don't support '{key}'. Only 'current_hp' and 'initiative' are supported for enemies/allies in combat." 
                });
                
            default:
                return JsonSerializer.Serialize(new { error = $"Unknown key: {key}. Valid keys for combatants: current_hp, initiative" });
        }
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
    /// Tool 4: display_read_aloud_text - Sends atmospheric text to the Read Aloud Text Box with optional tone/pacing
    /// </summary>
    private async Task<string> ExecuteDisplayReadAloudTextAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        if (!args.TryGetProperty("text", out var textElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: text" });
        }

        var text = textElement.GetString()!;
        var tone = args.TryGetProperty("tone", out var toneElement) ? toneElement.GetString() : null;
        var pacing = args.TryGetProperty("pacing", out var pacingElement) ? pacingElement.GetString() : null;
        
        await _stateService.SetReadAloudTextAsync(campaignId, text, tone, pacing, ct);

        _logger.LogInformation("Set read-aloud text ({Length} chars, tone: {Tone}, pacing: {Pacing})", 
            text.Length, tone ?? "none", pacing ?? "none");
        return JsonSerializer.Serialize(new { success = true, text_length = text.Length, tone, pacing });
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

        // Save to database (notifies DM via OnCampaignChanged event)
        await _stateService.SetPlayerChoicesAsync(campaignId, choices, ct);
        
        // Push to players via SignalR
        await _notificationService.NotifyPlayerChoicesAsync(campaignId, choices, ct);

        _logger.LogInformation("Set {Count} player choices and notified players", choices.Count);
        return JsonSerializer.Serialize(new { success = true, choice_count = choices.Count });
    }

    /// <summary>
    /// Tool 6: log_player_roll - Records a dice roll result to the dashboard
    /// </summary>
    private async Task<string> ExecuteLogPlayerRollAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        // Accept both character_name (preferred) and character_id (legacy) for backward compatibility
        if (!args.TryGetProperty("character_name", out var characterNameElement) && 
            !args.TryGetProperty("character_id", out characterNameElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: character_name" });
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

        var characterNameOrId = characterNameElement.GetString()!;
        var checkType = checkTypeElement.GetString()!;
        var result = resultElement.GetInt32();
        var outcome = outcomeElement.GetString()!;

        // Look up character by name or ID using normalized name matching
        var campaign = await _stateService.GetCampaignAsync(campaignId, ct);
        var character = campaign?.PartyState.FirstOrDefault(c => 
            c.Id == characterNameOrId || 
            NormalizeName(c.Name) == NormalizeName(characterNameOrId));
        
        var characterId = character?.Id ?? characterNameOrId;
        var characterName = character?.Name ?? characterNameOrId;

        // Create structured roll result
        var rollResult = new RollResult
        {
            CharacterId = characterId,
            CharacterName = characterName,
            CheckType = checkType,
            Result = result,
            Outcome = outcome
        };
        
        // Store in recent rolls (triggers in-process notification for same circuit)
        await _stateService.AddRollResultAsync(campaignId, rollResult, ct);
        
        // Broadcast via SignalR to all connected clients (DM + Players across circuits)
        await _notificationService.NotifyPlayerRollAsync(campaignId, rollResult, ct);
        
        // Also log as a narrative entry for history
        var logEntry = new LogEntry
        {
            Entry = $"[Roll] {characterName}: {checkType} = {result} ({outcome})",
            Importance = "minor"
        };
        await _stateService.AddLogEntryAsync(campaignId, logEntry, ct);

        _logger.LogInformation("Logged roll: {CharacterName} {CheckType} = {Result} ({Outcome})", 
            characterName, checkType, result, outcome);
        return JsonSerializer.Serialize(new 
        { 
            success = true, 
            character_id = characterId, 
            character_name = characterName,
            check_type = checkType, 
            result, 
            outcome,
            roll_id = rollResult.Id
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

    /// <summary>
    /// Tool 8: get_game_log - Retrieves recent narrative log entries in markdown format
    /// </summary>
    private async Task<string> ExecuteGetGameLogAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        var limit = args.TryGetProperty("limit", out var limitElement) ? limitElement.GetInt32() : 50;
        limit = Math.Min(Math.Max(1, limit), 100); // Clamp between 1-100

        var campaign = await _stateService.GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            return JsonSerializer.Serialize(new { error = "Campaign not found" });
        }

        var entries = campaign.NarrativeLog
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToList();

        if (!entries.Any())
        {
            return "# Game Log\n\n*No narrative events recorded yet.*";
        }

        var sb = new StringBuilder();
        sb.AppendLine("# Game Log");
        sb.AppendLine();
        sb.AppendLine("| Time | Importance | Event |");
        sb.AppendLine("|------|------------|-------|");

        foreach (var entry in entries)
        {
            var time = entry.Timestamp.ToString("HH:mm:ss");
            var importance = entry.Importance ?? "standard";
            var text = entry.Entry.Replace("|", "\\|").Replace("\n", " "); // Escape pipes and newlines
            sb.AppendLine($"| {time} | {importance} | {text} |");
        }

        _logger.LogInformation("Retrieved {Count} game log entries", entries.Count);
        return sb.ToString();
    }

    /// <summary>
    /// Tool 9: get_player_roll_log - Retrieves recent dice roll results in markdown format
    /// </summary>
    private async Task<string> ExecuteGetPlayerRollLogAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        var limit = args.TryGetProperty("limit", out var limitElement) ? limitElement.GetInt32() : 50;
        limit = Math.Min(Math.Max(1, limit), 100); // Clamp between 1-100

        var campaign = await _stateService.GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            return JsonSerializer.Serialize(new { error = "Campaign not found" });
        }

        var rolls = campaign.RecentRolls.Take(limit).ToList();

        if (!rolls.Any())
        {
            return "# Player Roll Log\n\n*No dice rolls recorded yet.*";
        }

        var sb = new StringBuilder();
        sb.AppendLine("# Player Roll Log");
        sb.AppendLine();
        sb.AppendLine("| Time | Character | Check Type | Result | Outcome |");
        sb.AppendLine("|------|-----------|------------|--------|---------|");

        foreach (var roll in rolls)
        {
            var time = roll.Timestamp.ToString("HH:mm:ss");
            var character = roll.CharacterName.Replace("|", "\\|");
            var checkType = roll.CheckType.Replace("|", "\\|");
            sb.AppendLine($"| {time} | {character} | {checkType} | {roll.Result} | {roll.Outcome} |");
        }

        _logger.LogInformation("Retrieved {Count} roll log entries", rolls.Count);
        return sb.ToString();
    }

    /// <summary>
    /// Tool 10: get_character_property_names - Returns categorized list of queryable property names
    /// </summary>
    private string ExecuteGetCharacterPropertyNamesAsync()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Character Property Names");
        sb.AppendLine();
        sb.AppendLine("Use these property names with `get_character_properties` tool.");
        sb.AppendLine();
        
        sb.AppendLine("## Identity");
        sb.AppendLine("- `Id` (string) - Unique character identifier");
        sb.AppendLine("- `Name` (string) - Character name");
        sb.AppendLine("- `Type` (string) - \"PC\" or \"NPC\"");
        sb.AppendLine("- `Race` (string) - Character race");
        sb.AppendLine("- `Class` (string) - Character class");
        sb.AppendLine("- `Level` (int) - Character level");
        sb.AppendLine("- `Background` (string) - Character background");
        sb.AppendLine("- `Alignment` (string) - Character alignment");
        sb.AppendLine();
        
        sb.AppendLine("## Ability Scores");
        sb.AppendLine("- `Strength`, `Dexterity`, `Constitution`, `Intelligence`, `Wisdom`, `Charisma` (int)");
        sb.AppendLine("- `StrengthModifier`, `DexterityModifier`, `ConstitutionModifier` (int) - Computed");
        sb.AppendLine("- `IntelligenceModifier`, `WisdomModifier`, `CharismaModifier` (int) - Computed");
        sb.AppendLine();
        
        sb.AppendLine("## Combat");
        sb.AppendLine("- `ArmorClass` (int) - AC value");
        sb.AppendLine("- `MaxHp` (int) - Maximum hit points");
        sb.AppendLine("- `CurrentHp` (int) - Current hit points");
        sb.AppendLine("- `TemporaryHp` (int) - Temporary HP");
        sb.AppendLine("- `Initiative` (int) - Initiative bonus");
        sb.AppendLine("- `Speed` (int) - Movement speed in feet");
        sb.AppendLine();
        
        sb.AppendLine("## Skills & Proficiencies");
        sb.AppendLine("- `PassivePerception` (int) - Passive perception score");
        sb.AppendLine("- `SavingThrowProficiencies` (string) - Comma-separated list");
        sb.AppendLine("- `SkillProficiencies` (string) - Comma-separated list");
        sb.AppendLine("- `ToolProficiencies` (string) - Comma-separated list");
        sb.AppendLine("- `Languages` (string) - Comma-separated list");
        sb.AppendLine();
        
        sb.AppendLine("## Spellcasting");
        sb.AppendLine("- `IsSpellcaster` (bool) - Whether character can cast spells");
        sb.AppendLine("- `SpellcastingAbility` (string) - INT, WIS, or CHA");
        sb.AppendLine("- `SpellSaveDC` (int) - Spell save DC");
        sb.AppendLine("- `SpellAttackBonus` (int) - Spell attack modifier");
        sb.AppendLine("- `Cantrips` (string) - Comma-separated list");
        sb.AppendLine("- `SpellsKnown` (string) - Comma-separated list");
        sb.AppendLine();
        
        sb.AppendLine("## Equipment");
        sb.AppendLine("- `Equipment` (string) - Comma-separated list of items");
        sb.AppendLine("- `Weapons` (string) - Comma-separated list");
        sb.AppendLine("- `GoldPieces` (int) - Gold pieces");
        sb.AppendLine();
        
        sb.AppendLine("## Roleplay");
        sb.AppendLine("- `PersonalityTraits` (string)");
        sb.AppendLine("- `Ideals` (string)");
        sb.AppendLine("- `Bonds` (string)");
        sb.AppendLine("- `Flaws` (string)");
        sb.AppendLine("- `Backstory` (string)");
        sb.AppendLine();
        
        sb.AppendLine("## State");
        sb.AppendLine("- `Conditions` (string) - Comma-separated active conditions");
        sb.AppendLine("- `StatusNotes` (string) - DM notes");
        sb.AppendLine("- `DeathSaveSuccesses` (int) - 0-3");
        sb.AppendLine("- `DeathSaveFailures` (int) - 0-3");
        sb.AppendLine("- `IsStable` (bool) - True if 3 successes and at 0 HP (computed)");
        sb.AppendLine("- `IsDead` (bool) - True if 3 failures (computed)");
        sb.AppendLine();
        
        sb.AppendLine("## Player Linking");
        sb.AppendLine("- `PlayerId` (string) - Linked player user ID");
        sb.AppendLine("- `PlayerName` (string) - Linked player display name");
        sb.AppendLine("- `IsClaimed` (bool) - Whether claimed by a player");

        _logger.LogInformation("Retrieved character property names");
        return sb.ToString();
    }

    /// <summary>
    /// Tool 11: get_character_properties - Retrieves specific properties for characters
    /// </summary>
    private async Task<string> ExecuteGetCharacterPropertiesAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        // Accept both character_names (preferred) and character_ids (legacy) for backward compatibility
        if (!args.TryGetProperty("character_names", out var characterNamesElement) && 
            !args.TryGetProperty("character_ids", out characterNamesElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: character_names" });
        }
        
        if (!args.TryGetProperty("prop_names", out var propNamesElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: prop_names" });
        }

        var characterNamesOrIds = characterNamesElement.EnumerateArray()
            .Select(e => e.GetString()!)
            .ToList();
        
        var propNames = propNamesElement.EnumerateArray()
            .Select(e => e.GetString()!)
            .ToList();

        if (!characterNamesOrIds.Any())
        {
            return JsonSerializer.Serialize(new { error = "character_names array is empty" });
        }

        if (!propNames.Any())
        {
            return JsonSerializer.Serialize(new { error = "prop_names array is empty" });
        }

        // Validate property names
        var invalidProps = propNames.Where(p => !CharacterPropertyGetters.ContainsKey(p)).ToList();
        if (invalidProps.Any())
        {
            return JsonSerializer.Serialize(new { 
                error = $"Invalid property names: {string.Join(", ", invalidProps)}. Use get_character_property_names for valid names." 
            });
        }

        var campaign = await _stateService.GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            return JsonSerializer.Serialize(new { error = "Campaign not found" });
        }

        var partyState = campaign.PartyState;
        
        // Build markdown table
        var sb = new StringBuilder();
        sb.AppendLine("# Character Properties");
        sb.AppendLine();
        
        // Header row
        sb.Append("| Character |");
        foreach (var prop in propNames)
        {
            sb.Append($" {prop} |");
        }
        sb.AppendLine();
        
        // Separator row
        sb.Append("|-----------|");
        foreach (var _ in propNames)
        {
            sb.Append("--------|");
        }
        sb.AppendLine();
        
        // Data rows - match by ID or normalized name
        foreach (var charNameOrId in characterNamesOrIds)
        {
            var character = partyState.FirstOrDefault(c => 
                c.Id == charNameOrId || 
                NormalizeName(c.Name) == NormalizeName(charNameOrId));
            
            if (character == null)
            {
                sb.Append($"| {charNameOrId} (not found) |");
                foreach (var _ in propNames)
                {
                    sb.Append(" N/A |");
                }
                sb.AppendLine();
                continue;
            }
            
            sb.Append($"| {character.Name} |");
            foreach (var prop in propNames)
            {
                var getter = CharacterPropertyGetters[prop];
                var value = getter(character);
                var displayValue = value?.ToString() ?? "";
                // Escape pipes and truncate long values
                displayValue = displayValue.Replace("|", "\\|");
                if (displayValue.Length > 50)
                {
                    displayValue = displayValue[..47] + "...";
                }
                sb.Append($" {displayValue} |");
            }
            sb.AppendLine();
        }

        _logger.LogInformation("Retrieved {PropCount} properties for {CharCount} characters", 
            propNames.Count, characterNamesOrIds.Count);
        return sb.ToString();
    }

    // ==================== Combat Management Tools ====================

    /// <summary>
    /// Tool: get_combat_state - Returns current combat state information
    /// </summary>
    private async Task<string> ExecuteGetCombatStateAsync(Guid campaignId, CancellationToken ct)
    {
        var combatState = await _combatService.GetCombatStateAsync(campaignId, ct);
        
        if (combatState == null || !combatState.IsActive)
        {
            return "# Combat State\n\n**Status:** No active combat\n\nTo start combat, use the `start_combat` tool with enemies and PC initiatives.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("# Combat State");
        sb.AppendLine();
        sb.AppendLine($"**Status:** Active");
        sb.AppendLine($"**Round:** {combatState.RoundNumber}");
        sb.AppendLine($"**Current Turn Index:** {combatState.CurrentTurnIndex}");
        sb.AppendLine();
        
        // Turn Order Table
        sb.AppendLine("## Turn Order");
        sb.AppendLine();
        sb.AppendLine("| # | Name | Type | Initiative | HP | Status |");
        sb.AppendLine("|---|------|------|------------|----|---------");
        
        for (int i = 0; i < combatState.TurnOrder.Count; i++)
        {
            var combatant = combatState.TurnOrder[i];
            var turnIndicator = i == combatState.CurrentTurnIndex ? "→" : (i + 1).ToString();
            var hpDisplay = $"{combatant.CurrentHp}/{combatant.MaxHp}";
            var status = combatant.IsDefeated ? "💀 Defeated" : 
                         combatant.IsSurprised ? "⚠️ Surprised" : "✓ Active";
            
            sb.AppendLine($"| {turnIndicator} | {combatant.Name} | {combatant.Type} | {combatant.Initiative} | {hpDisplay} | {status} |");
        }
        sb.AppendLine();
        
        // Current Turn Info
        if (combatState.CurrentTurnIndex >= 0 && combatState.CurrentTurnIndex < combatState.TurnOrder.Count)
        {
            var currentCombatant = combatState.TurnOrder[combatState.CurrentTurnIndex];
            sb.AppendLine($"**Current Turn:** {currentCombatant.Name} ({currentCombatant.Type})");
        }
        
        // Summary Stats
        var pcCount = combatState.TurnOrder.Count(c => c.Type == "PC");
        var enemyCount = combatState.TurnOrder.Count(c => c.Type == "Enemy");
        var defeatedCount = combatState.TurnOrder.Count(c => c.IsDefeated);
        
        sb.AppendLine();
        sb.AppendLine($"**Summary:** {pcCount} PCs, {enemyCount} enemies, {defeatedCount} defeated");

        _logger.LogInformation("Retrieved combat state: Round {Round}, {Count} combatants", 
            combatState.RoundNumber, combatState.TurnOrder.Count);
        return sb.ToString();
    }

    /// <summary>
    /// Tool: start_combat - Initiates combat encounter with enemies and PC initiatives
    /// </summary>
    private async Task<string> ExecuteStartCombatAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        // Check if combat is already active
        var existingCombat = await _combatService.GetCombatStateAsync(campaignId, ct);
        if (existingCombat?.IsActive == true)
        {
            return "Error: Combat already active. Call end_combat first to start new combat.";
        }

        if (!args.TryGetProperty("enemies", out var enemiesElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: enemies" });
        }
        
        if (!args.TryGetProperty("pc_initiatives", out var pcInitiativesElement))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: pc_initiatives" });
        }

        var combatants = new List<CombatantInfo>();
        var warnings = new List<string>();

        // Get party state for PC data
        var campaign = await _stateService.GetCampaignAsync(campaignId, ct);
        if (campaign == null)
        {
            return JsonSerializer.Serialize(new { error = "Campaign not found" });
        }

        // Process PC initiatives
        foreach (var pcProp in pcInitiativesElement.EnumerateObject())
        {
            var characterIdOrName = pcProp.Name;
            var initiative = pcProp.Value.GetInt32();
            
            // Clamp initiative to valid range (1-30)
            var clampedInitiative = Math.Clamp(initiative, 1, 30);
            if (clampedInitiative != initiative)
            {
                warnings.Add($"Initiative for {characterIdOrName} clamped from {initiative} to {clampedInitiative}");
            }

            // Match by ID (GUID) or by Name (case-insensitive, normalize separators)
            // LLMs often use underscores (Elara_Moonshadow) while stored names use spaces (Elara Moonshadow)
            var character = campaign.PartyState.FirstOrDefault(c => 
                c.Id == characterIdOrName || 
                NormalizeName(c.Name) == NormalizeName(characterIdOrName));
            if (character == null)
            {
                warnings.Add($"PC '{characterIdOrName}' not found in party state (searched by ID and name), skipping");
                continue;
            }

            combatants.Add(new CombatantInfo(
                Id: character.Id,
                Name: character.Name,
                Type: "PC",
                Initiative: clampedInitiative,
                CurrentHp: character.CurrentHp,
                MaxHp: character.MaxHp,
                IsDefeated: character.CurrentHp <= 0,
                IsSurprised: false
            ));
        }

        // Process enemies
        var enemyIndex = 0;
        foreach (var enemy in enemiesElement.EnumerateArray())
        {
            enemyIndex++;
            
            var name = enemy.TryGetProperty("name", out var nameEl) 
                ? nameEl.GetString()! 
                : $"Enemy {enemyIndex}";
            
            var initiative = enemy.TryGetProperty("initiative", out var initEl) 
                ? initEl.GetInt32() 
                : 10;
            
            var maxHp = enemy.TryGetProperty("max_hp", out var maxHpEl) 
                ? maxHpEl.GetInt32() 
                : 10;
            
            var currentHp = enemy.TryGetProperty("current_hp", out var curHpEl) 
                ? curHpEl.GetInt32() 
                : maxHp;
            
            var ac = enemy.TryGetProperty("ac", out var acEl) 
                ? acEl.GetInt32() 
                : 10;

            // Clamp initiative
            var clampedInit = Math.Clamp(initiative, 1, 30);
            if (clampedInit != initiative)
            {
                warnings.Add($"Initiative for {name} clamped from {initiative} to {clampedInit}");
            }

            combatants.Add(new CombatantInfo(
                Id: $"enemy_{Guid.NewGuid():N}",
                Name: name,
                Type: "Enemy",
                Initiative: clampedInit,
                CurrentHp: currentHp,
                MaxHp: maxHp,
                IsDefeated: currentHp <= 0,
                IsSurprised: false
            ));
        }

        if (combatants.Count == 0)
        {
            return "Error: No valid combatants provided. Check that PC IDs exist in party state.";
        }

        // Start combat
        var combatState = await _combatService.StartCombatAsync(campaignId, combatants, ct);

        // Log to narrative log
        var pcCount = combatants.Count(c => c.Type == "PC");
        var enemyCount = combatants.Count(c => c.Type == "Enemy");
        var logEntry = new LogEntry
        {
            Entry = $"[Combat] Combat initiated: {pcCount} PCs vs {enemyCount} enemies",
            Importance = "standard"
        };
        await _stateService.AddLogEntryAsync(campaignId, logEntry, ct);

        // Build response
        var combatantSummary = string.Join(", ", combatState.TurnOrder.Select(c => $"{c.Name} ({c.Type})"));
        var result = $"Combat started with {combatants.Count} combatants: {combatantSummary}. Turn order established.";
        
        if (warnings.Count > 0)
        {
            result += $"\n\nWarnings:\n{string.Join("\n", warnings.Select(w => $"• {w}"))}";
        }

        _logger.LogInformation("Combat started: {Count} combatants", combatants.Count);
        return result;
    }

    /// <summary>
    /// Tool: end_combat - Ends the current combat encounter
    /// </summary>
    private async Task<string> ExecuteEndCombatAsync(Guid campaignId, CancellationToken ct)
    {
        var combatState = await _combatService.GetCombatStateAsync(campaignId, ct);
        if (combatState?.IsActive != true)
        {
            return "Error: No active combat to end.";
        }

        var roundCount = combatState.RoundNumber;
        
        await _combatService.EndCombatAsync(campaignId, ct);

        // Log to narrative log
        var logEntry = new LogEntry
        {
            Entry = $"[Combat] Combat ended after {roundCount} round(s)",
            Importance = "standard"
        };
        await _stateService.AddLogEntryAsync(campaignId, logEntry, ct);

        _logger.LogInformation("Combat ended after {Rounds} rounds", roundCount);
        return $"Combat ended after {roundCount} round(s). Combat Tracker cleared.";
    }

    /// <summary>
    /// Tool: advance_turn - Advances to the next combatant's turn
    /// </summary>
    private async Task<string> ExecuteAdvanceTurnAsync(Guid campaignId, CancellationToken ct)
    {
        var combatState = await _combatService.GetCombatStateAsync(campaignId, ct);
        if (combatState?.IsActive != true)
        {
            return "Error: No active combat. Call start_combat first.";
        }

        var previousRound = combatState.RoundNumber;
        var (newTurnIndex, currentCombatantId) = await _combatService.AdvanceTurnAsync(campaignId, ct);

        // Get updated state for round number
        var updatedState = await _combatService.GetCombatStateAsync(campaignId, ct);
        var currentRound = updatedState?.RoundNumber ?? previousRound;
        
        var currentCombatant = updatedState?.TurnOrder
            .FirstOrDefault(c => c.Id == currentCombatantId);
        var combatantName = currentCombatant?.Name ?? currentCombatantId;

        // Log to narrative log
        var roundChanged = currentRound > previousRound;
        var logEntry = new LogEntry
        {
            Entry = roundChanged 
                ? $"[Combat] Round {currentRound} begins. {combatantName}'s turn"
                : $"[Combat] Turn advanced: {combatantName}'s turn",
            Importance = "minor"
        };
        await _stateService.AddLogEntryAsync(campaignId, logEntry, ct);

        _logger.LogInformation("Turn advanced to {Combatant} (Round {Round})", combatantName, currentRound);
        return $"Turn advanced: {combatantName}'s turn (Round {currentRound})";
    }

    /// <summary>
    /// Tool: add_combatant - Adds a combatant to active combat
    /// </summary>
    private async Task<string> ExecuteAddCombatantAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var combatState = await _combatService.GetCombatStateAsync(campaignId, ct);
        if (combatState?.IsActive != true)
        {
            return "Error: No active combat. Call start_combat first.";
        }

        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        if (!args.TryGetProperty("name", out var nameEl))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: name" });
        }
        
        if (!args.TryGetProperty("initiative", out var initEl))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: initiative" });
        }
        
        if (!args.TryGetProperty("max_hp", out var maxHpEl))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: max_hp" });
        }
        
        if (!args.TryGetProperty("is_enemy", out var isEnemyEl))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: is_enemy" });
        }

        var name = nameEl.GetString()!;
        var initiative = initEl.GetInt32();
        var maxHp = maxHpEl.GetInt32();
        var isEnemy = isEnemyEl.GetBoolean();
        
        var currentHp = args.TryGetProperty("current_hp", out var curHpEl) 
            ? curHpEl.GetInt32() 
            : maxHp;
        
        var ac = args.TryGetProperty("ac", out var acEl) 
            ? acEl.GetInt32() 
            : 10;

        // Clamp initiative
        var clampedInit = Math.Clamp(initiative, 1, 30);
        var initWarning = clampedInit != initiative 
            ? $" (initiative clamped from {initiative} to {clampedInit})" 
            : "";

        var combatant = new CombatantInfo(
            Id: $"{(isEnemy ? "enemy" : "ally")}_{Guid.NewGuid():N}",
            Name: name,
            Type: isEnemy ? "Enemy" : "Ally",
            Initiative: clampedInit,
            CurrentHp: currentHp,
            MaxHp: maxHp,
            IsDefeated: currentHp <= 0,
            IsSurprised: false
        );

        await _combatService.AddCombatantAsync(campaignId, combatant, ct);

        // Log to narrative log
        var logEntry = new LogEntry
        {
            Entry = $"[Combat] {name} joined combat (initiative: {clampedInit})",
            Importance = "minor"
        };
        await _stateService.AddLogEntryAsync(campaignId, logEntry, ct);

        _logger.LogInformation("Added combatant {Name} to combat", name);
        return $"{name} joined combat at initiative {clampedInit}{initWarning}";
    }

    /// <summary>
    /// Tool: remove_combatant - Removes a combatant from combat
    /// </summary>
    private async Task<string> ExecuteRemoveCombatantAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var combatState = await _combatService.GetCombatStateAsync(campaignId, ct);
        if (combatState?.IsActive != true)
        {
            return "Error: No active combat.";
        }

        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        if (!args.TryGetProperty("combatant_name", out var nameEl))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: combatant_name" });
        }

        var combatantName = nameEl.GetString()!;
        var reason = args.TryGetProperty("reason", out var reasonEl) 
            ? reasonEl.GetString() ?? "removed" 
            : "removed";

        // Find combatant by name or ID
        var combatant = combatState.TurnOrder.FirstOrDefault(c => 
            c.Name.Equals(combatantName, StringComparison.OrdinalIgnoreCase) ||
            c.Id.Equals(combatantName, StringComparison.OrdinalIgnoreCase));

        if (combatant == null)
        {
            return $"Error: Combatant '{combatantName}' not found in combat.";
        }

        await _combatService.RemoveCombatantAsync(campaignId, combatant.Id, ct);

        // Log to narrative log
        var logEntry = new LogEntry
        {
            Entry = $"[Combat] {combatant.Name} {reason}",
            Importance = "minor"
        };
        await _stateService.AddLogEntryAsync(campaignId, logEntry, ct);

        _logger.LogInformation("Removed combatant {Name} from combat ({Reason})", combatant.Name, reason);
        return $"{combatant.Name} {reason} from combat.";
    }

    // ==================== Atmospheric Tools (Player Screens) ====================

    /// <summary>
    /// Tool: broadcast_atmosphere_pulse - Sends fleeting sensory text to player screens
    /// </summary>
    private async Task<string> ExecuteBroadcastAtmospherePulseAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        if (!args.TryGetProperty("text", out var textEl))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: text" });
        }

        var text = textEl.GetString()!;
        var intensity = args.TryGetProperty("intensity", out var intensityEl) ? intensityEl.GetString() : null;
        var sensoryType = args.TryGetProperty("sensory_type", out var sensoryEl) ? sensoryEl.GetString() : null;

        var payload = new AtmospherePulsePayload(text, intensity, sensoryType);
        await _notificationService.NotifyAtmospherePulseAsync(campaignId, payload, ct);

        _logger.LogInformation("Broadcast atmosphere pulse: {Text} (intensity: {Intensity}, type: {SensoryType})",
            text.Length > 50 ? text[..50] + "..." : text, intensity ?? "default", sensoryType ?? "general");
        
        return JsonSerializer.Serialize(new { success = true, message = "Atmosphere pulse sent to player screens" });
    }

    /// <summary>
    /// Tool: set_narrative_anchor - Updates persistent mood banner on player screens
    /// </summary>
    private async Task<string> ExecuteSetNarrativeAnchorAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        if (!args.TryGetProperty("short_text", out var textEl))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: short_text" });
        }

        var shortText = textEl.GetString()!;
        var moodCategory = args.TryGetProperty("mood_category", out var moodEl) ? moodEl.GetString() : null;

        var payload = new NarrativeAnchorPayload(shortText, moodCategory);
        await _notificationService.NotifyNarrativeAnchorAsync(campaignId, payload, ct);

        _logger.LogInformation("Set narrative anchor: {ShortText} (mood: {MoodCategory})",
            shortText, moodCategory ?? "neutral");
        
        return JsonSerializer.Serialize(new { success = true, message = "Narrative anchor updated on player screens" });
    }

    /// <summary>
    /// Tool: trigger_group_insight - Flashes discovery notification on player screens
    /// </summary>
    private async Task<string> ExecuteTriggerGroupInsightAsync(Guid campaignId, string argumentsJson, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        
        if (!args.TryGetProperty("text", out var textEl))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: text" });
        }
        
        if (!args.TryGetProperty("relevant_skill", out var skillEl))
        {
            return JsonSerializer.Serialize(new { error = "Missing required parameter: relevant_skill" });
        }

        var text = textEl.GetString()!;
        var relevantSkill = skillEl.GetString()!;
        var highlightEffect = args.TryGetProperty("highlight_effect", out var highlightEl) && highlightEl.GetBoolean();

        var payload = new GroupInsightPayload(text, relevantSkill, highlightEffect);
        await _notificationService.NotifyGroupInsightAsync(campaignId, payload, ct);

        _logger.LogInformation("Triggered group insight: {Text} (skill: {RelevantSkill}, highlight: {Highlight})",
            text.Length > 50 ? text[..50] + "..." : text, relevantSkill, highlightEffect);
        
        return JsonSerializer.Serialize(new { success = true, message = "Group insight sent to player screens" });
    }

    // ==================== Helper Methods ====================

    /// <summary>
    /// Applies automatic death save rules when HP changes.
    /// - HP drops to 0: Add "Unconscious" condition, reset death saves
    /// - HP rises from 0: Remove "Unconscious"/"Stable", reset death saves
    /// </summary>
    private async Task ApplyDeathSaveRulesOnHpChangeAsync(Guid campaignId, Character character, int previousHp, CancellationToken ct)
    {
        var hpDroppedToZero = previousHp > 0 && character.CurrentHp <= 0;
        var hpRoseFromZero = previousHp <= 0 && character.CurrentHp > 0;
        
        if (hpDroppedToZero)
        {
            // Character dropped to 0 HP - apply Unconscious, reset death saves
            if (!character.Conditions.Contains("Unconscious"))
            {
                character.Conditions.Add("Unconscious");
            }
            character.DeathSaveSuccesses = 0;
            character.DeathSaveFailures = 0;
            
            _logger.LogInformation("{CharacterName} dropped to 0 HP - now Unconscious, death saves reset", character.Name);
            
            // Broadcast death save state
            await NotifyDeathSaveChangedAsync(campaignId, character, ct);
        }
        else if (hpRoseFromZero)
        {
            // Character healed from 0 HP - remove dying/stable conditions, reset death saves
            character.Conditions.Remove("Unconscious");
            character.Conditions.Remove("Stable");
            character.DeathSaveSuccesses = 0;
            character.DeathSaveFailures = 0;
            
            _logger.LogInformation("{CharacterName} healed from 0 HP - Unconscious removed, death saves reset", character.Name);
            
            // Broadcast death save state
            await NotifyDeathSaveChangedAsync(campaignId, character, ct);
        }
    }

    /// <summary>
    /// Broadcasts death save state change via SignalR.
    /// </summary>
    private async Task NotifyDeathSaveChangedAsync(Guid campaignId, Character character, CancellationToken ct)
    {
        var payload = new DeathSavePayload(
            CharacterId: character.Id,
            CharacterName: character.Name,
            DeathSaveSuccesses: character.DeathSaveSuccesses,
            DeathSaveFailures: character.DeathSaveFailures,
            IsStable: character.IsStable,
            IsDead: character.IsDead
        );
        
        await _notificationService.NotifyDeathSaveUpdatedAsync(campaignId, payload, ct);
    }

    /// <summary>
    /// Normalizes character names for comparison by converting to lowercase and replacing
    /// common separators (underscores, hyphens) with spaces.
    /// LLMs often use underscores (Elara_Moonshadow) while stored names use spaces (Elara Moonshadow).
    /// </summary>
    private static string NormalizeName(string name)
    {
        return name
            .ToLowerInvariant()
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Trim();
    }

    /// <summary>
    /// Parses an integer from a JsonElement, handling both numeric and string representations.
    /// LLMs often send integers as quoted strings (e.g., "15" instead of 15).
    /// </summary>
    private static (bool success, int value, string? error) ParseIntValue(JsonElement element, string fieldName)
    {
        // Try direct integer first
        if (element.ValueKind == JsonValueKind.Number)
        {
            return (true, element.GetInt32(), null);
        }
        
        // Try parsing from string
        if (element.ValueKind == JsonValueKind.String)
        {
            var str = element.GetString();
            if (int.TryParse(str, out var parsed))
            {
                return (true, parsed, null);
            }
            return (false, 0, $"Invalid {fieldName}: '{str}' is not a valid integer");
        }
        
        return (false, 0, $"Invalid {fieldName}: expected integer or string, got {element.ValueKind}");
    }
}
