# Phase 3 Implementation Plan: Party Management & Character Creation

**Version:** 1.0  
**Date:** December 28, 2025  
**Status:** Ready for Implementation  
**Phase:** Party Management & Character Creation (Week 3)

---

## [Overview]

Phase 3 builds the character management and multiplayer infrastructure for Project Riddle. This phase expands the Character model to support full D&D 5e attributes, implements invite links for remote players, and creates the UI flows for DM character creation and player character claiming.

**Key Objectives:**
1. Expand Character model with full D&D 5e fields (ability scores, race, class, proficiencies, spells, equipment)
2. Add invite code system to CampaignInstance
3. Create character management UI (list, form, card components)
4. Implement player join flow (join page, character claiming)
5. Build Player Dashboard for claimed characters
6. Add real-time notifications for character claims via SignalR

**Success Criteria:**
- DM can create characters with quick entry (name, class, HP, AC) or full D&D 5e details
- DM can generate and share invite links
- Players can join via invite link and claim unclaimed characters
- Claimed characters link to player accounts
- Player Dashboard shows character details and game state
- Real-time updates when players join/claim characters

**Dependencies:**
- Phase 2 complete (v0.4.1) ✅
- Google OAuth authentication working ✅
- CampaignInstance and Character models exist ✅

**BDD Feature Reference:**
- `tests/Riddle.Specs/Features/08_PartyManagement.feature`

---

## [Objectives Breakdown]

### Objective 1: Expand Character Model
**Scope:** Add D&D 5e fields to Character class
**Estimated Effort:** Medium
**Files:** `Models/Character.cs`, EF Core migrations

### Objective 2: Add Invite Code System
**Scope:** Add InviteCode to CampaignInstance, generate/display invite links
**Estimated Effort:** Small
**Files:** `Models/CampaignInstance.cs`, `Services/CampaignService.cs`, migrations

### Objective 3: Character Management UI
**Scope:** CharacterList, CharacterForm, CharacterCard components
**Estimated Effort:** Large
**Files:** `Components/Characters/*.razor`, `Pages/DM/Campaign.razor`

### Objective 4: Player Join Flow
**Scope:** Join page, character claiming, authentication redirect
**Estimated Effort:** Medium
**Files:** `Pages/Join.razor`, `Services/CharacterService.cs`

### Objective 5: Player Dashboard
**Scope:** Player-facing dashboard with character card and game state
**Estimated Effort:** Medium
**Files:** `Pages/Player/Dashboard.razor`, `Components/Player/*.razor`

### Objective 6: Real-time Notifications
**Scope:** SignalR events for character claims, player connections
**Estimated Effort:** Medium
**Files:** `Hubs/GameHub.cs`, `Services/NotificationService.cs`

---

## [Types]

### Expanded Character Model

```csharp
namespace Riddle.Web.Models;

/// <summary>
/// Represents a character in the game (PC or NPC)
/// Stored as JSON within CampaignInstance.PartyStateJson
/// </summary>
public class Character
{
    // === Identity ===
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    public string Name { get; set; } = null!;
    public string Type { get; set; } = "PC"; // "PC" or "NPC"
    
    // === Core D&D 5e Fields ===
    public string? Race { get; set; }           // e.g., "High Elf", "Human", "Dwarf"
    public string? Class { get; set; }          // e.g., "Fighter", "Wizard", "Rogue"
    public int Level { get; set; } = 1;
    public string? Background { get; set; }     // e.g., "Sage", "Soldier", "Criminal"
    public string? Alignment { get; set; }      // e.g., "Neutral Good", "Chaotic Evil"
    
    // === Ability Scores ===
    public int Strength { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Constitution { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    public int Wisdom { get; set; } = 10;
    public int Charisma { get; set; } = 10;
    
    // === Combat Stats ===
    public int ArmorClass { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int TemporaryHp { get; set; }
    public int Initiative { get; set; }
    public string Speed { get; set; } = "30 ft";
    
    // === Skills & Proficiencies ===
    public int PassivePerception { get; set; }
    public List<string> SavingThrowProficiencies { get; set; } = [];
    public List<string> SkillProficiencies { get; set; } = [];      // e.g., ["Arcana", "History"]
    public List<string> ToolProficiencies { get; set; } = [];
    public List<string> Languages { get; set; } = ["Common"];
    
    // === Spellcasting (Optional) ===
    public bool IsSpellcaster { get; set; }
    public string? SpellcastingAbility { get; set; }   // "Intelligence", "Wisdom", "Charisma"
    public int? SpellSaveDC { get; set; }
    public int? SpellAttackBonus { get; set; }
    public List<string> Cantrips { get; set; } = [];
    public List<string> SpellsKnown { get; set; } = [];
    public Dictionary<int, int> SpellSlots { get; set; } = new();   // Level -> Slots
    public Dictionary<int, int> SpellSlotsUsed { get; set; } = new();
    
    // === Equipment & Inventory ===
    public List<string> Equipment { get; set; } = [];
    public List<string> Weapons { get; set; } = [];
    public int CopperPieces { get; set; }
    public int SilverPieces { get; set; }
    public int GoldPieces { get; set; }
    public int PlatinumPieces { get; set; }
    
    // === Roleplay ===
    public string? PersonalityTraits { get; set; }
    public string? Ideals { get; set; }
    public string? Bonds { get; set; }
    public string? Flaws { get; set; }
    public string? Backstory { get; set; }
    
    // === State ===
    public List<string> Conditions { get; set; } = [];
    public string? StatusNotes { get; set; }
    public int DeathSaveSuccesses { get; set; }
    public int DeathSaveFailures { get; set; }
    
    // === Player Linking ===
    public string? PlayerId { get; set; }       // User ID of controlling player
    public string? PlayerName { get; set; }     // Display name of player
    public bool IsClaimed => !string.IsNullOrEmpty(PlayerId);
    
    // === Computed Properties ===
    public int StrengthModifier => CalculateModifier(Strength);
    public int DexterityModifier => CalculateModifier(Dexterity);
    public int ConstitutionModifier => CalculateModifier(Constitution);
    public int IntelligenceModifier => CalculateModifier(Intelligence);
    public int WisdomModifier => CalculateModifier(Wisdom);
    public int CharismaModifier => CalculateModifier(Charisma);
    
    public string DisplayLevel => $"{Class ?? "Unknown"} L{Level}";
    public string DisplayRaceClass => $"{Race ?? "Unknown"} {Class ?? "Unknown"}";
    
    private static int CalculateModifier(int score) => (score - 10) / 2;
}
```

### CampaignInstance Extension

```csharp
// Add to CampaignInstance.cs
public class CampaignInstance
{
    // ... existing fields ...
    
    /// <summary>
    /// Unique invite code for players to join this campaign
    /// Generated on campaign creation, can be regenerated by DM
    /// </summary>
    public string InviteCode { get; set; } = GenerateInviteCode();
    
    private static string GenerateInviteCode()
    {
        // Generate a 6-character alphanumeric code
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude confusing chars
        var random = Random.Shared;
        return new string(Enumerable.Range(0, 6).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
    
    /// <summary>
    /// Regenerate the invite code (invalidates old links)
    /// </summary>
    public void RegenerateInviteCode()
    {
        InviteCode = GenerateInviteCode();
    }
}
```

### Service Interfaces

```csharp
namespace Riddle.Web.Services;

/// <summary>
/// Service for character-specific operations
/// </summary>
public interface ICharacterService
{
    /// <summary>
    /// Add a new character to a campaign's party
    /// </summary>
    Task<Character> AddCharacterAsync(Guid campaignId, Character character, CancellationToken ct = default);
    
    /// <summary>
    /// Update an existing character
    /// </summary>
    Task<Character> UpdateCharacterAsync(Guid campaignId, Character character, CancellationToken ct = default);
    
    /// <summary>
    /// Remove a character from a campaign
    /// </summary>
    Task RemoveCharacterAsync(Guid campaignId, string characterId, CancellationToken ct = default);
    
    /// <summary>
    /// Claim a character for a player
    /// </summary>
    Task<Character> ClaimCharacterAsync(Guid campaignId, string characterId, string playerId, string playerName, CancellationToken ct = default);
    
    /// <summary>
    /// Release a character claim (DM only)
    /// </summary>
    Task<Character> ReleaseCharacterClaimAsync(Guid campaignId, string characterId, CancellationToken ct = default);
    
    /// <summary>
    /// Get unclaimed characters in a campaign
    /// </summary>
    Task<List<Character>> GetUnclaimedCharactersAsync(Guid campaignId, CancellationToken ct = default);
    
    /// <summary>
    /// Get characters claimed by a specific player
    /// </summary>
    Task<List<Character>> GetPlayerCharactersAsync(Guid campaignId, string playerId, CancellationToken ct = default);
}

/// <summary>
/// Extended campaign service for invite functionality
/// </summary>
public interface ICampaignService
{
    // ... existing methods ...
    
    /// <summary>
    /// Get a campaign by its invite code
    /// </summary>
    Task<CampaignInstance?> GetByInviteCodeAsync(string inviteCode, CancellationToken ct = default);
    
    /// <summary>
    /// Regenerate the invite code for a campaign
    /// </summary>
    Task<string> RegenerateInviteCodeAsync(Guid campaignId, CancellationToken ct = default);
}
```

---

## [Files]

### New Files to Create

#### Models
| File | Description |
|------|-------------|
| (none - Character model updated in place) | |

#### Services
| File | Description |
|------|-------------|
| `src/Riddle.Web/Services/ICharacterService.cs` | Character service interface |
| `src/Riddle.Web/Services/CharacterService.cs` | Character service implementation |

#### Components - Characters
| File | Description |
|------|-------------|
| `src/Riddle.Web/Components/Characters/CharacterList.razor` | Party roster display |
| `src/Riddle.Web/Components/Characters/CharacterCard.razor` | Individual character card |
| `src/Riddle.Web/Components/Characters/CharacterForm.razor` | Add/edit character form |
| `src/Riddle.Web/Components/Characters/QuickEntryForm.razor` | Simplified quick entry |
| `src/Riddle.Web/Components/Characters/FullEntryForm.razor` | Full D&D 5e entry |
| `src/Riddle.Web/Components/Characters/AbilityScoreInput.razor` | Ability score entry component |
| `src/Riddle.Web/Components/Characters/SpellSelector.razor` | Spell selection component |

#### Components - Shared
| File | Description |
|------|-------------|
| `src/Riddle.Web/Components/Shared/InviteLinkModal.razor` | Invite link display/copy modal |
| `src/Riddle.Web/Components/Shared/ConfirmationModal.razor` | Confirmation dialog |

#### Pages
| File | Description |
|------|-------------|
| `src/Riddle.Web/Components/Pages/Join.razor` | Player join page `/join/{InviteCode}` |
| `src/Riddle.Web/Components/Pages/Player/Dashboard.razor` | Player dashboard |

### Files to Modify

| File | Changes |
|------|---------|
| `src/Riddle.Web/Models/Character.cs` | Add D&D 5e fields |
| `src/Riddle.Web/Models/CampaignInstance.cs` | Add InviteCode field |
| `src/Riddle.Web/Services/CampaignService.cs` | Add invite code methods |
| `src/Riddle.Web/Services/ICampaignService.cs` | Add invite code interface methods |
| `src/Riddle.Web/Components/Pages/DM/Campaign.razor` | Add party management panel |
| `src/Riddle.Web/Program.cs` | Register CharacterService |
| `src/Riddle.Web/Data/RiddleDbContext.cs` | Any index changes if needed |

---

## [Implementation Order]

### Objective 1: Expand Character Model (Day 1)

**Step 1.1: Update Character.cs**
- Add all D&D 5e fields (ability scores, race, class, proficiencies, etc.)
- Add computed properties for modifiers
- Add IsClaimed computed property
- Maintain backward compatibility (existing characters still work)

**Step 1.2: Test Serialization**
- Verify JSON serialization/deserialization works
- Test with existing campaign data
- Ensure no data loss on migration

**Verification:**
- [ ] Build passes
- [ ] Existing campaigns load without errors
- [ ] New character fields serialize correctly

### Objective 2: Add Invite Code System (Day 1)

**Step 2.1: Update CampaignInstance.cs**
- Add `InviteCode` property with default generator
- Add `RegenerateInviteCode()` method

**Step 2.2: Update CampaignService**
- Add `GetByInviteCodeAsync(string code)` method
- Add `RegenerateInviteCodeAsync(Guid campaignId)` method

**Step 2.3: Create Migration**
- Generate EF Core migration for InviteCode column
- Apply migration

**Verification:**
- [ ] New campaigns get auto-generated invite codes
- [ ] Existing campaigns work (backfill codes)
- [ ] Can look up campaign by invite code

### Objective 3: Character Management UI (Days 2-3)

**Step 3.1: Create CharacterCard.razor**
- Display character info (name, class, level, HP)
- Show claimed/unclaimed status
- Edit and Remove action buttons

**Step 3.2: Create CharacterList.razor**
- Display party roster using CharacterCard
- "Add Character" button
- Empty state when no characters

**Step 3.3: Create QuickEntryForm.razor**
- Minimal fields: Name, Class, Max HP, AC
- Save and Cancel buttons

**Step 3.4: Create FullEntryForm.razor**
- All D&D 5e fields in organized sections
- Ability scores with modifier calculation
- Proficiency checkboxes
- Spell selection (if spellcaster)

**Step 3.5: Create CharacterForm.razor (Wrapper)**
- Toggle between Quick Entry and Full Entry modes
- Handle save/cancel callbacks

**Step 3.6: Update Campaign.razor**
- Add "Party" panel to sidebar
- Integrate CharacterList
- Add character modal workflow

**Verification:**
- [ ] Can add character via Quick Entry
- [ ] Can add character via Full Entry
- [ ] Can edit existing character
- [ ] Can remove character
- [ ] Characters persist to database

### Objective 4: Invite Link Modal (Day 3)

**Step 4.1: Create InviteLinkModal.razor**
- Display invite code and full URL
- "Copy Link" button with clipboard API
- "Regenerate" button (with confirmation)

**Step 4.2: Add to Campaign.razor**
- "Invite Players" button in header
- Open modal on click

**Verification:**
- [ ] Invite link displays correctly
- [ ] Copy to clipboard works
- [ ] Regenerate creates new code

### Objective 5: Player Join Flow (Day 4)

**Step 5.1: Create ICharacterService/CharacterService**
- Implement claim/release methods
- Implement get unclaimed/player characters

**Step 5.2: Create Join.razor Page**
- Route: `/join/{InviteCode}`
- Load campaign by invite code
- Display campaign name and available characters
- Character claim buttons
- Error handling for invalid codes

**Step 5.3: Handle Authentication Redirect**
- If not logged in, redirect to login
- After login, return to join page
- Store invite code in return URL

**Step 5.4: Implement Character Claiming**
- Link character to player account
- Redirect to Player Dashboard after claim

**Verification:**
- [ ] Join page loads for valid invite code
- [ ] Error shown for invalid code
- [ ] Unclaimed characters shown
- [ ] Can claim character
- [ ] Claimed character linked to account

### Objective 6: Player Dashboard (Day 5)

**Step 6.1: Create PlayerDashboard.razor**
- Route: `/play/{CampaignId}`
- Load player's characters in campaign
- Select character if multiple
- Display character card with full details

**Step 6.2: Create PlayerCharacterCard.razor**
- Full character sheet display
- HP bar, conditions, spells
- Read-only (player cannot edit)

**Step 6.3: Add Game State Panels**
- Read Aloud Text (if any)
- Active Player Choices (clickable)
- Scene Image (if any)

**Verification:**
- [ ] Player Dashboard loads for claimed character
- [ ] Character details display correctly
- [ ] Game state (RATB, choices) visible
- [ ] Player cannot access DM controls

### Objective 7: Real-time Notifications (Day 5)

**Step 7.1: Add SignalR Events to GameHub**
- `CharacterClaimed(campaignId, characterId, playerId, playerName)`
- `PlayerConnected(campaignId, characterId)`
- `PlayerDisconnected(campaignId, characterId)`

**Step 7.2: Wire CharacterService to Hub**
- Broadcast CharacterClaimed when player claims
- Update character list in real-time on DM dashboard

**Step 7.3: Add Connection Status Tracking**
- Track connected players in campaign
- Show online/offline status on DM dashboard

**Verification:**
- [ ] DM sees notification when player claims character
- [ ] Character list updates in real-time
- [ ] Online/offline status shows correctly

---

## [Testing]

### Manual Testing Checklist

#### Character Model (Objective 1)
- [ ] Build passes with new Character fields
- [ ] Existing campaigns load without errors
- [ ] New characters save all D&D 5e fields
- [ ] Computed properties (modifiers) calculate correctly

#### Invite Code System (Objective 2)
- [ ] New campaigns auto-generate invite codes
- [ ] Can look up campaign by invite code
- [ ] Can regenerate invite code
- [ ] Old invite codes become invalid after regeneration

#### Character Management UI (Objective 3)
- [ ] CharacterList displays all party members
- [ ] Can add character via Quick Entry
- [ ] Can add character via Full Entry
- [ ] Can edit existing character
- [ ] Can remove character with confirmation
- [ ] Characters persist after page refresh

#### Invite Link Modal (Objective 4)
- [ ] Modal opens from "Invite Players" button
- [ ] Displays correct invite URL
- [ ] Copy button copies to clipboard
- [ ] Regenerate shows confirmation dialog

#### Player Join Flow (Objective 5)
- [ ] `/join/{code}` loads for valid code
- [ ] Invalid code shows error message
- [ ] Unauthenticated users redirected to login
- [ ] After login, returns to join page
- [ ] Can see unclaimed characters
- [ ] Can claim a character
- [ ] After claim, redirected to player dashboard

#### Player Dashboard (Objective 6)
- [ ] Dashboard loads for player with claimed character
- [ ] Character card shows all details
- [ ] HP bar displays correctly
- [ ] Conditions display as badges
- [ ] Game state panels visible (RATB, choices, image)
- [ ] Cannot access DM-only controls

#### Real-time Notifications (Objective 7)
- [ ] DM sees toast when player claims character
- [ ] Character card updates to show player name
- [ ] Online/offline indicator works

---

## [Phase 3 Completion Checklist]

### Objective 1: Character Model Expansion
- [ ] Character.cs updated with D&D 5e fields
- [ ] Computed properties for ability modifiers
- [ ] IsClaimed property
- [ ] Backward compatible with existing data
- [ ] Build passes

### Objective 2: Invite Code System
- [ ] InviteCode field added to CampaignInstance
- [ ] GenerateInviteCode() implementation
- [ ] RegenerateInviteCode() method
- [ ] GetByInviteCodeAsync() in CampaignService
- [ ] EF Core migration created and applied

### Objective 3: Character Management UI
- [ ] CharacterCard.razor created
- [ ] CharacterList.razor created
- [ ] QuickEntryForm.razor created
- [ ] FullEntryForm.razor created
- [ ] CharacterForm.razor wrapper created
- [ ] Campaign.razor updated with party panel
- [ ] Add/Edit/Remove workflows complete

### Objective 4: Invite Link Modal
- [ ] InviteLinkModal.razor created
- [ ] Clipboard copy functionality
- [ ] Regenerate with confirmation
- [ ] Integrated into Campaign.razor

### Objective 5: Player Join Flow
- [ ] ICharacterService interface created
- [ ] CharacterService implementation
- [ ] Join.razor page created
- [ ] Authentication redirect handling
- [ ] Character claiming workflow
- [ ] Redirect to Player Dashboard

### Objective 6: Player Dashboard
- [ ] Dashboard.razor created
- [ ] PlayerCharacterCard component
- [ ] Game state panels (RATB, choices, image)
- [ ] Character selector for multi-character players
- [ ] Read-only view (no DM controls)

### Objective 7: Real-time Notifications
- [ ] SignalR events in GameHub
- [ ] CharacterService broadcasts claims
- [ ] Toast notifications on DM dashboard
- [ ] Online/offline status tracking

### Version Bump
- [ ] Version updated to 0.5.0 in Riddle.Web.csproj
- [ ] CHANGELOG.md updated with Phase 3 changes
- [ ] Git tag v0.5.0 created

---

## [Risk Mitigation]

| Risk | Mitigation |
|------|------------|
| Character data migration | New fields have defaults, existing data unaffected |
| Invite code collisions | 6-char alphanumeric = 887M combinations, collision unlikely |
| SignalR disconnections | Implement reconnection logic in client |
| Complex form validation | Start with Quick Entry, add Full Entry incrementally |
| Player claiming race condition | Use database transaction for claim operation |

---

## [UI Mockups]

### DM Dashboard - Party Panel
```
┌─────────────────────────────────────┐
│ Party                    [+ Add] [👥]│
├─────────────────────────────────────┤
│ ┌─────────────────────────────────┐ │
│ │ 🧙 Elara         Wizard L3     │ │
│ │ HP: 15/18  AC: 12             │ │
│ │ 👤 Alice (Online)    [✏️] [🗑️] │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ ⚔️ Thorin        Fighter L2    │ │
│ │ HP: 12/12  AC: 16             │ │
│ │ ⚪ Unclaimed         [✏️] [🗑️] │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ 🗡️ Shade         Rogue L2      │ │
│ │ HP: 10/10  AC: 14             │ │
│ │ 👤 Bob (Offline)     [✏️] [🗑️] │ │
│ └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

### Invite Link Modal
```
┌─────────────────────────────────────────┐
│ Invite Players                      [X] │
├─────────────────────────────────────────┤
│                                         │
│  Share this link with your players:     │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │ https://riddle.app/join/ABC123   │  │
│  └───────────────────────────────────┘  │
│                                         │
│  [📋 Copy Link]   [🔄 Regenerate]       │
│                                         │
│  ⚠️ Regenerating will invalidate the    │
│     current link.                       │
│                                         │
└─────────────────────────────────────────┘
```

### Player Join Page
```
┌─────────────────────────────────────────┐
│              🎲 Project Riddle          │
├─────────────────────────────────────────┤
│                                         │
│    Join "Tuesday Night Group"           │
│    Lost Mine of Phandelver              │
│                                         │
│    Choose your character:               │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │ ⚔️ Thorin - Fighter L2           │  │
│  │    HP: 12  AC: 16                │  │
│  │                      [Claim]     │  │
│  └───────────────────────────────────┘  │
│  ┌───────────────────────────────────┐  │
│  │ 🗡️ Shade - Rogue L2              │  │
│  │    HP: 10  AC: 14                │  │
│  │                      [Claim]     │  │
│  └───────────────────────────────────┘  │
│  ┌───────────────────────────────────┐  │
│  │ 🧙 Elara - Wizard L3             │  │
│  │    Claimed by Alice              │  │
│  └───────────────────────────────────┘  │
│                                         │
└─────────────────────────────────────────┘
```

---

## Next Phase Preview

**Phase 4: SignalR & Real-time (Week 4)**
- Full GameHub implementation for multi-client sync
- Combat tracker with real-time turn order
- Player choice submission flow
- Read Aloud Text Box real-time updates
- Scene image synchronization
