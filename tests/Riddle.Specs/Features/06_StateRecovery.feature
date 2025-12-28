@phase2 @llm @state
Feature: State Recovery
  As a Dungeon Master
  I want the system to recover game context automatically
  So that starting a new conversation doesn't lose progress

  Background:
    Given I am logged in as a Dungeon Master
    And I have an active campaign instance "Tuesday Night Group"

  # --- Context Restoration ---

  Scenario: New conversation restores party state
    Given the campaign instance has the following party:
      | Name   | Class   | Current HP | Max HP | Conditions |
      | Thorin | Fighter | 7          | 12     | Poisoned   |
      | Elara  | Rogue   | 8          | 8      |            |
    When I start a new conversation with Riddle
    Then Riddle should call the get_game_state tool
    And Riddle should know Thorin has 7 HP
    And Riddle should know Thorin is Poisoned
    And Riddle should know Elara is at full health

  Scenario: New conversation restores location context
    Given the campaign instance's current location is "Cragmaw Hideout"
    And the campaign instance's current chapter is "Chapter 1"
    When I start a new conversation with Riddle
    Then Riddle should know the party is at "Cragmaw Hideout"
    And Riddle should provide location-appropriate context

  Scenario: New conversation restores quest state
    Given the campaign instance has active quests:
      | Title               | State     |
      | Find Cragmaw Hideout | Completed |
      | Rescue Sildar        | Active    |
    When I start a new conversation with Riddle
    Then Riddle should know which quests are active
    And Riddle should reference "Rescue Sildar" as the current objective

  Scenario: New conversation restores combat state
    Given combat was active when the last conversation ended
    And the turn order was:
      | Position | Character | Initiative |
      | 1        | Elara     | 18         |
      | 2        | Goblin    | 15         |
      | 3        | Thorin    | 12         |
    And it was the Goblin's turn on round 2
    When I start a new conversation with Riddle
    Then Riddle should know combat is active
    And Riddle should know it's round 2
    And Riddle should know it's the Goblin's turn

  # --- Narrative Summary Recovery ---

  Scenario: Riddle uses narrative summary for context
    Given the narrative log contains:
      | Entry                                              |
      | Party ambushed on Triboar Trail                    |
      | Defeated 4 goblins, 1 escaped                      |
      | Found trail leading to Cragmaw Hideout             |
      | Entered cave, triggered flood trap                 |
    And the last narrative summary is "The party survived a goblin ambush and tracked the goblins to Cragmaw Hideout. After narrowly avoiding a flood trap, they are now inside the cave system."
    When I start a new conversation with Riddle
    Then Riddle should use the narrative summary for context
    And Riddle should understand the party's journey
    And Riddle should not ask "where did we leave off?"

  Scenario: Riddle provides welcome back message
    Given the campaign instance has existing progress
    When I start a new conversation with Riddle
    Then Riddle should greet me with context
    And the greeting should reference the current situation
    And Riddle should be ready to continue the adventure

  # --- Preferences Restoration ---

  Scenario: Riddle respects saved party preferences
    Given the campaign instance has party preferences:
      | Setting        | Value       |
      | Combat Focus   | Low         |
      | Roleplay Focus | High        |
      | Tone           | Adventurous |
    When I start a new conversation with Riddle
    And I ask for a scene description
    Then Riddle should emphasize roleplay elements
    And the tone should match "Adventurous"

  # --- Play Session Timeout Recovery ---

  Scenario: Campaign instance resumes after browser close
    Given I closed my browser mid-play session
    And the campaign instance state was saved
    When I return and open the campaign instance
    Then all game state should be intact
    And I can continue where I left off
    And player positions should be preserved

  Scenario: Campaign instance resumes after connection timeout
    Given my connection timed out during gameplay
    When I reconnect to the campaign instance
    Then the game state should match before timeout
    And any pending actions should be recoverable

  # --- Multi-Campaign Support ---

  Scenario: Switching between campaign instances maintains separate state
    Given I have two campaign instances:
      | Campaign Name   | Location         | Party HP Status |
      | Tuesday Night   | Cragmaw Hideout  | Wounded         |
      | Saturday Group  | Phandalin        | Full health     |
    When I switch from "Tuesday Night" to "Saturday Group"
    Then Riddle should load "Saturday Group" context
    And Riddle should know the party is in Phandalin
    And Riddle should know the party is at full health

  # --- Edge Cases ---

  Scenario: Fresh campaign instance has no prior context
    Given I just created a new campaign instance
    And no gameplay has occurred yet
    When Riddle calls get_game_state
    Then Riddle should recognize this is a new game
    And Riddle should offer to help set up the adventure
    And Riddle should not reference non-existent history

  Scenario: Corrupted state is handled gracefully
    Given the campaign instance state has missing data
    When I start a new conversation with Riddle
    Then Riddle should report what data is available
    And Riddle should suggest how to proceed
    And the system should not crash
