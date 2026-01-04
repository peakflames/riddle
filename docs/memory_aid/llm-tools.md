# LLM Tools Patterns

> **Keywords:** Tool name, character ID, type conversion, Enum.Parse, dual data sources, streaming
> **Related:** [EF Core Patterns](./ef-core-patterns.md), [D&D Rules](./dnd-rules.md)

This document covers patterns for LLM tool implementations (the AI Dungeon Master tools).

---

## CRITICAL: Always Use Character Name, Never ID

When defining LLM tools with character parameters, **always use `name` instead of `id`**. LLMs work better with human-readable names:

```csharp
// ✅ CORRECT - Character identified by name
[Description("Update the HP for a character identified by name")]
public async Task<string> UpdateCharacterHpAsync(
    [Description("The name of the character")] string name,
    [Description("The new HP value")] int hp)

// ❌ WRONG - LLM doesn't know GUIDs
public async Task<string> UpdateCharacterHpAsync(string characterId, int hp)
```

The tool implementation then looks up the character by name in the party.

---

## Enum Parsing

When LLM provides enum-like values, parse with fallback:

```csharp
var condition = Enum.TryParse<Condition>(conditionString, ignoreCase: true, out var c) 
    ? c 
    : Condition.None;
```

---

## Tool Dual Data Sources Pattern

**LLM tools must update BOTH:**
1. **`CampaignInstance.PartyStateJson`** - The authoritative source (persisted to DB)
2. **`CombatEncounter.Combatants`** - The combat-time working copy (for initiative tracking)

When combat is active, state exists in both places:
- `PartyStateJson` → Master record (restored on refresh/reconnect)
- `CombatEncounter.Combatants` → Working copy during combat

**Pattern:**
```csharp
// 1. Update authoritative source
var character = campaign.PartyState.FirstOrDefault(c => c.Name == characterName);
character.CurrentHp = newHp;
campaign.PartyState = partyState;  // Re-serialize

// 2. Update combat state if active
if (combat != null && combat.Combatants.TryGetValue(character.Id, out var combatant))
{
    combatant.CurrentHp = newHp;
    await _combatService.UpdateCombatStateAsync(combat);
}
```

**Why both?** PartyState is the truth that survives combat end. Combatants is the ephemeral tracker that gets cleared when combat ends. Both need the same HP during combat for consistency.

---

## LLM Tool Change Verification

When implementing tool changes:
1. Build succeeds → Start app
2. In chat, test the tool directly (e.g., "Deal 5 damage to Elara")
3. Check `python build.py log` for tool execution output
4. Verify via `python build.py db party` that state persisted
