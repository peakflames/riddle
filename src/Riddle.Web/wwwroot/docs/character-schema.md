# Character JSON Schema

This document describes the JSON structure for character templates. Use this format when importing characters via JSON.

## Minimal Example

```json
{
  "name": "Aria Stormwind",
  "race": "Human",
  "class": "Fighter",
  "level": 1,
  "maxHp": 12,
  "currentHp": 12,
  "armorClass": 16
}
```

## Full Example

```json
{
  "name": "Elara Moonshadow",
  "type": "PC",
  "race": "High Elf",
  "class": "Wizard",
  "level": 5,
  "background": "Sage",
  "alignment": "Neutral Good",
  "strength": 8,
  "dexterity": 14,
  "constitution": 13,
  "intelligence": 18,
  "wisdom": 12,
  "charisma": 10,
  "armorClass": 12,
  "maxHp": 27,
  "currentHp": 27,
  "temporaryHp": 0,
  "initiative": 2,
  "speed": "30 ft",
  "passivePerception": 13,
  "savingThrowProficiencies": ["Intelligence", "Wisdom"],
  "skillProficiencies": ["Arcana", "History", "Investigation"],
  "toolProficiencies": [],
  "languages": ["Common", "Elvish", "Draconic"],
  "isSpellcaster": true,
  "spellcastingAbility": "Intelligence",
  "spellSaveDC": 15,
  "spellAttackBonus": 7,
  "cantrips": ["Fire Bolt", "Mage Hand", "Light", "Prestidigitation"],
  "spellsKnown": ["Magic Missile", "Shield", "Fireball", "Counterspell"],
  "spellSlots": { "1": 4, "2": 3, "3": 2 },
  "equipment": ["Spellbook", "Component Pouch", "Scholar's Pack"],
  "weapons": ["Quarterstaff", "Dagger"],
  "goldPieces": 50,
  "personalityTraits": "I use polysyllabic words that convey the impression of great erudition.",
  "ideals": "Knowledge. The path to power and self-improvement is through knowledge.",
  "bonds": "I have an ancient tome that holds terrible secrets that must not fall into the wrong hands.",
  "flaws": "I overlook obvious solutions in favor of complicated ones.",
  "backstory": "A scholar who discovered an ancient spellbook in a forgotten library..."
}
```

---

## Field Reference

### Identity Fields

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `name` | string | **Yes** | - | Character's name |
| `type` | string | No | `"PC"` | `"PC"` for player character, `"NPC"` for non-player |
| `race` | string | No | - | Race (e.g., "Human", "High Elf", "Dwarf") |
| `class` | string | No | - | Class (e.g., "Fighter", "Wizard", "Rogue") |
| `level` | integer | No | `1` | Character level (1-20) |
| `background` | string | No | - | Background (e.g., "Sage", "Soldier") |
| `alignment` | string | No | - | Alignment (e.g., "Neutral Good") |

### Ability Scores

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `strength` | integer | No | `10` | Strength score (1-30) |
| `dexterity` | integer | No | `10` | Dexterity score (1-30) |
| `constitution` | integer | No | `10` | Constitution score (1-30) |
| `intelligence` | integer | No | `10` | Intelligence score (1-30) |
| `wisdom` | integer | No | `10` | Wisdom score (1-30) |
| `charisma` | integer | No | `10` | Charisma score (1-30) |

### Combat Stats

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `armorClass` | integer | No | `0` | Armor Class (AC) |
| `maxHp` | integer | No | `0` | Maximum hit points |
| `currentHp` | integer | No | `0` | Current hit points |
| `temporaryHp` | integer | No | `0` | Temporary hit points |
| `initiative` | integer | No | `0` | Initiative modifier |
| `speed` | string | No | `"30 ft"` | Movement speed |

### Skills & Proficiencies

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `passivePerception` | integer | No | `0` | Passive Perception score |
| `savingThrowProficiencies` | string[] | No | `[]` | Saving throw proficiencies |
| `skillProficiencies` | string[] | No | `[]` | Skill proficiencies |
| `toolProficiencies` | string[] | No | `[]` | Tool proficiencies |
| `languages` | string[] | No | `["Common"]` | Known languages |

### Spellcasting

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `isSpellcaster` | boolean | No | `false` | Can cast spells |
| `spellcastingAbility` | string | No | - | Spellcasting ability |
| `spellSaveDC` | integer | No | - | Spell save DC |
| `spellAttackBonus` | integer | No | - | Spell attack bonus |
| `cantrips` | string[] | No | `[]` | Known cantrips |
| `spellsKnown` | string[] | No | `[]` | Known/prepared spells |
| `spellSlots` | object | No | `{}` | Spell slots by level |

**Spell Slots Format:**
```json
{
  "1": 4,
  "2": 3,
  "3": 2
}
```

### Equipment & Currency

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `equipment` | string[] | No | `[]` | General equipment |
| `weapons` | string[] | No | `[]` | Weapons carried |
| `copperPieces` | integer | No | `0` | Copper pieces (CP) |
| `silverPieces` | integer | No | `0` | Silver pieces (SP) |
| `goldPieces` | integer | No | `0` | Gold pieces (GP) |
| `platinumPieces` | integer | No | `0` | Platinum pieces (PP) |

### Roleplay

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `personalityTraits` | string | No | - | Personality traits |
| `ideals` | string | No | - | Character ideals |
| `bonds` | string | No | - | Character bonds |
| `flaws` | string | No | - | Character flaws |
| `backstory` | string | No | - | Character backstory |

### State (Usually Set During Play)

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `conditions` | string[] | No | `[]` | Active conditions |
| `statusNotes` | string | No | - | Additional status notes |
| `deathSaveSuccesses` | integer | No | `0` | Death save successes (0-3) |
| `deathSaveFailures` | integer | No | `0` | Death save failures (0-3) |

---

## Common Conditions

- Blinded
- Charmed
- Deafened
- Exhaustion
- Frightened
- Grappled
- Incapacitated
- Invisible
- Paralyzed
- Petrified
- Poisoned
- Prone
- Restrained
- Stunned
- Unconscious

---

## Tips

1. **Field names are case-insensitive** - `"Name"`, `"name"`, and `"NAME"` all work.
2. **Only `name` is required** - All other fields have sensible defaults.
3. **Arrays can be empty** - Use `[]` for empty lists.
4. **Spell slots use string keys** - Even though levels are numbers, use `"1"`, `"2"`, etc.
5. **Don't include computed fields** - Fields like `strengthModifier` are calculated automatically.
