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
  "name": "Henry 'The Chosen One' Ashford",
  "type": "PC",
  "race": "Human (Variant)",
  "class": "Wizard",
  "level": 1,
  "background": "Urchin",
  "alignment": "Chaotic Good",
  "strength": 10,
  "dexterity": 14,
  "constitution": 12,
  "intelligence": 15,
  "wisdom": 10,
  "charisma": 14,
  "armorClass": 12,
  "maxHp": 8,
  "currentHp": 8,
  "temporaryHp": 0,
  "initiative": 2,
  "speed": "30 ft",
  "passivePerception": 10,
  "savingThrowProficiencies": ["Intelligence", "Wisdom"],
  "skillProficiencies": ["Arcana", "Investigation", "Sleight of Hand", "Stealth"],
  "toolProficiencies": ["Disguise Kit", "Thieves' Tools"],
  "languages": ["Common", "Draconic"],
  "isSpellcaster": true,
  "spellcastingAbility": "Intelligence",
  "spellSaveDC": 12,
  "spellAttackBonus": 4,
  "cantrips": ["Fire Bolt", "Light", "Mage Hand"],
  "spellsKnown": ["Shield", "Magic Missile", "Feather Fall", "Detect Magic", "Disguise Self", "Mage Armor"],
  "spellSlots": { "1": 2 },
  "equipment": [
    "Spellbook (leather-bound, gifted by headmaster)",
    "Holly wand with phoenix feather core",
    "Round spectacles",
    "School robes (black with crimson trim)",
    "Pet snowy owl named Alba",
    "Father's old cloak (worn but treasured)",
    "Cracked hand mirror (mother's keepsake)",
    "Bag of sweets from train ride",
    "Letter of acceptance to the Academy"
  ],
  "weapons": ["Dagger (hidden in boot)"],
  "silverPieces": 11,
  "goldPieces": 7,
  "personalityTraits": "I am fiercely loyal to my friends, sometimes to a fault. Despite everyone telling me I'm special, I never feel like I truly belong.",
  "ideals": "Protection. Those with power have a duty to protect those who cannot protect themselves.",
  "bonds": "I never knew my parents, who died protecting me from a dark sorcerer. I carry their memory in everything I do.",
  "flaws": "I have a 'saving people thing' - I rush headlong into danger when my friends are threatened, consequences be damned.",
  "backstory": "Orphaned as an infant when a dark sorcerer murdered his parents, Henry was placed with his cruel relatives who kept him in a cupboard under the stairs and treated him as a servant. He always knew he was different - strange things happened around him when he was angry or scared. On his eleventh birthday, a letter arrived revealing the truth: his parents were powerful wizards, and he had been accepted to a prestigious academy of magic. Now he must navigate a world where he is famous for something he cannot remember, while the shadow of the dark lord who killed his parents looms ever closer."
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
