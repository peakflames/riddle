# ==============================================================================
# E2E Test Coverage: tests/Riddle.Web.IntegrationTests/E2ETests/CombatEncounterTests.cs
# 
# Each @HLR-COMBAT-XXX scenario has a corresponding test method:
#   @HLR-COMBAT-001 → HLR_COMBAT_001_DM_starts_combat_from_narrative()
#   @HLR-COMBAT-002 → HLR_COMBAT_002_DM_inputs_initiative_rolls()
#   ... (pattern: replace hyphens with underscores)
# 
# See docs/e2e_testing_philosophy.md for testing patterns.
# ==============================================================================

@phase2 @phase3 @combat @llm @signalr
Feature: Combat Encounter
  As a Dungeon Master
  I want Riddle to help manage tactical combat
  So that combat is fast, accurate, and dramatic

  Background:
    Given I am logged in as a Dungeon Master
    And I have an active campaign instance "Tuesday Night Group"
    And the party contains:
      | Name   | Class   | HP | AC | Initiative |
      | Thorin | Fighter | 12 | 16 | 0          |
      | Elara  | Rogue   | 8  | 14 | 0          |

  # --- Combat Initiation ---

  @HLR-COMBAT-001
  Scenario: DM starts combat from narrative
    Given the party is at "Triboar Trail"
    When I tell Riddle "Goblins attack from the bushes!"
    Then Riddle should initiate combat mode
    And Riddle should request initiative rolls
    And I should see "Roll initiative for the party" in the chat

  @HLR-COMBAT-002
  Scenario: DM inputs initiative rolls
    Given combat is starting
    When I tell Riddle "Thorin rolled 15, Elara rolled 18"
    Then Riddle should set Thorin's initiative to 15
    And Riddle should set Elara's initiative to 18
    And the turn order should be established

  @HLR-COMBAT-003
  Scenario: Combat includes enemy combatants
    Given I start combat with goblins
    When Riddle processes the encounter
    Then enemies should be added to the combat:
      | Name       | HP | AC |
      | Goblin 1   | 7  | 15 |
      | Goblin 2   | 7  | 15 |
    And enemies should have initiative rolled by Riddle

  # --- Turn Order Management ---

  @HLR-COMBAT-004
  Scenario: Turn order displays correctly
    Given combat is active with turn order:
      | Position | Character  | Initiative |
      | 1        | Elara      | 18         |
      | 2        | Goblin 1   | 16         |
      | 3        | Thorin     | 15         |
      | 4        | Goblin 2   | 12         |
    Then the Combat Tracker should show this order
    And the current turn should be highlighted
    And the round number should display as 1

  @HLR-COMBAT-005
  Scenario: DM advances to next turn
    Given combat is active
    And it is "Elara's" turn
    When I tell Riddle "Elara's turn is done"
    Then the current turn should advance to the next combatant
    And all connected clients should see the update

  @HLR-COMBAT-006
  Scenario: Round advances after all turns
    Given combat is active on round 1
    And it is the last combatant's turn
    When that combatant's turn ends
    Then the round number should increase to 2
    And the turn should return to the first combatant

  # --- Attack Resolution ---

  @HLR-COMBAT-007
  Scenario: Damage is applied to enemy
    Given combat is active
    And Thorin hit Goblin 1
    When I tell Riddle "Thorin deals 8 damage"
    Then Goblin 1's HP should decrease by 8
    And the Combat Tracker should update
    And all players should see the HP change

  @HLR-COMBAT-008
  Scenario: Enemy is defeated
    Given combat is active
    And "Goblin 1" has 3 HP remaining
    When I tell Riddle "Thorin deals 5 damage to Goblin 1"
    Then Goblin 1 should be marked as defeated
    And Riddle should narrate the defeat
    And Goblin 1 should be removed from the turn order

  # --- Special Combat Situations ---

  @HLR-COMBAT-009
  Scenario: Surprise round
    Given I tell Riddle "The goblins have surprise"
    When combat begins
    Then the party members should be marked as surprised
    And surprised characters should skip round 1
    And Riddle should explain the surprise rules

  @HLR-COMBAT-010
  Scenario: Player takes damage
    Given combat is active
    And "Thorin" has 12 HP
    When I tell Riddle "The goblin hits Thorin for 5 damage"
    Then Thorin's HP should decrease to 7
    And the Party Tracker should update in real-time
    And Player screens should show the updated HP

  @HLR-COMBAT-011
  Scenario: Condition is applied
    Given combat is active
    When I tell Riddle "Thorin is poisoned"
    Then Thorin should have the "Poisoned" condition
    And the condition should appear in the Party Tracker
    And Riddle should explain the Poisoned condition effects

  # --- Combat Conclusion ---

  @HLR-COMBAT-012
  Scenario: All enemies defeated
    Given combat is active
    And only one enemy remains with 2 HP
    When that enemy is defeated
    Then combat should end
    And Riddle should generate victory narration
    And the Combat Tracker should indicate combat is over

  @HLR-COMBAT-013
  Scenario: DM ends combat manually
    Given combat is active
    When I tell Riddle "End combat, the goblins flee"
    Then combat should end
    And Riddle should narrate the enemy retreat
    And the campaign instance should return to exploration mode

  # --- Real-time Updates ---

  @HLR-COMBAT-014
  Scenario: Player sees combat updates
    Given Player "Alice" is connected to the campaign instance
    And combat is active
    When enemy HP changes
    Then Alice should see the update without refreshing
    And the update should appear within 1 second

  @HLR-COMBAT-015
  Scenario: Turn order syncs across all clients
    Given the DM and 2 Players are connected
    When the turn advances
    Then all clients should see the new current turn
    And the highlight should move to the correct combatant
