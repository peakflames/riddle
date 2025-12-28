@phase2 @llm @chat
Feature: Dungeon Master Chat
  As a Dungeon Master
  I want to communicate with Riddle (the LLM assistant)
  So that I can get rules advice, narrative suggestions, and gameplay guidance

  Background:
    Given I am logged in as a Dungeon Master
    And I have an active campaign instance "Tuesday Night Group"
    And I am on the DM Dashboard

  # --- Basic Chat Interaction ---

  Scenario: DM sends a message and receives a response
    When I type "What happens when the party enters the cave?" in the chat
    And I press Enter
    Then I should see my message in the chat history
    And Riddle should respond with narrative suggestions
    And the response should stream in real-time

  Scenario: DM asks a rules question
    When I ask Riddle "How does the Rogue's Sneak Attack work?"
    Then Riddle should explain the Sneak Attack rules
    And the explanation should reference D&D 5th Edition mechanics

  Scenario: DM reports a dice roll
    Given combat is active
    When I type "Thorin attacks the goblin. He rolled 17."
    Then Riddle should calculate if the attack hits
    And Riddle should request damage roll if applicable
    And Riddle should update the game state accordingly

  # --- Contextual Awareness ---

  Scenario: Riddle uses party state in responses
    Given the party contains:
      | Name   | Class   | Current HP | Max HP |
      | Thorin | Fighter | 5          | 12     |
      | Elara  | Rogue   | 8          | 8      |
    When I ask "What's the party's status?"
    Then Riddle should mention that Thorin is wounded
    And Riddle should indicate Elara is at full health

  Scenario: Riddle references active quests
    Given the active quests include:
      | Title              | State  |
      | Find Cragmaw Hideout | Active |
      | Rescue Sildar       | Active |
    When I ask "What should the party do next?"
    Then Riddle should reference the active quests
    And Riddle should suggest quest-related actions

  Scenario: Riddle respects party preferences
    Given the party preferences are:
      | Setting        | Value  |
      | Combat Focus   | Low    |
      | Roleplay Focus | High   |
      | Tone           | Comedic|
    When I ask "Describe what happens when they meet the goblin"
    Then Riddle should emphasize roleplay over combat
    And the narrative tone should be light and comedic

  # --- Private DM Information ---

  Scenario: DM receives secret information
    Given there is a hidden trap in the room
    When I ask "Are there any dangers the party hasn't noticed?"
    Then Riddle should describe the hidden trap
    And this information should only appear in the DM chat
    And the trap details should not be broadcast to players

  Scenario: DM gets tactical advice during combat
    Given combat is active with enemies:
      | Name       | HP | AC | Notes          |
      | Goblin Boss| 21 | 15 | Has shortbow   |
      | Goblin     | 7  | 15 | Flanking party |
    When I ask "What's the smartest move for the goblins?"
    Then Riddle should provide tactical suggestions
    And the suggestions should consider enemy abilities

  # --- Error Handling ---

  Scenario: DM sends message when LLM is unavailable
    Given the LLM service is temporarily unavailable
    When I send a message to Riddle
    Then I should see an error notification
    And I should be able to retry the message
    And my previous messages should still be visible

  Scenario: DM interrupts a streaming response
    Given Riddle is streaming a long response
    When I click the "Stop" button
    Then the response should stop streaming
    And I should be able to send a new message
