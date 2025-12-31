# Implementation Plan: Death Saves & Dying System

Implement D&D 5e death saving throw mechanics with automatic rule enforcement when character HP reaches zero.

[Overview]

This implementation adds automatic D&D 5e death and dying rules to the game state management system. When a character's HP drops to 0, the system will automatically apply the "Unconscious" condition, reset death save counters, and enforce the 3-success/3-failure resolution mechanics. The existing `PlayerCharacterCard.razor` already has the death save circle UI - this plan focuses on the backend logic, LLM tool updates, and SignalR event broadcasting.

**Key Features:**
1. Auto-apply "Unconscious" condition when CurrentHp reaches 0
2. Auto-reset DeathSaveSuccesses/DeathSaveFailures when healed from 0 HP
3. Auto-mark character as "Stable" at 3 successes, "Dead" at 3 failures
4. Natural 20 special handling (regain 1 HP, clear saves)
5. Damage at 0 HP causes automatic death save failure
6. Massive damage instant death (damage remaining >= MaxHp)
7. New tool keys: `death_save_success`, `death_save_failure`, `stabilize`

[Types]

Add new death-related state tracking to the existing Character model.

**Character.cs additions:**
- `IsStable` (computed property): Returns `true` if DeathSaveSuccesses >= 3 and CurrentHp <= 0
- `IsDead` (computed property): Returns `true` if DeathSaveFailures >= 3

**GameHubEvents.cs additions:**
```csharp
// New payload for death save tracking
public record DeathSavePayload(
    string CharacterId,
    string CharacterName,
    int DeathSaveSuccesses,
    int DeathSaveFailures,
    bool IsStable,
    bool IsDead
);
```

**New SignalR event:**
- `DeathSaveUpdated` - Broadcasts death save state changes to all clients

[Files]

Modify existing files to implement death save mechanics.

**Files to Modify:**

| File | Changes |
|------|---------|
| `src/Riddle.Web/Models/Character.cs` | Add `IsStable`, `IsDead` computed properties |
| `src/Riddle.Web/Services/ToolExecutor.cs` | Add death save key handlers, auto-rule enforcement |
| `src/Riddle.Web/Services/INotificationService.cs` | Add `NotifyDeathSaveUpdatedAsync` method |
| `src/Riddle.Web/Services/NotificationService.cs` | Implement death save notification |
| `src/Riddle.Web/Hubs/GameHubEvents.cs` | Add `DeathSaveUpdated` event and payload |
| `src/Riddle.Web/Components/Player/PlayerCharacterCard.razor` | Add "Stable"/"Dead" badge display |
| `src/Riddle.Web/Components/Combat/CombatantCard.razor` | Add death save indicator for PCs |

**No new files required** - all changes extend existing infrastructure.

[Functions]

Modify and add functions for death save processing.

**ToolExecutor.cs - New Private Methods:**

1. `ApplyDeathSaveRulesAsync(Guid campaignId, Character character, CancellationToken ct)`
   - Called after HP changes
   - If HP drops to 0: adds "Unconscious", resets death saves
   - If HP rises from 0: removes "Unconscious", clears "Stable", resets death saves
   - Returns bool indicating if rules were applied

2. `ProcessDeathSaveSuccessAsync(Guid campaignId, Character character, bool isNatural20, CancellationToken ct)`
   - Increments DeathSaveSuccesses
   - If natural 20: sets HP to 1, removes Unconscious, resets saves
   - If 3 successes: adds "Stable" condition

3. `ProcessDeathSaveFailureAsync(Guid campaignId, Character character, int failureCount, CancellationToken ct)`
   - Increments DeathSaveFailures by failureCount (1 for normal, 2 for crit)
   - If 3 failures: marks character as Dead, removes from combat

4. `CheckMassiveDamageAsync(Guid campaignId, Character character, int damageOverflow, CancellationToken ct)`
   - If damageOverflow >= character.MaxHp: instant death

**ToolExecutor.cs - Modified switch in ExecuteUpdateCharacterStateAsync:**

Add new case handlers:
- `"death_save_success"` → ProcessDeathSaveSuccessAsync
- `"death_save_failure"` → ProcessDeathSaveFailureAsync  
- `"stabilize"` → Adds "Stable" condition, stops death saves

Modify existing `"current_hp"` case:
- Call ApplyDeathSaveRulesAsync after HP change
- Check for massive damage if going below 0

**INotificationService.cs - New Method:**
```csharp
Task NotifyDeathSaveUpdatedAsync(Guid campaignId, DeathSavePayload payload, CancellationToken ct);
```

**NotificationService.cs - Implement:**
```csharp
public async Task NotifyDeathSaveUpdatedAsync(Guid campaignId, DeathSavePayload payload, CancellationToken ct)
{
    var group = GetCampaignGroup(campaignId);
    await _hubContext.Clients.Group(group).SendAsync(GameHubEvents.DeathSaveUpdated, payload, ct);
}
```

[Classes]

No new classes required - extend existing models and services.

**Character.cs - Add Computed Properties:**
```csharp
/// <summary>
/// Whether the character is stable (3 death save successes while at 0 HP)
/// </summary>
public bool IsStable => CurrentHp <= 0 && DeathSaveSuccesses >= 3;

/// <summary>
/// Whether the character is dead (3 death save failures)
/// </summary>
public bool IsDead => DeathSaveFailures >= 3;
```

**CharacterPropertyGetters dictionary - Add entries:**
```csharp
["IsStable"] = c => c.IsStable,
["IsDead"] = c => c.IsDead,
```

[Dependencies]

No new dependencies required.

All functionality uses existing packages:
- ASP.NET Core SignalR (already configured)
- System.Text.Json (already in use)
- Entity Framework Core (already configured)

[Testing]

Testing approach follows existing E2E patterns.

**Test Files to Create/Modify:**

1. `tests/Riddle.Web.IntegrationTests/E2ETests/DeathSaveTests.cs` (new)
   - Tests for automatic Unconscious condition at 0 HP
   - Tests for death save success/failure incrementing
   - Tests for natural 20 healing
   - Tests for 3 failures = Dead
   - Tests for 3 successes = Stable
   - Tests for damage at 0 HP causing auto-failure
   - Tests for massive damage instant death
   - Tests for SignalR broadcast on death save changes

2. Modify `UpdateCharacterStateToolTests.cs`:
   - Add tests for new keys: `death_save_success`, `death_save_failure`, `stabilize`
   - Add tests for HP change auto-rules

**Validation Scenarios (from feature files):**
- HLR-COMBAT-016 through HLR-COMBAT-026
- HLR-PLAYER-016 through HLR-PLAYER-023

[Implementation Order]

Implement in dependency order to ensure each step builds on completed work.

1. **Character.cs** - Add `IsStable` and `IsDead` computed properties
2. **GameHubEvents.cs** - Add `DeathSaveUpdated` event constant and `DeathSavePayload` record
3. **INotificationService.cs** - Add `NotifyDeathSaveUpdatedAsync` method signature
4. **NotificationService.cs** - Implement `NotifyDeathSaveUpdatedAsync`
5. **ToolExecutor.cs** - Add helper methods for death save processing
6. **ToolExecutor.cs** - Modify `current_hp` case to apply auto-rules
7. **ToolExecutor.cs** - Add new case handlers for death save keys
8. **ToolExecutor.cs** - Update `CharacterPropertyGetters` dictionary
9. **ToolExecutor.cs** - Update `get_character_property_names` output
10. **PlayerCharacterCard.razor** - Add "Stable"/"Dead" badge display
11. **CombatantCard.razor** - Add death save indicator for PC combatants
12. **DeathSaveTests.cs** - Create E2E test suite
13. **Manual verification** - Test with running app via browser

**Estimated Complexity:** Medium
**Estimated LOC:** ~200 new/modified lines
**Risk Areas:** 
- Combat turn order management when character dies (remove from order)
- SignalR event ordering (HP change → death save update → UI refresh)
