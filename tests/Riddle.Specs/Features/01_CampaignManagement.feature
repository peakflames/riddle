@phase1 @campaign
Feature: Campaign Instance Management
  As a Dungeon Master
  I want to create and manage campaign instances
  So that I can run D&D adventures for my players across multiple play sessions

  Background:
    Given I am logged in as a Dungeon Master

  # --- Campaign Instance Creation ---

  Scenario: DM creates a new campaign instance
    Given I am on the home page
    When I click "New Campaign"
    And I enter "Tuesday Night Group" as the campaign name
    And I select "Lost Mine of Phandelver" as the campaign module
    And I click "Create Campaign"
    Then I should see the DM Dashboard
    And the campaign instance "Tuesday Night Group" should be created
    And the campaign instance should have an empty party

  Scenario: DM creates a campaign instance with party preferences
    Given I am on the "New Campaign" page
    When I enter "Dark Descent" as the campaign name
    And I set the following preferences:
      | Setting        | Value       |
      | Combat Focus   | High        |
      | Roleplay Focus | Medium      |
      | Pacing         | Fast        |
      | Tone           | Dark        |
    And I click "Create Campaign"
    Then the campaign instance should be created with my preferences

  Scenario: DM creates parallel campaign instances with the same module
    Given I have a campaign instance "Tuesday Night Group" using module "Lost Mine of Phandelver"
    When I click "New Campaign"
    And I enter "Saturday Group" as the campaign name
    And I select "Lost Mine of Phandelver" as the campaign module
    And I click "Create Campaign"
    Then the campaign instance "Saturday Group" should be created
    And "Tuesday Night Group" and "Saturday Group" should be independent instances
    And each instance should track its own party and progress

  # --- Campaign Instance Access ---

  Scenario: DM generates a join link for players
    Given I have a campaign instance "Tuesday Night Group"
    When I click "Invite Players"
    Then I should see a join link
    And the link should be copyable to clipboard

  Scenario: Player joins a campaign instance via link
    Given a campaign instance "Tuesday Night Group" exists with join code "ABC123"
    And I am a Player named "Alice"
    When I navigate to the join link with code "ABC123"
    Then I should see the Player Dashboard
    And I should be prompted to select or create a character

  Scenario: Player creates a character when joining
    Given I am joining campaign instance "Tuesday Night Group" as Player "Alice"
    When I create a new character:
      | Field      | Value   |
      | Name       | Thorin  |
      | Class      | Fighter |
      | Max HP     | 12      |
      | Armor Class| 16      |
    Then my character "Thorin" should appear in the party roster
    And the DM should be notified that "Thorin" joined

  # --- Play Session Management ---

  Scenario: DM starts a new play session within a campaign instance
    Given I have a campaign instance "Tuesday Night Group"
    And the campaign instance has completed 2 play sessions
    When I click "Start Play Session"
    Then a new play session #3 should be created
    And the play session should record the current location as the starting point
    And the play session should be marked as active

  Scenario: DM ends a play session
    Given I have an active play session #3 in campaign instance "Tuesday Night Group"
    When I click "End Play Session"
    Then play session #3 should be marked as ended
    And the ending location should be recorded
    And I should be prompted to add key events from this session

  Scenario: DM views play session history for a campaign
    Given I have a campaign instance "Tuesday Night Group" with play sessions:
      | Session # | Date      | Key Events                        |
      | 1         | Sept 1    | Character creation, Goblin ambush |
      | 2         | Sept 8    | Explored Cragmaw Hideout          |
      | 3         | Sept 15   | Rescued Sildar                    |
    When I view the play session history
    Then I should see all 3 play sessions with their key events
    And I should see the progression through the campaign

  # --- Campaign Instance Resumption ---

  Scenario: DM resumes an existing campaign instance
    Given I have previously created a campaign instance "Tuesday Night Group"
    And the campaign instance has 3 characters in the party
    When I click on "Tuesday Night Group" from my campaign list
    Then I should see the DM Dashboard
    And the party roster should show 3 characters
    And the last activity timestamp should be updated

  Scenario: Player reconnects to an active campaign instance
    Given Player "Alice" was previously connected to campaign instance "Tuesday Night Group"
    And "Alice" controls the character "Thorin"
    When "Alice" navigates to the campaign instance
    Then "Alice" should see the Player Dashboard
    And "Thorin" should be displayed as their active character

  # --- Campaign Instance List ---

  Scenario: DM views their campaign instance history
    Given I have the following campaign instances:
      | Name             | Module                     | Last Active | Play Sessions |
      | Tuesday Night    | Lost Mine of Phandelver    | Today       | 3             |
      | Saturday Group   | Lost Mine of Phandelver    | Last Week   | 1             |
    When I am on the home page
    Then I should see my campaign instances sorted by last activity
    And I should see "Tuesday Night" first
    And each campaign instance should show the play session count
