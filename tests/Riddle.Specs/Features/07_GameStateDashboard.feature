@phase4 @ui @dm
Feature: Game State Dashboard
  As a Dungeon Master
  I want to view and manage all game state from my dashboard
  So that I have complete control over the session

  Background:
    Given I am logged in as a Dungeon Master
    And I have an active campaign instance "Tuesday Night Group"
    And I am on the DM Dashboard

  # --- Party Tracker ---

  Scenario: DM sees full party status
    Given the party contains:
      | Name   | Class   | Current HP | Max HP | AC | Conditions |
      | Thorin | Fighter | 7          | 12     | 16 | Poisoned   |
      | Elara  | Rogue   | 8          | 8      | 14 |            |
    When I view the Party Tracker panel
    Then I should see both characters listed
    And I should see HP bars for each character
    And I should see Thorin's "Poisoned" condition badge
    And Thorin's HP bar should be in the warning color

  Scenario: HP bar colors reflect health status
    Given characters with varying HP:
      | Name    | Current HP | Max HP | Expected Color |
      | Healthy | 10         | 10     | Green          |
      | Wounded | 6          | 10     | Yellow         |
      | Hurt    | 3          | 10     | Orange         |
      | Critical| 1          | 10     | Red            |
    When I view the Party Tracker
    Then each character's HP bar should match the expected color

  Scenario: DM sees NPC/Enemy stats in Party Tracker
    Given combat is active with enemies:
      | Name       | HP | AC | Conditions |
      | Goblin Boss| 15 | 15 | None       |
      | Goblin     | 4  | 15 | Prone      |
    When I view the Party Tracker
    Then I should see enemy HP (DM-only information)
    And I should see the Goblin's "Prone" condition

  # --- Manual HP Override ---

  Scenario: DM manually heals a character
    Given "Thorin" has 5 HP
    When I click the "Heal" button for Thorin
    And I enter 3 as the healing amount
    And I confirm the change
    Then Thorin's HP should increase to 8
    And the Party Tracker should update
    And players should see the HP change

  Scenario: DM manually damages a character
    Given "Thorin" has 10 HP
    When I click the "Damage" button for Thorin
    And I enter 4 as the damage amount
    And I confirm the change
    Then Thorin's HP should decrease to 6
    And all connected clients should see the update

  Scenario: DM adds a condition manually
    Given "Elara" has no conditions
    When I click "Add Condition" for Elara
    And I select "Stunned" from the condition list
    Then Elara should have the "Stunned" condition
    And the condition badge should appear immediately

  Scenario: DM removes a condition manually
    Given "Thorin" has the "Poisoned" condition
    When I click the "X" on Thorin's "Poisoned" badge
    Then the condition should be removed
    And Thorin's character card should update

  # --- Combat Tracker ---

  Scenario: Combat Tracker displays turn order
    Given combat is active with turn order:
      | Position | Character  | Initiative | HP  |
      | 1        | Elara      | 18         | 8   |
      | 2        | Goblin Boss| 16         | 21  |
      | 3        | Thorin     | 15         | 12  |
      | 4        | Goblin     | 12         | 7   |
    When I view the Combat Tracker
    Then I should see all combatants in initiative order
    And the current turn should be highlighted
    And I should see initiative values for each combatant

  Scenario: Combat Tracker shows round number
    Given combat is active on round 3
    When I view the Combat Tracker
    Then I should see "Round 3" displayed
    And the round counter should be prominent

  Scenario: DM advances turn manually
    Given combat is active
    And it is Elara's turn
    When I click "Next Turn"
    Then the turn should advance to the next combatant
    And the highlight should move accordingly
    And all players should see the turn change

  Scenario: DM can force end combat
    Given combat is active
    When I click "End Combat"
    And I confirm the action
    Then combat should end
    And the Combat Tracker should indicate combat is over
    And the campaign instance should return to exploration mode

  # --- Quest Log ---

  Scenario: DM views active quests
    Given the campaign instance has quests:
      | Title               | State     | Main Story |
      | Rescue Sildar       | Active    | Yes        |
      | Find the Lost Mine  | Active    | Yes        |
      | Collect Herbs       | Active    | No         |
      | Clear the Goblins   | Completed | No         |
    When I view the Quest Log panel
    Then I should see active quests listed
    And main story quests should be visually distinguished
    And completed quests should be collapsed or greyed

  Scenario: DM marks quest as completed
    Given "Rescue Sildar" is an active quest
    When I click "Complete" on "Rescue Sildar"
    Then the quest should be marked as completed
    And Riddle should be notified of the update
    And the quest should move to the completed section

  Scenario: DM adds a new quest
    When I click "Add Quest"
    And I enter:
      | Field       | Value              |
      | Title       | Investigate Ruins  |
      | Main Story  | No                 |
      | Objectives  | Find the entrance  |
    And I save the quest
    Then "Investigate Ruins" should appear in the Quest Log
    And the quest should be marked as Active

  # --- Campaign Instance Information ---

  Scenario: DM sees current location
    Given the campaign instance location is "Cragmaw Hideout - Main Cave"
    When I view the campaign instance info panel
    Then I should see the current location displayed
    And I should see the chapter name

  Scenario: DM updates current location
    Given the party is at "Cragmaw Hideout - Entrance"
    When I update the location to "Cragmaw Hideout - Bridge"
    Then the location should update in the display
    And Riddle should be aware of the new location

  # --- Dashboard Layout ---

  Scenario: Dashboard panels are resizable
    When I drag the border between panels
    Then the panels should resize accordingly
    And my layout preference should be remembered

  Scenario: Dashboard is responsive on smaller screens
    Given I am using a tablet-sized screen
    When I view the DM Dashboard
    Then panels should stack appropriately
    And all functionality should remain accessible

  # --- Real-time Sync Indicator ---

  Scenario: DM sees connection status
    Given I am connected to the session
    Then I should see a connection indicator
    And the indicator should show "Connected"
    And player count should be displayed

  Scenario: DM sees when players connect
    Given 1 player is connected
    When Player "Alice" joins the session
    Then the player count should update to 2
    And I should see a notification that Alice joined
