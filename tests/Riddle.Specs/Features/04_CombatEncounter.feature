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

  Scenario: DM starts combat from narrative
    Given the party is at "Triboar Trail"
    When I tell Riddle "Goblins attack from the bushes!"
    Then Riddle should initiate combat mode
    And Riddle should request initiative rolls
    And I should see "Roll initiative for the party" in the chat

  Scenario: DM inputs initiative rolls
    Given combat is starting
    When I tell Riddle "Thorin rolled 15, Elara rolled 18"
    Then Riddle should set Thorin's initiative to 15
    And Riddle should set Elara's initiative to 18
    And the turn order should be established

  Scenario: Combat includes enemy combatants
    Given I start combat with goblins
    When Riddle processes the encounter
    Then enemies should be added to the combat:
      | Name       | HP | AC |
      | Goblin 1   | 7  | 15 |
      | Goblin 2   | 7  | 15 |
    And enemies should have initiative rolled by Riddle

  # --- Turn Order Management ---

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

  Scenario: DM advances to next turn
    Given combat is active
    And it is "Elara's" turn
    When I tell Riddle "Elara's turn is done"
    Then the current turn should advance to the next combatant
    And all connected clients should see the update

  Scenario: Round advances after all turns
    Given combat is active on round 1
    And it is the last combatant's turn
    When that combatant's turn ends
    Then the round number should increase to 2
    And the turn should return to the first combatant

  # --- Attack Resolution ---

  Scenario: Player attacks and hits
    Given combat is active
    And it is "Thorin's" turn
    When I tell Riddle "Thorin attacks Goblin 1. He rolled 17."
    And Goblin 1 has AC 15
    Then Riddle should confirm the attack hits
    And Riddle should request a damage roll

  Scenario: Player attacks and misses
    Given combat is active
    And it is "Thorin's" turn
    When I tell Riddle "Thorin attacks Goblin 1. He rolled 12."
    And Goblin 1 has AC 15
    Then Riddle should report the attack misses
    And no damage should be applied

  Scenario: Damage is applied to enemy
    Given combat is active
    And Thorin hit Goblin 1
    When I tell Riddle "Thorin deals 8 damage"
    Then Goblin 1's HP should decrease by 8
    And the Combat Tracker should update
    And all players should see the HP change

  Scenario: Enemy is defeated
    Given combat is active
    And "Goblin 1" has 3 HP remaining
    When I tell Riddle "Thorin deals 5 damage to Goblin 1"
    Then Goblin 1 should be marked as defeated
    And Riddle should narrate the defeat
    And Goblin 1 should be removed from the turn order

  # --- Special Combat Situations ---

  Scenario: Surprise round
    Given I tell Riddle "The goblins have surprise"
    When combat begins
    Then the party members should be marked as surprised
    And surprised characters should skip round 1
    And Riddle should explain the surprise rules

  Scenario: Player takes damage
    Given combat is active
    And "Thorin" has 12 HP
    When I tell Riddle "The goblin hits Thorin for 5 damage"
    Then Thorin's HP should decrease to 7
    And the Party Tracker should update in real-time
    And Player screens should show the updated HP

  Scenario: Condition is applied
    Given combat is active
    When I tell Riddle "Thorin is poisoned"
    Then Thorin should have the "Poisoned" condition
    And the condition should appear in the Party Tracker
    And Riddle should explain the Poisoned condition effects

  # --- Combat Conclusion ---

  Scenario: All enemies defeated
    Given combat is active
    And only one enemy remains with 2 HP
    When that enemy is defeated
    Then combat should end
    And Riddle should generate victory narration
    And the Combat Tracker should indicate combat is over

  Scenario: DM ends combat manually
    Given combat is active
    When I tell Riddle "End combat, the goblins flee"
    Then combat should end
    And Riddle should narrate the enemy retreat
    And the campaign instance should return to exploration mode

  # --- Real-time Updates ---

  Scenario: Player sees combat updates
    Given Player "Alice" is connected to the campaign instance
    And combat is active
    When enemy HP changes
    Then Alice should see the update without refreshing
    And the update should appear within 1 second

  Scenario: Turn order syncs across all clients
    Given the DM and 2 Players are connected
    When the turn advances
    Then all clients should see the new current turn
    And the highlight should move to the correct combatant
