@phase3 @party @character @multiplayer
Feature: Party Management and Character Creation
  As a Dungeon Master
  I want to create characters and invite players to my campaign
  So that remote players can join and play their characters

  Background:
    Given I am logged in as a Dungeon Master
    And I have a campaign instance "Tuesday Night Group"

  # --- Character Creation (Quick Entry) ---

  Scenario: DM adds a character using quick entry
    Given I am on the DM Dashboard for "Tuesday Night Group"
    When I click "Add Character"
    And I select "Quick Entry" mode
    And I enter the following:
      | Field       | Value   |
      | Name        | Thorin  |
      | Class       | Fighter |
      | Max HP      | 12      |
      | Armor Class | 16      |
    And I click "Save Character"
    Then "Thorin" should appear in the party roster
    And "Thorin" should show as "Unclaimed"

  # --- Character Creation (Full Entry) ---

  Scenario: DM adds a character with full D&D 5e details
    Given I am on the "Add Character" page
    When I select "Full Entry" mode
    And I enter the following character details:
      | Field              | Value          |
      | Name               | Elara          |
      | Race               | High Elf       |
      | Class              | Wizard         |
      | Level              | 3              |
      | Background         | Sage           |
      | Strength           | 8              |
      | Dexterity          | 14             |
      | Constitution       | 12             |
      | Intelligence       | 17             |
      | Wisdom             | 13             |
      | Charisma           | 10             |
      | Max HP             | 18             |
      | Armor Class        | 12             |
      | Speed              | 30 ft          |
      | Passive Perception | 13             |
    And I add proficiencies:
      | Proficiency        |
      | Arcana             |
      | History            |
      | Investigation      |
    And I add spells:
      | Spell              |
      | Fire Bolt          |
      | Mage Armor         |
      | Magic Missile      |
    And I click "Save Character"
    Then "Elara" should appear in the party roster
    And "Elara" should display "Wizard L3"
    And "Elara" should have all entered ability scores saved

  # --- Character Editing ---

  Scenario: DM edits an existing character
    Given the party has a character "Thorin" with Max HP 12
    When I click "Edit" on "Thorin"
    And I change Max HP to 15
    And I add the equipment "Longsword +1"
    And I click "Save Character"
    Then "Thorin" should show Max HP as 15
    And "Thorin" should have "Longsword +1" in equipment

  Scenario: DM removes a character from the party
    Given the party has characters "Thorin" and "Elara"
    When I click "Remove" on "Thorin"
    And I confirm the removal
    Then "Thorin" should no longer appear in the party roster
    And "Elara" should still appear in the party roster

  # --- Invite Link Generation ---

  Scenario: DM generates an invite link for the campaign
    Given I am on the DM Dashboard for "Tuesday Night Group"
    When I click "Invite Players"
    Then I should see an invite link
    And the link should contain "/join/"
    And I should see a "Copy Link" button

  Scenario: DM copies the invite link to clipboard
    Given I am viewing the invite link modal
    When I click "Copy Link"
    Then the clipboard should contain the invite link
    And I should see a "Copied!" confirmation

  Scenario: Invite code persists across sessions
    Given "Tuesday Night Group" has invite code "ABC123"
    When I close and reopen the invite modal
    Then the invite code should still be "ABC123"

  # --- Player Join Flow ---

  @player
  Scenario: Player accesses the join page via invite link
    Given a campaign "Tuesday Night Group" exists with invite code "XYZ789"
    And I am logged in as Player "Alice"
    When I navigate to "/join/XYZ789"
    Then I should see the campaign name "Tuesday Night Group"
    And I should see a list of available characters

  @player
  Scenario: Player sees only unclaimed characters
    Given "Tuesday Night Group" has the following characters:
      | Name   | Claimed By |
      | Thorin | (none)     |
      | Elara  | Bob        |
      | Shade  | (none)     |
    And I am logged in as Player "Alice"
    When I navigate to the join page for "Tuesday Night Group"
    Then I should see "Thorin" as available
    And I should see "Shade" as available
    And I should NOT see "Elara" as available
    And I should see "Elara" as "Claimed by Bob"

  @player
  Scenario: Player claims a character
    Given "Tuesday Night Group" has an unclaimed character "Thorin"
    And I am logged in as Player "Alice"
    When I navigate to the join page
    And I click "Claim" on "Thorin"
    Then "Thorin" should be linked to my account
    And I should be redirected to the Player Dashboard
    And "Thorin" should display as my active character

  @player
  Scenario: Player cannot claim an already-claimed character
    Given "Thorin" is claimed by Player "Bob"
    And I am logged in as Player "Alice"
    When I navigate to the join page
    Then I should see "Thorin" as "Claimed by Bob"
    And I should NOT see a "Claim" button for "Thorin"

  # --- Player Dashboard Access ---

  @player
  Scenario: Player accesses their dashboard after claiming a character
    Given I am Player "Alice" controlling "Thorin" in "Tuesday Night Group"
    When I navigate to the campaign
    Then I should see the Player Dashboard
    And I should see my character card for "Thorin"
    And I should NOT see the DM controls

  @player
  Scenario: Player with multiple characters selects one
    Given I am Player "Alice" controlling "Thorin" and "Grimm" in "Tuesday Night Group"
    When I navigate to the campaign
    Then I should be prompted to select a character
    And I should see options for "Thorin" and "Grimm"

  # --- DM Notifications ---

  @dm @signalr
  Scenario: DM is notified when a player claims a character
    Given I am the DM viewing the dashboard for "Tuesday Night Group"
    And "Thorin" is unclaimed
    When Player "Alice" claims "Thorin"
    Then I should see a notification "Alice claimed Thorin"
    And "Thorin" should update to show "Alice" as the player

  @dm @signalr
  Scenario: DM sees real-time player connection status
    Given I am the DM viewing the dashboard
    And Player "Alice" is controlling "Thorin"
    When "Alice" connects to the campaign
    Then I should see "Thorin" with a green "Connected" indicator
    When "Alice" disconnects
    Then I should see "Thorin" with a gray "Offline" indicator

  # --- Character Sheet Display ---

  @player
  Scenario: Player sees full character details on their dashboard
    Given I am Player "Alice" controlling "Elara" with:
      | Field      | Value     |
      | Race       | High Elf  |
      | Class      | Wizard    |
      | Level      | 3         |
      | Max HP     | 18        |
      | Current HP | 15        |
      | AC         | 12        |
      | Spells     | Fire Bolt, Magic Missile |
    When I view my Player Dashboard
    Then I should see my HP as "15 / 18"
    And I should see my AC as 12
    And I should see "High Elf Wizard L3"
    And I should see my spell list

  # --- Unauthenticated Access ---

  Scenario: Unauthenticated user is redirected to login
    Given I am not logged in
    When I navigate to "/join/ABC123"
    Then I should be redirected to the login page
    And after logging in, I should return to "/join/ABC123"

  Scenario: Invalid invite code shows error
    Given I am logged in as Player "Alice"
    When I navigate to "/join/INVALID"
    Then I should see an error "Campaign not found"
    And I should see a link to return home
