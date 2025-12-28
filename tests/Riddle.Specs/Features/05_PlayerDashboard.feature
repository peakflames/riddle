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

  Scenario: Player sees their character card
    When I open the Player Dashboard
    Then I should see my character card for "Thorin"
    And the card should show my HP as "12 / 12"
    And the card should show my Armor Class as 16
    And the card should show my class as "Fighter"

  Scenario: Player sees HP changes in real-time
    Given I am on the Player Dashboard
    And "Thorin" has 12 HP
    When the DM applies 5 damage to Thorin
    Then my HP display should update to "7 / 12"
    And I should not need to refresh the page
    And the HP bar should visually reflect the damage

  Scenario: Player sees conditions applied
    Given I am on the Player Dashboard
    When the DM applies the "Poisoned" condition to Thorin
    Then I should see a "Poisoned" badge on my character card
    And the condition should appear without refreshing

  Scenario: Player sees conditions removed
    Given Thorin has the "Poisoned" condition
    When the DM removes the "Poisoned" condition
    Then the "Poisoned" badge should disappear
    And my character card should update automatically

  # --- Player Choices ---

  Scenario: Player receives action choices
    Given the DM is running the campaign instance
    When Riddle presents choices:
      | Choice        |
      | Attack        |
      | Hide          |
      | Negotiate     |
    Then I should see choice buttons on my dashboard
    And I should see buttons for "Attack", "Hide", and "Negotiate"

  Scenario: Player selects a choice
    Given I have choice buttons displayed
    When I click the "Attack" button
    Then my choice should be sent to the DM
    And the DM should see "Thorin chose: Attack"
    And my choice buttons should become disabled

  Scenario: Choices are cleared after selection
    Given I selected "Attack"
    When the DM acknowledges my choice
    Then the choice buttons should be removed
    And the dashboard should show a waiting state

  Scenario: New choices replace old choices
    Given I have choices displayed
    When Riddle sends new choices:
      | Choice           |
      | Cast Spell       |
      | Use Healing Potion |
    Then the old choices should be replaced
    And I should see "Cast Spell" and "Use Healing Potion"

  # --- Scene Display ---

  Scenario: Player sees the scene image
    Given a scene image has been set
    When I view the Player Dashboard
    Then I should see the current scene image
    And the image should be prominently displayed

  Scenario: Scene image updates when location changes
    Given I am viewing the Player Dashboard
    When the party moves to a new location
    And Riddle updates the scene image
    Then my scene display should update automatically
    And I should see the new location image

  # --- Events Log ---

  Scenario: Player sees recent public events
    Given public events have occurred:
      | Event                           |
      | Thorin attacked the goblin      |
      | The goblin missed Elara         |
    When I view the events log
    Then I should see these events listed
    And the most recent event should appear first

  Scenario: Events update in real-time
    Given I am viewing the Player Dashboard
    When a new event "Elara deals 6 damage" occurs
    Then the event should appear in my log immediately
    And I should not need to refresh the page

  # --- Restricted Information ---

  Scenario: Player cannot see enemy HP
    Given combat is active with enemies
    When I view the Player Dashboard
    Then I should not see exact enemy HP values
    And enemy health may show as descriptive text only

  Scenario: Player cannot see DM chat
    Given the DM is chatting with Riddle
    When Riddle provides secret information to the DM
    Then I should not see that information
    And my dashboard should only show public events

  # --- Connection Status ---

  Scenario: Player sees connection indicator
    Given I am connected to the campaign instance
    Then I should see a "Connected" status indicator
    And the indicator should be green

  Scenario: Player reconnects after disconnect
    Given I was disconnected from the campaign instance
    When I reconnect
    Then my dashboard should reload with current state
    And my character card should show accurate HP
    And any active choices should be displayed

  # --- Multi-Character Support ---

  Scenario: Player with multiple characters selects one
    Given I control multiple characters:
      | Name   | Class   |
      | Thorin | Fighter |
      | Gandor | Wizard  |
    When I join the campaign instance
    Then I should be prompted to select a character
    And I can choose to play as "Thorin" or "Gandor"
