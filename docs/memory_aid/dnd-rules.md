# D&D 5e Rules Implementation

> **Keywords:** Death saves, 0 HP, PCs, enemies, combat mechanics, healing
> **Related:** [LLM Tools](./llm-tools.md), [Blazor Components](./blazor-components.md)

This document covers D&D 5th Edition rules implementation details specific to this project.

---

## Death Saves (PC Only)

Death saving throws are a **PC-only mechanic**. When a PC drops to 0 HP:

1. PC becomes **unconscious** and must make death saves on their turn
2. Three successes → stabilized (unconscious but not dying)
3. Three failures → dead
4. Natural 20 → regain 1 HP and become conscious
5. Natural 1 → counts as two failures
6. Taking damage while at 0 HP → automatic death save failure

**Implementation:**
```csharp
public class Character
{
    public int DeathSaveSuccesses { get; set; }
    public int DeathSaveFailures { get; set; }
    public bool IsStabilized { get; set; }
}
```

---

## PC vs Enemy Display at 0 HP

The UI should handle 0 HP differently for PCs vs enemies:

| State | PC Display | Enemy Display |
|-------|-----------|---------------|
| 0 HP | Show death save UI, HP bar empty | Remove from tracker (defeated) |
| Healed from 0 | Clear death saves, show HP | N/A (enemies don't heal typically) |
| Dead | Show "Dead" status | Remove from tracker |

**Why?** Enemies typically die at 0 HP without death saves (DM discretion). PCs get the dramatic death save mechanic.

---

## Combat Tracker HP Update Rules

When updating HP via LLM tools:

1. **Damage:** Subtract from current HP, floor at 0
2. **Healing:** Add to current HP, cap at max HP
3. **Temp HP:** Track separately, damage temp HP first
4. **Massive damage:** If remaining damage after hitting 0 equals or exceeds max HP → instant death

---

## Initiative & Turn Order

- Initiative is rolled once at combat start (d20 + DEX modifier)
- Turn order is sorted descending (highest first)
- Ties go to higher DEX modifier, then alphabetically
- "Delay" and "Ready" actions are DM adjudicated (not system-tracked)

---

## Conditions Reference

Common conditions that affect character state:

| Condition | Effect Summary |
|-----------|---------------|
| Unconscious | At 0 HP, auto-fail STR/DEX saves, attacks have advantage |
| Prone | Disadvantage on attacks, melee attacks against have advantage |
| Stunned | Can't act, auto-fail STR/DEX saves |
| Poisoned | Disadvantage on attacks and ability checks |
| Frightened | Disadvantage on checks/attacks while source visible |

These map to the `Condition` enum in the codebase.
