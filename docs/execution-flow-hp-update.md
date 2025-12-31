# Execution Flow: Character HP Update via Tool Executor

This document simulates the complete execution path when the LLM calls the `update_character_state` tool to update a character's HP, including the special case when HP reaches zero.

---

## Scenario Setup

**Context:**
- Combat is active with 4 combatants:
  - `Elara Moonshadow` (PC) - 25/30 HP
  - `Thoric Ironbeard` (PC) - 40/45 HP  
  - `Goblin Warrior 1` (Enemy) - 12/12 HP
  - `Goblin Warrior 2` (Enemy) - 12/12 HP
- Current turn: Elara Moonshadow (index 0)
- Round: 1

**LLM Action:**
The DM narrates that Elara attacks Goblin Warrior 1 and deals 12 damage, reducing it to 0 HP.

---

## Phase 1: Tool Invocation

```
LLM → RiddleLlmService
    Tool Call: "update_character_state"
    Arguments JSON: {
        "character_name": "Goblin_Warrior_1",
        "key": "current_hp",
        "value": 0
    }
```

### Pseudocode: RiddleLlmService.ProcessToolCallAsync

```
FUNCTION ProcessToolCallAsync(toolCall, campaignId):
    toolName = toolCall.FunctionName         // "update_character_state"
    arguments = toolCall.FunctionArguments   // JSON string
    
    result = await ToolExecutor.ExecuteAsync(campaignId, toolName, arguments)
    RETURN result
END
```

---

## Phase 2: Tool Executor Routing

### Pseudocode: ToolExecutor.ExecuteAsync

```
FUNCTION ExecuteAsync(campaignId, toolName, argumentsJson):
    LOG "Executing tool {toolName} for campaign {campaignId}"
    
    SWITCH toolName:
        CASE "update_character_state":
            RETURN await ExecuteUpdateCharacterStateAsync(campaignId, argumentsJson)
        // ... other tools ...
    END SWITCH
END
```

---

## Phase 3: Character Lookup & Update

### Pseudocode: ToolExecutor.ExecuteUpdateCharacterStateAsync

```
FUNCTION ExecuteUpdateCharacterStateAsync(campaignId, argumentsJson):
    // Step 1: Parse arguments
    args = JSON.Parse(argumentsJson)
    characterNameOrId = args["character_name"]  // "Goblin_Warrior_1"
    key = args["key"]                           // "current_hp"
    value = args["value"]                       // 0
    
    // Step 2: Load campaign state
    campaign = await StateService.GetCampaignAsync(campaignId)
    IF campaign IS NULL:
        RETURN { error: "Campaign not found" }
    
    // Step 3: Try to find character in Party State (PCs)
    character = campaign.PartyState.FirstOrDefault(c => 
        c.Id == characterNameOrId OR 
        NormalizeName(c.Name) == NormalizeName(characterNameOrId)
    )
    // NormalizeName: "Goblin_Warrior_1" → "goblin warrior 1"
    // Result: NOT FOUND (goblins are enemies, not in PartyState)
    
    // Step 4: If not in party, check combat combatants
    IF character IS NULL:
        combatState = await CombatService.GetCombatStateAsync(campaignId)
        
        IF combatState.IsActive == true:
            combatant = combatState.TurnOrder.FirstOrDefault(c =>
                c.Id == characterNameOrId OR
                NormalizeName(c.Name) == NormalizeName(characterNameOrId)
            )
            // NormalizeName: "Goblin Warrior 1" → "goblin warrior 1"
            // Result: FOUND! combatant = { Id: "enemy_abc123", Name: "Goblin Warrior 1", ... }
            
            IF combatant IS NOT NULL:
                // Route to combatant-specific update handler
                RETURN await UpdateCombatantStateAsync(campaignId, combatant, key, value)
    
    // Would reach here if character truly not found
    RETURN { error: "Character not found in party or combat" }
END
```

---

## Phase 4: Combatant State Update

### Pseudocode: ToolExecutor.UpdateCombatantStateAsync

```
FUNCTION UpdateCombatantStateAsync(campaignId, combatant, key, valueElement):
    SWITCH key:
        CASE "current_hp":
            // Parse integer from JSON (handles "0" or 0)
            newHp = ParseIntValue(valueElement)  // 0
            
            // Delegate to CombatService for HP update + defeat detection
            await CombatService.UpdateCombatantHpAsync(campaignId, combatant.Id, newHp)
            
            // Broadcast state change to all connected clients
            payload = new CharacterStatePayload(combatant.Id, key, newHp)
            await NotificationService.NotifyCharacterStateUpdatedAsync(campaignId, payload)
            
            LOG "Updated combatant {combatant.Name} HP to {newHp}"
            RETURN { success: true, character_name: combatant.Name, key: "current_hp", updated: true }
        
        CASE "initiative":
            // Similar flow for initiative updates...
        
        DEFAULT:
            RETURN { error: "Combatants only support current_hp and initiative" }
    END SWITCH
END
```

---

## Phase 5: Combat Service HP Update with Defeat Detection

### Pseudocode: CombatService.UpdateCombatantHpAsync

```
FUNCTION UpdateCombatantHpAsync(campaignId, characterId, newHp):
    // Step 1: Load campaign and active combat
    campaign = await DbContext.CampaignInstances.FindAsync(campaignId)
    combat = campaign.ActiveCombat
    
    // Step 2: Find combatant in persisted dictionary
    combatant = combat.Combatants[characterId]
    // combatant = { Id: "enemy_abc123", Name: "Goblin Warrior 1", CurrentHp: 12, MaxHp: 12, IsDefeated: false }
    
    // Step 3: Track state before update
    wasDefeated = combatant.IsDefeated          // false
    isNowDefeated = (newHp <= 0)                // true (newHp = 0)
    
    // Step 4: Update combatant state
    combatant.CurrentHp = Math.Max(0, newHp)    // 0
    combatant.IsDefeated = isNowDefeated        // true
    
    // Step 5: Persist to database
    campaign.ActiveCombat = combat
    campaign.LastActivityAt = DateTime.UtcNow
    await DbContext.SaveChangesAsync()
    
    // Step 6: CRITICAL - Detect transition to defeated state
    IF (NOT wasDefeated) AND isNowDefeated:
        LOG "Combatant just became defeated, triggering MarkDefeatedAsync"
        await MarkDefeatedAsync(campaignId, characterId)
        RETURN  // MarkDefeatedAsync handles all further logic
    END IF
    
    // Step 7: If NOT newly defeated, just broadcast HP change
    await NotificationService.NotifyCharacterStateUpdatedAsync(
        campaignId, 
        new CharacterStatePayload(characterId, "CurrentHp", newHp)
    )
END
```

---

## Phase 6: Mark Defeated & Remove from Turn Order

### Pseudocode: CombatService.MarkDefeatedAsync

```
FUNCTION MarkDefeatedAsync(campaignId, characterId):
    campaign = await DbContext.CampaignInstances.FindAsync(campaignId)
    combat = campaign.ActiveCombat
    
    // Step 1: Update persisted combatant (redundant but safe)
    combatant = combat.Combatants[characterId]
    combatant.IsDefeated = true
    combatant.CurrentHp = 0
    
    // Step 2: Find combatant position in turn order
    currentTurnIndex = combat.CurrentTurnIndex  // 0 (Elara's turn)
    defeatedIndex = combat.TurnOrder.IndexOf(characterId)
    // TurnOrder BEFORE: ["elara_id", "goblin1_id", "thoric_id", "goblin2_id"]
    // defeatedIndex = 1 (Goblin Warrior 1)
    
    // Step 3: REMOVE from turn order (key step!)
    IF defeatedIndex >= 0:
        combat.TurnOrder.RemoveAt(defeatedIndex)
        // TurnOrder AFTER: ["elara_id", "thoric_id", "goblin2_id"]
        
        // Step 4: Adjust current turn index if needed
        IF defeatedIndex < currentTurnIndex:
            // Defeated combatant was BEFORE current turn
            // Decrement to maintain correct position
            combat.CurrentTurnIndex = combat.CurrentTurnIndex - 1
        ELSE IF defeatedIndex == currentTurnIndex:
            // Defeated combatant WAS the current turn (rare edge case)
            IF combat.CurrentTurnIndex >= combat.TurnOrder.Count:
                combat.CurrentTurnIndex = 0  // Wrap to start of round
        END IF
        // In our case: defeatedIndex(1) > currentTurnIndex(0), no adjustment needed
    END IF
    
    // Step 5: Persist changes
    campaign.ActiveCombat = combat
    campaign.LastActivityAt = DateTime.UtcNow
    await DbContext.SaveChangesAsync()
    
    LOG "Combatant {combatant.Name} marked as defeated"
    
    // Step 6: Check for combat victory condition
    activeEnemies = combat.Combatants.Values
        .Where(c => c.Type == "Enemy" AND NOT c.IsDefeated)
        .ToList()
    // activeEnemies = [Goblin Warrior 2] - 1 remaining
    
    IF activeEnemies.Count == 0:
        LOG "All enemies defeated, auto-ending combat"
        await EndCombatAsync(campaignId)
        RETURN
    END IF
    
    // Step 7: Broadcast FULL updated combat state (not just HP change)
    // This is crucial - clients need the new TurnOrder without the defeated combatant
    payload = await GetCombatStateAsync(campaignId)
    // payload.TurnOrder = [Elara, Thoric, Goblin2] - Goblin1 is GONE
    
    await NotificationService.NotifyCombatStartedAsync(campaignId, payload)
    // Note: Reuses CombatStarted event to broadcast full state update
END
```

---

## Phase 7: Build Combat State Payload

### Pseudocode: CombatService.BuildCombatStatePayload

```
FUNCTION BuildCombatStatePayload(combat):
    // Build combatant list in turn order (ONLY includes active combatants)
    combatants = combat.TurnOrder
        .Where(id => combat.Combatants.ContainsKey(id))  // Filter valid IDs
        .Select(id => {
            c = combat.Combatants[id]
            RETURN new CombatantInfo(
                Id: c.Id,
                Name: c.Name,
                Type: c.Type,
                Initiative: c.Initiative,
                CurrentHp: c.CurrentHp,
                MaxHp: c.MaxHp,
                IsDefeated: c.IsDefeated,  // Will be false for active combatants
                IsSurprised: combat.SurprisedEntities.Contains(c.Id)
            )
        })
        .ToList()
    
    // IMPORTANT: Defeated combatants are NOT in TurnOrder anymore
    // So they do NOT appear in this list!
    
    RETURN new CombatStatePayload(
        CombatId: combat.Id,
        IsActive: combat.IsActive,
        RoundNumber: combat.RoundNumber,  // 1
        TurnOrder: combatants,            // [Elara, Thoric, Goblin2]
        CurrentTurnIndex: combat.CurrentTurnIndex  // 0
    )
END
```

---

## Phase 8: SignalR Broadcast

### Pseudocode: NotificationService.NotifyCombatStartedAsync

```
FUNCTION NotifyCombatStartedAsync(campaignId, payload):
    LOG "Broadcasting CombatStarted to campaign {campaignId}: Round {payload.RoundNumber}, {payload.TurnOrder.Count} combatants"
    
    // Get SignalR group for all campaign participants (DM + Players)
    groupName = "campaign_{campaignId}_all"
    
    // Send CombatStatePayload to all connected clients
    await HubContext.Clients
        .Group(groupName)
        .SendAsync("CombatStarted", payload)
    
    // Payload contents:
    // {
    //     CombatId: "guid",
    //     IsActive: true,
    //     RoundNumber: 1,
    //     CurrentTurnIndex: 0,
    //     TurnOrder: [
    //         { Id: "elara_id", Name: "Elara Moonshadow", Type: "PC", CurrentHp: 25, MaxHp: 30, IsDefeated: false },
    //         { Id: "thoric_id", Name: "Thoric Ironbeard", Type: "PC", CurrentHp: 40, MaxHp: 45, IsDefeated: false },
    //         { Id: "goblin2_id", Name: "Goblin Warrior 2", Type: "Enemy", CurrentHp: 12, MaxHp: 12, IsDefeated: false }
    //     ]
    // }
    // NOTE: Goblin Warrior 1 is NOT in the list - it was removed from TurnOrder
END
```

---

## Phase 9: UI Update (CombatTracker.razor)

### Pseudocode: CombatTracker SignalR Handler

```
// This handler was registered in OnInitializedAsync:
hubConnection.On<CombatStatePayload>("CombatStarted", payload => {
    LOG "[CombatTracker] CombatStarted SignalR event RECEIVED"
    
    // Invoke callback to notify parent component (DM Dashboard)
    // Parent owns the Combat state - we don't modify it directly
    await InvokeAsync(async () => {
        await CombatChanged.InvokeAsync(payload)
    })
})
```

### Pseudocode: Parent Component (DM Dashboard) State Update

```
// In parent component:
FUNCTION HandleCombatChanged(newCombatState):
    Combat = newCombatState  // Update the @bind-Combat parameter
    StateHasChanged()        // Trigger re-render
END
```

---

## Phase 10: UI Re-render (CombatTracker.razor)

### Pseudocode: CombatTracker Render Logic

```
// Razor template rendering:
IF Combat.IsActive != true:
    RENDER "No active combat" message
ELSE:
    // Combat IS active, render combatant cards
    FOR EACH (combatant, index) IN Combat.TurnOrder:
        RENDER <CombatantCard 
            Combatant={combatant}
            Position={index + 1}
            IsCurrentTurn={index == Combat.CurrentTurnIndex}
            IsDm={IsDm}
        />
    END FOR
END IF

// With our updated state, this renders:
// Position 1: Elara Moonshadow (PC) ▶ [Current Turn] - 25/30 HP
// Position 2: Thoric Ironbeard (PC) - 40/45 HP
// Position 3: Goblin Warrior 2 (Enemy) - 12/12 HP
//
// Goblin Warrior 1 is GONE from the list entirely!
```

---

## Summary: What the User Sees

### Before HP Update:
```
⚔️ Combat Tracker                         Round 1
┌─────────────────────────────────────────────────┐
│ ▶ 1  🧙 Elara Moonshadow      Init: 18   25/30 │  ← Current turn
│   2  👹 Goblin Warrior 1      Init: 15   12/12 │
│   3  🧙 Thoric Ironbeard      Init: 12   40/45 │
│   4  👹 Goblin Warrior 2      Init: 10   12/12 │
└─────────────────────────────────────────────────┘
```

### After HP Update (0 HP):
```
⚔️ Combat Tracker                         Round 1
┌─────────────────────────────────────────────────┐
│ ▶ 1  🧙 Elara Moonshadow      Init: 18   25/30 │  ← Current turn
│   2  🧙 Thoric Ironbeard      Init: 12   40/45 │  ← Was position 3
│   3  👹 Goblin Warrior 2      Init: 10   12/12 │  ← Was position 4
└─────────────────────────────────────────────────┘

Goblin Warrior 1 is REMOVED from the list (not shown as defeated)
```

---

## Important Observations

### 1. Defeated Combatants Are Removed, Not Shown With Defeat Badge
The `CombatantCard.razor` component DOES have logic to display a `☠️ Defeated` badge and strikethrough styling, BUT this code is never reached because:
- `MarkDefeatedAsync` removes the combatant from `TurnOrder`
- `BuildCombatStatePayload` only includes combatants in `TurnOrder`
- The defeated combatant never reaches the UI

### 2. The IsDefeated Visual Styling Exists But Isn't Used
```razor
@if (Combatant.IsDefeated)
{
    <span class="px-1.5 py-0.5 text-xs rounded bg-gray-100 ...">
        ☠️ Defeated
    </span>
}
```
This code exists but is effectively dead code in the current flow.

### 3. Alternative Design Consideration
If you wanted to SHOW defeated combatants (grayed out, at bottom of list), you would need to:
1. Keep defeated combatants in `TurnOrder` (don't remove in `MarkDefeatedAsync`)
2. Filter them from turn progression logic
3. Let `BuildCombatStatePayload` include them with `IsDefeated: true`
4. Let `CombatantCard` render them with defeat styling

---

## Data Flow Diagram

```
┌─────────────────┐
│   LLM Tool Call │  update_character_state { character_name: "Goblin_Warrior_1", key: "current_hp", value: 0 }
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  ToolExecutor   │  ExecuteUpdateCharacterStateAsync → UpdateCombatantStateAsync
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  CombatService  │  UpdateCombatantHpAsync → detects HP <= 0 → MarkDefeatedAsync
└────────┬────────┘
         │
         ├──────────────────────────────────────┐
         │                                      │
         ▼                                      ▼
┌─────────────────┐                    ┌─────────────────┐
│    Database     │                    │ NotificationSvc │
│ (EF Core Save)  │                    │ (SignalR Hub)   │
└─────────────────┘                    └────────┬────────┘
                                                │
                                                │  SendAsync("CombatStarted", payload)
                                                │
         ┌──────────────────────────────────────┼──────────────────────────────────────┐
         │                                      │                                      │
         ▼                                      ▼                                      ▼
┌─────────────────┐                    ┌─────────────────┐                    ┌─────────────────┐
│  DM Dashboard   │                    │ CombatTracker   │                    │ Player Screens  │
│  (Blazor)       │                    │  (Blazor)       │                    │  (Blazor)       │
└────────┬────────┘                    └────────┬────────┘                    └─────────────────┘
         │                                      │
         │ CombatChanged.InvokeAsync(payload)   │
         ▼                                      ▼
┌─────────────────┐                    ┌─────────────────┐
│  StateHasChanged │                   │ Parent Updates  │
│  → Re-render     │                   │ Combat State    │
└─────────────────┘                    └────────┬────────┘
                                                │
                                                ▼
                                       ┌─────────────────┐
                                       │ UI Re-renders   │
                                       │ (Goblin removed)│
                                       └─────────────────┘
```

---

## Related Files

| File | Purpose |
|------|---------|
| `src/Riddle.Web/Services/ToolExecutor.cs` | Routes LLM tool calls, dispatches to services |
| `src/Riddle.Web/Services/CombatService.cs` | Combat state management, defeat detection |
| `src/Riddle.Web/Services/NotificationService.cs` | SignalR broadcast to clients |
| `src/Riddle.Web/Hubs/GameHubEvents.cs` | SignalR event names and payload records |
| `src/Riddle.Web/Components/Combat/CombatTracker.razor` | Combat UI component |
| `src/Riddle.Web/Components/Combat/CombatantCard.razor` | Individual combatant display |

---

## Key Takeaways

1. **Automatic Defeat Detection**: When HP <= 0, `CombatService` automatically calls `MarkDefeatedAsync`
2. **Removal from Turn Order**: Defeated combatants are removed, not just marked
3. **Index Adjustment**: Current turn index is adjusted to prevent turn skipping
4. **Victory Detection**: Combat auto-ends when all enemies are defeated
5. **Full State Broadcast**: Defeat triggers a full `CombatStarted` event (reused for state updates)
6. **UI Simply Renders**: The UI is "dumb" - it just renders what's in the payload
