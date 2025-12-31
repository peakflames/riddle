# ==============================================================================
# E2E Test Coverage: tests/Riddle.Web.IntegrationTests/E2ETests/PlayerDashboardTests.cs
# 
# Each @HLR-PLAYER-XXX scenario has a corresponding test method:
#   @HLR-PLAYER-001 → HLR_PLAYER_001_Player_sees_their_character_card()
#   @HLR-PLAYER-002 → HLR_PLAYER_002_Player_sees_HP_changes_in_real_time()
#   ... (pattern: replace hyphens with underscores)
# 
# See docs/e2e_testing_philosophy.md for testing patterns.
# ==============================================================================

@phase3 @phase4 @player @signalr @ui
Feature: Player Dashboard
  As a Player
  I want to view my character and interact with the game
  So that I can participate in the D&D session

  Background:
    Given a campaign instance "Tuesday Night Group" exists
    And I am Player "Alice"
    And I control the character "Thorin" with:
      | Field       | Value   |
      | Class       | Fighter |
      | Max HP      | 12      |
      | Current HP  | 12      |
      | Armor Class | 16      |

  # --- Character Display ---

  @HLR-PLAYER-001
  Scenario: Player sees their character card
    When I open the Player Dashboard
    Then I should see my character card for "Thorin"
    And the card should show my HP as "12 / 12"
    And the card should show my Armor Class as 16
    And the card should show my class as "Fighter"

  @HLR-PLAYER-002
  Scenario: Player sees HP changes in real-time
    Given I am on the Player Dashboard
    And "Thorin" has 12 HP
    When the DM applies 5 damage to Thorin
    Then my HP display should update to "7 / 12"
    And I should not need to refresh the page
    And the HP bar should visually reflect the damage

  @HLR-PLAYER-003
  Scenario: Player sees conditions applied
    Given I am on the Player Dashboard
    When the DM applies the "Poisoned" condition to Thorin
    Then I should see a "Poisoned" badge on my character card
    And the condition should appear without refreshing

  @HLR-PLAYER-004
  Scenario: Player sees conditions removed
    Given Thorin has the "Poisoned" condition
    When the DM removes the "Poisoned" condition
    Then the "Poisoned" badge should disappear
    And my character card should update automatically

  # --- Player Choices ---

  @HLR-PLAYER-005
  Scenario: Player receives action choices
    Given the DM is running the campaign instance
    When Riddle presents choices:
      | Choice        |
      | Attack        |
      | Hide          |
      | Negotiate     |
    Then I should see choice buttons on my dashboard
    And I should see buttons for "Attack", "Hide", and "Negotiate"

  @HLR-PLAYER-007
  Scenario: New choices replace old choices
    Given I have choices displayed
    When Riddle sends new choices:
      | Choice           |
      | Cast Spell       |
      | Use Healing Potion |
    Then the old choices should be replaced
    And I should see "Cast Spell" and "Use Healing Potion"

  # --- Atmospheric & Narrative Events ---

  @HLR-PLAYER-008
  Scenario: Player sees atmosphere pulse
    Given I am on the Player Dashboard
    When Riddle broadcasts an atmosphere pulse with:
      | Field       | Value                                    |
      | Text        | A faint howling echoes through the trees |
      | SensoryType | sound                                    |
      | Intensity   | medium                                   |
    Then I should see the atmosphere pulse message
    And it should display a sound icon
    And the pulse should auto-dismiss after 10 seconds

  @HLR-PLAYER-009
  Scenario: Player sees narrative anchor
    Given I am on the Player Dashboard
    When Riddle sets a narrative anchor with:
      | Field        | Value                    |
      | ShortText    | The forest grows darker  |
      | MoodCategory | danger                   |
    Then I should see a persistent mood banner
    And the banner should have danger styling
    And the banner should remain until replaced

  @HLR-PLAYER-010
  Scenario: Player sees group insight
    Given I am on the Player Dashboard
    When Riddle triggers a group insight with:
      | Field          | Value                                  |
      | Text           | You notice goblin tracks leading east  |
      | RelevantSkill  | Perception                             |
      | HighlightEffect| true                                   |
    Then I should see a group insight notification
    And it should show the skill badge "Perception"
    And the notification should pulse briefly
    And it should auto-dismiss after 8 seconds

  # --- Restricted Information ---

  @HLR-PLAYER-011
  Scenario: Player cannot see enemy HP
    Given combat is active with enemies
    When I view the Player Dashboard
    Then I should not see exact enemy HP values
    And enemy health may show as descriptive text only

  @HLR-PLAYER-012
  Scenario: Player cannot see DM chat
    Given the DM is chatting with Riddle
    When Riddle provides secret information to the DM
    Then I should not see that information
    And my dashboard should only show public events

  # --- Connection Status ---

  @HLR-PLAYER-013
  Scenario: Player sees connection indicator
    Given I am connected to the campaign instance
    Then I should see a "Connected" status indicator
    And the indicator should be green

  @HLR-PLAYER-014
  Scenario: Player reconnects after disconnect
    Given I was disconnected from the campaign instance
    When I reconnect
    Then my dashboard should reload with current state
    And my character card should show accurate HP
    And any active choices should be displayed

  # --- Multi-Character Support ---

  @HLR-PLAYER-015
  Scenario: Player with multiple characters selects one
    Given I control multiple characters:
      | Name   | Class   |
      | Thorin | Fighter |
      | Gandor | Wizard  |
    When I join the campaign instance
    Then I should be prompted to select a character
    And I can choose to play as "Thorin" or "Gandor"

  # --- Death Saves & 0 HP Display ---

  @HLR-PLAYER-016
  Scenario: Player sees death save tracker at 0 HP
    Given I am on the Player Dashboard
    And "Thorin" is at 0 HP
    When the character card renders
    Then I should see the Death Save tracker section
    And I should see 3 empty success circles
    And I should see 3 empty failure circles

  @HLR-PLAYER-017
  Scenario: Death save success fills green circle
    Given I am on the Player Dashboard
    And "Thorin" is at 0 HP with 0 death saves
    When the DM records a death save success for Thorin
    Then the first success circle should turn green
    And the other success circles should remain empty
    And the update should appear without refreshing

  @HLR-PLAYER-018
  Scenario: Death save failure fills red circle
    Given I am on the Player Dashboard
    And "Thorin" is at 0 HP with 1 death save success
    When the DM records a death save failure for Thorin
    Then the first failure circle should turn red
    And the other failure circles should remain empty
    And the update should appear without refreshing

  @HLR-PLAYER-019
  Scenario: Death save tracker shows multiple saves
    Given I am on the Player Dashboard
    And "Thorin" is at 0 HP with:
      | DeathSaveSuccesses | 2 |
      | DeathSaveFailures  | 1 |
    When the character card renders
    Then I should see 2 green success circles
    And I should see 1 red failure circle
    And the remaining circles should be empty

  @HLR-PLAYER-020
  Scenario: Death save tracker resets on healing
    Given I am on the Player Dashboard
    And "Thorin" is at 0 HP with 2 successes and 1 failure
    When the DM heals Thorin for 5 HP
    Then Thorin's HP should update to "5 / 12"
    And the Death Save tracker should disappear
    And the "Unconscious" condition should be removed

  @HLR-PLAYER-021
  Scenario: Death save tracker resets on natural 20
    Given I am on the Player Dashboard
    And "Thorin" is at 0 HP with 1 success and 2 failures
    When the DM records a natural 20 death save for Thorin
    Then Thorin's HP should update to "1 / 12"
    And the Death Save tracker should disappear
    And the "Unconscious" condition should be removed

  @HLR-PLAYER-022
  Scenario: Player sees Stable condition after 3 successes
    Given I am on the Player Dashboard
    And "Thorin" is at 0 HP with 2 death save successes
    When the DM records a third death save success
    Then Thorin should show the "Stable" condition badge
    And the Death Save tracker should show 3 green circles
    And Thorin should remain Unconscious

  @HLR-PLAYER-023
  Scenario: Player sees character marked as Dead
    Given I am on the Player Dashboard
    And "Thorin" is at 0 HP with 2 death save failures
    When the DM records a third death save failure
    Then Thorin's character card should show a "Dead" indicator
    And the Death Save tracker should show 3 red circles
    And the character card should have visual death styling
