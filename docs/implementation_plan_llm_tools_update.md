# Implementation Plan: LLM Service Tools Update

Enhance LLM tools with read-aloud metadata and add four new query tools for game logs and character data.

[Overview]
This implementation adds tone/pacing arguments to `display_read_aloud_text` and introduces four new read-only query tools (`get_game_log`, `get_player_roll_log`, `get_character_property_names`, `get_character_properties`) that allow the LLM to retrieve game history and character data in markdown format.

The changes span the LLM service layer (`RiddleLlmService.cs` and `ToolExecutor.cs`), the model layer (`CampaignInstance.cs`), and the UI layer (`Campaign.razor`) to properly display tone/pacing metadata alongside read-aloud text.

**Scope:**
- 1 modified tool definition (`display_read_aloud_text`)
- 4 new tool definitions
- 5 tool executor handler methods (1 modified, 4 new)
- 1 model update (add tone/pacing properties)
- 1 UI update (display tone/pacing badges)
- 1 system prompt update (add get_game_log to startup sequence)
- No database migration required (string fields serialize to existing JSON)

**Key Design Decisions:**
- All new tools return markdown strings for LLM-friendly consumption
- Character property reflection uses explicit property mapping (not runtime reflection) for safety and predictability
- Tone/pacing on read-aloud are optional parameters that default to neutral values
- Tone and pacing are displayed as visual badges/indicators above the read-aloud text
- System prompt instructs LLM to call both `get_game_state()` and `get_game_log()` on conversation start
- The `get_character_properties` tool accepts arrays for batch queries across multiple characters and properties

[Types]
No new types required; modify existing CampaignInstance model.

**Modified Types:**

`CampaignInstance` - Add two new string properties:
- `CurrentReadAloudTone` (string?, default null) - Tone hint for read-aloud text (e.g., "ominous", "cheerful", "urgent")
- `CurrentReadAloudPacing` (string?, default null) - Pacing hint for read-aloud text (e.g., "slow", "normal", "fast")

**Existing Types Used:**
- `LogEntry` - has `Id`, `Timestamp`, `Entry`, `Importance` fields
- `RollResult` - has `Id`, `CharacterId`, `CharacterName`, `CheckType`, `Result`, `Outcome`, `Timestamp`
- `Character` - comprehensive model with 50+ properties across identity, ability scores, combat stats, skills, spellcasting, equipment, roleplay, and state categories

**Implicit Return Types:**
- All new tools return `string` (markdown formatted)
- The modified `display_read_aloud_text` tool continues to return success JSON

[Files]
Modify existing service and UI files.

**Modified Files:**

1. `src/Riddle.Web/Services/RiddleLlmService.cs`
   - **System Prompt Update**: Modify `BuildSystemPrompt()` to instruct LLM to call `get_game_log()` in addition to `get_game_state()` for context recovery
   - Modify `display_read_aloud_text` tool definition to add `tone` and `pacing` optional parameters
   - Add `get_game_log` tool definition with `limit` optional parameter (default 50)
   - Add `get_player_roll_log` tool definition with `limit` optional parameter (default 50)
   - Add `get_character_property_names` tool definition (no parameters)
   - Add `get_character_properties` tool definition with `character_ids` and `prop_names` array parameters

2. `src/Riddle.Web/Services/ToolExecutor.cs`
   - Modify `ExecuteDisplayReadAloudTextAsync` to parse and store `tone` and `pacing` in campaign
   - Add `ExecuteGetGameLogAsync` handler method
   - Add `ExecuteGetPlayerRollLogAsync` handler method
   - Add `ExecuteGetCharacterPropertyNamesAsync` handler method
   - Add `ExecuteGetCharacterPropertiesAsync` handler method
   - Update switch statement in `ExecuteAsync` to route new tool names

3. `src/Riddle.Web/Services/IGameStateService.cs`
   - Add `SetReadAloudTextAsync(Guid campaignId, string text, string? tone, string? pacing, CancellationToken ct)` overload

4. `src/Riddle.Web/Services/GameStateService.cs`
   - Implement updated `SetReadAloudTextAsync` with tone/pacing parameters
   - Update `CampaignChangedEventArgs` notification to include tone/pacing

5. `src/Riddle.Web/Models/CampaignInstance.cs`
   - Add `CurrentReadAloudTone` string property (MaxLength 50)
   - Add `CurrentReadAloudPacing` string property (MaxLength 50)

6. `src/Riddle.Web/Components/Pages/DM/Campaign.razor`
   - Update Read-Aloud Text card to display tone/pacing badges above the text
   - Add `HandleCampaignChanged` cases for `CurrentReadAloudTone` and `CurrentReadAloudPacing`
   - Add helper methods for tone/pacing badge styling

[Functions]
Modify functions and add new functions in ToolExecutor and GameStateService.

**Modified Functions:**

1. `RiddleLlmService.BuildSystemPrompt(CampaignInstance campaign) -> string`
   - Current: Instructs LLM to call `get_game_state()` first
   - Change: Update `<<system_constraints>>` section to instruct calling both `get_game_state()` and `get_game_log()` for full context recovery
   - Example new instruction: "Your first tool calls MUST be `get_game_state()` followed by `get_game_log()` to understand the current reality and recent events."

2. `ToolExecutor.ExecuteDisplayReadAloudTextAsync(Guid campaignId, string argumentsJson, CancellationToken ct)`
   - Current: Parses `text` parameter only
   - Change: Also parse optional `tone` (string, default null) and `pacing` (string, default null)
   - Call updated `SetReadAloudTextAsync` with tone/pacing
   - Return JSON includes tone and pacing in response

3. `GameStateService.SetReadAloudTextAsync` - Add overload or update signature:
   - Current: `SetReadAloudTextAsync(Guid campaignId, string text, CancellationToken ct)`
   - Change: `SetReadAloudTextAsync(Guid campaignId, string text, string? tone, string? pacing, CancellationToken ct)`
   - Store all three values to campaign
   - Fire OnCampaignChanged events for all changed properties

**New Functions:**

4. `ToolExecutor.ExecuteGetGameLogAsync(Guid campaignId, string argumentsJson, CancellationToken ct) -> Task<string>`
   - Parse `limit` parameter (default 50, max 100)
   - Retrieve `NarrativeLog` from campaign
   - Format as markdown table or list with columns: Timestamp, Importance, Entry
   - Return markdown string

5. `ToolExecutor.ExecuteGetPlayerRollLogAsync(Guid campaignId, string argumentsJson, CancellationToken ct) -> Task<string>`
   - Parse `limit` parameter (default 50, max 100)
   - Retrieve `RecentRolls` from campaign
   - Format as markdown table with columns: Time, Character, Check Type, Result, Outcome
   - Return markdown string

6. `ToolExecutor.ExecuteGetCharacterPropertyNamesAsync(Guid campaignId, string argumentsJson, CancellationToken ct) -> Task<string>`
   - Return static markdown list of all queryable Character property names
   - Group by category: Identity, Ability Scores, Combat, Skills, Spellcasting, Equipment, Roleplay, State
   - Include property type hints (string, int, List<string>, etc.)

7. `ToolExecutor.ExecuteGetCharacterPropertiesAsync(Guid campaignId, string argumentsJson, CancellationToken ct) -> Task<string>`
   - Parse `character_ids` array (required) and `prop_names` array (required)
   - Look up characters by ID from campaign.PartyState
   - Extract requested properties using explicit mapping dictionary
   - Format as markdown table with character names as rows, properties as columns
   - Handle missing characters/properties gracefully with "N/A" or error notes

**Updated Switch Statement in ExecuteAsync:**
```csharp
var result = toolName switch
{
    "get_game_state" => await ExecuteGetGameStateAsync(...),
    "update_character_state" => await ExecuteUpdateCharacterStateAsync(...),
    "update_game_log" => await ExecuteUpdateGameLogAsync(...),
    "display_read_aloud_text" => await ExecuteDisplayReadAloudTextAsync(...),
    "present_player_choices" => await ExecutePresentPlayerChoicesAsync(...),
    "log_player_roll" => await ExecuteLogPlayerRollAsync(...),
    "update_scene_image" => await ExecuteUpdateSceneImageAsync(...),
    "get_game_log" => await ExecuteGetGameLogAsync(...),                    // NEW
    "get_player_roll_log" => await ExecuteGetPlayerRollLogAsync(...),      // NEW
    "get_character_property_names" => await ExecuteGetCharacterPropertyNamesAsync(...),  // NEW
    "get_character_properties" => await ExecuteGetCharacterPropertiesAsync(...),         // NEW
    _ => JsonSerializer.Serialize(new { error = $"Unknown tool: {toolName}" })
};
```

[Classes]
No new classes required.

**Modified Classes:**

1. `CampaignInstance` - Add two string properties for tone/pacing metadata

2. `ToolExecutor` - Add 4 new private methods, modify 1 existing method, add static property getter dictionary

3. `GameStateService` - Update `SetReadAloudTextAsync` method signature

4. `Campaign.razor` - Add tone/pacing UI display

**Helper Data Structure (private static in ToolExecutor):**
```csharp
// Mapping of property names to getter functions for safe property access
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
    
    // Player Linking
    ["PlayerId"] = c => c.PlayerId,
    ["PlayerName"] = c => c.PlayerName,
    ["IsClaimed"] = c => c.IsClaimed,
};
```

[Dependencies]
No new package dependencies required.

All functionality uses existing:
- `System.Text.Json` for JSON parsing
- `System.Text` for StringBuilder (markdown generation)
- LLM Tornado SDK for tool definitions
- Flowbite Blazor for Badge components

[Testing]
Manual testing via DM Chat interface.

**Test Scenarios:**

1. **display_read_aloud_text with tone/pacing**
   - Send chat message asking LLM to describe a dramatic, tense scene
   - Verify tool is called with text, tone (e.g., "ominous"), and pacing (e.g., "slow")
   - Verify read-aloud text appears in UI with tone/pacing badges displayed above

2. **System prompt startup sequence**
   - Start a new conversation with the LLM
   - Verify LLM calls both `get_game_state` AND `get_game_log` before responding
   - Check App Events tab to confirm both tool calls

3. **get_game_log**
   - First add several log entries via `update_game_log`
   - Then ask LLM "what has happened so far?"
   - Verify LLM calls `get_game_log` and receives markdown
   - Verify markdown is well-formatted in LLM response

4. **get_player_roll_log**
   - First log several rolls via `log_player_roll`
   - Then ask LLM "show me recent dice rolls"
   - Verify LLM calls `get_player_roll_log` and receives markdown

5. **get_character_property_names**
   - Ask LLM "what character properties can you query?"
   - Verify LLM calls tool and returns categorized list

6. **get_character_properties**
   - With characters in party, ask "what are the HP values for all characters?"
   - Verify LLM calls tool with appropriate character_ids and prop_names
   - Verify markdown table response

**Verification Commands:**
```bash
python build.py start
# Test via browser at /dm/{campaign-id}
python build.py log tool --tail 50  # Check tool execution logs
python build.py db party            # Verify character data
```

[Implementation Order]
Implement in sequence to minimize integration risk.

1. **Add tone/pacing properties to CampaignInstance model** in `CampaignInstance.cs`
   - Add `CurrentReadAloudTone` and `CurrentReadAloudPacing` string properties
   - No migration needed (strings serialize in existing schema)

2. **Update IGameStateService interface** in `IGameStateService.cs`
   - Add updated `SetReadAloudTextAsync` signature with tone/pacing parameters

3. **Update GameStateService** in `GameStateService.cs`
   - Implement `SetReadAloudTextAsync` with tone/pacing storage and events

4. **Update Campaign.razor UI** in `Campaign.razor`
   - Add tone/pacing badge display above read-aloud text
   - Add event handlers for new properties
   - Add styling helper methods

5. **Update display_read_aloud_text tool definition** in `RiddleLlmService.cs`
   - Add `tone` and `pacing` optional parameters to schema

6. **Update ExecuteDisplayReadAloudTextAsync** in `ToolExecutor.cs`
   - Parse `tone` and `pacing` from arguments
   - Call updated GameStateService method
   - Include in success response

7. **Update system prompt** in `RiddleLlmService.BuildSystemPrompt()`
   - Change startup instructions to call both `get_game_state()` and `get_game_log()`

8. **Add get_game_log tool definition** in `RiddleLlmService.cs`
   - Simple tool with optional `limit` parameter

9. **Implement ExecuteGetGameLogAsync** in `ToolExecutor.cs`
   - Add switch case routing
   - Implement markdown generation from NarrativeLog

10. **Add get_player_roll_log tool definition** in `RiddleLlmService.cs`
    - Simple tool with optional `limit` parameter

11. **Implement ExecuteGetPlayerRollLogAsync** in `ToolExecutor.cs`
    - Add switch case routing
    - Implement markdown generation from RecentRolls

12. **Add get_character_property_names tool definition** in `RiddleLlmService.cs`
    - No parameters needed

13. **Implement ExecuteGetCharacterPropertyNamesAsync** in `ToolExecutor.cs`
    - Add static property getter dictionary
    - Generate categorized markdown list

14. **Add get_character_properties tool definition** in `RiddleLlmService.cs`
    - Array parameters for character_ids and prop_names

15. **Implement ExecuteGetCharacterPropertiesAsync** in `ToolExecutor.cs`
    - Character lookup and property extraction
    - Markdown table generation

16. **Build and smoke test**
    - `python build.py` to verify compilation
    - Manual test each tool via DM Chat

17. **Update developer documentation** (optional)
    - Add new tools to any existing tool documentation
