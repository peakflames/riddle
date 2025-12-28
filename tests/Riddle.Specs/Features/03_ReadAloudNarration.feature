@phase2 @llm @ratb
Feature: Read Aloud Narration
  As a Dungeon Master
  I want Riddle to generate atmospheric "Read Aloud" text
  So that I can deliver immersive narration to my players

  Background:
    Given I am logged in as a Dungeon Master
    And I have an active campaign instance "Tuesday Night Group"
    And I am on the DM Dashboard

  # --- Read Aloud Text Box (RATB) Display ---

  Scenario: Riddle generates scene narration
    When I ask Riddle "Describe the entrance to the Cragmaw Hideout"
    Then the Read Aloud Text Box should display atmospheric prose
    And the text should describe the cave entrance
    And the text should be ready to read to players

  Scenario: RATB shows empty state before narration
    Given no narration has been generated yet
    Then the Read Aloud Text Box should show a placeholder message
    And the placeholder should explain its purpose

  Scenario: RATB updates in real-time
    Given I am viewing the DM Dashboard
    When Riddle generates new read-aloud text
    Then the Read Aloud Text Box should update immediately
    And I should not need to refresh the page

  # --- Narration Quality ---

  Scenario: Narration matches the party's tone preference
    Given the party preferences have Tone set to "Dark"
    When I request narration for entering a goblin cave
    Then the Read Aloud Text should use dark, foreboding language
    And the atmosphere should feel tense and dangerous

  Scenario: Narration adapts to comedic tone
    Given the party preferences have Tone set to "Comedic"
    When I request narration for entering a goblin cave
    Then the Read Aloud Text should include lighter elements
    And the prose should have moments of levity

  Scenario: Narration includes sensory details
    When I request narration for a forest scene
    Then the Read Aloud Text should describe what players see
    And it should describe sounds and smells
    And it should create an immersive atmosphere

  # --- Scene Transitions ---

  Scenario: Riddle generates transition narration
    Given the party was at "Triboar Trail"
    When I tell Riddle "The party travels to the cave entrance"
    Then Riddle should generate travel narration
    And the Read Aloud Text should describe the journey
    And the current location should update to the new location

  Scenario: Narration for entering combat
    Given the party is exploring peacefully
    When I trigger a goblin ambush
    Then Riddle should generate dramatic combat narration
    And the Read Aloud Text should set up the encounter
    And players should feel the urgency of battle

  Scenario: Narration for combat conclusion
    Given combat was active
    When the last enemy is defeated
    Then Riddle should generate victory or aftermath narration
    And the Read Aloud Text should describe the scene post-combat

  # --- Manual Override ---

  Scenario: DM can clear the Read Aloud Text
    Given the Read Aloud Text Box displays narration
    When I click "Clear Narration"
    Then the Read Aloud Text Box should be empty
    And the placeholder message should appear

  Scenario: DM requests specific narration style
    When I ask Riddle "Describe the goblin cave, but keep it short - two sentences max"
    Then the Read Aloud Text should be concise
    And it should be approximately two sentences

  # --- Long Narration Handling ---

  Scenario: RATB handles long narration text
    When Riddle generates a lengthy scene description
    Then the Read Aloud Text Box should be scrollable
    And all text should be accessible
    And the formatting should remain readable
