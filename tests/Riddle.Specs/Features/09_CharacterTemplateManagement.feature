# ==============================================================================
# E2E Test Coverage: tests/Riddle.Web.IntegrationTests/E2ETests/CharacterTemplateManagementTests.cs
# 
# Each @HLR-TEMPLATE-XXX scenario has a corresponding test method:
#   @HLR-TEMPLATE-001 → HLR_TEMPLATE_001_Anonymous_user_redirected_to_login()
#   @HLR-TEMPLATE-002 → HLR_TEMPLATE_002_Authenticated_user_sees_template_list()
#   ... (pattern: replace hyphens with underscores)
# 
# See docs/e2e_testing_philosophy.md for testing patterns.
# ==============================================================================

@templates @crud @auth
Feature: Character Template Management
  As a Dungeon Master
  I want to create, edit, and manage character templates
  So that I can quickly add pre-made characters to my campaigns

  # --- Authentication & Authorization ---

  @HLR-TEMPLATE-001
  Scenario: Anonymous user is redirected to login
    Given I am not logged in
    When I navigate to the Character Templates page
    Then I should be redirected to the login page

  @HLR-TEMPLATE-002
  Scenario: Authenticated user sees template list
    Given I am logged in as "alice@example.com"
    When I navigate to the Character Templates page
    Then I should see the Character Templates page
    And I should see the "Create Template" button
    And I should see the "Import JSON" button
    And I should see the "View Schema" button

  # --- Viewing Templates (Filter Tabs) ---

  @HLR-TEMPLATE-003
  Scenario: All tab shows public and user's own templates
    Given I am logged in as "alice@example.com"
    And the following templates exist:
      | Name          | Owner               | IsPublic |
      | Gandalf       | (system)            | true     |
      | Frodo         | alice@example.com   | true     |
      | SecretAlice   | alice@example.com   | false    |
      | BobsChar      | bob@example.com     | true     |
      | SecretBob     | bob@example.com     | false    |
    When I view the "All" tab on the Character Templates page
    Then I should see templates: Gandalf, Frodo, SecretAlice, BobsChar
    And I should NOT see template: SecretBob

  @HLR-TEMPLATE-004
  Scenario: Mine tab shows only user's templates
    Given I am logged in as "alice@example.com"
    And the following templates exist:
      | Name          | Owner               | IsPublic |
      | Gandalf       | (system)            | true     |
      | Frodo         | alice@example.com   | true     |
      | SecretAlice   | alice@example.com   | false    |
      | BobsChar      | bob@example.com     | true     |
    When I view the "Mine" tab on the Character Templates page
    Then I should see templates: Frodo, SecretAlice
    And I should NOT see templates: Gandalf, BobsChar

  @HLR-TEMPLATE-005
  Scenario: System tab shows only system templates
    Given I am logged in as "alice@example.com"
    And the following templates exist:
      | Name          | Owner               | IsPublic |
      | Gandalf       | (system)            | true     |
      | Elara         | (system)            | true     |
      | Frodo         | alice@example.com   | true     |
    When I view the "System" tab on the Character Templates page
    Then I should see templates: Gandalf, Elara
    And I should NOT see template: Frodo

  @HLR-TEMPLATE-006
  Scenario: Admin user sees all templates including others' private
    Given I am logged in as "admin@example.com"
    And "admin@example.com" is configured as an admin
    And the following templates exist:
      | Name          | Owner               | IsPublic |
      | Gandalf       | (system)            | true     |
      | SecretBob     | bob@example.com     | false    |
    When I view the "All" tab on the Character Templates page
    Then I should see templates: Gandalf, SecretBob

  # --- Creating Templates via Form ---

  @HLR-TEMPLATE-007
  Scenario: User creates template via form
    Given I am logged in as "alice@example.com"
    And I am on the Character Templates page
    When I click "Create Template"
    And I fill in the template form:
      | Field | Value           |
      | Name  | Aragorn         |
      | Race  | Human           |
      | Class | Ranger          |
      | Level | 5               |
    And I set visibility to "Public"
    And I click "Save"
    Then I should see a success message
    And the template "Aragorn" should appear in the list
    And "Aragorn" should be marked as "Public"

  @HLR-TEMPLATE-008
  Scenario: User creates private template
    Given I am logged in as "alice@example.com"
    And I am on the Character Templates page
    When I click "Create Template"
    And I fill in the template form with name "MySecret"
    And I set visibility to "Private"
    And I click "Save"
    Then the template "MySecret" should appear in the list
    And "MySecret" should be marked as "Private"

  @HLR-TEMPLATE-009
  Scenario: Form validation requires name
    Given I am logged in as "alice@example.com"
    And I am on the Character Templates page
    When I click "Create Template"
    And I leave the Name field empty
    And I click "Save"
    Then I should see a validation error for "Name"
    And the modal should remain open

  # --- Creating Templates via JSON Import ---

  @HLR-TEMPLATE-010
  Scenario: User imports valid JSON template
    Given I am logged in as "alice@example.com"
    And I am on the Character Templates page
    When I click "Import JSON"
    And I paste valid character JSON:
      """
      {
        "name": "Legolas",
        "race": "Elf",
        "class": "Ranger",
        "level": 8,
        "currentHp": 45,
        "maxHp": 45
      }
      """
    And I set visibility to "Public"
    And I click "Import"
    Then I should see a success message containing "Legolas"
    And the template "Legolas" should appear in the list

  @HLR-TEMPLATE-011
  Scenario: JSON import rejects invalid JSON
    Given I am logged in as "alice@example.com"
    And I am on the Character Templates page
    When I click "Import JSON"
    And I paste invalid JSON: "{ not valid json"
    And I click "Import"
    Then I should see an error message containing "Invalid JSON"
    And the modal should remain open

  @HLR-TEMPLATE-012
  Scenario: JSON import requires character name
    Given I am logged in as "alice@example.com"
    And I am on the Character Templates page
    When I click "Import JSON"
    And I paste JSON without a name:
      """
      {
        "race": "Dwarf",
        "class": "Fighter"
      }
      """
    And I click "Import"
    Then I should see an error message containing "name is required"

  # --- Editing Templates ---

  @HLR-TEMPLATE-013
  Scenario: User edits their own template
    Given I am logged in as "alice@example.com"
    And I own a template named "MyHero"
    When I click "Edit" on "MyHero"
    And I change the name to "MyUpdatedHero"
    And I click "Save"
    Then I should see "MyUpdatedHero" in the list
    And I should NOT see "MyHero" in the list

  @HLR-TEMPLATE-014
  Scenario: User cannot edit another user's template
    Given I am logged in as "alice@example.com"
    And "bob@example.com" owns a public template named "BobsChar"
    When I view "BobsChar" in the template list
    Then the "Edit" button should NOT be visible for "BobsChar"

  @HLR-TEMPLATE-015
  Scenario: User cannot edit system templates
    Given I am logged in as "alice@example.com"
    And a system template "Gandalf" exists
    When I view "Gandalf" in the template list
    Then the "Edit" button should NOT be visible for "Gandalf"

  @HLR-TEMPLATE-016
  Scenario: Admin can edit any user's template
    Given I am logged in as "admin@example.com"
    And "admin@example.com" is configured as an admin
    And "bob@example.com" owns a template named "BobsChar"
    When I click "Edit" on "BobsChar"
    And I change the name to "AdminEdited"
    And I click "Save"
    Then I should see "AdminEdited" in the list

  @HLR-TEMPLATE-017
  Scenario: Admin can edit system templates
    Given I am logged in as "admin@example.com"
    And "admin@example.com" is configured as an admin
    And a system template "Gandalf" exists
    When I click "Edit" on "Gandalf"
    Then the edit modal should open for "Gandalf"

  # --- Deleting Templates ---

  @HLR-TEMPLATE-018
  Scenario: User deletes their own template
    Given I am logged in as "alice@example.com"
    And I own a template named "ToDelete"
    When I click "Delete" on "ToDelete"
    And I confirm the deletion
    Then "ToDelete" should no longer appear in the list

  @HLR-TEMPLATE-019
  Scenario: User cannot delete another user's template
    Given I am logged in as "alice@example.com"
    And "bob@example.com" owns a public template named "BobsChar"
    When I view "BobsChar" in the template list
    Then the "Delete" button should NOT be visible for "BobsChar"

  @HLR-TEMPLATE-020
  Scenario: User cannot delete system templates
    Given I am logged in as "alice@example.com"
    And a system template "Gandalf" exists
    When I view "Gandalf" in the template list
    Then the "Delete" button should NOT be visible for "Gandalf"

  @HLR-TEMPLATE-021
  Scenario: Admin can delete any template
    Given I am logged in as "admin@example.com"
    And "admin@example.com" is configured as an admin
    And "bob@example.com" owns a template named "BobsChar"
    When I click "Delete" on "BobsChar"
    And I confirm the deletion
    Then "BobsChar" should no longer appear in the list

  # --- Visibility Toggle ---

  @HLR-TEMPLATE-022
  Scenario: User toggles template from public to private
    Given I am logged in as "alice@example.com"
    And I own a public template named "MyPublic"
    When I click "Edit" on "MyPublic"
    And I set visibility to "Private"
    And I click "Save"
    Then "MyPublic" should be marked as "Private"

  @HLR-TEMPLATE-023
  Scenario: Private templates not visible to other users
    Given "alice@example.com" owns a private template named "AliceSecret"
    And I am logged in as "bob@example.com"
    When I view the "All" tab on the Character Templates page
    Then I should NOT see template: AliceSecret

  # --- Schema Viewer ---

  @HLR-TEMPLATE-024
  Scenario: User opens schema viewer
    Given I am logged in as "alice@example.com"
    And I am on the Character Templates page
    When I click "View Schema"
    Then the Schema Viewer modal should open
    And I should see the character JSON schema documentation
    And I should see property descriptions for "name", "race", "class"

  @HLR-TEMPLATE-025
  Scenario: Schema viewer can be closed
    Given I am logged in as "alice@example.com"
    And the Schema Viewer modal is open
    When I close the Schema Viewer modal
    Then the modal should no longer be visible

  # --- Character Picker Integration ---

  @HLR-TEMPLATE-026
  Scenario: Character picker shows importable templates
    Given I am logged in as "alice@example.com"
    And the following templates exist:
      | Name          | Owner               | IsPublic |
      | Gandalf       | (system)            | true     |
      | Frodo         | alice@example.com   | true     |
      | SecretAlice   | alice@example.com   | false    |
      | BobsChar      | bob@example.com     | true     |
      | SecretBob     | bob@example.com     | false    |
    And I have an active campaign
    When I open the Character Template Picker
    Then I should see templates: Gandalf, Frodo, SecretAlice, BobsChar
    And I should NOT see template: SecretBob

  @HLR-TEMPLATE-027
  Scenario: User imports template to campaign
    Given I am logged in as "alice@example.com"
    And I have an active campaign "TestCampaign"
    And a public template "Gandalf" exists
    When I open the Character Template Picker
    And I select "Gandalf"
    And I click "Import"
    Then "Gandalf" should be added to the campaign's party
    And the character should have a new unique ID

  # --- Badge Display ---

  @HLR-TEMPLATE-028
  Scenario: Public templates show Public badge
    Given I am logged in as "alice@example.com"
    And I own a public template named "MyPublic"
    When I view the template list
    Then "MyPublic" should display a "Public" badge

  @HLR-TEMPLATE-029
  Scenario: Private templates show Private badge
    Given I am logged in as "alice@example.com"
    And I own a private template named "MyPrivate"
    When I view the template list
    Then "MyPrivate" should display a "Private" badge

  @HLR-TEMPLATE-030
  Scenario: System templates show System badge
    Given I am logged in as "alice@example.com"
    And a system template "Gandalf" exists
    When I view the template list
    Then "Gandalf" should display a "System" badge
